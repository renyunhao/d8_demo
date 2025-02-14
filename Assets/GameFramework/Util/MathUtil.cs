public static class MathUtil
{
    /// <summary>
    /// 在已知高度与时间的情况下，反推抛物线所需要的重力加速度与垂直方向的初始速度
    /// </summary>
    /// <param name="height">抛物线的高度</param>
    /// <param name="time">抛物线从起抛到回落的时间</param>
    /// <returns></returns>
    public static (float gravity, float verticleVelocity) CalculateParabolaParam(float height, float time)
    {
        //这里的公式是经过简化的，推理过程如下：
        //设v为垂直方向初速度，g为重力加速度
        //当到达抛物线最高点处时 v * t = 1/2 * g * t * t = height 且 v = 1/2 * g * t 且 t = time / 2
        //因此，将v，t的值代入后，得到g，进而可得到v
        float gravity = 8 * height / time / time;
        float verticleVelocity = 0.5f * gravity * time;
        return (gravity, verticleVelocity);
    }
}
