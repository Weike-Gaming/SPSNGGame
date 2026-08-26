using System;
using TMPro;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    /// <summary>
    /// Simulation bet
    /// </summary>
    public class JIXRYSimFgCountDisplayObject : WkSimDisplayObject
    {
        [SerializeField] private TextMeshProUGUI text = null!;

        #region Binding Properties

        /// <summary>
        /// Bet Amount
        /// </summary>
        public ulong totalFreeGamePlayed { get; set; }

        #endregion


        #region Unity Interface

        ///<inheritdoc/>
        protected override void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TextMeshProUGUI>();
            }

            base.Awake();
        }

        ///<inheritdoc/>
        protected override void OnAllowedEnable()
        {
            InitModelData();
        }

        #endregion

        #region WkDisplayObject Interface
        ///<inheritdoc/>
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            WkMainGameManager gm = owningPlayerController?.owner as WkMainGameManager ?? throw new InvalidCastException();

            wkModelCollection.AddModel(gm.dataModel);

        }

        ///<inheritdoc/>
        protected override void ResetToDefault()
        {
        }

        /// <inheritdoc />
        protected override void UpdateUI()
        {
            base.UpdateUI();

            if (owningPlayerController is null) return;
            if (owningPlayerController!.owner is not WkMainGameManager { gameObject: { activeInHierarchy: bool isActive } }) return;
            if (!isActive) return;

            UpdateCreditText();
        }

        #endregion

        private void UpdateCreditText()
        {
            ulong count = totalFreeGamePlayed;
            text.text = $"{count}";
        }

    }
}