using UnityEngine;

namespace GameFramework
{
    public class MonoBehaviourPool<T> : GenericPool<T> where T : MonoBehaviour, new()
    {
        public Transform PoolContainer { get; private set; }
        public Transform SpawnContainer { get; private set; }
        public T Prefab { get; private set; }

        public MonoBehaviourPool(T prefab, Transform poolTransform, Transform spawnTransform = null)
        {
            Prefab = prefab;
            PoolContainer = new GameObject().transform;
            PoolContainer.name = prefab.name;
            SpawnContainer = spawnTransform;
            if (spawnTransform == null)
            {
                SpawnContainer = PoolContainer;
            }

            if (poolTransform != null)
            {
                PoolContainer.SetParent(poolTransform);
                PoolContainer.transform.localPosition = Vector3.zero;
                PoolContainer.transform.localScale = Vector3.one;
                PoolContainer.transform.localRotation = Quaternion.identity;
            }
        }

        protected override T CreateInstance()
        {
            Object go = Object.Instantiate(Prefab);
            go.name = Prefab.name;
            return go as T;
        }

        public override T GetInstance()
        {
            T t = base.GetInstance();
            t.transform.SetParent(SpawnContainer, false);
            t.gameObject.SetActive(true);
            return t;
        }

        public override void RecycleInstance(T instance)
        {
            instance.transform.SetParent(PoolContainer, false);
            instance.gameObject.SetActive(false);
            base.RecycleInstance(instance);
        }

        public override void RecycleAllInstance()
        {
            foreach (var item in allOutPoolInstance)
            {
                item.transform.SetParent(PoolContainer, false);
                item.gameObject.SetActive(false);
            }
            base.RecycleAllInstance();
        }

        public override void EnsureCapacity(int capacity)
        {
            while (pool.Count < capacity)
            {
                var instance = CreateInstance();
                instance.transform.SetParent(PoolContainer, false);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
        }
    }
}