using UnityEngine;

namespace GameFramework
{
    public class RenderTextureAutoSize : MonoBehaviour
    {
        private static bool calculated = false;
        private static Vector3 calculateScale;

        private float basicScale = 1;

        private void Awake()
        {
            if (calculated)
            {
                transform.localScale = calculateScale * basicScale;
            }
            else
            {
                int screenWidth = Screen.width;
                int screenHeight = Screen.height;
                float screenAspect = (float)screenWidth / screenHeight;

                int designWidth = UISystem.DesignWidth;
                int designHeight = UISystem.DesignHeight;
                float designAspect = (float)designWidth / designHeight;

                float widthScale = (float)designWidth / screenWidth;

                if (screenAspect < designAspect)
                {
                    //缩小
                    calculateScale = Vector3.one * widthScale;
                }
                else
                {
                    //放大
                    float finalWidth = designHeight * screenWidth / (float)screenHeight;
                    float scale = finalWidth / designWidth;
                    calculateScale = Vector3.one * scale * widthScale;
                }

                transform.localScale = calculateScale * basicScale;
                calculated = true;
            }
        }

        public void SetBasicScale(float scale)
        {
            basicScale = scale;
            transform.localScale = calculateScale * basicScale;
        }
    }
}