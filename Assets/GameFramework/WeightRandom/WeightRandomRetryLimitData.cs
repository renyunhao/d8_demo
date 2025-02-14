namespace GameFramework
{
	public class WeightRandomRetryLimitData : WeightRandomIDCountData
	{
		public int randomCount;

		public int lastRandomIndex;

		public WeightRandomRetryLimitData(int id, int weight)
			: base(id, weight)
		{
		}

		public void RandomSelf(int randomindex)
		{
			randomCount++;
			lastRandomIndex = randomindex;
		}

		public bool GetCanRandom(int randomindex, int maxcount)
		{
			if (lastRandomIndex == randomindex)
			{
				if (randomCount >= maxcount)
				{
					return false;
				}
			}
			else
			{
				randomCount = 0;
			}
			return true;
		}
	}
}
