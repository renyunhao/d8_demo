using System;
using UnityEngine.UI;

namespace GameFramework
{
    public abstract class CloseablePanel : BasePanel
    {
        public Button closeButton;

        protected override void OnAwake()
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }

        private void OnCloseButtonClick()
        {
            Hide();
        }
    }
}