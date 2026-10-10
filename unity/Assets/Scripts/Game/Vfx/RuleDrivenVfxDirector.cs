using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru dikey dilim yönetmeni: VfxPlan dinler, Kılıç ATIL katmanlarını oynatır.
    /// Zamanlama Animation Event (Trail_On/Off, Impact, Ejder) veya spec yedeği (uç hızı);
    /// sabit klip karesi yok. Mekanik / hitbox değişmez.
    /// </summary>
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
        Vector3 _lastTip;
        Vector3 _dashDir = Vector3.forward;
        float _footSparkCooldown;
        float _tipSpeedMps;

        public VfxPlan ActivePlan => _plan;
        public bool IsSliceActive => _armed && _plan.IsSwordDashSlice;

        public void Bind(FeelVfxRuntime feel)
        {
            _feel = feel;
            _body = GetComponent<MotionTemplateBodyHost>();
            EnsureChildren();
            EnsureAnimRelay();
        }

        /// <summary>
        /// Yol bağımsız skill başlangıcı (CastPipeline shout). Yalnız uyanış + rün;
        /// iz / silüet / isabet anim event veya teslim olayını bekler.
        /// </summary>
        public void BeginSkill(in SkillResolution skill, string weaponKey)
        {
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
            _lastTip = BladeTip();
            _tipSpeedMps = 0f;
            _footSparkCooldown = 0f;
            _dashDir = transform.forward;
            _dashDir.y = 0f;
            if (_dashDir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                _dashDir = Vector3.forward;
            _dashDir.Normalize();

            if (_plan.SkillAwaken)
                _kor.Begin(_plan.CoreColor);

            if (_plan.RuneLetterFlash)
            {
                Color c = new Color(
                    _plan.CoreColor.R * RuleVfxArtDefaults.CoreHdrMult,
                    _plan.CoreColor.G * RuleVfxArtDefaults.CoreHdrMult,
                    _plan.CoreColor.B * RuleVfxArtDefaults.CoreHdrMult,
                    1f);
                _runes.Play(_plan.VerbRuneId, _plan.AdjectiveRuneId, c);
            }
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
            _meshTrail?.StopEmit();
        }

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
            if (!_armed || _plan.IsEmpty)
                return;

            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 tip = BladeTip();
            Vector3 delta = pos - _lastPos;
            if (dt > 0f)
                _tipSpeedMps = (tip - _lastTip).magnitude / dt;

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

            // Trail_On yoksa yedek: uç hızı > TrailAutoOpenTipSpeedMps (§3.1); kare numarası yok.
            if (!_trailOn
                && _plan.LightningDashTrail
                && _tipSpeedMps > RuleVfxDefaults.TrailAutoOpenTipSpeedMps)
                StartTrail();

            if (_trailOn)
            {
                Vector3 guard = BladeGuard();
                _meshTrail.SampleBlade(guard, tip);
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
            _lastTip = tip;

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
            _trailOn = true;
            _dashStart = transform.position;
            float trailLife = _plan.TrailLifeSec > 0f
                ? _plan.TrailLifeSec
                : RuleVfxDefaults.SwordDashTrailLifeSec;
            _meshTrail.Begin(_plan.CoreColor, trailLife, _dashDir);
            Vector3 tip = BladeTip();
            Vector3 guard = BladeGuard();
            _meshTrail.SampleBlade(guard, tip);
            _lightning.Play(
                _dashStart + Vector3.up * RuleVfxArtDefaults.DashStartLift,
                tip,
                _plan.CoreColor,
                trailLife);
        }

        void SpawnSilhouetteAlongDash()
        {
            if (_silhouetteSpawned)
                return;
            _silhouetteSpawned = true;
            Vector3 from = transform.position + Vector3.up * RuleVfxArtDefaults.SilhouettePathLift;
            Vector3 to = from + _dashDir * RuleVfxDefaults.DashForeshadowM;
            float life = _plan.SilhouetteLifeSec > 0f
                ? _plan.SilhouetteLifeSec
                : RuleVfxDefaults.SwordDashSilhouetteLifeSec;
            int cell = _plan.DragonAtlasCell >= 0
                ? _plan.DragonAtlasCell
                : RuleVfxDefaults.SwordDashSilhouetteAtlasCell;
            _silhouette.PlayAlongLine(from, to, _plan.CoreColor, life, cell);
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

        Vector3 BladeTip()
        {
            return transform.position
                + Vector3.up * RuleVfxDefaults.WeaponTipLocalY
                + _dashDir * RuleVfxDefaults.BladeTipForwardM;
        }

        Vector3 BladeGuard()
        {
            return transform.position
                + Vector3.up * RuleVfxDefaults.WeaponGuardLocalY
                + _dashDir * RuleVfxDefaults.BladeGuardForwardM;
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
