using System;
using TMPro;
using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    /// <summary>
    /// Simulation bet
    /// </summary>
    public class JIXRYSimFgIndividualCountDisplayObject : WkSimDisplayObject
    {
        [SerializeField] private TextMeshProUGUI textB = null!;
        [SerializeField] private TextMeshProUGUI textBRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textR = null!;
        [SerializeField] private TextMeshProUGUI textRRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textG = null!;
        [SerializeField] private TextMeshProUGUI textGRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textBG = null!;
        [SerializeField] private TextMeshProUGUI textBGRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textBR = null!;
        [SerializeField] private TextMeshProUGUI textBRRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textRG = null!;
        [SerializeField] private TextMeshProUGUI textRGRetrigger = null!;
        [SerializeField] private TextMeshProUGUI textBRG = null!;
        [SerializeField] private TextMeshProUGUI textBRGRetrigger = null!;

        #region Binding Properties

        public ulong[] potFgTriggered { get; set; }
        public ulong[] potFgRetriggered { get; set; }

        #endregion


        #region Unity Interface

        ///<inheritdoc/>
        protected override void Awake()
        {
            if (textB == null)
                textB = GetComponent<TextMeshProUGUI>();
            if (textBRetrigger == null)
                textBRetrigger = GetComponent<TextMeshProUGUI>();

            if (textR == null)
                textR = GetComponent<TextMeshProUGUI>();
            if (textRRetrigger == null)
                textRRetrigger = GetComponent<TextMeshProUGUI>();

            if (textG == null)
                textG = GetComponent<TextMeshProUGUI>();
            if (textGRetrigger == null)
                textGRetrigger = GetComponent<TextMeshProUGUI>();
            
            if (textBG == null)
                textBG = GetComponent<TextMeshProUGUI>();
            if (textBGRetrigger == null)
                textBGRetrigger = GetComponent<TextMeshProUGUI>();

            if (textBR == null)
                textBR = GetComponent<TextMeshProUGUI>();
            if (textBRRetrigger == null)
                textBRRetrigger = GetComponent<TextMeshProUGUI>();

            if (textRG == null)
                textRG = GetComponent<TextMeshProUGUI>();
            if (textRGRetrigger == null)
                textRGRetrigger = GetComponent<TextMeshProUGUI>();

            if (textBRG == null)
                textBRG = GetComponent<TextMeshProUGUI>();
            if (textBRGRetrigger == null)
                textBRGRetrigger = GetComponent<TextMeshProUGUI>();

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

            UpdateText();
        }

        #endregion

        private void UpdateText()
        {
            textB.text = $"{potFgTriggered[1]}"!;
            textBRetrigger.text = $"{potFgRetriggered[1]}"!;

            textR.text = $"{potFgTriggered[2]}"!;
            textRRetrigger.text = $"{potFgRetriggered[2]}"!;

            textG.text = $"{potFgTriggered[3]}"!;
            textGRetrigger.text = $"{potFgRetriggered[3]}"!;

            textBG.text = $"{potFgTriggered[4]}"!;
            textBGRetrigger.text = $"{potFgRetriggered[4]}"!;

            textBR.text = $"{potFgTriggered[5]}"!;
            textBRRetrigger.text = $"{potFgRetriggered[5]}"!;

            textRG.text = $"{potFgTriggered[6]}"!;
            textRGRetrigger.text = $"{potFgRetriggered[6]}"!;

            textBRG.text = $"{potFgTriggered[7]}"!;
            textBRGRetrigger.text = $"{potFgRetriggered[7]}"!;
        }

    }
}