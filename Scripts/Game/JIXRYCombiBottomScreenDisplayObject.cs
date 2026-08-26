using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiBottomScreenDisplayObject : WkCombiDisplayObject
    {
        [SerializeField] private GameObject bottomScreen;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            bottomScreen.SetActive(false);
        }

        protected override void OnAllowedEnable()
        {
            bottomScreen.SetActive(true);
            base.OnAllowedEnable();
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            bottomScreen.SetActive(false);
        }
    }
}
