using UnityEngine;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYLobbyJackpotOdoMeterDenomToggleDO : WkDisplayObject
    {
        [SerializeField] private GameObject firstAmountObj;
        [SerializeField] private GameObject secAmountObj;

        private GameObject _current;
        private GameObject _previous;

        #region binding
        public ulong targetDenomInLobby { get; set; }
        #endregion

        public JIXRYLobbyJackpotOdoMeterDenomToggleDO()
        {
            isLobby = true;
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            WkLobbySceneManager? lobbyManager = WkLobbySceneManager.instance;
            if (lobbyManager is null || lobbyManager.dataModel is null)
            {
                return;
            }
            _current = firstAmountObj;
            _previous = secAmountObj;
            wkModelCollection.AddModel(lobbyManager.dataModel);
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();

            if(targetDenomInLobby == 0 || machineContext is null)
            {
                return;
            }

            Credits outCredit = new();
            bool noCredit = machineContext.platformInterface!.GetCredit(outCredit) <= 0;

            if (noCredit)
            {
                _current.GetComponent<JIXRYLobbyOdoMeterFixAmountDO>().ToggleOffDigits();
                _previous.GetComponent<JIXRYLobbyOdoMeterFixAmountDO>().SetUpAndGenerateDigits(targetDenomInLobby);
                GameObject temp = _current;
                _current = _previous;
                _previous = temp;
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
