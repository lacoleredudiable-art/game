using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Ambiyans portu (PR #43 "deneme sahnesi" → ana dövüş sahnesi): sahne koddan kurulduğu için
    /// (.unity/.prefab elle düzenlenmez) bu küçük Resources asset'i gerçek sanat varlıklarına
    /// (Assets/Art/DenemeSahnesi, Assets/Art/PolyHaven — ejderha hariç) doğrudan referans tutar.
    /// Asset <c>Assets/Resources/Environment/CombatAmbienceAssets.asset</c> yolunda, Editor
    /// kodu ile (elle değil) oluşturulur. Bulunamazsa <see cref="CombatAmbienceEnvironment"/>
    /// eski prosedürel <see cref="ArenaHorizon"/>'a geri döner.
    /// </summary>
    public sealed class CombatAmbienceAssets : ScriptableObject
    {
        public const string ResourcePath = "Environment/CombatAmbienceAssets";

        [System.Serializable]
        public struct RockKind
        {
            public GameObject Model;
            public Material Material;
        }

        [Header("Zemin (DenemeSahnesi/Meshes + PolyHaven dark_rock)")]
        public GameObject GroundNearModel;
        public GameObject GroundFarModel;
        public Material GroundMaterial;

        [Header("Ufuk (Skyline silüeti + kenar sisi)")]
        public GameObject SkylineModel;
        public Material SkylineMaterial;
        public GameObject EdgeMistModel;
        public Material EdgeMistMaterial;

        [Header("Sıcak vurgu — tek izinli renkli öğe (lav çatlağı)")]
        public GameObject LavaCracksModel;
        public Material LavaCracksMaterial;

        /// <summary>DenemeSahnesiLayout.json "boundary.radius" — tüm ölçekler bu referansa göre.</summary>
        public const float DesignBoundaryRadiusM = 33f;

        [Header("Kayalar (PolyHaven CC0 — oyun alanı dışına, collider'sız)")]
        public RockKind[] Rocks;

        [Header("Kenar detay (Quaternius CC0 — duvar hemen dışı, collider'sız)")]
        public GameObject[] EdgeProps;
    }
}
