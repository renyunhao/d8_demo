using UnityEngine.UI;

namespace GameFramework
{
    public abstract class BaseUI<T> : BaseUI where T : BaseUI
    {
        public override void OnCreate()
        {
            base.OnCreate();
        }

        protected void Hide()
        {
            UISystem.Hide<T>();
        }
    }
}