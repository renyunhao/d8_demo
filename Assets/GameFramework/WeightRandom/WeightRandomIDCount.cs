namespace GameFramework
{
    public class WeightRandomIDCount : WeightRandom<WeightRandomIDCountData>
    {
        public WeightRandomIDCount(int capacity) : base(capacity)
        {
        }

        public int GetID()
        {
            var result = GetResult();
            return result.data.id;
        }

        public void Add(int id, int weight)
        {
            this.Add(new WeightRandomIDCountData(id, 1, weight));
        }

        public void Add(int id, int count, int weight)
        {
            this.Add(new WeightRandomIDCountData(id, count, weight));
        }
    }
}
