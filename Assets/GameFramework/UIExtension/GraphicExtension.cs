using UnityEngine.UI;
namespace GameFramework
{
    public static class GraphicExtension
    {
        public static T SetAlpha<T>(this T selfGraphic, float alpha) where T : Graphic
        {
            var color = selfGraphic.color;
            color.a = alpha;
            selfGraphic.color = color;
            return selfGraphic;
        }
    }
}