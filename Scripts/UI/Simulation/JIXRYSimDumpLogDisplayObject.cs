using System;
using UnityEngine;
using UnityEngine.UI;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYSimDumpLogDisplayObject : WkSimDisplayObject
    {
        [SerializeField] private WkButton dumpBtn;

        protected override void OnAllowedEnable()
        {
            dumpBtn.enabled = true;
        }

        protected override void ResetToDefault()
        {

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            dumpBtn.enabled = false;
        }

        #region Unity Interface

        protected override void Awake()
        {
            if (dumpBtn == null)
            {
                dumpBtn = GetComponent<WkButton>();
            }

            dumpBtn.transition = Selectable.Transition.ColorTint;

            base.Awake();

            dumpBtn.onClick.AddListener(OnClickDumpLog);
        }

        #endregion

        private void OnClickDumpLog()
        {
            JIXRYSimulationGameManager gm = owningPlayerController?.owner as JIXRYSimulationGameManager ?? throw new InvalidCastException();
            gm.DumpLog();
        }
    }
}
