using MalbersAnimations.Scriptables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MalbersAnimations
{
    [System.Serializable]
    [UnityEngine.CreateAssetMenu(menuName = "Malbers Animations/Effects/Surface Effect", fileName = "New Surface Effect", order = -1000)]
    public class SurfaceEffects : ScriptableObject
    {
        [Tooltip("Custom Hit Effects if the Damageable has a Surface ID. Default [0]")]
        public List<EffectType> surfaceEffects = new();

        public EffectType Get(SurfaceID id)
        {
            if (surfaceEffects.Count == 0) return null;

            foreach (var effect in surfaceEffects)
            {
                if (effect.surface == id) return effect;
            }
            return surfaceEffects[0]; // Return [0] if no matching SurfaceID is found
        }
    }

    [System.Serializable]
    public class EffectType
    {
        public SurfaceID surface;
        public GameObjectReference effect;

        public ObjectPool<GameObject> Pool { get; set; }

        private GameObject _PoolHolder;

        public GameObject Play(Vector3 position, Quaternion rotation, Transform parent = null, int poolSize = 10, int MaxPoolSize = 10)
        {
            if (effect.Value == null) return null;


            if (_PoolHolder == null)
            {
                _PoolHolder = new GameObject($"Effect Pool - [{effect.Value.name}]");
                _PoolHolder.transform.SetPositionAndRotation(position, rotation);
            }

            if (Pool == null)
            {
                Pool = new ObjectPool<GameObject>
                     (CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, false,
                     poolSize, MaxPoolSize);
            }

            var Effect = Pool.Get(); // Get an item from the pool

            Effect.transform.SetPositionAndRotation(position, rotation); // Set the position and rotation of the effect
            Effect.transform.SetParent(parent, true); // Set the parent of the effect if provided

            return Effect;
        }

        // If the pool capacity is reached then any items returned will be destroyed.
        // We can control what the destroy behavior does, here we destroy the GameObject.
        private void OnDestroyPoolObject(GameObject Go)
        {
            //GameObject.Destroy(Go); // When pool size excides max size destroy projectile
        }

        private void OnReturnedToPool(GameObject Go)
        {
            Go.transform.SetParent(_PoolHolder.transform, false); // or to Weapon
            Go.transform.ResetLocal();
            Go.SetActive(false); // Disable
        }

        private void OnTakeFromPool(GameObject Go)
        {
            Go.transform.SetParent(null, true); // Unparent projectile
            Go.SetActive(true); // Enable 
        }

        private GameObject CreatePooledItem()
        {
            var go = GameObject.Instantiate(effect.Value);
            go.name = go.name.Replace("(Clone)", "(Pool)"); // Remove (Clone) from the name
            return go;
        }

        /// <summary> Release the effect back to the pool  </summary>
        internal void Release(GameObject effect)
        {
            Pool.Release(effect);
        }
    }
}

