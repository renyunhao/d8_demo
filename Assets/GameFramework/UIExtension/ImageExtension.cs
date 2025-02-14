using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public static class ImageExtension
    {
        /// <summary>
        /// 将原图修改为指定大小，如果原图比制定大小还小，那么SetNativeSize()保持原图大小
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sprite"></param>
        /// <param name="size"></param>
        public static void SetSpriteWithSize(this Image image, Sprite sprite, Vector2 size)
        {
            if (sprite == null)
            {
                return;
            }

            image.preserveAspect = true;
            image.sprite = sprite;
            image.rectTransform.anchorMin = Vector2.one * 0.5f;
            image.rectTransform.anchorMax = Vector2.one * 0.5f;
            if (sprite.rect.width < size.x && sprite.rect.height < size.y)
            {
                image.SetNativeSize();
            }
            else
            {
                image.rectTransform.sizeDelta = size;
            }
        }
    }
}