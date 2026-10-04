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
    /// <summary>StatusBoard taşıyıcısı — oyuncu veya boss.</summary>
    public sealed class ActorStatus : MonoBehaviour
    {
        public StatusBoard Board { get; } = new StatusBoard();

        /// <summary>Slot pasif çarpanları (reflect) — yalnız oyuncu.</summary>
        public SlotPassiveDirector SlotPassiveDirector { get; set; }

        /// <summary>Kalkan sonrası gerçek gelen hasar; radial kesme ve poise için.</summary>
        public event System.Action<float> DamageTaken;

        /// <summary>Kalkan vuruşu yuttu (can düşmese de). Karşı saldırı penceresi bunu dinler.</summary>
        public event System.Action<float> DamageBlocked;

        /// <summary>
        /// Can bağı / yönlendirme adaptörü. Gelen miktarı takım arkadaşına veya düşmana
        /// paylaştırır ve oyuncuda kalacak miktarı döndürür.
        /// </summary>
        public System.Func<float, float> IncomingDamageRedirect { get; set; }

        string _castMobility = string.Empty;
        double _castMobilityUntilMs;

        bool CastMobilityActive => _clock != null && _clock.Director.WorldTimeMs < _castMobilityUntilMs;

        /// <summary>KinematicMotor bunu okur — düşman CC'sinden ayrı cast mobility.</summary>
        public float EffectiveMoveSpeedMult
        {
            get
            {
                TeamModifierHub hub = _team != null ? _team.Hub : TeamModifierHub.Neutral;
                return Board.MoveSpeedMult
                       * ActorStatusTeamMoveSpeed.TeamMoveSpeedMult(
                           hub.Table,
                           _playerVitals != null,
                           hub.PlayerActorId)
                       * (CastMobilityActive && _castMobility == Dovus.Core.Grammar.SkillMobility.SlowedMove
                           ? _tuning.SlowSpeedMult
                           : 1f);
            }
        }

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

        TeamComboAccess _team;
        StatusTuning _tuning = new();
        GameClock _clock;
        BossVitals _bossVitals;
        PlayerVitals _playerVitals;
        BossReactor _reactor;
        bool _stealthVisual;
        Renderer[] _renderers;

        /// <summary>Oyuncu reflect pasifi için boss canı (Bind'de bossVitals yoksa ayrıca set).</summary>
        public BossVitals ReflectBossVitals { get; set; }
        /// <summary>Doluysa yansıyan hasar buraya gider (bölünen yansıma vb.); boşsa doğrudan boss'a.</summary>
        public Action<float> ReflectSink { get; set; }

        float _skillReflectRatio;
        double _skillReflectUntilMs;

        /// <summary>Yansıma fiili / Aynalama sıfatı: reflect_ratio, reflect_duration_sec boyunca.</summary>
        public void GrantReflect(float ratio, double untilWorldMs)
        {
            if (ratio <= 0f)
                return;
            _skillReflectRatio = ratio;
            _skillReflectUntilMs = untilWorldMs;
        }

        public float ActiveSkillReflectRatio =>
            _clock != null && _clock.Director.WorldTimeMs < _skillReflectUntilMs ? _skillReflectRatio : 0f;

        public void BindTeam(TeamComboAccess team) => _team = team;

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
                // S4: DoT tiki (yanma/zehir) kaçışla yutulmaz ve zırhı deler (tasarım kararı); boss'ta sayı gösterilir.
                ApplyDamage(payload, dodgeable: false, pierceArmor: true);
                if (_bossVitals != null && LastAppliedDamage > 0f)
                    DamageOverTimeDealt?.Invoke(LastAppliedDamage);
            }
            else
                ApplyHeal(-payload);
        }

        /// <summary>Gizlilik: camgöbeği yarı saydam (oyuncu efekt rengi kuralı).</summary>
        void SyncStealthVisual()
        {
            bool stealth = Board.IsStealthed;
            if (stealth == _stealthVisual)
                return;
            _stealthVisual = stealth;
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);

            float a = stealth ? ActorStatusDefaults.StealthAlpha : 1f;
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
        /// <summary>Son ApplyDamage çağrısında cana (boss/oyuncu) gerçekten geçen hasar; kalkan/i-frame/Stasis yuttuysa 0 (S6).</summary>
        public float LastAppliedDamage { get; private set; }
        /// <summary>S4: boss'a işleyen DoT tiki (hasar sayısı için).</summary>
        public event Action<float> DamageOverTimeDealt;
        public bool LastHitWasCrit { get; set; }
        public float LastThreat { get; set; }
        public float LastPoise { get; set; }

        public void ApplyDamage(float raw, bool dodgeable = true, bool pierceArmor = false)
        {
            LastAppliedDamage = 0f;
            if (raw <= 0f) return;
            // İ-frame, hesaptan önce. Yutulan vuruş boruya girmez.
            if (_playerVitals != null && PlayerDodgeRig.BlocksIncoming(this, dodgeable))
                return;
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            float taken = Board.IncomingDamageMult;
            if (_playerVitals != null && _team != null)
                taken *= _team.Hub.PlayerDamageTakenMult;
            // Kalkanın kısa kalkanı, tahta kalkanıyla aynı son aşamada (f) erir.
            // Dodge yukarıda yuttuysa bu havuza hiç girilmez.
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
            // 16 Eylül: "Kavurucu Yara" — yanık hedefte pasif regen tick'i de azalır.
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

        /// <summary>çekme sıfatı — boss'u towardWorld yönüne (oyuncuya) çeker.</summary>
        public void ApplyPullToward(Vector3 towardWorld)
        {
            if (_reactor == null)
                return;
            // React fromWorld'dan uzağa iter; karşı taraftan itince pull olur.
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
