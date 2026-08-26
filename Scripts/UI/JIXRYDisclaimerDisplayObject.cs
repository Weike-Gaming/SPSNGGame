using UnityEngine;
using Weike.Core;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYDisclaimerDisplayObject : WkDisplayObject
    {
        [SerializeField] private GameObject progEnabledDisclaimer;
        [SerializeField] private GameObject progEnabledNoYearDisclaimer;

        protected void Update()
        {
            UpdateDisclaimer();
        }

        private void UpdateDisclaimer()
        {
            if (machineContext is null) return;
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);
            bool hideYear = machInfo.canHideCopyrightYear;

            if (progEnabledDisclaimer != null)
            {
                progEnabledDisclaimer.SetActive(!hideYear);
            }
            if (progEnabledNoYearDisclaimer != null)
            {
                progEnabledNoYearDisclaimer.SetActive(hideYear);
            }
        }

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
        }
    }
}