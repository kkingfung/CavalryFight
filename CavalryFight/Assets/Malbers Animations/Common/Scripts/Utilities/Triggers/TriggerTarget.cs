using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Utilities
{
    public class TriggerTarget : MonoBehaviour
    {
        private HashSet<TriggerProxy> _proxies = new();

        //   public Collider CachedCollider => _cachedCollider;
        public HashSet<TriggerProxy> Proxies => _proxies;

        private Collider _cachedCollider;

        private void Awake()
        {
            hideFlags = HideFlags.HideInInspector;
            _cachedCollider = GetComponent<Collider>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_cachedCollider)
            {
                Debug.LogWarning($"TriggerTarget on {gameObject.name} has no attached collider!", this);
            }
#endif
        }

        private void OnEnable()
        {
            TriggerRegistry.RegisterTarget(this);

            if (_cachedCollider)
                TriggerRegistry.RegisterCollider(_cachedCollider, this);
        }

        private void OnDisable()
        {
            if (_proxies != null)
            {
                foreach (var p in _proxies)
                {
                    if (p) p.TriggerExit(_cachedCollider, false);
                }
                _proxies.Clear();
            }


            TriggerRegistry.UnregisterTarget(this);

            if (_cachedCollider) TriggerRegistry.UnregisterCollider(_cachedCollider);
        }


        public void AddProxy(TriggerProxy trigger) => _proxies.Add(trigger);

        public void RemoveProxy(TriggerProxy trigger) => _proxies.Remove(trigger);
    }
}