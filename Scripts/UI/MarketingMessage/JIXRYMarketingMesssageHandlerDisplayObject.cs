using System;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYMarketingMesssageHandlerDisplayObject : WkDisplayObject
    {
        [SerializeField] private GameObject mgMessage;
        [SerializeField] private GameObject fgMessage;

        private bool _isPIError = false;
        private bool _mgStatus = false;
        private bool _fgStatus = false;

        #region WkDisplayObject interface
        protected override void OnAllowedEnable()
        {
            UpdateMessageStatus();
        }

        protected override void ResetToDefault()
        {
            _isPIError = false;
            _mgStatus = false;
            _fgStatus = false;
            mgMessage.SetActive(false);
            fgMessage.SetActive(false);
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            Func<string, WkStateCore> getState = gm.gameState.GetState<WkStateCore>;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            #region Main Game
            string[] states = new string[] { "idle", "spin" };
            foreach (string s in states)
            {
                getState(s).onEnterState += () =>
                {
                    _mgStatus = true;
                    _fgStatus = false;
                    UpdateMessageStatus();
                };
                getState(s).onRecoverState += () =>
                {
                    _mgStatus = true;
                    _fgStatus = false;
                    UpdateMessageStatus();
                };
            }
            #endregion

            #region Random Jackpot
            getState("jp-announcement").onEnterState += () =>
            {
                _mgStatus = false;
                _fgStatus = false;
                UpdateMessageStatus();
            };
            #endregion

            #region Free Game
            states = new string[] { "fg-transition", "fg-init", "fg-feature-animation", "fg-end-panel" };
            foreach (string s in states)
            {
                getState(s).onEnterState += () =>
                {
                    _mgStatus = false;
                    _fgStatus = false;
                    UpdateMessageStatus();
                };
            }

            getState("fg-spin").onEnterState += () =>
            {
                _mgStatus = false;
                _fgStatus = true;
                UpdateMessageStatus();
            };

            getState("fg-spin").onRecoverState += () =>
            {
                _mgStatus = false;
                _fgStatus = true;
                UpdateMessageStatus();
            };
            #endregion

            #region Free Game Jackpot
            getState("fg-jp-announcement").onEnterState += () =>
            {
                _mgStatus = false;
                _fgStatus = false;
                UpdateMessageStatus();
            };
            #endregion

            UpdateMessageStatus();
        }
        #endregion

        private void UpdateMessageStatus()
        {
            if (owningPlayerController?.owner is not JIXRYGameManager { gameObject: { activeInHierarchy: bool isActive } }) return;

            if (!isActive) return;

            if (_isPIError)
            {
                mgMessage.SetActive(false);
                fgMessage.SetActive(false);
                return;
            }

            mgMessage.SetActive(_mgStatus);
            fgMessage.SetActive(_fgStatus);
        }

        public override void OnPIError()
        {
            base.OnPIError();
            _isPIError = true;
            UpdateMessageStatus();
        }

        public override void OnPIIdle()
        {
            base.OnPIIdle();
            _isPIError = false;
            UpdateMessageStatus();
        }
    }
}

