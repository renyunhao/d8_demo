namespace GameFramework
{
    public class WeightRandomRange : WeightRandom<WeightRandomRangeData>
    {
        public WeightRandomRange(int capacity) : base(capacity)
        {
        }

        public void Add(int min, int max, int weight)
        {
            this.Add(new WeightRandomRangeData(min, max, weight));
        }
    }
}
