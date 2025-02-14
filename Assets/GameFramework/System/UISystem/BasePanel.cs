using System;
using UnityEngine.UI;

namespace GameFramework
{
    public abstract class BasePanel : BindableMonoBehaviour
    {
        protected void Awake()
        {
            OnAwake();
        }

        public void Show()
        {
            this.gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            this.gameObject.SetActive(false);
            OnHide();
        }

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }
    }
}