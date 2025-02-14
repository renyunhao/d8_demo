using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFramework
{
    public class ClickCloseUI : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            UISystem.Hide(this.gameObject.GetComponentInParent<BaseUI>());
        }
    }
}
