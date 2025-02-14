using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class RandomUtil
    {
        static SimpleRandom simpleRandom = new SimpleRandom((uint)System.DateTime.Now.Ticks);
        /// <summary>
        /// 在指定的整型范围内进行一次roll点动作，如果在目标概率内则返回true，否则返回false
        /// </summary>
        /// <param name="benginVal"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static bool Roll(int targetProb, int rangeBegin = 1, int rangeEnd = 100)
        {
            if (targetProb > rangeEnd)
            {
                Debug.LogErrorFormat("目标概率{0}超出范围{1}~{2}", targetProb, rangeBegin, rangeEnd);
                return false;
            }

            int random = Random.Range(rangeBegin, rangeEnd + 1);
            Debug.Log($"目标概率：{targetProb},随机范围[{rangeBegin},{rangeEnd}],命中：{random}");
            return random < targetProb ? true : false;
        }

        /// <summary>
        /// 从指定范围中随机指定个数，且不重复，
        /// </summary>
        /// rangeBegin和rangeEnd之间必须是连续的范围
        /// <param name="targetCnt">目标个数，必须小于rangeEnd-rangeBegin</param>
        public static List<int> GetRandomInRange(int rangeBegin, int rangeEnd, int targetCnt)
        {
            if ((rangeEnd - rangeBegin) + 1 < targetCnt)
            {
                Debug.LogError($"targetCnt不能被满足 Range:[{rangeBegin},{rangeEnd}]");
                return null;
            }
            if (rangeEnd <= rangeBegin)
            {
                Debug.LogError($"rangeEnd必须大于rangeBegin");
                return null;
            }

            //构建待随机列表
            List<int> waitingSelectList = new List<int>();
            for (int i = rangeBegin; i <= rangeEnd; i++)
            {
                waitingSelectList.Add(i);
            }

            //随机到指定数量的结果
            List<int> randomList = new List<int>();
            while (randomList.Count < targetCnt)
            {
                var hitIdx = Random.Range(0, waitingSelectList.Count);
                randomList.Add(waitingSelectList[hitIdx]);
                waitingSelectList.RemoveAt(hitIdx);
            }

            return randomList;
        }

        /// <summary>
        /// 轮盘赌算法
        /// </summary>
        /// <param name="weightArr">所有个体的权重值</param>
        /// <returns>返回被选中个体索引值</returns>
        /// 参考：https://www.cnblogs.com/gaosheng12138/p/7534956.html
        public static int RouletteAlogrithm(int[] weightArr)
        {
            //个体概率(个体权重/总权重)

            //总权重
            int sumWeight = 0;
            for (int i = 0; i < weightArr.Length; i++)
            {
                sumWeight += weightArr[i];
            }

            //生成一个0~1的随机数
            int random = simpleRandom.Next(0, sumWeight);
            //命中索引值
            int hitIndex = 0;
            //累计概率
            int accumulate = 0;

            //判断随机数落在累计概率的哪个区间
            for (int k = 0; k < weightArr.Length; k++)
            {
                accumulate += weightArr[k];

                if (random <= accumulate)
                {
                    hitIndex = k;
                    break;
                }
            }

            return hitIndex;
        }


        /// <summary>
        /// 生成正态分布随机数的方法
        /// </summary>
        /// <param name="a">左区间</param>
        /// <param name="b">右区间</param>
        /// <returns></returns>
        public static float GenerateNormalRandom(float a, float b)
        {
            var mean = (a + b) / 2;
            var stdDev = (b - a) / 4;

            // 使用Box-Muller变换生成标准正态分布随机数
            var u1 = Random.Range(0f, 1f);
            var u2 = Random.Range(0f, 1f);
            var z0 = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);

            // 转换为指定均值和标准差的正态分布
            return mean + z0 * stdDev;
        }
    }
}