using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFramework
{
    [DisallowMultipleComponent]
    public class ToggleButtonGroup : UIBehaviour
    {
        [SerializeField]
        private bool allowSwitchOff = false;

        protected List<ToggleButton> toggles = new List<ToggleButton>();

        public ToggleButton ActiveToggle
        {
            get;
            private set;
        }

        public bool AllowSwitchOff
        {
            get
            {
                return allowSwitchOff;
            }
            set
            {
                allowSwitchOff = value;
            }
        }

        protected override void Start()
        {
            EnsureValidState();
            base.Start();
        }

        protected override void OnEnable()
        {
            EnsureValidState();
            base.OnEnable();
        }

        private void ValidateToggleIsInGroup(ToggleButton toggle)
        {
            if (toggle == null || !toggles.Contains(toggle))
            {
                throw new ArgumentException(string.Format("Toggle {0} is not part of ToggleGroup {1}", new object[2] { toggle, this }));
            }
        }

        public void NotifyFirstToggleOn()
        {
            SetAllTogglesOff();
            toggles.FirstOrDefault()!.IsOn = true;
        }

        public void NotifyToggleOn(ToggleButton toggle, bool sendCallback = true)
        {
            ActiveToggle = toggle;
            ValidateToggleIsInGroup(toggle);
            for (int i = 0; i < toggles.Count; i++)
            {
                if (!(toggles[i] == toggle))
                {
                    if (sendCallback)
                    {
                        toggles[i].IsOn = false;
                    }
                    else
                    {
                        toggles[i].SetIsOnWithoutNotify(value: false);
                    }
                }
            }
        }

        public void UnregisterToggle(ToggleButton toggle)
        {
            if (toggles.Contains(toggle))
            {
                toggles.Remove(toggle);
            }
        }

        public void RegisterToggle(ToggleButton toggle)
        {
            if (!toggles.Contains(toggle))
            {
                toggles.Add(toggle);
            }
        }

        public void EnsureValidState()
        {
            if (!allowSwitchOff && !AnyTogglesOn() && toggles.Count != 0)
            {
                toggles[0].IsOn = true;
                NotifyToggleOn(toggles[0]);
            }
            IEnumerable<ToggleButton> activeToggles = ActiveToggles();
            if (activeToggles.Count() <= 1)
            {
                return;
            }
            ToggleButton firstActive = GetFirstActiveToggle();
            foreach (ToggleButton toggle in activeToggles)
            {
                if (!(toggle == firstActive))
                {
                    toggle.IsOn = false;
                }
            }
        }

        public bool AnyTogglesOn()
        {
            return toggles.Find((ToggleButton x) => x.IsOn) != null;
        }

        public IEnumerable<ToggleButton> ActiveToggles()
        {
            return toggles.Where((ToggleButton x) => x.IsOn);
        }

        public ToggleButton GetFirstActiveToggle()
        {
            IEnumerable<ToggleButton> activeToggles = ActiveToggles();
            return (activeToggles.Count() > 0) ? activeToggles.First() : null;
        }

        public void SetAllTogglesOff(bool sendCallback = true)
        {
            bool oldAllowSwitchOff = allowSwitchOff;
            allowSwitchOff = true;
            if (sendCallback)
            {
                for (int j = 0; j < toggles.Count; j++)
                {
                    toggles[j].IsOn = false;
                }
            }
            else
            {
                for (int i = 0; i < toggles.Count; i++)
                {
                    toggles[i].SetIsOnWithoutNotify(value: false);
                }
            }
            allowSwitchOff = oldAllowSwitchOff;
        }
    }
}