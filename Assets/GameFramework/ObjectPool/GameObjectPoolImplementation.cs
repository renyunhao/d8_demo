using UnityEngine;

namespace GameFramework
{
    public class GameObjectPoolImplementation : IPoolImplementation<GameObject>
    {
        private GameObjectPool pool;
        public GameObjectPool Pool => pool;

        public GameObjectPoolImplementation(GameObject prefab, Transform poolTransform, Transform spawnTransform = null)
        {
            pool = new GameObjectPool(prefab, poolTransform, spawnTransform);
        }

        public GameObject GetInstance()
        {
            return pool.GetInstance();
        }

        public void RecycleInstance(GameObject instance)
        {
            pool.RecycleInstance(instance);
        }

        public void RecycleAllInstance()
        {
            pool.RecycleAllInstance();
        }
    }
}