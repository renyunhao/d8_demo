using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleValidator : MonoBehaviour
    {
        /// <summary>
        /// 当Toggle为false时，需要停响应的控件
        /// </summary>
        public Selectable[] selectableControls;
        /// <summary>
        /// 当Toggle为false时，控件的替换材质
        /// </summary>
        public Material interactableDisabledMaterial;
        /// <summary>
        /// 当Toggle为false时，要替换材质的控件
        /// </summary>
        public Image[] interactableReplaceMaterialImages;

        private Toggle toggle;

        void Start()
        {
            toggle = this.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                foreach (var control in selectableControls)
                {
                    control.interactable = true;
                }
                foreach (var image in interactableReplaceMaterialImages)
                {
                    image.material = null;
                }
            }
            else
            {
                foreach (var control in selectableControls)
                {
                    control.interactable = false;
                }
                foreach (var image in interactableReplaceMaterialImages)
                {
                    image.material = interactableDisabledMaterial;
                }
            }
        }
    }
}