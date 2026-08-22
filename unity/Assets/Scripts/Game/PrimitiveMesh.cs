using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Yerleşik primitive mesh'lerini tek yerden verir; kimse `GameObject.CreatePrimitive`
    /// çağırmadığı için collider hiç oluşmaz (teknoloji-kararlari §4: fizik dışarıda).
    ///
    /// Adlar tuzaklı: `Resources.GetBuiltinResource` iki ayrı set tutuyor. "New-" öneksiz
    /// olanlar (`Plane.fbx`, `Capsule.fbx`, `Sphere.fbx`) Maya kaynaklı eski mesh'ler ve
    /// ölçüleri farklı — Plane 10×10 değil 1×1, Capsule 1×2×1 değil 2×4×2, Sphere 1 çap değil 2.
    /// `CreatePrimitive`'in kullandıkları "New-" önekli olanlar. Bu ayrım T7.2/T7.3'te arenayı
    /// on kat küçültüp aktörleri iki katına çıkarmıştı; ölçüler docs/durum.md T7.4'te.
    /// </summary>
    public static class PrimitiveMesh
    {
        static readonly Dictionary<PrimitiveType, Mesh> Cache = new();

        public static Mesh Get(PrimitiveType type)
        {
            if (Cache.TryGetValue(type, out Mesh cached) && cached != null)
                return cached;

            Mesh mesh = Resources.GetBuiltinResource<Mesh>(BuiltinName(type))
                        ?? FromTemporaryPrimitive(type);

            Cache[type] = mesh;
            return mesh;
        }

        static string BuiltinName(PrimitiveType type) => type switch
        {
            PrimitiveType.Plane => "New-Plane.fbx",
            PrimitiveType.Sphere => "New-Sphere.fbx",
            PrimitiveType.Capsule => "New-Capsule.fbx",
            PrimitiveType.Cylinder => "New-Cylinder.fbx",
            PrimitiveType.Quad => "Quad.fbx",
            _ => "Cube.fbx"
        };

        /// <summary>
        /// Ad Unity sürümüyle değişirse sessiz `null` dönmesin: görünmez zemin/aktör, sebebi
        /// bulunması en zor hata türü. Mesh'i primitive'in kendisinden al, geçici nesneyi aynı
        /// karede yok et — collider dünyaya hiç karışmaz.
        /// </summary>
        static Mesh FromTemporaryPrimitive(PrimitiveType type)
        {
            Debug.LogWarning(
                $"Yerleşik mesh bulunamadı ({BuiltinName(type)}); {type} geçici primitive'den alınıyor.");

            var temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
