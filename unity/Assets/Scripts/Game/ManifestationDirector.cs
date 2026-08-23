using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Cümleyi dünyadaki yaşayan etkiye bağlar. Kapalı rün geri bildirimi T6.1'de — burada yok.
    /// T12: kapanış ödülü × ClosingDamagePerEffect → BossVitals; tür son rüne bağlı tepki.
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
        BossVitals _bossVitals;
        GroundScarField _scars;
        KinematicMotor _motor;
        DamageNumberHud _damageHud;
        BossDirector _bossDirector;

        readonly List<LivingEffectView> _active = new();
        readonly List<PendingClosing> _pending = new();
        readonly HashSet<LivingEffect> _closingStamped = new();

        // Cümlenin şu an sözcük aldığı etki — nokta sayısına göre değil, kimliğe göre izlenir
        // (aynı karede birden fazla nokta kaydı sayı polling'ini atlayabilir, bkz. T7.1).
        LivingEffectView _buildingView;
        int _lastWordCount;
        bool _hooked;
        bool _posedForRecovery;

        // §11 ölüm: yavaş çekim başladıktan sonra bitince Revive.
        bool _deathPending;
        bool _sawDeathSlowmo;

        struct PendingClosing
        {
            public LivingEffectView View;
            public ClosingHit Closing;
            public double BangAtWorldMs;
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

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
            BossVitals bossVitals,
            GroundScarField scars,
            PrototypeTuning colors,
            DamageNumberHud damageHud = null,
            BossDirector bossDirector = null)
        {
            _clock = clock;
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _boss = boss;
            _bossVitals = bossVitals;
            _scars = scars;
            _damageHud = damageHud;
            _bossDirector = bossDirector;
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

            // Kilit kesildi (§5): poz da kesilir. Kapanış patlaması kesilmez, kendi
            // zamanlamasıyla gelir (TickPendingClosings).
            if (_posedForRecovery && _engine.State.Phase != SentencePhase.Recovering)
            {
                _pose?.EndRecovery();
                _posedForRecovery = false;
            }

            SyncFromSentence(worldMs);
            ApplyWindowCue();
            TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            TickPendingClosings(worldMs);
            TickBossDeath();
        }

        void TickBossDeath()
        {
            if (!_deathPending || _clock == null)
                return;

            bool slow = _clock.Director.IsSlowmoActive;
            if (slow)
            {
                _sawDeathSlowmo = true;
                return;
            }

            if (!_sawDeathSlowmo)
                return;

            _deathPending = false;
            _sawDeathSlowmo = false;
            _bossVitals?.Revive();
            _boss?.EndCollapse();
            _bossDirector?.NotifyBossRevived(_clock.Director.WorldTimeMs);
        }

        void SyncFromSentence(double worldMs)
        {
            var state = _engine.State;
            if (state.Phase != SentencePhase.Building)
                return;

            int count = state.Words.Count;
            if (count == 0)
                return;

            // Kimliğe göre karar: elde yaşayan (Building'e ait) etki yoksa spawn et; varsa
            // sadece SetWords çağır. Sayı polling'i (count==1) EnhancedTouch'ın bir karede
            // birden fazla nokta kaydettiği durumda 0→2 sıçrayıp spawn'ı hiç tetiklemeyebilir.
            if (_buildingView == null || _buildingView.Logic == null
                || _buildingView.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                _buildingView = SpawnEffect(state.Words, worldMs);
                _pose?.PulseRune(state.Words[0].Rune, worldMs);
                _lastWordCount = count;
                return;
            }

            _buildingView.Logic.SetWords(state.Words);
            if (count > _lastWordCount)
                _pose?.PulseRune(state.Words[count - 1].Rune, worldMs);

            _lastWordCount = count;
        }

        void ApplyWindowCue()
        {
            if (_buildingView == null)
                return;

            var state = _engine.State;
            if (state.Phase != SentencePhase.Building || state.ArmedWindowMs <= 0.5)
            {
                _buildingView.SetWindowCue(1f);
                return;
            }

            _buildingView.SetWindowCue((float)(state.RemainingWindowMs / state.ArmedWindowMs));
        }

        LivingEffectView SpawnEffect(IReadOnlyList<SentenceWord> words, double worldMs)
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
            return view;
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            LivingEffectView view = _buildingView;
            _buildingView = null;
            _lastWordCount = 0;

            if (sentence.Phase == SentencePhase.Aborted || !sentence.Closing.HasValue)
            {
                if (view != null && view.Logic != null)
                    view.Logic.Abort();
                return;
            }

            // Düz vuruş (T6.2) OnDotTouched + Commit'i AYNI karede çağırır: Director'ın Update'i
            // araya girmediği için Building fazı hiç görülmez ve spawn yutulur. Ödenmiş kapanış
            // dünyada mutlaka yaşamak zorunda (§8/T2), o yüzden burada doğuyor. Elde yaşayan bir
            // etki ARAMIYORUZ — önceki cümlenin hâlâ patlayan etkisine bu kapanışı bağlamak
            // yanlış hedefe ödeme yapmak olur.
            if (view == null || view.Logic == null
                || view.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                view = SpawnEffect(sentence.Words, _clock.Director.WorldTimeMs);
                _pose?.PulseRune(sentence.Words[0].Rune, _clock.Director.WorldTimeMs);
            }

            // Kapanış kurulmadan önce son kelime listesi etkiye iletilir — dördüncü kelime
            // (cümlenin en pahalı sıfatı) burada gelmezse hedef silüete hiç işlemez, çünkü
            // SentenceEngine dördüncü noktada cümleyi dokunuş anında çözer ve SyncFromSentence
            // artık Building fazında değilken çalışmaz. LivingEffect.SetWords AwaitingClosing
            // fazında da kabul eder, sıra önemli değil.
            view.Logic.SetWords(sentence.Words);

            ClosingHit closing = sentence.Closing.Value;
            view.Logic.ArmClosing(closing);

            float recoverySec = _combat.Sentence.StepForDots(closing.DotCount).RecoverySec;
            double bangAt = _clock.Director.WorldTimeMs
                            + recoverySec * 1000.0
                            + _combat.Feel.PostHitSilenceMs;

            _pose?.BeginRecovery(recoverySec, _clock.Director.WorldTimeMs);
            _posedForRecovery = true;
            _pending.Add(new PendingClosing
            {
                View = view,
                Closing = closing,
                BangAtWorldMs = bangAt
            });
        }

        void TickPendingClosings(double worldMs)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                if (p.View == null || p.View.Logic == null)
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
                _pending.RemoveAt(i);
            }
        }

        void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View.Logic;
            logic.FireClosingBang();
            StampScar(logic, p.Closing);
            ApplyBossClosing(logic, p.Closing);
            ApplyClosingDamage(p.Closing);
        }

        /// <summary>
        /// §5: TotalEffect × ClosingDamagePerEffect. Tür hasarı DEĞİŞTİRMEZ (§12).
        /// Yarıda abort edilen cümle Closing taşımaz → buraya hiç gelmez.
        /// </summary>
        void ApplyClosingDamage(ClosingHit closing)
        {
            if (_bossVitals == null || _bossVitals.IsDown)
                return;

            float damage = closing.TotalEffect * _combat.ClosingDamagePerEffect;
            if (damage <= 0f)
                return;

            _damageHud?.ShowDamage(damage);

            bool killed = _bossVitals.ApplyDamage(damage);
            if (!killed)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            // Önce telegrafı kapat (Hide taban ölçeğe çeker), sonra çökme — sıra tersine
            // dönseydi Hide çökmeyi ezerdi.
            _bossDirector?.NotifyBossDown(worldMs);
            float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
            _boss?.BeginCollapse(collapseSec, worldMs);
            // Yeni rampa yazılmaz — T4 TimeDirector + SlowmoTuning tek kaynak (§11 / durum T12).
            _clock.Director.TriggerSlowmo();
            _deathPending = true;
            _sawDeathSlowmo = false;
        }

        // Kapanış izi (bu metot) ve seyahat izi (TickEffects, view.Scarred) iki ayrı bayrak:
        // biri seyahat çatlağının damgalanıp damgalanmadığını, diğeri kapanışın kendi izini
        // takip eder. Aynı bayrağı paylaşınca odaklı SARSINTI (`5-1`) seyahatte çatlak
        // bıraktığı için kapanış izini hiç bırakmıyordu (T7.1).
        void StampScar(LivingEffect logic, ClosingHit closing)
        {
            if (_closingStamped.Contains(logic))
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

            _closingStamped.Add(logic);
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing)
        {
            if (_boss == null || (_bossVitals != null && _bossVitals.IsDown))
                return;

            Vector3 from = new Vector3(logic.OriginX, 0f, logic.OriginZ);
            var man = _combat.Manifestation;
            float knock = man.BossKnockbackM;
            float lift = 0f;
            float shake = man.BossShakeSec;
            double worldMs = _clock.Director.WorldTimeMs;

            // Tür = fiziksel tepki ekseni. Hasar miktarı burada yok (§5 + §12).
            switch (closing.Type)
            {
                case Rune.Sarsinti:
                    // Havalandırma — spec §5 açıkça yazar.
                    knock = man.BossKnockbackM * 0.55f;
                    lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                    shake = man.BossShakeSec * 0.9f;
                    break;
                case Rune.Igne:
                    // Tek yöne derin geri tepme (§4 daralt/odakla).
                    knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                    lift = 0.05f;
                    shake = man.BossShakeSec * 0.55f;
                    break;
                case Rune.Suru:
                    // Yerinde çok noktalı sarsılma, yer değiştirme az (§4 çoğalt/yay).
                    knock = man.BossKnockbackM * 0.12f;
                    lift = 0.08f;
                    shake = man.BossShakeSec * 1.6f;
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.React(from, knock, lift, shake * 0.45f, worldMs);
                    _boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    _boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    return;
                case Rune.Kabuk:
                    // Sabitleme — §5.
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.Pin(0.55f, worldMs);
                    return;
                case Rune.Zehir:
                    // Birikinti izi StampScar'da; gövde hafif sarsılır.
                    knock = man.BossKnockbackM * 0.2f;
                    lift = 0f;
                    shake = man.BossShakeSec * 0.7f;
                    break;
            }

            if (!IsClosingInRange(logic, closing))
                return;

            _boss.React(from, knock, lift, shake, worldMs);
        }

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            Vector3 bossPos = _boss.transform.position;
            float dx = bossPos.x - logic.TipX;
            float dz = bossPos.z - logic.TipZ;
            float reach = _combat.Manifestation.ClosingBangRadiusM;
            if (closing.Type == Rune.Sarsinti)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.5f))
                {
                    float radial = Vector2.Distance(
                        new Vector2(bossPos.x, bossPos.z),
                        new Vector2(logic.OriginX, logic.OriginZ));
                    if (radial > logic.TipDistance + reach && radial > reach)
                        return false;
                }

                return true;
            }

            if (dx * dx + dz * dz > reach * reach)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.35f))
                    return false;
            }

            return true;
        }

        void TickEffects(float dtSec, double worldMs)
        {
            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            var man = _combat.Manifestation;
            bool bossDown = _bossVitals != null && _bossVitals.IsDown;

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

                if (!bossDown && logic.Phase == LivingEffectPhase.Traveling && _boss != null && !view.TravelHitDone)
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
                    _closingStamped.Remove(logic);
                    Destroy(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
