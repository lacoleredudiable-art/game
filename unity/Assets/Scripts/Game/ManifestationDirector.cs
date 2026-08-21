using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Cümleyi dünyadaki yaşayan etkiye bağlar. Kapalı rün geri bildirimi T6.1'de — burada yok.
    /// </summary>
    public sealed class ManifestationDirector : MonoBehaviour
    {
        GameClock _clock;
        SentenceEngine _engine;
        CombatTuning _combat;
        PrototypeTuning _colors;
        Transform _player;
        ActorPose _pose;
        BossReactor _boss;
        GroundScarField _scars;
        KinematicMotor _motor;

        readonly List<LivingEffectView> _active = new();
        readonly List<PendingClosing> _pending = new();

        int _lastWordCount;
        Rune? _lastVerb;
        bool _hooked;

        struct PendingClosing
        {
            public LivingEffectView View;
            public ClosingHit Closing;
            public double BangAtWorldMs;
            public bool Fired;
        }

        public LivingEffect? ActiveLogic => CurrentBuilding()?.Logic;

        public int ActiveCount => _active.Count;

        /// <summary>Editör/prob: Update beklemeden cümle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }

        public void Bind(
            GameClock clock,
            PentagonInput input,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            GroundScarField scars,
            PrototypeTuning colors)
        {
            _clock = clock;
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _boss = boss;
            _scars = scars;
            _motor = player.GetComponent<KinematicMotor>();

            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
        }

        void OnDestroy()
        {
            if (_engine != null && _hooked)
                _engine.SentenceCompleted -= OnSentenceCompleted;
        }

        void Update()
        {
            if (_engine == null || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);

            SyncFromSentence(worldMs);
            TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            TickPendingClosings(worldMs);
        }

        void SyncFromSentence(double worldMs)
        {
            var state = _engine.State;
            if (state.Phase != SentencePhase.Building)
            {
                _lastWordCount = 0;
                _lastVerb = null;
                return;
            }

            int count = state.Words.Count;
            if (count == 0)
                return;

            Rune verb = state.Words[0].Rune;
            // count 1'e düşmesi yeni fiil (önceki cümle kapandı ya da ilk dokunuş)
            bool newSentence = count == 1 && _lastWordCount != 1;
            bool grew = count > _lastWordCount;

            if (newSentence)
            {
                SpawnEffect(state.Words, worldMs);
                _pose?.PulseRune(verb, worldMs);
            }
            else if (grew)
            {
                LivingEffectView current = CurrentBuilding();
                current?.Logic.SetWords(state.Words);
                Rune last = state.Words[count - 1].Rune;
                _pose?.PulseRune(last, worldMs);
            }
            else if (count == _lastWordCount && CurrentBuilding() != null)
            {
                // dwell yoğunluğu — aynı sayıda kelime, yığın arttı
                CurrentBuilding()!.Logic.SetWords(state.Words);
            }

            _lastWordCount = count;
            _lastVerb = verb;
        }

        void SpawnEffect(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            _ = worldMs;
            Vector3 pos = _player.position;
            Vector3 facing = _player.forward;
            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.05f)
                facing = _motor.Velocity.normalized;
            if (_boss != null)
            {
                Vector3 toBoss = _boss.transform.position - pos;
                toBoss.y = 0f;
                if (toBoss.sqrMagnitude > 0.01f)
                    facing = toBoss.normalized;
            }

            var logic = new LivingEffect(
                words[0].Rune,
                pos.x,
                pos.z,
                facing.x,
                facing.z,
                words,
                _combat.Manifestation);

            var go = new GameObject("LivingEffect_" + words[0].Rune);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<LivingEffectView>();
            view.Bind(logic, _combat.Manifestation, _colors);
            _active.Add(view);
        }

        LivingEffectView? CurrentBuilding()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var v = _active[i];
                if (v == null || v.Logic == null)
                    continue;
                if (v.Logic.Phase is LivingEffectPhase.Traveling or LivingEffectPhase.AwaitingClosing)
                    return v;
            }

            return null;
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            LivingEffectView? view = CurrentBuilding();
            // Abort sonrası CurrentBuilding fading olabilir — son traveling'i bul
            if (view == null)
            {
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    if (_active[i] != null && _active[i].Logic != null &&
                        _active[i].Logic.Phase != LivingEffectPhase.Dead)
                    {
                        view = _active[i];
                        break;
                    }
                }
            }

            if (view == null)
                return;

            if (sentence.Phase == SentencePhase.Aborted || !sentence.Closing.HasValue)
            {
                view.Logic.Abort();
                return;
            }

            ClosingHit closing = sentence.Closing.Value;
            view.Logic.ArmClosing(closing);

            float recoverySec = _combat.Sentence.StepForDots(closing.DotCount).RecoverySec;
            double bangAt = _clock.Director.WorldTimeMs
                            + recoverySec * 1000.0
                            + _combat.Feel.PostHitSilenceMs;

            _pose?.BeginRecovery(recoverySec, _clock.Director.WorldTimeMs);
            _pending.Add(new PendingClosing
            {
                View = view,
                Closing = closing,
                BangAtWorldMs = bangAt,
                Fired = false
            });
        }

        void TickPendingClosings(double worldMs)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                if (p.Fired || p.View == null || p.View.Logic == null)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (p.View.Logic.Phase is LivingEffectPhase.Fading or LivingEffectPhase.Dead)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (worldMs < p.BangAtWorldMs)
                    continue;

                FireClosing(p);
                p.Fired = true;
                _pending.RemoveAt(i);
            }
        }

        void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View.Logic;
            logic.FireClosingBang();
            StampScar(logic, p.Closing);
            ApplyBossClosing(logic, p.Closing);
        }

        void StampScar(LivingEffect logic, ClosingHit closing)
        {
            if (IsViewScarred(logic))
                return;

            Vector3 along = new Vector3(logic.DirX, 0f, logic.DirZ);
            Vector3 tip = new Vector3(logic.TipX, 0f, logic.TipZ);
            float scale = _combat.Manifestation.ScarScaleM * (0.7f + 0.15f * closing.DotCount);

            ScarKind kind = closing.Type switch
            {
                Rune.Sarsinti => ScarKind.Crack,
                Rune.Igne => ScarKind.Needle,
                Rune.Suru => ScarKind.Swarm,
                Rune.Zehir => ScarKind.Acid,
                _ => ScarKind.Crack
            };

            // Hat boyunca çatlak: kökten uca birkaç damga
            if (kind == ScarKind.Crack && logic.Current.Focus > 0.5f)
            {
                Vector3 origin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
                for (int i = 1; i <= 3; i++)
                {
                    float u = i / 3f;
                    _scars.Stamp(Vector3.Lerp(origin, tip, u), scale * 0.85f, kind, along);
                }
            }
            else
            {
                _scars.Stamp(tip, scale, kind, along);
            }

            MarkScarred(logic);
        }

        bool IsViewScarred(LivingEffect logic)
        {
            foreach (var v in _active)
            {
                if (v != null && v.Logic == logic)
                    return v.Scarred;
            }

            return false;
        }

        void MarkScarred(LivingEffect logic)
        {
            foreach (var v in _active)
            {
                if (v != null && v.Logic == logic)
                    v.Scarred = true;
            }
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing)
        {
            if (_boss == null)
                return;

            Vector3 from = new Vector3(logic.OriginX, 0f, logic.OriginZ);
            float knock = _combat.Manifestation.BossKnockbackM * (0.6f + 0.2f * closing.DotCount);
            float lift = 0f;

            switch (closing.Type)
            {
                case Rune.Sarsinti:
                    lift = _combat.Manifestation.BossLiftM * (0.7f + logic.Current.Lift);
                    break;
                case Rune.Igne:
                    knock *= 1.35f + logic.Current.Pierce;
                    break;
                case Rune.Suru:
                    knock *= 0.7f;
                    // Kısa çoklu sarsıntı
                    _boss.React(from, knock * 0.45f, 0.15f, _combat.Manifestation.BossShakeSec,
                        _clock.Director.WorldTimeMs);
                    break;
                case Rune.Kabuk:
                    _boss.Pin(0.55f, _clock.Director.WorldTimeMs);
                    return;
                case Rune.Zehir:
                    knock *= 0.4f;
                    break;
            }

            // Kapanış menzilinde mi?
            Vector3 bossPos = _boss.transform.position;
            float dx = bossPos.x - logic.TipX;
            float dz = bossPos.z - logic.TipZ;
            float reach = _combat.Manifestation.ClosingBangRadiusM;
            if (closing.Type == Rune.Sarsinti)
            {
                // Şok dalgası: odaklıysa hat, değilse yarıçap
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.5f))
                {
                    float radial = Vector2.Distance(
                        new Vector2(bossPos.x, bossPos.z),
                        new Vector2(logic.OriginX, logic.OriginZ));
                    if (radial > logic.TipDistance + reach && radial > reach)
                        return;
                }
            }
            else if (dx * dx + dz * dz > reach * reach)
            {
                // İğne/sürü: uçtan uzaksa yine de menzil kontrolü kök-uç segmentine
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.35f))
                    return;
            }

            _boss.React(from, knock, lift, _combat.Manifestation.BossShakeSec,
                _clock.Director.WorldTimeMs);
        }

        void TickEffects(float dtSec, double worldMs)
        {
            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            var man = _combat.Manifestation;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                LivingEffectView view = _active[i];
                if (view == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                LivingEffect logic = view.Logic;
                logic.Tick(dtSec);
                view.TickVisual(dtSec);

                if (logic.Phase == LivingEffectPhase.Traveling && _boss != null && !view.TravelHitDone)
                {
                    if (logic.OverlapsBoss(bossPos.x, bossPos.z, man.TravelHitRadiusM))
                    {
                        view.TravelHitDone = true;
                        _boss.React(
                            new Vector3(logic.OriginX, 0f, logic.OriginZ),
                            man.BossKnockbackM * 0.25f,
                            0.08f * logic.Current.Lift,
                            man.BossShakeSec * 0.45f,
                            worldMs);
                    }
                }

                // Seyahat izi: odaklı sarsıntı hattı yerde hafif çatlak bırakır (T4 erken kanıt)
                if (!view.Scarred && logic.Verb == Rune.Sarsinti && logic.Current.Focus > 0.7f
                    && logic.Travel > 2.5f)
                {
                    Vector3 mid = new Vector3(
                        logic.OriginX + logic.DirX * logic.TipDistance * 0.5f,
                        0f,
                        logic.OriginZ + logic.DirZ * logic.TipDistance * 0.5f);
                    _scars.Stamp(mid, man.ScarScaleM * 0.5f, ScarKind.Crack,
                        new Vector3(logic.DirX, 0f, logic.DirZ));
                    view.Scarred = true;
                }

                if (!logic.IsAlive)
                {
                    Destroy(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
