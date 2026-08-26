using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYUIBaseDisplayObject : WkDisplayObject
    {
        [SerializeField] private GameObject uiBase;

        protected override void OnAllowedEnable()
        {
            uiBase.SetActive(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            uiBase.SetActive(false);
        }

        protected override void ResetToDefault()
        {

        }
    }
}
