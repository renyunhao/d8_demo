
namespace GameFramework
{
	public class WeightRandomIDCountData : WeightRandomGenericData<IDCount>
	{
		public WeightRandomIDCountData()
		{
		}

		public WeightRandomIDCountData(int id, int weight)
		{
			this.data = new IDCount(id, 1);
			this.weight = weight;
		}

		public WeightRandomIDCountData(int id, int count, int weight)
		{
			this.data = new IDCount(id, count);
			this.weight = weight;
		}
	}
}
