using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weike.Common;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYSimStartDisplayObject : WkSimDisplayObject
    {
        [SerializeField] private TextMeshProUGUI startLabel;
        [SerializeField] private WkButton startBtn;

        #region Binding Properties
        public bool isSimulationStarted { get; set; }
        #endregion

        protected override void Awake()
        {
            WkAssert.EnsureMsgf(startLabel != null, "Start label is null");
            WkAssert.EnsureMsgf(startBtn != null, "Start btn is null");

            startBtn.transition = Selectable.Transition.ColorTint;

            base.Awake();
        }
        
        protected override void OnAllowedEnable()
        {
            startLabel.enabled = true;
            startBtn.enabled = true;
        }

        protected override void ResetToDefault()
        {

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            startLabel.enabled = false;
            startBtn.enabled = false;
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYSimulationGameManager gm = owningPlayerController?.owner as JIXRYSimulationGameManager ?? throw new InvalidCastException();

            wkModelCollection.AddModel(gm.dataModel);

            startBtn.onClickDown.AddListener(() =>
            {
                gm.ToggleSimulation();
            });

        }

        protected override void UpdateUI()
        {
            base.UpdateUI();

            startLabel.text = isSimulationStarted ? "Stop" : "Start";
        }
    }
}
