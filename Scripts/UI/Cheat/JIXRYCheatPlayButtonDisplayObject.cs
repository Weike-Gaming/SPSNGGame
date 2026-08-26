using System;
using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatPlayButtonDisplayObject : WkCheatPlayButtonDisplayObject
    {
        [SerializeField] JIXRYCheatHiddenPanelContentWrapperDisplayObject contentWrapper;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            getState("fg-cheat").onEnterState += () =>
            {
                button!.onClickDown.RemoveAllListeners();
                button.onClickDown.AddListener(() =>
                {
                    gm.TriggerCheatFreeGamePlay();
                });
                button.gameObject.SetActive(true);
            };

            getState("fg-cheat")!.onExitState += () =>
            {
                button!.gameObject.SetActive(false);
                contentWrapper.CloseMoreSettings();
            };

            getState("cheat")!.onExitState += () =>
            {
                contentWrapper.CloseMoreSettings();
            };
        }
    }
}