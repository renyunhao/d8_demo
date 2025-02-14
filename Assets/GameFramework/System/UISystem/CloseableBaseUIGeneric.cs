using UnityEngine.UI;

namespace GameFramework
{
    public abstract class CloseableBaseUIGeneric<T> : BaseUI<T> where T : BaseUI
    {
        public Button closeButton;

        public override void OnCreate()
        {
            base.OnCreate();
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }

        protected virtual void OnCloseButtonClick()
        {
            Hide();
        }
    }
}