using System.Collections.Generic;
using Dovus.Core.Layers;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Bağlama 7: ZoneDirector ActiveZones → dünyada renkli disk (PlaceholderFactory).
    /// GroundScarField / StateBridgeView deseni — soft-cap veya süre bitince obje düşer.
    /// </summary>
    public sealed class ZoneFieldView : MonoBehaviour
    {
        Transform _root;
        readonly Dictionary<int, GameObject> _visuals = new();

        public void EnsureRoot()
        {
            if (_root != null)
                return;
            var go = new GameObject("ZoneFieldVisuals");
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        /// <summary>
        /// ActiveZones ile birebir senkron: yeni Id → CreateZoneDisk; eksik Id → Destroy.
        /// </summary>
        public void Sync(IReadOnlyList<ZoneInstance> zones)
        {
            EnsureRoot();
            if (zones == null)
            {
                ClearAll();
                return;
            }

            var live = new HashSet<int>();
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneInstance z = zones[i];
                live.Add(z.Id);

                if (!_visuals.TryGetValue(z.Id, out GameObject go) || go == null)
                {
                    string colorKey = ColorKeyForZoneElement(z.Element);
                    // CC alanları daha opak okunur (root/slow).
                    float alpha = string.IsNullOrEmpty(z.CcKind) ? 0.6f : 0.85f;
                    go = PlaceholderFactory.CreateZoneDisk(
                        colorKey,
                        new Vector3(z.X, z.Y, z.Z),
                        z.RadiusM,
                        _root,
                        alpha);
                    go.name = string.IsNullOrEmpty(z.CcKind)
                        ? $"Zone_{z.Id}_{z.Element}"
                        : $"Zone_{z.Id}_{z.Element}_{z.CcKind}";
                    _visuals[z.Id] = go;
                }
                else
                {
                    go.transform.position = new Vector3(z.X, 0.04f, z.Z);
                    float r = Mathf.Max(0.1f, z.RadiusM);
                    go.transform.localScale = new Vector3(r * 2f, 0.06f, r * 2f);
                    if (!go.activeSelf)
                        go.SetActive(true);
                }
            }

            if (_visuals.Count == live.Count)
                return;

            var stale = new List<int>();
            foreach (KeyValuePair<int, GameObject> kv in _visuals)
            {
                if (!live.Contains(kv.Key))
                    stale.Add(kv.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                int id = stale[i];
                if (_visuals.TryGetValue(id, out GameObject doomed) && doomed != null)
                {
                    // Destroy kare sonuna ertelenir; aynı karede kaybolsun diye önce kapat.
                    doomed.SetActive(false);
                    Destroy(doomed);
                }
                _visuals.Remove(id);
            }
        }

        void ClearAll()
        {
            foreach (KeyValuePair<int, GameObject> kv in _visuals)
            {
                if (kv.Value != null)
                    Destroy(kv.Value);
            }
            _visuals.Clear();
        }

        void OnDestroy() => ClearAll();

        /// <summary>
        /// prezentasyon-katmani yalnızca 6 core rengi tutar; zone_layer bileşikleri aile köküne düşer.
        /// </summary>
        static string ColorKeyForZoneElement(string element)
        {
            if (string.IsNullOrEmpty(element))
                return element;

            if (PlaceholderFactory.TryGetElementColor(element, out _))
                return element;

            // Toprak ailesi (Bağlama 7 odak) + diğer bilinen zone elementleri.
            switch (element)
            {
                case "Kaya":
                case "Çamur":
                case "Toz":
                    return "Toprak";
                case "Lav":
                case "Kor":
                    return "Ateş";
                case "Buhar":
                case "Pınar":
                    return "Su";
                case "Girdap":
                    return "Hava";
                case "Katran":
                case "Mühür":
                case "Sis":
                    return "Karanlık";
                default:
                    return element;
            }
        }
    }
}
