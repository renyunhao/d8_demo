namespace GameFramework
{
	public abstract class WeightRandomData
	{
		public int weight;
		public int index;

        public float Prob { get; set; }

		public WeightRandomData()
		{
		}

		public WeightRandomData(int weight)
		{
			this.weight = weight;
		}
	}
}
