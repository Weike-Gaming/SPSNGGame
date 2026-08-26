using Weike.SlotCore;
using Weike.LobbyManagement;

namespace Weike.Games.JIXRY
{
    public class JIXRYSelectDenomAnimDisplayObject : WkLobbyTouchPanelSelectDenomAnimationDisplayObject
    {
        protected override void OnLanguageChange()
        {
            bool isEn = language == WkGameLanguage.EN;
            selectDenomAnim.Play(isEn ? "SelectDenomLobby_en" : "SelectDenomLobby_zh");
        }

    }
}
