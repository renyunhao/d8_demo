using FixPointUnity;
using UnityEngine;

public static class BattleTool
{
    public static Vector3 ToVector3(this F64Vec3 fixedVec)
    {
        return new Vector3(fixedVec.X.Float, fixedVec.Y.Float, fixedVec.Z.Float);
    }

    public static F64Vec3 ToF64Vec3(this Vector3 vector)
    {
        return F64Vec3.FromFloat(vector.x, vector.y, vector.z);
    }
}
