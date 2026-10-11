using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru dikey dilim yönetmeni: VfxPlan dinler, Kılıç ATIL katmanlarını oynatır.
    /// Zamanlama yalnız Animation Event (Trail_On/Off, Impact, Ejder); sabit klip karesi yok.
    /// Kor izi elde görünen silahın Vfx_Kabza / Vfx_Tip noktalarından örneklenir; nokta yoksa iz yok.
    /// Mekanik / hitbox değişmez.
    /// </summary>
    // Kılıç, kalıp (MotionTemplateBodyHost 12) ve zemin (ActorGroundingController 100) LateUpdate'te
    // kökü yazdıktan sonra örneklenmeli; önce örneklenirse iz bir kare geride kalır.
    [DefaultExecutionOrder(150)]
    public sealed class RuleDrivenVfxDirector : MonoBehaviour
    {
        MotionTemplateBodyHost _body;
        FeelVfxRuntime _feel;
        KorAwakenView _kor;
        EmberMeshTrailView _meshTrail;
        LightningDashTrailView _lightning;
        DragonSilhouetteCardView _silhouette;
        DragonRuneFlashView _runes;
        SkillAnimVfxEventHost _animRelay;
        EjderVfxBatchHost _batch;
        Transform _fxRoot;

        VfxPlan _plan;
        bool _armed;
        bool _trailOn;
        bool _silhouetteSpawned;
        bool _hitDone;
        bool _endDone;
        bool _wasDisplacing;
        Vector3 _dashStart;
        Vector3 _lastPos;
        Vector3 _dashDir = Vector3.forward;
        Transform _bladeHilt;
        Transform _bladeTip;
        float _footSparkCooldown;
        int _entryCell = RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
        float _entryLifeSec;
        float _entryFadeInSec;
        float _entryFadeOutSec;
        VfxColorRgb _entryColor;
        Vector3 _entryStart;
        bool _entryWasDisplacing;
        bool _runesPreflashed;

        public VfxPlan ActivePlan => _plan;
        public bool IsSliceActive => _armed && _plan.IsSwordDashSlice;
        /// <summary>Siluet, harf flaşı ve hedef halkasının ortak atlas batch'i.</summary>
        public EjderVfxBatchHost Batch => _batch;

        public void Bind(FeelVfxRuntime feel, EjderVfxBatchHost batch)
        {
            _feel = feel;
            _batch = batch;
            _body = GetComponent<MotionTemplateBodyHost>();
            EnsureChildren();
            _silhouette.Bind(batch);
            _runes.Bind(batch);
            EnsureAnimRelay();
        }

        /// <summary>
        /// Yol bağımsız skill başlangıcı (CastPipeline shout). Yalnız uyanış + rün;
        /// iz / silüet / isabet anim event veya teslim olayını bekler.
        /// </summary>
        public void BeginSkill(in SkillResolution skill, string weaponKey)
        {
            bool runesPreflashed = _runesPreflashed;
            _runesPreflashed = false;
            _plan = VfxPlanResolver.Resolve(skill, weaponKey);
            if (_plan.IsEmpty)
                return;

            EnsureChildren();
            EnsureAnimRelay();
            _armed = true;
            _trailOn = false;
            _silhouetteSpawned = false;
            _hitDone = false;
            _endDone = false;
            _wasDisplacing = _body != null && _body.IsDisplacing;
            _dashStart = transform.position;
            _lastPos = _dashStart;
            _footSparkCooldown = 0f;
            _dashDir = transform.forward;
            _dashDir.y = 0f;
            if (_dashDir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                _dashDir = Vector3.forward;
            _dashDir.Normalize();

            if (_plan.SkillAwaken)
                _kor.Begin(_plan.CoreColor);

            if (_plan.RuneLetterFlash && !runesPreflashed)
                PlayRunes(_plan);
        }

        /// <summary>
        /// v4 basılı tutma: harf flaşı 2. rün konduğu an yanar; skill parmak kalkınca
        /// <see cref="BeginSkill"/> ile çıkar ve flaşı tekrar çalmaz.
        /// </summary>
        public void FlashRunes(in SkillResolution skill, string weaponKey)
        {
            VfxPlan plan = VfxPlanResolver.Resolve(skill, weaponKey);
            if (plan.IsEmpty || !plan.RuneLetterFlash)
                return;
            EnsureChildren();
            PlayRunes(plan);
            _runesPreflashed = true;
        }

        /// <summary>Basılı tutulan skill iptal edildi: sonraki BeginSkill flaşı yeniden çalar.</summary>
        public void ClearRunePreflash() => _runesPreflashed = false;

        /// <summary>Kombo tanındı ama atılmadı (mana / bekleme): sönük kül harfleri.</summary>
        public void NotCast(int verbRuneId, int adjectiveRuneId, RuleEngineV4CastDenial denial)
        {
            EnsureChildren();
            _runes.PlayNotCast(verbRuneId, adjectiveRuneId, denial);
            _runesPreflashed = false;
        }

        void PlayRunes(in VfxPlan plan)
        {
            Color c = new Color(
                plan.CoreColor.R * RuleVfxArtDefaults.CoreHdrMult,
                plan.CoreColor.G * RuleVfxArtDefaults.CoreHdrMult,
                plan.CoreColor.B * RuleVfxArtDefaults.CoreHdrMult,
                1f);
            _runes.Play(plan.VerbRuneId, plan.AdjectiveRuneId, c);
        }

        /// <summary>
        /// skill_anim entry_silhouette: hücre ≥ 0 ise gövde yer değiştirmeye başladığı karede
        /// (hareket sisteminin Takla/Dodge state'ine girdiği an) bir kez silüet doğar; -1 bekleyeni siler.
        /// </summary>
        public void ArmEntrySilhouette(int atlasCell, float lifeSec, in VfxColorRgb color) =>
            ArmEntrySilhouette(atlasCell, lifeSec, 0f, 0f, color);

        /// <summary>fade_in_sec / fade_out_sec 0 ise kartın varsayılan belirme / sönmesi.</summary>
        public void ArmEntrySilhouette(int atlasCell, float lifeSec, float fadeInSec, float fadeOutSec, in VfxColorRgb color)
        {
            _entryCell = atlasCell;
            if (atlasCell < 0)
                return;
            EnsureChildren();
            _entryLifeSec = lifeSec;
            _entryFadeInSec = fadeInSec;
            _entryFadeOutSec = fadeOutSec;
            _entryColor = color;
            _entryStart = transform.position;
            _entryWasDisplacing = _body != null && _body.IsDisplacing;
            _silhouetteSpawned = false;
        }

        /// <summary>Animation Event: Trail_On — akan iz / şimşek açılır.</summary>
        public void OnTrailOn()
        {
            if (!_armed || _plan.IsEmpty || _trailOn)
                return;
            if (!_plan.LightningDashTrail && _plan.Carrier != VfxCarrierKind.FlowingEmberTrail)
                return;
            StartTrail();
        }

        /// <summary>Animation Event: Trail_Off.</summary>
        public void OnTrailOff()
        {
            if (!_trailOn)
                return;
            _trailOn = false;
            _bladeHilt = null;
            _bladeTip = null;
            _meshTrail?.StopEmit();
        }

        public bool TrailOn => _trailOn;
        public EmberMeshTrailView MeshTrail => _meshTrail;

        /// <summary>Animation Event: Impact — isabet görseli (gameplay NotifyHit ile aynı kapı).</summary>
        public void OnImpactAnimEvent()
        {
            if (!_armed)
                return;
            Vector3 origin = transform.position + _dashDir * RuleVfxArtDefaults.HitForwardPad;
            NotifyHit(origin, _dashDir);
        }

        /// <summary>Animation Event: Ejder — silüet kartı (Çekiç vb.; Kılıç ATIL state girişi kodda).</summary>
        public void OnEjderAnimEvent()
        {
            if (!_armed || _plan.IsEmpty || _silhouetteSpawned)
                return;
            if (!_plan.DragonTailArcSilhouette && _plan.DragonAtlasCell < 0)
                return;
            SpawnSilhouetteAlongDash();
        }

        public void NotifyHit(Vector3 hitOrigin, Vector3 hitDir)
        {
            if (!_armed || _hitDone || _plan.IsEmpty)
                return;
            _hitDone = true;
            if (hitDir.sqrMagnitude > RuleVfxDefaults.PathSampleEpsSq)
            {
                _dashDir = hitDir;
                _dashDir.y = 0f;
                _dashDir.Normalize();
            }

            Vector3 target = hitOrigin + _dashDir * Mathf.Max(
                RuleVfxArtDefaults.HitForwardMin,
                hitOrigin == default ? 0.5f : RuleVfxArtDefaults.HitForwardPad);

            if (_plan.SlashArcOnHit || _plan.ZararClawMarksOnHit)
                SwordAtilHitVfx.Spawn(_fxRoot, target, _dashDir, _plan, _feel);

            _kor?.SignalDelivery();
        }

        public void NotifyMotionEnded(bool stoppedAtBodyEdge)
        {
            // Engellenen atılış yer değiştirmeden biter; bekleyen giriş silüeti sonraki harekete kalmasın.
            _entryCell = RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
            if (!_armed || _endDone)
                return;
            _endDone = true;
            if (_trailOn)
                OnTrailOff();
            _kor?.SignalDelivery();

            if (stoppedAtBodyEdge && _plan.EdgeStopEmberSpark)
                EdgeStopEmberSparks.Spawn(transform.position, _plan.CoreColor, _feel);

            _armed = false;
        }

        void LateUpdate()
        {
            TickEntrySilhouette();
            if (!_armed || _plan.IsEmpty)
                return;

            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 delta = pos - _lastPos;

            if (delta.sqrMagnitude > RuleVfxDefaults.PathSampleEpsSq)
            {
                _dashDir = delta;
                _dashDir.y = 0f;
                if (_dashDir.sqrMagnitude > RuleVfxDefaults.PathSampleEpsSq)
                    _dashDir.Normalize();
            }

            bool displacing = _body != null && _body.IsDisplacing;
            bool justStartedDisplace = displacing && !_wasDisplacing;
            bool justEndedDisplace = !displacing && _wasDisplacing;

            // §3.4 Kılıç ATIL: silüet state girişi (kod) — klip karesi değil.
            if (justStartedDisplace && _plan.DragonTailArcSilhouette && !_silhouetteSpawned)
                SpawnSilhouetteAlongDash();

            if (_trailOn && !BladeAlive())
            {
                Debug.LogError("[RuleDrivenVfx] Kor izi: Vfx_Kabza / Vfx_Tip iz sırasında kayboldu; iz kesildi.");
                OnTrailOff();
            }

            if (_trailOn)
            {
                Vector3 tip = _bladeTip.position;
                _meshTrail.SampleBlade(_bladeHilt.position, tip);
                if (_plan.LightningDashTrail)
                    _lightning.ExtendTip(tip);

                if (_plan.WingFootSparks)
                {
                    _footSparkCooldown -= dt;
                    if (_footSparkCooldown <= 0f && delta.sqrMagnitude > RuleVfxDefaults.FootSparkMoveEpsSq)
                    {
                        EmitFootWingSparks();
                        _footSparkCooldown = RuleVfxDefaults.FootSparkIntervalSec;
                    }
                }
            }

            _lastPos = pos;

            // Shout BeginSkill dash'ten önce gelir; bitiş yalnız displace→idle geçişinde.
            if (justEndedDisplace && !_endDone && _armed)
            {
                if (_plan.IsSwordDashSlice || _plan.LightningDashTrail || _trailOn)
                    NotifyMotionEnded(_body.SweepRunner != null && _body.SweepRunner.StoppedAtBodyEdge);
            }

            _wasDisplacing = displacing;
        }

        void StartTrail()
        {
            if (!ResolveBlade())
            {
                Debug.LogError("[RuleDrivenVfx] Kor izi çizilmedi: elde görünen Silah_* altında "
                    + WeaponBladePoints.HiltPointName + " ve " + WeaponBladePoints.TipPointName + " yok.");
                return;
            }
            _trailOn = true;
            _dashStart = transform.position;
            RuleEngineV4BladeTrailFx fx = RuleVfxSettings.Slice.BladeTrail;
            _meshTrail.Begin(_plan.CoreColor, fx);
            Vector3 tip = _bladeTip.position;
            _meshTrail.SampleBlade(_bladeHilt.position, tip);
            if (_plan.LightningDashTrail)
            {
                _lightning.Play(
                    _dashStart + Vector3.up * RuleVfxArtDefaults.DashStartLift,
                    tip,
                    _plan.CoreColor,
                    fx.LifeSec);
            }
        }

        bool ResolveBlade()
        {
            ActorView view = GetComponent<ActorView>();
            Transform root = view != null && view.Animator != null ? view.Animator.transform : transform;
            return WeaponBladePoints.TryActiveBladePoints(root, out _bladeHilt, out _bladeTip);
        }

        bool BladeAlive() =>
            _bladeHilt != null && _bladeTip != null
            && _bladeHilt.gameObject.activeInHierarchy && _bladeTip.gameObject.activeInHierarchy;

        void TickEntrySilhouette()
        {
            if (_entryCell < 0)
                return;
            bool displacing = _body != null && _body.IsDisplacing;
            bool started = displacing && !_entryWasDisplacing;
            _entryWasDisplacing = displacing;
            if (!started)
                return;
            int cell = _entryCell;
            _entryCell = RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
            Vector3 dir = transform.position - _entryStart;
            dir.y = 0f;
            if (dir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
            {
                dir = transform.forward;
                dir.y = 0f;
            }
            if (dir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                dir = Vector3.forward;
            float life = _entryLifeSec > 0f ? _entryLifeSec : RuleVfxDefaults.SwordDashSilhouetteLifeSec;
            SpawnSilhouette(dir.normalized, _entryColor, life, cell, _entryFadeInSec, _entryFadeOutSec);
        }

        void SpawnSilhouetteAlongDash()
        {
            float life = _plan.SilhouetteLifeSec > 0f
                ? _plan.SilhouetteLifeSec
                : RuleVfxDefaults.SwordDashSilhouetteLifeSec;
            int cell = _plan.DragonAtlasCell >= 0
                ? _plan.DragonAtlasCell
                : RuleVfxDefaults.SwordDashSilhouetteAtlasCell;
            SpawnSilhouette(_dashDir, _plan.CoreColor, life, cell);
        }

        void SpawnSilhouette(Vector3 dir, in VfxColorRgb color, float lifeSec, int cell, float fadeInSec = 0f, float fadeOutSec = 0f)
        {
            if (_silhouetteSpawned)
                return;
            _silhouetteSpawned = true;
            Vector3 from = transform.position + Vector3.up * RuleVfxArtDefaults.SilhouettePathLift;
            Vector3 to = from + dir * RuleVfxDefaults.DashForeshadowM;
            _silhouette.PlayAlongLine(from, to, color, lifeSec, cell, fadeInSec, fadeOutSec);
        }

        void EmitFootWingSparks()
        {
            Vector3 foot = transform.position;
            foot.y = FeelVfxRuntime.GroundY + RuleVfxDefaults.FootSparkLiftM;
            Color tint = new Color(_plan.CoreColor.R, _plan.CoreColor.G, _plan.CoreColor.B, 1f);
            _feel?.HitSpark(foot, tint, crit: false);
            Vector3 side = Vector3.Cross(Vector3.up, _dashDir).normalized * RuleVfxDefaults.FootWingSideM;
            _feel?.HitSpark(foot + side, tint, crit: false);
            _feel?.HitSpark(foot - side, tint, crit: false);
        }

        void EnsureAnimRelay()
        {
            if (_animRelay != null)
            {
                _animRelay.Bind(this);
                return;
            }

            ActorView view = GetComponent<ActorView>();
            Animator anim = view != null ? view.Animator : GetComponentInChildren<Animator>();
            GameObject host = anim != null ? anim.gameObject : gameObject;
            _animRelay = host.GetComponent<SkillAnimVfxEventHost>()
                ?? host.AddComponent<SkillAnimVfxEventHost>();
            _animRelay.Bind(this);
        }

        void EnsureChildren()
        {
            if (_fxRoot == null)
            {
                var go = new GameObject("RuleDrivenVfx");
                go.transform.SetParent(transform, false);
                _fxRoot = go.transform;
            }

            if (_kor == null)
                _kor = _fxRoot.gameObject.AddComponent<KorAwakenView>();
            if (_meshTrail == null)
                _meshTrail = _fxRoot.gameObject.AddComponent<EmberMeshTrailView>();
            if (_lightning == null)
                _lightning = _fxRoot.gameObject.AddComponent<LightningDashTrailView>();
            if (_silhouette == null)
            {
                var silGo = new GameObject("DragonSilhouette");
                silGo.transform.SetParent(_fxRoot, false);
                _silhouette = silGo.AddComponent<DragonSilhouetteCardView>();
            }

            if (_runes == null)
                _runes = _fxRoot.gameObject.AddComponent<DragonRuneFlashView>();
        }
    }
}
