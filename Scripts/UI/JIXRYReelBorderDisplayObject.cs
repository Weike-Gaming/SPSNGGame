using System;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYReelBorderDisplayObject : WkDisplayObject
    {
        [SerializeField] private SpriteRenderer border;
        [SerializeField] private Sprite mainGameBorder;
        [SerializeField] private Sprite freeGameBorder;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            Func<string, WkStateCore> getState = gm!.gameState!.GetState<WkStateCore>;


            getState("fg-init").onExitState += () =>
            {
                border.sprite = freeGameBorder;
            };

            getState("fg-session-end").onEnterState += () =>
            {
                border.sprite = mainGameBorder;
            };

            string[] fgrecoverStates = { "fg-spin", "fg-end", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot"};

            foreach (string state in fgrecoverStates)
            {
                getState(state).onRecoverState += () =>
                {
                    border.sprite = freeGameBorder;
                    getActiveAudioManager.StopAudioOfClass("BGM"); // not working with PlayAudioUnique
                    getActiveAudioManager.PlayAudio("FreeGameBgm");
                };               
            }

            getState("fg-end-panel").onRecoverState += () =>
            {
                border.sprite = freeGameBorder;
            };

            getState("fg-init").onExitState += () =>
            {
                getActiveAudioManager.StopAudioOfClass("BGM"); // not working with PlayAudioUnique
                getActiveAudioManager.PlayAudio("FreeGameBgm");
            };

            getState("fg-end-panel").onEnterState += () =>
            {
                getActiveAudioManager.StopAudio("FreeGameBgm");
            };

            wkModelCollection.AddModel(dm);
        }

        protected override void OnAllowedEnable()
        {
            
        }

        protected override void ResetToDefault()
        {
            
        }
    }
}
