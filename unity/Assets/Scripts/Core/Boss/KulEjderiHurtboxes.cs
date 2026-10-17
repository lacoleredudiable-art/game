using System;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// KE-2.12 vuruş kutuları ve KE-2.6 menzil ölçümü ([K] 17 Ekim: her silah her bölgeye vurur). Her bölgenin gerçek
    /// gövde kutusu ve, gövde yerden yüksekteyse, altında yere inen bir sütunu vardır (çarpışmasız; oyuncu içinde durur).
    /// Menzil üç boyutlu, en yakın yüzeye ölçülür. Sütuna değen vuruşta sunum kor hilalini silahtan gerçek gövdedeki
    /// vuruş noktasına uçurur; parlama ve hasar sayısı o noktada çıkar. Sayılar <see cref="KulEjderiDefaults"/>'tan.
    /// </summary>
    public static class KulEjderiHurtboxes
    {
        /// <summary>Faz ve sendeleme/çökmeye göre model pozu (KE-6.2, KE-7.1, KE-7.8, KE-8.1).</summary>
        public static KulEjderiPose PoseFor(KulEjderiPhase phase, bool slumped)
        {
            if (slumped)
                return KulEjderiPose.Slumped;
            switch (phase)
            {
                case KulEjderiPhase.Phase1:
                    return KulEjderiPose.Lying;
                case KulEjderiPhase.Phase2:
                    return KulEjderiPose.Reared;
                default:
                    return KulEjderiPose.ChestDown;
            }
        }

        /// <summary>Bölgenin bu pozdaki parça sayısı (pençe 2, öbürleri 1; bu pozda yoksa 0).</summary>
        public static int PartCount(KulEjderiHitZone zone, KulEjderiPose pose)
        {
            switch (zone)
            {
                case KulEjderiHitZone.Claw:
                    return 2;
                case KulEjderiHitZone.Head:
                    return 1;
                case KulEjderiHitZone.Chest:
                    // KE-2.10: göğüs yalnız faz 2'de; faz 2 sendelemesi Poz_Sendeleme'dir
                    return pose == KulEjderiPose.Reared || pose == KulEjderiPose.Slumped ? 1 : 0;
                default:
                    return 0;
            }
        }

        /// <summary>Gerçek gövde kutusu; <paramref name="index"/> pençede 0 = batı, 1 = doğu. Yoksa null.</summary>
        public static KulEjderiBox? Body(KulEjderiHitZone zone, KulEjderiPose pose, int index)
        {
            if (index < 0 || index >= PartCount(zone, pose))
                return null;
            switch (zone)
            {
                case KulEjderiHitZone.Claw:
                {
                    float cx = index == 0 ? -KulEjderiDefaults.ClawCenterAbsX : KulEjderiDefaults.ClawCenterAbsX;
                    float half = KulEjderiDefaults.ClawWidthM * 0.5f;
                    return new KulEjderiBox(cx - half, cx + half, 0f, KulEjderiDefaults.ClawHeightM,
                        KulEjderiDefaults.ClawMinZ, KulEjderiDefaults.ClawMaxZ);
                }
                case KulEjderiHitZone.Head:
                    return HeadBox(pose);
                case KulEjderiHitZone.Chest:
                    return pose == KulEjderiPose.Reared
                        ? new KulEjderiBox(KulEjderiDefaults.ChestRearedMinX, KulEjderiDefaults.ChestRearedMaxX,
                            KulEjderiDefaults.ChestRearedMinY, KulEjderiDefaults.ChestRearedMaxY,
                            KulEjderiDefaults.ChestRearedMinZ, KulEjderiDefaults.ChestRearedMaxZ)
                        : new KulEjderiBox(KulEjderiDefaults.ChestSlumpedMinX, KulEjderiDefaults.ChestSlumpedMaxX,
                            KulEjderiDefaults.ChestSlumpedMinY, KulEjderiDefaults.ChestSlumpedMaxY,
                            KulEjderiDefaults.ChestSlumpedMinZ, KulEjderiDefaults.ChestSlumpedMaxZ);
                default:
                    return null;
            }
        }

        /// <summary>Gövdenin altında yere inen sütun (aynı X/Z izdüşümü, Y 0'dan gövdenin altına). Gövde yerdeyse null.</summary>
        public static KulEjderiBox? Column(KulEjderiBox body)
        {
            if (body.MinY <= 0f)
                return null;
            return new KulEjderiBox(body.MinX, body.MaxX, 0f, body.MinY, body.MinZ, body.MaxZ);
        }

        /// <summary>
        /// KE-2.6: <paramref name="attacker"/> noktasından menzili <paramref name="weaponRangeM"/> olan silah bölgeye
        /// ulaşır mı. Önce gerçek gövde, sonra sütun denenir; pençede yakın olan pençe seçilir.
        /// </summary>
        public static KulEjderiReach Probe(KulEjderiHitZone zone, KulEjderiPose pose, KulEjderiPoint attacker, float weaponRangeM)
        {
            KulEjderiReach best = KulEjderiReach.OutOfReach(float.PositiveInfinity);
            int count = PartCount(zone, pose);
            for (int i = 0; i < count; i++)
            {
                KulEjderiReach r = ProbePart(Body(zone, pose, i)!.Value, attacker, weaponRangeM);
                if (Better(r, best))
                    best = r;
            }

            return best;
        }

        static KulEjderiReach ProbePart(KulEjderiBox body, KulEjderiPoint attacker, float weaponRangeM)
        {
            float bodyDistance = body.Distance(attacker);
            KulEjderiPoint hitPoint = body.Nearest(attacker);
            if (bodyDistance <= weaponRangeM)
                return KulEjderiReach.Body(hitPoint, bodyDistance);
            KulEjderiBox? column = Column(body);
            if (column.HasValue && column.Value.Distance(attacker) <= weaponRangeM)
                return KulEjderiReach.Column(hitPoint, bodyDistance);
            return KulEjderiReach.OutOfReach(bodyDistance);
        }

        /// <summary>Ulaşan ulaşmayandan, doğrudan değen sütundan, sonra yakın olan uzaktan iyidir.</summary>
        static bool Better(KulEjderiReach a, KulEjderiReach b)
        {
            if (a.Reachable != b.Reachable)
                return a.Reachable;
            if (a.Reachable && a.ViaColumn != b.ViaColumn)
                return !a.ViaColumn;
            return a.DistanceM < b.DistanceM;
        }

        static KulEjderiBox HeadBox(KulEjderiPose pose)
        {
            switch (pose)
            {
                case KulEjderiPose.Lying:
                    return new KulEjderiBox(KulEjderiDefaults.HeadLyingMinX, KulEjderiDefaults.HeadLyingMaxX,
                        KulEjderiDefaults.HeadLyingMinY, KulEjderiDefaults.HeadLyingMaxY,
                        KulEjderiDefaults.HeadLyingMinZ, KulEjderiDefaults.HeadLyingMaxZ);
                case KulEjderiPose.Reared:
                    return new KulEjderiBox(KulEjderiDefaults.HeadRearedMinX, KulEjderiDefaults.HeadRearedMaxX,
                        KulEjderiDefaults.HeadRearedMinY, KulEjderiDefaults.HeadRearedMaxY,
                        KulEjderiDefaults.HeadRearedMinZ, KulEjderiDefaults.HeadRearedMaxZ);
                case KulEjderiPose.ChestDown:
                    return new KulEjderiBox(KulEjderiDefaults.HeadChestDownMinX, KulEjderiDefaults.HeadChestDownMaxX,
                        KulEjderiDefaults.HeadChestDownMinY, KulEjderiDefaults.HeadChestDownMaxY,
                        KulEjderiDefaults.HeadChestDownMinZ, KulEjderiDefaults.HeadChestDownMaxZ);
                case KulEjderiPose.Slumped:
                    return new KulEjderiBox(KulEjderiDefaults.HeadSlumpedMinX, KulEjderiDefaults.HeadSlumpedMaxX,
                        KulEjderiDefaults.HeadSlumpedMinY, KulEjderiDefaults.HeadSlumpedMaxY,
                        KulEjderiDefaults.HeadSlumpedMinZ, KulEjderiDefaults.HeadSlumpedMaxZ);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pose), pose, "Tanımsız poz.");
            }
        }
    }
}
