using System;
using UnityEngine.Serialization;

[Serializable]
public class AttributeValue
{
    /// <summary>
    /// 物品ID
    /// </summary>
    [FormerlySerializedAs("id")] public int ID;
    /// <summary>
    /// 权重
    /// </summary>
    public float attribute;

    public AttributeValue()
    {

    }

    public AttributeValue(int id, float _attribute)
    {
        this.ID = id;
        this.attribute = _attribute;
    }
}
