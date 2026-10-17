using System.Collections;
using System.Collections.Generic;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// SI-3.7 karakter kenar ışığı: oyuncunun (ve silahının) her renderer'ının son materyal slotu RimOverlay;
    /// dost ve boss'ta yok. Gerçek yol: sahne ActorsBuilder'dan kurulur.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float RimEps = 1e-4f;

        [UnityTest]
        public IEnumerator SI_3_7_KenarIsigi_YalnizOyuncudaVeSilahta()
        {
            yield return null;

            Transform visual = _player.Find("Visual");
            Assert.That(visual, Is.Not.Null, "oyuncu Visual çocuğu");
            var renderers = new List<Renderer>();
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                if (r is SkinnedMeshRenderer || r is MeshRenderer)
                    renderers.Add(r);
            Assert.That(renderers.Count, Is.GreaterThan(0), "oyuncu görselinde renderer yok");

            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                Material last = mats[mats.Length - 1];
                Assert.That(last, Is.Not.Null, $"{r.name}: son slot boş");
                Assert.That(last.shader.name, Is.EqualTo(RimLightDefaults.ShaderName), $"{r.name}: son slot kenar ışığı değil");
                Assert.That(last.GetFloat("_RimPower"), Is.EqualTo(3f).Within(RimEps), "SI-3.7 üs 3");
                Assert.That(last.GetFloat("_RimIntensity"), Is.EqualTo(0.35f).Within(RimEps), "SI-3.7 şiddet 0,35");
                Color c = last.GetColor("_RimColor");
                Assert.That(c.r, Is.EqualTo(0xCF / 255f).Within(0.01f), "SI-3.7 renk #CFD8E0 (R)");
                Assert.That(c.g, Is.EqualTo(0xD8 / 255f).Within(0.01f), "SI-3.7 renk #CFD8E0 (G)");
                Assert.That(c.b, Is.EqualTo(0xE0 / 255f).Within(0.01f), "SI-3.7 renk #CFD8E0 (B)");
                Assert.That(mats[0].shader.name, Is.Not.EqualTo(RimLightDefaults.ShaderName), $"{r.name}: gövde slotu bozuldu");
            }

            var others = new List<Transform>(_allies) { _boss };
            foreach (Transform t in others)
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        if (m != null)
                            Assert.That(m.shader.name, Is.Not.EqualTo(RimLightDefaults.ShaderName),
                                $"{t.name}/{r.name}: dost/boss'ta kenar ışığı olmamalı (SI-3.7)");
        }
    }
}
