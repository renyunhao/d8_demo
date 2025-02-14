using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public class UICollapseToggleAnimation : MonoBehaviour
    {
        public Button collapseToggleButton;
        public RectTransform bg;
        public Vector2 bgCollapsedSize;
        public GameObject collapseIcon;
        public GameObject expandIcon;
        public Transform[] animationItems;
        public float animationDelta = 0.07f;
        public GameObject[] activeToggleItems;

        private Vector2 bgExpandSize;
        private CanvasGroup[] collapseContents;
        private Vector3[] collapseContentsPos;
        private bool isCollapsed = false;

        private void Start()
        {
            collapseToggleButton.onClick.AddListener(OnExpandButtonClick);

            if (bg != null)
            {
                bgExpandSize = bg.sizeDelta;
            }

            collapseContentsPos = new Vector3[animationItems.Length];
            collapseContents = new CanvasGroup[animationItems.Length];

            for (int i = 0; i < animationItems.Length; i++)
            {
                if (i > 0)
                {
                    var cg = animationItems[i].gameObject.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        collapseContents[i] = animationItems[i].gameObject.AddComponent<CanvasGroup>();
                    }
                    else
                    {
                        collapseContents[i] = cg;
                    }
                }
                collapseContentsPos[i] = animationItems[i].localPosition;
            }
        }

        private void OnExpandButtonClick()
        {
            for (int i = 1; i < collapseContents.Length; i++)
            {
                if (DOTween.IsTweening(collapseContents[i]))
                {
                    return;
                }
                if (bg != null)
                {
                    if (DOTween.IsTweening(bg))
                    {
                        return;
                    }
                }
            }
            PlayExpandAnimation();
        }

        private void PlayExpandAnimation()
        {
            int startIndex = isCollapsed ? 1 : collapseContents.Length - 1;
            int endIndex = collapseContents.Length - 1 - startIndex + 1;
            int delta = isCollapsed ? 1 : -1;
            int alpha = isCollapsed ? 1 : 0;
            if (bg != null)
            {
                bg.DOSizeDelta(isCollapsed ? bgExpandSize : bgCollapsedSize, animationDelta * animationItems.Length).SetEase(Ease.Linear);
            }

            foreach (var item in activeToggleItems)
            {
                item.SetActive(isCollapsed);
            }

            for (int i = startIndex; (endIndex - i) * delta >= 0; i += delta)
            {
                int delayIndex = Mathf.Abs(i - startIndex);
                int posStartIndex = isCollapsed ? i - 1 : i;
                int posEndIndex = isCollapsed ? i : i - 1;
                collapseContents[i].alpha = 1 - alpha;
                collapseContents[i].transform.localPosition = collapseContentsPos[posStartIndex];
                collapseContents[i].transform.DOLocalMove(collapseContentsPos[posEndIndex], animationDelta).SetDelay(delayIndex * animationDelta).SetEase(Ease.Linear);
                collapseContents[i].interactable = (i == 0 || isCollapsed);
                collapseContents[i].blocksRaycasts = collapseContents[i].interactable;
                Tweener tweener = collapseContents[i].DOFade(alpha, animationDelta).ChangeStartValue(1 - alpha).SetDelay(delayIndex * animationDelta).SetEase(Ease.Linear);
                if ((endIndex - i) * delta == 0)
                {
                    tweener.OnComplete(() =>
                    {
                        isCollapsed = !isCollapsed;
                        if (collapseIcon != null)
                        {
                            collapseIcon.SetActive(!isCollapsed);
                        }
                        if (expandIcon != null)
                        {
                            expandIcon.SetActive(isCollapsed);
                        }
                    });
                }
            }
        }
    }
}