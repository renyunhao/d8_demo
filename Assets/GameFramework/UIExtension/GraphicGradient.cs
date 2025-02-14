using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public class GraphicGradient : BaseMeshEffect
    {
        public Color32 startColor = Color.black;
        public Color32 endColor = Color.white;
        public Direction direction = Direction.TopToBottom;

        private readonly UIVertex[] _tempVerts = new UIVertex[4];
        public override void ModifyMesh(VertexHelper vh)
        {
            if (vh.currentVertCount <= 0) return;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                int tempVertsIndex = i & 3;
                vh.PopulateUIVertex(ref _tempVerts[tempVertsIndex], i);
                _tempVerts[tempVertsIndex].color =
                    direction switch
                    {
                        Direction.TopToBottom => (tempVertsIndex % 3 != 0 ? startColor : endColor),
                        Direction.BottomToTop => (tempVertsIndex % 3 == 0 ? startColor : endColor),
                        Direction.LeftToRight => (tempVertsIndex < 2 ? startColor : endColor),
                        Direction.RightToLeft => (tempVertsIndex >= 2 ? startColor : endColor),
                        _ => endColor
                    };
                vh.SetUIVertex(_tempVerts[tempVertsIndex], i);
            }
        }

        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }
    }
}