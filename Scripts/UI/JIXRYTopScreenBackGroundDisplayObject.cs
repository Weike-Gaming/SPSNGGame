using System;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYTopScreenBackGroundDisplayObject : WkDisplayObject
    {
        [SerializeField] private Sprite mgBackground;
        [SerializeField] private Sprite fgBackground;
        private SpriteRenderer _spriteRenderer;
        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
        }

        protected override void Awake()
        {
            base.Awake();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            Func<string, WkSlotStateCore> getState = gm.gameState.GetState<WkSlotStateCore>;
            getState("fg-session-end").onEnterState += () => _spriteRenderer.sprite = mgBackground;
            getState("fg-init").onExitState += () => _spriteRenderer.sprite = fgBackground;

            string[] fgrecoverStates = { "fg-spin", "fg-end", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot", "fg-end-panel" };

            foreach (string state in fgrecoverStates)
            {
                getState(state).onRecoverState += () =>
                {
                    _spriteRenderer.sprite = fgBackground;
                };
            }
        }
    }
}
