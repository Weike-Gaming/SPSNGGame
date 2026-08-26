using System;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYElephantDisplayObject : WkDisplayObject
    {
        [SerializeField] Animator animator;
        
        #region Binding
        public bool playFeatureTriggerCoinAnim { get; set; }
        #endregion

        protected override void OnAllowedEnable()
        {
        }
        protected override void ResetToDefault()
        {
            ResetIdle();
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            wkModelCollection.AddModel(dm);

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            string[] recoverState = { "spin", "fg-spin" };
            foreach (string state in recoverState)
            {
                getState(state)!.onRecoverState += ResetIdle;
            }
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            if (playFeatureTriggerCoinAnim)
            {
                PlayNoseUpAnim();
            }
        }

        private void PlayNoseUpAnim()
        {
            animator.SetTrigger("FreeSpin");
        }

        public void ResetIdle()
        {
            animator.ResetTrigger("FreeSpin");
            animator.Play("Idle", 0, 0f);
        }
    }
}
