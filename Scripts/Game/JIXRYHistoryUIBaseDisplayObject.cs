using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYHistoryUIBaseDisplayObject : WkHistoryDisplayObject
    {
        [SerializeField] private GameObject historyUiBase;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            historyUiBase.SetActive(false);
        }

        protected override void OnAllowedEnable()
        {
            historyUiBase.SetActive(true);
            base.OnAllowedEnable();
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            historyUiBase.SetActive(false);
        }
    }
}
