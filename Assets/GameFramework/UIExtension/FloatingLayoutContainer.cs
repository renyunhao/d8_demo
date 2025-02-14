using UnityEngine;

namespace GameFramework
{
    [RequireComponent(typeof(Canvas))]
    public class FloatingLayoutContainer : BindableMonoBehaviour
    {
        private FloatingLayout floatingLayout;
        private bool screenDimensionsChanged = true;

        protected virtual void Start()
        {
            floatingLayout = this.GetComponentInChildren<FloatingLayout>();
        }

        protected virtual void Update()
        {
            if (screenDimensionsChanged)
            {
                floatingLayout.SnapToScreenEdge(true);
                screenDimensionsChanged = false;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            screenDimensionsChanged = true;
        }
    }
}