using Dovus.App.Team;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Team;
using Dovus.Game.Weapons;
using System;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>StatusBoard taÅŸÄ±yÄ±cÄ±sÄ± â€” oyuncu veya boss.</summary>
    public sealed class ActorStatus : MonoBehaviour
    {
        public StatusBoard Board { get; } = new StatusBoard();

        /// <summary>Slot pasif Ã§arpanlarÄ± (reflect) â€” yalnÄ±z oyuncu.</summary>
        public SlotPassiveDirector SlotPassiveDirector { get; set; }

        /// <summary>Kalkan sonrasÄ± gerÃ§ek gelen hasar; radial kesme ve poise iÃ§in.</summary>
        public event System.Action<float> DamageTaken;

        /// <summary>Kalkan vuruÅŸu yuttu (can dÃ¼ÅŸmese de). KarÅŸÄ± saldÄ±rÄ± penceresi bunu dinler.</summary>
        public event System.Action<float> DamageBlocked;

        /// <summary>
        /// Can baÄŸÄ± / yÃ¶nlendirme adaptÃ¶rÃ¼. Gelen miktarÄ± takÄ±m arkadaÅŸÄ±na veya dÃ¼ÅŸmana
        /// paylaÅŸtÄ±rÄ±r ve oyuncuda kalacak miktarÄ± dÃ¶ndÃ¼rÃ¼r.
        /// </summary>
        public System.Func<float, float> IncomingDamageRedirect { get; set; }

        string _castMobility = string.Empty;
        double _castMobilityUntilMs;

        bool CastMobilityActive => _clock != null && _clock.Director.WorldTimeMs < _castMobilityUntilMs;

        /// <summary>KinematicMotor bunu okur â€” dÃ¼ÅŸman CC'sinden ayrÄ± cast mobility.</summary>
        public float EffectiveMoveSpeedMult =>
            Board.MoveSpeedMult
            * ActorStatusTeamMoveSpeed.TeamMoveSpeedMult(
                PortalBorderTeamHooks.Table,
                _playerVitals != null,
                PortalBorderTeamHooks.PlayerActorId)
            * (CastMobilityActive && _castMobility == Dovus.Core.Grammar.SkillMobility.SlowedMove
                ? _tuning.SlowSpeedMult
                : 1f);

        public bool EffectiveBlocksMovement =>
            Board.BlocksMovement
            || (CastMobilityActive && _castMobility == Dovus.Core.Grammar.SkillMobility.Rooted);

        public void GrantCastMobility(string mobility, double untilWorldMs)
        {
            _castMobility = mobility ?? string.Empty;
            _castMobilityUntilMs = untilWorldMs;
        }

        public void ClearCastMobility()
        {
            _castMobility = string.Empty;
            _castMobilityUntilMs = 0;
        }

        StatusTuning _tuning = new();
        GameClock _clock;
        BossVitals _bossVitals;
        PlayerVitals _playerVitals;
        BossReactor _reactor;
        bool _stealthVisual;
        Renderer[] _renderers;

        /// <summary>Oyuncu reflect pasifi iÃ§in boss canÄ± (Bind'de bossVitals yoksa ayrÄ±ca set).</summary>
        public BossVitals ReflectBossVitals { get; set; }
        /// <summary>Doluysa yansÄ±yan hasar buraya gider (bÃ¶lÃ¼nen yansÄ±ma vb.); boÅŸsa doÄŸrudan boss'a.</summary>
        public Action<float> ReflectSink { get; set; }

        float _skillReflectRatio;
        double _skillReflectUntilMs;

        /// <summary>YansÄ±ma fiili / Aynalama sÄ±fatÄ±: reflect_ratio, reflect_duration_sec boyunca.</summary>
        public void GrantReflect(float ratio, double untilWorldMs)
        {
            if (ratio <= 0f)
                return;
            _skillReflectRatio = ratio;
            _skillReflectUntilMs = untilWorldMs;
        }

        public float ActiveSkillReflectRatio =>
            _clock != null && _clock.Director.WorldTimeMs < _skillReflectUntilMs ? _skillReflectRatio : 0f;

        public void Bind(
            GameClock clock,
            StatusTuning tuning,
            PlayerVitals playerVitals = null,
            BossVitals bossVitals = null,
            BossReactor reactor = null)
        {
            _clock = clock;
            _tuning = tuning ?? new StatusTuning();
            _playerVitals = playerVitals;
            _bossVitals = bossVitals;
            _reactor = reactor;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public StatusTuning Tuning
        {
            get => _tuning;
            set => _tuning = value ?? new StatusTuning();
        }

        void Update()
        {
            if (_clock == null)
                return;

            float payload = Board.Tick(_clock.WorldDeltaMs, _tuning);
            SyncStealthVisual();
            if (Mathf.Abs(payload) < 0.001f)
                return;

            if (payload > 0f)
            {
                // S4: DoT tiki (yanma/zehir) kaÃ§Ä±ÅŸla yutulmaz ve zÄ±rhÄ± deler (tasarÄ±m kararÄ±); boss'ta sayÄ± gÃ¶sterilir.
                ApplyDamage(payload, dodgeable: false, pierceArmor: true);
                if (_bossVitals != null && LastAppliedDamage > 0f)
                    DamageOverTimeDealt?.Invoke(LastAppliedDamage);
            }
            else
                ApplyHeal(-payload);
        }

        /// <summary>Gizlilik: camgÃ¶beÄŸi yarÄ± saydam (oyuncu efekt rengi kuralÄ±).</summary>
        void SyncStealthVisual()
        {
            bool stealth = Board.IsStealthed;
            if (stealth == _stealthVisual)
                return;
            _stealthVisual = stealth;
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);

            float a = stealth ? 0.35f : 1f;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;
                Material mat = r.material;
                if (mat == null)
                    continue;
                bool hasBase = mat.HasProperty("_BaseColor");
                bool hasColor = mat.HasProperty("_Color");
                if (!hasBase && !hasColor)
                    continue;
                if (hasBase)
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = a;
                    mat.SetColor("_BaseColor", c);
                }
                else
                {
                    Color c = mat.color;
                    c.a = a;
                    mat.color = c;
                }
            }
        }

        public ArmorSheet Armor { get; } = new ArmorSheet();
        /// <summary>Son ApplyDamage Ã§aÄŸrÄ±sÄ±nda cana (boss/oyuncu) gerÃ§ekten geÃ§en hasar; kalkan/i-frame/Stasis yuttuysa 0 (S6).</summary>
        public float LastAppliedDamage { get; private set; }
        /// <summary>S4: boss'a iÅŸleyen DoT tiki (hasar sayÄ±sÄ± iÃ§in).</summary>
        public event Action<float> DamageOverTimeDealt;
        public bool LastHitWasCrit { get; set; }
        public float LastThreat { get; set; }
        public float LastPoise { get; set; }

        public void ApplyDamage(float raw, bool dodgeable = true, bool pierceArmor = false)
        {
            LastAppliedDamage = 0f;
            if (raw <= 0f) return;
            // Ä°-frame, hesaptan Ã¶nce. Yutulan vuruÅŸ boruya girmez.
            if (_playerVitals != null && PlayerDodgeRig.BlocksIncoming(this, dodgeable))
                return;
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            float taken = Board.IncomingDamageMult;
            if (_playerVitals != null)
                taken *= PortalBorderTeamHooks.PlayerDamageTakenMult;
            // KalkanÄ±n kÄ±sa kalkanÄ±, tahta kalkanÄ±yla aynÄ± son aÅŸamada (f) erir.
            // Dodge yukarÄ±da yuttuysa bu havuza hiÃ§ girilmez.
            float shortShield = 0f;
            WeaponShortShieldHost shortHost = null;
            if (_playerVitals != null)
            {
                shortHost = GetComponent<WeaponShortShieldHost>();
                if (shortHost != null && shortHost.Shield.Active(now))
                    shortShield = shortHost.Shield.Points;
            }
            var outcome = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = raw,
                AttackPower = 1f,
                Multiplier = 1f,
                CanCrit = false,
                Armor = pierceArmor ? 0f : Armor.Effective(now),
                DamageTakenFactor = taken,
                Shield = Board.ShieldRemaining + shortShield,
                Invulnerable = BossStatusMath.DamageInvulnerable(Board.IsInvulnerable),
                Poise = raw,
                ScaleMagnitudes = true
            });
            LastHitWasCrit = outcome.WasCrit;
            LastThreat = outcome.Threat;
            LastPoise = outcome.Poise;
            if (outcome.ShieldAbsorbed > 0f)
            {
                float fromBoard = Mathf.Min(Board.ShieldRemaining, outcome.ShieldAbsorbed);
                if (fromBoard > 0f)
                    Board.ConsumeShield(fromBoard);
                float fromShort = outcome.ShieldAbsorbed - fromBoard;
                if (fromShort > 0f && shortHost != null)
                    shortHost.Shield.Consume(fromShort, now);
            }
            float afterShield = outcome.Amount;
            if (outcome.ShieldAbsorbed > 0f)
                DamageBlocked?.Invoke(outcome.ShieldAbsorbed);
            float reflectBase = BossStatusMath.ReflectBase(afterShield, outcome.ShieldAbsorbed, scaled: true);
            TryReflect(reflectBase);
            if (afterShield <= 0f) return;
            if (IncomingDamageRedirect != null)
                afterShield = Mathf.Max(0f, IncomingDamageRedirect(afterShield));
            if (afterShield <= 0f) return;

            if (_bossVitals != null)
            {
                LastAppliedDamage = afterShield;
                _bossVitals.ApplyDamage(afterShield);
            }
            else if (_playerVitals != null)
            {
                LastAppliedDamage = afterShield;
                DamageTaken?.Invoke(afterShield);
                _playerVitals.ApplyDamage(Mathf.CeilToInt(afterShield), dodgeable, shortShieldAlreadyApplied: true);
            }
        }

        void TryReflect(float reflectBase)
        {
            if (reflectBase <= 0f || _playerVitals == null)
                return;
            float reflect = ActiveSkillReflectRatio;
            reflect += SlotPassiveDirector?.ReflectRatioAdd ?? 0f;
            BossVitals reflectTarget = ReflectBossVitals;
            if (reflect <= 0f || reflectTarget == null || reflectTarget.IsDown)
                return;
            if (ReflectSink != null)
                ReflectSink(reflectBase * reflect);
            else
                reflectTarget.ApplyDamage(reflectBase * reflect);
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f)
                return;
            // 16 EylÃ¼l: "Kavurucu Yara" â€” yanÄ±k hedefte pasif regen tick'i de azalÄ±r.
            float healMult = Board.HealEffectivenessMult;
            var healedOutcome = DamagePipeline.Resolve(new DamageQuery
            {
                Heal = true,
                HealPower = amount,
                HealMultiplier = healMult,
                ScaleMagnitudes = true
            });
            LastThreat = healedOutcome.Threat;
            if (_playerVitals != null)
            {
                _playerVitals.ApplyHeal(Mathf.CeilToInt(healedOutcome.Amount));
            }
        }

        public void ApplyKnockbackFrom(Vector3 fromWorld)
        {
            if (_reactor == null)
                return;
            _reactor.React(
                fromWorld,
                _tuning.KnockbackMeters,
                _tuning.KnockbackLiftM,
                _tuning.KnockbackShakeSec,
                _clock != null ? _clock.Director.WorldTimeMs : 0);
        }

        /// <summary>Ã§ekme sÄ±fatÄ± â€” boss'u towardWorld yÃ¶nÃ¼ne (oyuncuya) Ã§eker.</summary>
        public void ApplyPullToward(Vector3 towardWorld)
        {
            if (_reactor == null)
                return;
            // React fromWorld'dan uzaÄŸa iter; karÅŸÄ± taraftan itince pull olur.
            Vector3 pos = transform.position;
            Vector3 away = pos + (pos - towardWorld);
            _reactor.React(
                away,
                _tuning.KnockbackMeters,
                _tuning.KnockbackLiftM,
                _tuning.KnockbackShakeSec,
                _clock != null ? _clock.Director.WorldTimeMs : 0);
        }
    }
}
