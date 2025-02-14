using GameFramework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public class UIGray : MonoBehaviour
    {
        private static Material grayMaterial;

        public List<MaskableGraphic> needGrayList;

        private List<Image> imageList = new List<Image>();
        private List<TMP_Text> textMeshProList = new List<TMP_Text>();
        private List<Color> textOrginColor = new List<Color>();
        private List<VertexGradient> textGradientColor = new List<VertexGradient>();

        public void Awake()
        {
            foreach (var p in needGrayList)
            {
                var image = p.GetComponent<Image>();
                if (image != null)
                {
                    imageList.Add(image);
                    continue;
                }

                var text = p.GetComponent<TMP_Text>();
                if (text != null)
                {
                    textMeshProList.Add(text);
                    textOrginColor.Add(text.color);
                    textGradientColor.Add(text.colorGradient);
                }
            }
        }

        public void SetGray(bool isGray)
        {
            if (isGray == false)
            {
                foreach (var p in imageList)
                {
                    p.material = null;
                }
                for (var i = 0; i < textMeshProList.Count; i++)
                {
                    var text = textMeshProList[i];
                    if (text.enableVertexGradient)
                    {
                        text.colorGradient = textGradientColor[i];
                    }
                    else
                    {
                        text.color = textOrginColor[i];
                    }
                }
            }
            else
            {
                foreach (var p in imageList)
                {
                    p.material = GrayMaterial;
                }
                for (var i = 0; i < textMeshProList.Count; i++)
                {
                    var text = textMeshProList[i];
                    if (text.enableVertexGradient)
                    {
                        text.colorGradient = textGradientColor[i].ToGray();
                    }
                    else
                    {
                        text.color = textOrginColor[i].ToGray();
                    }
                }
            }
        }

        public static Material GrayMaterial
        {
            get
            {
                if (grayMaterial == null)
                {
                    grayMaterial = GameFramework.AssetSystem.Load<Material>("UIGray");
                }
                return grayMaterial;
            }
        }
    }
}