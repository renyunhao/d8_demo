namespace GameFramework
{
    public interface IPoolImplementation<T>
    {
        T GetInstance();

        void RecycleInstance(T instance);

        void RecycleAllInstance();
    }
}