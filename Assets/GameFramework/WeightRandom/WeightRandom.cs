using System;
using System.Collections.Generic;

namespace GameFramework
{
    public class WeightRandom<T> where T : WeightRandomData, new()
    {
        private List<T> dataList;
        private int weightSum;

        public int WeightSum => weightSum;
        public List<T> DataList => dataList;

        public float prob = 1;
        public float Prob => prob;

        public WeightRandom(int capacity)
        {
            dataList = new List<T>(capacity);
        }

        public void Add(T t)
        {
            t.index = dataList.Count;
            dataList.Add(t);
            weightSum += t.weight;
            foreach (var data in dataList)
            {
                data.Prob = data.weight / (float)weightSum;
            }
        }

        public void Add(int weight)
        {
            var data = new T();
            data.weight = weight;
            Add(data);
        }

        public void SetProb(float prob)
        {
            this.prob = prob;
        }

        /// <summary>
        /// 进行整体的成功概率的随机，随机成功才可以进行下一步获取随机结果
        /// </summary>
        /// <returns></returns>
        public bool DoProbCheck()
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            return randomValue <= this.prob;
        }

        public T GetResult()
        {
            int randomValue = UnityEngine.Random.Range(0, weightSum);
            foreach (var data in dataList)
            {
                if (randomValue < data.weight)
                {
                    return data;
                }
                randomValue -= data.weight;
            }
            throw new Exception(string.Format("WeightRandom<{0}>.GetRandom Weight Random Error!!!", GetType().ToString()));
        }

        public T GetResult(int weightThreshold)
        {
            int randomValue = weightThreshold;
            foreach (var data in dataList)
            {
                if (randomValue < data.weight)
                {
                    return data;
                }
                randomValue -= data.weight;
            }
            throw new Exception(string.Format("WeightRandom<{0}>.GetRandom Weight Random Error!!!", GetType().ToString()));
        }
    }
}
