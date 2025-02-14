using TMPro;
using UnityEngine;

namespace GameFramework
{
    public static class TMP_TextExtension
    {
        public static void SetPreferredWidth(this TMP_Text textComponent)
        {
            float width = textComponent.preferredWidth;
            var rt = textComponent.transform as RectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        public static void SetPreferredHeight(this TMP_Text textComponent)
        {
            float height = textComponent.preferredHeight;
            var rt = textComponent.transform as RectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}