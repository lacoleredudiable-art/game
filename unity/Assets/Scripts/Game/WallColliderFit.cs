using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Modüler dungeon parçalarına (duvar/sütun/kemer/mobilya) BoxCollider ekler. Quaternius
    /// mesh'leri collider'sız geliyor (bkz. ArenaWalkFit "Fizik collider yok" — dış çevre için
    /// bilinçli bir basitleştirmeydi), bu yüzden oyuncu iç dekorun içine giriyordu (16 Eylül
    /// bug raporu: "duvarların içine giriliyor"). Yalnızca yürünmez parçalara eklenir; zemin/
    /// küçük dekor (torch, cobweb, banner, vase) dokunulmaz — gereksiz blokaj/maliyet olmasın.
    /// </summary>
    public static class WallColliderFit
    {
        static readonly string[] ObstacleNameHints =
        {
            "wall", "column", "pillar", "arch", "gate", "door",
            "barrel", "crate", "chest", "table", "statue", "trapdoor",
        };

        /// <summary>Uygun mesh'lere collider ekler; kaç adet eklendiğini döner (log/tanı için).</summary>
        public static int AddCollidersToObstacles(GameObject arena)
        {
            if (arena == null)
                return 0;

            int added = 0;
            var filters = arena.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf == null || mf.sharedMesh == null)
                    continue;
                if (mf.GetComponent<Collider>() != null)
                    continue;

                string n = mf.gameObject.name.ToLowerInvariant();
                if (!IsObstacleName(n))
                    continue;

                var box = mf.gameObject.AddComponent<BoxCollider>();
                Bounds b = mf.sharedMesh.bounds;
                box.center = b.center;
                box.size = b.size;
                added++;
            }

            return added;
        }

        static bool IsObstacleName(string lowerName)
        {
            for (int i = 0; i < ObstacleNameHints.Length; i++)
            {
                if (lowerName.Contains(ObstacleNameHints[i]))
                    return true;
            }
            return false;
        }
    }
}
