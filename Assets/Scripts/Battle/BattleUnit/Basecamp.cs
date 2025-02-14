using FixPointUnity;
using UnityEngine;

public class Basecamp : MonoBehaviour
{
    public Transform battleUnitSpawnPointA;
    public Transform battleUnitSpawnPointB;

    public F64Vec3 GetSpawnPosition(float lerpValue)
    {
        return F64Vec3.Lerp(battleUnitSpawnPointA.transform.position.ToF64Vec3(), battleUnitSpawnPointB.transform.position.ToF64Vec3(), F64.FromFloat(lerpValue));
    }
}
