using System;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYHelpPageDisplayObject : WkDisplayObject
    {
        [SerializeField] private GameObject panel;

        public bool enableSelectionHelpPage { get; set; } = true;

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
            panel.SetActive(false);
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            panel.SetActive(enableSelectionHelpPage);
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            wkModelCollection.AddModel(dm);

            panel.SetActive(false);
            enableSelectionHelpPage = false;
        }
    }
}
