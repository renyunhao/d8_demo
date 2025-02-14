namespace GameFramework
{
    public class GenericPoolImplementation<T> : IPoolImplementation<T> where T : new()
    {
        private GenericPool<T> pool = new GenericPool<T>();

        public GenericPool<T> Pool => pool;

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