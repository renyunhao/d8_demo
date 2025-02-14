using System;
using UnityEngine;

/// <summary>
/// 数据生产线自定义类型：范围
/// </summary>
[Serializable]
public class Range
{
    /// <summary>
    /// 最小值
    /// </summary>
    public float min;

    /// <summary>
    /// 最大值
    /// </summary>
    public float max;

    public float GetValue()
    {
        if (IsInteger(min) && IsInteger(max))
            return GetIntValue();
        return GetFloatValue();
    }

    public int GetIntValue()
    {
        return UnityEngine.Random.Range((int)min, (int)max + 1);
    }

    public float GetFloatValue()
    {
        return UnityEngine.Random.Range(min, max);
    }

    public static bool IsInteger(float value)
    {
        return Math.Abs(value - Math.Floor(value)) < 0.0001;
    }

    public static Range operator -(Range range) => new() { max = -range.min, min = -range.max };
}