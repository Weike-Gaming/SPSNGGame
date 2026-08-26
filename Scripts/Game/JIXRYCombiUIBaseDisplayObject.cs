using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiUIBaseDisplayObject : WkCombiDisplayObject
    {
        [SerializeField] private GameObject combiUiBase;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            combiUiBase.SetActive(true);
            ResetToDefault();
        }

        protected override void OnAllowedEnable()
        {
            combiUiBase.SetActive(true);
            base.OnAllowedEnable();
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            combiUiBase.SetActive(false);
        }
    }
}
