
namespace GameFramework
{
    public class WeightRandomRangeData : WeightRandomGenericData<RangeRandom>
    {
        public WeightRandomRangeData()
        {
        }

        public WeightRandomRangeData(int min, int max, int weight)
        {
            this.data = new RangeRandom();
            this.data.min = min;
            this.data.max = max;
            this.weight = weight;
        }
    }
}
