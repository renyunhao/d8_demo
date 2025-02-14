using System;
using System.Collections.Generic;

namespace GameFramework
{
	public class WeightRandomRetryLimit
	{
		private List<WeightRandomRetryLimitData> list = new List<WeightRandomRetryLimitData>();

		private int allWeight;

		private int maxRetryCount;

		private int ran;

		private int randomIndex;

		public WeightRandomRetryLimit(int maxContinueCount)
		{
			this.maxRetryCount = maxContinueCount;
		}

		public WeightRandomRetryLimit(int maxContinueCount, int maxCount)
		{
			this.maxRetryCount = maxContinueCount;
			for (int i = 0; i < maxCount; i++)
			{
				Add(i, 1);
			}
		}

		public void Add(int id, int weight)
		{
			WeightRandomRetryLimitData weightRandomCountData = new WeightRandomRetryLimitData(id, weight);
			list.Add(weightRandomCountData);
			allWeight += weight;
		}

		public int GetRandom()
		{
			ran = UnityEngine.Random.Range(0, allWeight);
			int i = 0;
			for (int count = list.Count; i < count; i++)
			{
				WeightRandomRetryLimitData weightRandomCountData = list[i];
				if (ran < weightRandomCountData.weight)
				{
					if (weightRandomCountData.GetCanRandom(randomIndex, maxRetryCount))
					{
						weightRandomCountData.RandomSelf(++randomIndex);
						return weightRandomCountData.data.id;
					}
					return GetRandom();
				}
				ran -= weightRandomCountData.weight;
			}
			throw new Exception("WeightRandom.GetRandom Weight Random Error!!!");
		}
	}
}
