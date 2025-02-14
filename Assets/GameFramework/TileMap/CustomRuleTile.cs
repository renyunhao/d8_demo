using System;
using UnityEngine;
using UnityEngine.UI;

public class CustomRuleTile : MonoBehaviour
{
    public virtual Type m_NeighborType { get { return typeof(TilingRule.Neighbor); } }
    [HideInInspector] public TilingRule[] m_TilingRules;
    [HideInInspector] public TiledType m_TiledType;
    private static readonly int NeighborCount = 9;

    public void Refresh(int neighbourIndexExistSameSum, int neighbourIndexMissingSameSum, int neighbourIndexExistAllSum, int neighbourIndexMissingAllSum, bool brightOrDark)
    {
        TilingRule mainRule = null;

        foreach (TilingRule tilingRule in m_TilingRules)
        {
            tilingRule.ApplyRule(neighbourIndexExistSameSum, neighbourIndexMissingSameSum, neighbourIndexExistAllSum, neighbourIndexMissingAllSum);
            if (tilingRule.is_Important)
            {
                if (mainRule == null)
                {
                    mainRule = tilingRule;
                }
                else
                {
                    Debug.LogError("只能有一个MainRule，现在有多个！");
                }
            }
        }
        if (mainRule != null)
        {
            foreach (TilingRule rule in m_TilingRules)
            {
                if (rule.isCheckerboardStyle)
                {
                    rule.ApplyBrightOrDark(brightOrDark, mainRule);
                }
            }
        }
    }

    [Serializable]
    public class TilingRule
    {
        public int[] neighbors;
        public int existValue;
        public string existValueStr;
        public int noExistValue;
        public string noExistValueStr;
        public Image image;
        public Image image_Dark;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer spriteRenderer_Dark;
        public GameObject gameObject;
        public GameObject gameObject_Dark;
        /// <summary>
        /// 是否显示为明暗相间的样式（类似棋盘格）
        /// </summary>
        public bool isCheckerboardStyle = false;
        /// <summary>
        /// 标识是否为主体（用于明暗关系基准参照）
        /// </summary>
        public bool is_Important = false;
        /// <summary>
        /// Tile内部相对主体的棋盘格关系，如果主体为明，相对主体变化，则当前为暗，反之亦然
        /// </summary>
        public bool m_brightOrDarkRelative = true;
        public NeighbourTileType m_NeighbourTileType;
        /// <summary>
        /// 从Image 、 Sprite 、GameObject 中选择一种
        /// </summary>
        public TiledType m_TiledType;
        public bool hasDarkSprite = false;

        public TilingRule()
        {
            neighbors = new int[NeighborCount];
            image = null;
            spriteRenderer = null;
            gameObject = null;

            for (int i = 0; i < neighbors.Length; i++)
                neighbors[i] = Neighbor.DontCare;
        }

        public class Neighbor
        {
            public const int DontCare = 0;
            public const int This = 1;
            public const int NotThis = 2;
        }

        public enum NeighbourTileType
        {
            SameTile,
            AllTile
        }

        public int GetSelfValue()
        {
            int[] arrayValue = new int[9];
            int[] noArrayValue = new int[9];
            //int value = 0;


            //1代表存在
            //2代表不存在
            for (int i = 0; i < 9; i++)
            {
                if (neighbors[i] == 1)
                {
                    if (i == 0)
                    {
                        //value += 128;
                        arrayValue[7] = 1;
                    }
                    else if (i == 1)
                    {
                        //value += 8;
                        arrayValue[3] = 1;
                    }
                    else if (i == 2)
                    {
                        //value += 64;
                        arrayValue[6] = 1;
                    }
                    else if (i == 3)
                    {
                        //value += 16;
                        arrayValue[4] = 1;
                    }
                    else if (i == 4)
                    {
                        //value += 1;
                        arrayValue[0] = 1;
                    }
                    else if (i == 5)
                    {
                        //value += 4;
                        arrayValue[2] = 1;
                    }
                    else if (i == 6)
                    {
                        //value += 256;
                        arrayValue[8] = 1;
                    }
                    else if (i == 7)
                    {
                        //value += 2;
                        arrayValue[1] = 1;
                    }
                    else if (i == 8)
                    {
                        //value += 32;
                        arrayValue[5] = 1;
                    }
                }

                if (neighbors[i] == 2)
                {
                    if (i == 0)
                    {
                        noArrayValue[7] = 1;
                    }
                    else if (i == 1)
                    {
                        noArrayValue[3] = 1;
                    }
                    else if (i == 2)
                    {
                        //value += 64;
                        noArrayValue[6] = 1;
                    }
                    else if (i == 3)
                    {
                        //value += 16;
                        noArrayValue[4] = 1;
                    }
                    else if (i == 4)
                    {
                        //value += 1;
                        noArrayValue[0] = 1;
                    }
                    else if (i == 5)
                    {
                        //value += 4;
                        noArrayValue[2] = 1;
                    }
                    else if (i == 6)
                    {
                        //value += 256;
                        noArrayValue[8] = 1;
                    }
                    else if (i == 7)
                    {
                        //value += 2;
                        noArrayValue[1] = 1;
                    }
                    else if (i == 8)
                    {
                        //value += 32;
                        noArrayValue[5] = 1;
                    }
                }
            }
            existValue = 0;
            existValueStr = "";
            for (int i = 0; i < arrayValue.Length; i++)
            {
                if (arrayValue[i] == 1)
                {
                    existValue += (1 << i);
                    existValueStr += $"{(1 << i)}(+)&";
                }
            }
            if (existValueStr.Length > 1)
            {
                existValueStr = existValueStr.Substring(0, existValueStr.Length - 1);
            }
            noExistValue = 0;
            noExistValueStr = "";
            for (int i = 0; i < noArrayValue.Length; i++)
            {
                if (noArrayValue[i] == 1)
                {
                    noExistValue += (1 << i);
                    noExistValueStr += $"{(1 << i)}(-)&";
                }
            }
            if (noExistValueStr.Length > 1)
            {
                noExistValueStr = noExistValueStr.Substring(0, noExistValueStr.Length - 1);
            }
            return 0;
        }

        public void ApplyRule(int neighbourIndexExistSameSum, int neighbourIndexMissingSameSum, int neighbourIndexExistAllSum, int neighbourIndexMissingAllSum)
        {
            bool existValid;
            bool missingValid;
            if (m_NeighbourTileType == NeighbourTileType.SameTile)
            {
                existValid = (existValue == (neighbourIndexExistSameSum & existValue));
                missingValid = (noExistValue == (neighbourIndexMissingSameSum & noExistValue));
            }
            else
            {
                existValid = (existValue == (neighbourIndexExistAllSum & existValue));
                missingValid = (noExistValue == (neighbourIndexMissingAllSum & noExistValue));
            }

            bool visible = (existValid && missingValid);
            SetObjectVisible(image, visible);
            SetObjectVisible(gameObject, visible);
            SetObjectVisible(spriteRenderer, visible);
            if (isCheckerboardStyle)
            {
                SetObjectVisible(image_Dark, visible);
                SetObjectVisible(gameObject_Dark, visible);
                SetObjectVisible(spriteRenderer_Dark, visible);
            }
        }

        private void SetObjectVisible<T>(T obj, bool visible) where T : Component
        {

            if (obj != null && obj.gameObject != null)
            {
                obj.gameObject.SetActive(visible);
            }
        }

        private void SetObjectVisible(GameObject obj, bool visible)
        {

            if (obj != null && obj.gameObject != null)
            {
                obj.gameObject.SetActive(visible);
            }
        }

        public void ApplyBrightOrDark(bool brightOrDark, TilingRule mainRule)
        {
            bool isBright = brightOrDark && m_brightOrDarkRelative == mainRule.m_brightOrDarkRelative ||
                !brightOrDark && m_brightOrDarkRelative != mainRule.m_brightOrDarkRelative;
            if (image != null && image.gameObject.activeSelf)
            {
                SetObjectVisible(image, isBright);
                SetObjectVisible(image_Dark, !isBright);
            }
            if (gameObject != null && gameObject.gameObject.activeSelf)
            {
                SetObjectVisible(gameObject, isBright);
                SetObjectVisible(gameObject_Dark, !isBright);
            }
            if (spriteRenderer != null && spriteRenderer.gameObject.activeSelf)
            {
                SetObjectVisible(spriteRenderer, isBright);
                SetObjectVisible(spriteRenderer_Dark, !isBright);
            }
        }
    }
    [Serializable]
    public enum TiledType
    {
        GameObject,
        Image,
        SpriteRenderer,
    }
}

