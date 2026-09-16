using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Play mode doğrulama probu — Start'ta Ateş trail + impact placeholder üretir.
    /// Bootstrap'e bağlı değil; MCP / elle sahneye eklenir.
    /// </summary>
    public sealed class PlaceholderFactoryProbe : MonoBehaviour
    {
        [SerializeField] string _element = "Ateş";
        [SerializeField] string _trailStyle = "straight";
        [SerializeField] string _impactStyle = "strike";

        public GameObject LastTrail { get; private set; }
        public GameObject LastImpact { get; private set; }
        bool _spawned;

        void Start() => Spawn();

        public void Spawn()
        {
            if (_spawned)
                return;
            _spawned = true;

            Vector3 origin = transform.position + Vector3.up * 1.2f;
            LastTrail = PlaceholderFactory.CreateTrail(
                _trailStyle,
                _element,
                origin,
                origin + Vector3.forward * 2f,
                transform);
            LastImpact = PlaceholderFactory.CreateImpact(
                _impactStyle,
                _element,
                origin + Vector3.right * 1.2f,
                transform);
        }
    }
}
