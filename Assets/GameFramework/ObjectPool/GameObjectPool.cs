using UnityEngine;

namespace GameFramework
{
    public class GameObjectPool : GenericPool<GameObject>
    {
        public Transform PoolContainer { get; private set; }
        public Transform SpawnContainer { get; private set; }
        public GameObject Prefab { get; private set; }

        public GameObjectPool(GameObject prefab, Transform transform, Transform spawnTransform = null)
        {
            Prefab = prefab;
            PoolContainer = new GameObject().transform;
            PoolContainer.name = prefab.name;
            SpawnContainer = spawnTransform;
            if (spawnTransform == null)
            {
                SpawnContainer = PoolContainer;
            }

            if (transform != null)
            {
                PoolContainer.SetParent(transform);
                PoolContainer.transform.localPosition = Vector3.zero;
                PoolContainer.transform.localScale = Vector3.one;
                PoolContainer.transform.localRotation = Quaternion.identity;
            }
        }

        protected override GameObject CreateInstance()
        {
            GameObject instance = Object.Instantiate(Prefab);
            instance.name = Prefab.name;
            return instance;
        }

        public override GameObject GetInstance()
        {
            GameObject instance = base.GetInstance();
            instance.transform.SetParent(SpawnContainer, false);
            instance.transform.localScale = Prefab.transform.localScale;
            instance.SetActive(true);
            return instance;
        }

        public GameObject GetInstance(bool active)
        {
            GameObject instance = base.GetInstance();
            instance.transform.SetParent(PoolContainer, false);
            instance.transform.localScale = Vector3.one;
            instance.SetActive(active);
            return instance;
        }

        public override void RecycleInstance(GameObject instance)
        {
            instance.transform.SetParent(PoolContainer, false);
            instance.SetActive(false);
            base.RecycleInstance(instance);
        }

        public override void RecycleAllInstance()
        {
            foreach (var instance in allOutPoolInstance)
            {
                instance.transform.SetParent(PoolContainer, false);
                instance.SetActive(false);
            }
            base.RecycleAllInstance();
        }

        public override void EnsureCapacity(int capacity)
        {
            while (pool.Count < capacity)
            {
                var instance = CreateInstance();
                instance.transform.SetParent(PoolContainer, false);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }
    }
}