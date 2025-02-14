using UnityEngine;

[CreateAssetMenu(fileName = "AnimationCurveAsset", menuName = "ScriptableObjects/AnimationCurveAsset", order = 1)]
public class AnimationCurveAsset : ScriptableObject
{
    public AnimationCurve animationCurve;
}
