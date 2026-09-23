using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYBottomScreenDisplayObject : WkDisplayObject
    {
        [SerializeField] private GameObject bottomScreen;

        protected override void OnAllowedEnable()
        {
            bottomScreen.SetActive(true);
           
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            bottomScreen.SetActive(false);
        }

        protected override void ResetToDefault()
        {

        }
    }
}
