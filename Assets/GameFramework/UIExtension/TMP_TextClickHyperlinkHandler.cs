using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFramework
{
    public class TMP_TextClickHyperlinkHandler : MonoBehaviour, IPointerClickHandler
    {
        public delegate bool OnClickLink(string linkText);

        public event OnClickLink Event_OnClickLink;

        private TMP_Text textComponent;

        private Camera canvasCamera;
        private Canvas canvas;

        void Start()
        {
            textComponent = gameObject.GetComponent<TMP_Text>();

            if (textComponent is TextMeshProUGUI)
            {
                canvas = gameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvasCamera = canvas.worldCamera;
                }
            }
            else
            {
                canvasCamera = Camera.main;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, Input.mousePosition, canvasCamera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
                string url = linkInfo.GetLinkID();
                if (Event_OnClickLink != null)
                {
                    Event_OnClickLink?.Invoke(url);
                }
                else
                {
                    Application.OpenURL(url);
                }
            }
        }
    }
}
