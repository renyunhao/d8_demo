using UnityEngine;

namespace GameFramework
{
    public class MonoBehaviourPoolImplementation<T> : IPoolImplementation<T> where T : MonoBehaviour, new()
    {
        private MonoBehaviourPool<T> pool;
        public MonoBehaviourPool<T> Pool => pool;

        public MonoBehaviourPoolImplementation(T prefab, Transform poolTransform, Transform spawnTransform = null)
        {
            pool = new MonoBehaviourPool<T>(prefab, poolTransform, spawnTransform);
        }

        public T GetInstance()
        {
            return pool.GetInstance();
        }

        public void RecycleInstance(T instance)
        {
            pool.RecycleInstance(instance);
        }

        public void RecycleAllInstance()
        {
            pool.RecycleAllInstance();
        }
    }
}