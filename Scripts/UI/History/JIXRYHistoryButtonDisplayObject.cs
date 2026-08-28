using System;
using UnityEngine;
using UnityEngine.UI;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYHistoryButtonDisplayObject : WkHistoryButtonDisplayObject
    {
        /// <summary>
        /// Button that will show "BEFORE" or "AFTER" if there was nudge performed 
        /// </summary>
        [SerializeField] protected WkButton nudgeButton = null!;
        private Text nudgeButtonText = null!;

        protected override void Awake()
        {
            base.Awake();

            if (nudgeButton is null)
                Debug.LogError("Nudge button is missing!");
            nudgeButton.transition = Selectable.Transition.ColorTint;

            nudgeButtonText = nudgeButton.GetComponentInChildren<Text>();
            if (nudgeButtonText is null)
                Debug.LogError("Nudge button text component is missing!");
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            nudgeButton.onClick.AddListener(OnClickNudgeButton);
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            nudgeButton?.gameObject.SetActive(false);
        }

        protected override void UpdateButtonStatus()
        {
            base.UpdateButtonStatus();
            JIXRYHistoryGameManager gm = owningPlayerController?.owner as JIXRYHistoryGameManager ?? throw new InvalidCastException();

            prevBtn.gameObject.SetActive(!gm.currentlyInSubGame);
            nextBtn.gameObject.SetActive(!gm.currentlyInSubGame);
            subBtn.gameObject.SetActive(!gm.currentlyInSubGame);

            prevSubBtn.gameObject.SetActive(gm.currentlyInSubGame);
            nextSubBtn.gameObject.SetActive(gm.currentlyInSubGame);
            mainBtn.gameObject.SetActive(gm.currentlyInSubGame);
            nudgeButton?.gameObject.SetActive(gm.currentlyInSubGame);

            prevSubBtn.interactable = gm.HasPrevSubGame();
            nextSubBtn.interactable = gm.HasNextSubGame();

            subBtn.interactable = gm.replayHistorySubRecoverData.Count > 0;

            // Nudge Button
            //if (gm.currentlyInSubGame && gm.HasNudge())
            //{
            //    nudgeButton.interactable = true;
            //    nudgeButtonText.text = gm.IsInPreNudge() ? "AFTER" : "BEFORE";
            //}
            //else
            //{
                nudgeButton.interactable = false;
                nudgeButtonText.text = "N/A";
           // }
        }

        protected void OnClickNudgeButton()
        {
            JIXRYHistoryGameManager gm = owningPlayerController?.owner as JIXRYHistoryGameManager ?? throw new InvalidCastException();
            gm.ToggleNudge();
        }
    }
}
