namespace GameFramework
{
	public class WeightRandomGenericData<T> : WeightRandomData
	{
		public T data;

		public WeightRandomGenericData()
		{
		}

		public WeightRandomGenericData(T data, int weight)
		{
			this.data = data;
			this.weight = weight;
		}
	}
}
