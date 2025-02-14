using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFramework
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class ToggleButton : Selectable
    {
        [SerializeField]
        private bool isOn;
        [SerializeField]
        private GameObject activeItem;
        [SerializeField]
        private GameObject inactiveItem;
        [SerializeField]
        private GameObject relatedPanel;
        [SerializeField]
        private ToggleButtonGroup group;

        public UnityEvent<bool> onValueChanged;

        public ToggleButtonGroup Group
        {
            get
            {
                return group;
            }
            set
            {
                SetToggleGroup(value, setMemberValue: true);
                PlayEffect();
            }
        }

        public bool IsOn
        {
            get
            {
                return isOn;
            }
            set
            {
                Set(value);
            }
        }

        protected override void Start()
        {
            InitChildObject();
            PlayEffect();
        }

        protected override void OnDestroy()
        {
            if (group != null)
            {
                group.EnsureValidState();
            }
            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetToggleGroup(group, setMemberValue: false);
            PlayEffect();
        }

        protected override void OnDisable()
        {
            SetToggleGroup(null, setMemberValue: false);
            base.OnDisable();
        }

        private void InitChildObject()
        {
            if (activeItem == null)
                activeItem = transform.Find("ActiveItem")?.gameObject;
            if (inactiveItem == null)
                inactiveItem = transform.Find("InactiveItem")?.gameObject;
        }

        private void SetToggleGroup(ToggleButtonGroup newGroup, bool setMemberValue)
        {
            if (group != null)
            {
                group.UnregisterToggle(this);
            }
            if (setMemberValue)
            {
                group = newGroup;
            }
            if (newGroup != null && IsActive())
            {
                newGroup.RegisterToggle(this);
            }
            if (newGroup != null && isOn && IsActive())
            {
                newGroup.NotifyToggleOn(this);
            }
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, sendCallback: false);
        }

        private void Set(bool value, bool sendCallback = true)
        {
            if (isOn != value)
            {
                isOn = value;
                if (group != null && group.isActiveAndEnabled && IsActive() && (isOn || (!group.AnyTogglesOn() && !group.AllowSwitchOff)))
                {
                    isOn = true;
                    group.NotifyToggleOn(this, sendCallback);
                }
                PlayEffect();
                if (sendCallback)
                {
                    UISystemProfilerApi.AddMarker("ToggleButton.value", (Object)(object)this);
                    onValueChanged?.Invoke(isOn);
                }
            }
        }

        private void PlayEffect()
        {
            if (activeItem != null)
            {
                activeItem.SetActive(isOn);
            }
            if (inactiveItem != null)
            {
                inactiveItem.SetActive(!isOn);
            }
            if (relatedPanel != null)
            {
                relatedPanel.SetActive(isOn);
            }
        }

        private void InternalToggle()
        {
            if (IsActive())
            {
                IsOn = !IsOn;
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (interactable == false)
            {
                return;
            }
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InternalToggle();
            }
        }
    }
}