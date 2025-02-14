using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Scrollbar;

namespace GameFramework
{
    /// <summary>
    /// 将本脚本挂到Dropdown组件所在的GameObject上可在打开下拉列表时自动定位到选择项处
    /// </summary>
    public class AutoScrollDropdown : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Dropdown target = null!;
        private ScrollRect scrollRect;

        private void Awake()
        {
            target = this.GetComponent<TMP_Dropdown>();
        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (target == null)
            {
                return;
            }
            if (pointerEventData.pointerPress != pointerEventData.pointerClick)
            {
                //这是为了避免点击Dropdown展开的列表以及滚动条也触发自动定位导致的回滚现象
                return;
            }
            StartCoroutine(DelayScroll());
        }

        private IEnumerator DelayScroll()
        {
            yield return new WaitForEndOfFrame();
            scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
            {
                if (target.options.Count > 1 && scrollRect != null)
                {
                    var valuePosition = (float)target.value / target.options.Count;
                    var value = scrollRect.verticalScrollbar.direction == Direction.TopToBottom ? valuePosition : 1f - valuePosition;
                    scrollRect.verticalNormalizedPosition = value;
                    scrollRect.verticalScrollbar.value = value;
                    scrollRect.content.anchoredPosition = new Vector2(0, scrollRect.content.sizeDelta.y * (1 - value));
                }
            }
        }
    }
}