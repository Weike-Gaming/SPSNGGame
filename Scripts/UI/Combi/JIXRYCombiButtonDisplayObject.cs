using UnityEngine;
using UnityEngine.UI;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiButtonDisplayObject : WkCombiDisplayObject
    {
        [SerializeField] private WkButton toggleReelStripSet = null!;
        [SerializeField] private Text valueReelStripSet = null!;

        [SerializeField] private WkButton toggleFeatureSelected = null!;
        [SerializeField] private Text valueFeatureSelected = null!;

        [SerializeField] private WkButton toggleRandomJackpot = null!;
        [SerializeField] private Text valueRandomJackpot = null!;

        [SerializeField] private WkButton togglePlayOption = null!;
        [SerializeField] private Text valuePlayOption = null!;

        [SerializeField] private WkButton toggleBetMultiplier = null!;
        [SerializeField] private Text valueBetMultiplier = null!;

        [SerializeField] private WkButton returnBtn = null!;

        [SerializeField] private GameObject label;

        public byte upcomingPotFeatureGameFlag { get; set; }
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            wkModelCollection.AddModel(dm);
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            SetTextValues();
        }

        #region Unity Interface

        protected override void Awake()
        {
            base.Awake();

            toggleReelStripSet.transition = Selectable.Transition.ColorTint;
            toggleFeatureSelected.transition = Selectable.Transition.ColorTint;
            toggleRandomJackpot.transition = Selectable.Transition.ColorTint;

            togglePlayOption.transition = Selectable.Transition.ColorTint;
            toggleBetMultiplier.transition = Selectable.Transition.ColorTint;

            returnBtn.transition = Selectable.Transition.ColorTint;
        }

        ///<inheritdoc/>
        protected override void OnAllowedEnable()
        {
            base.OnAllowedEnable();

            label?.SetActive(true);

            toggleReelStripSet?.gameObject.SetActive(true);
            toggleFeatureSelected?.gameObject.SetActive(true);
            toggleRandomJackpot?.gameObject.SetActive(true);

            togglePlayOption?.gameObject.SetActive(true);
            toggleBetMultiplier?.gameObject.SetActive(true);

            returnBtn?.gameObject.SetActive(true);
        }

        #endregion

        #region WkDisplayObject

        ///<inheritdoc/>
        protected override void ResetToDefault()
        {
            base.ResetToDefault();

            label?.SetActive(false);

            toggleReelStripSet?.gameObject.SetActive(false);
            toggleFeatureSelected?.gameObject.SetActive(false);
            toggleRandomJackpot?.gameObject.SetActive(false);

            togglePlayOption?.gameObject.SetActive(false);
            toggleBetMultiplier?.gameObject.SetActive(false);

            returnBtn?.gameObject.SetActive(false);

            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) return;
            gm.ResetUpcomingFeatureFlag();
        }

        ///<inheritdoc/>
        public override void OnDeselected()
        {
            base.OnDeselected();

            toggleReelStripSet?.onClickDown.RemoveAllListeners();
            toggleFeatureSelected?.onClickDown.RemoveAllListeners();
            toggleRandomJackpot?.onClickDown.RemoveAllListeners();

            togglePlayOption?.onClickDown.RemoveAllListeners();
            toggleBetMultiplier?.onClickDown.RemoveAllListeners();

            returnBtn?.onClickDown.RemoveAllListeners();
        }

        private void SetTextValues()
        {
            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) { return; }

            valuePlayOption.text = gm.GetPlayOption().ToString();

            valueBetMultiplier.text = $"x{gm.GetBetMultiplier().ToString()}";

            SetReelStripSet();

            SetFeatureSelected();

            SetRandomJackpot();
        }

        ///<inheritdoc/>
        protected override void SafeSelected()
        {
            base.SafeSelected();

            toggleReelStripSet?.onClickDown.RemoveAllListeners();
            toggleFeatureSelected?.onClickDown.RemoveAllListeners();
            toggleRandomJackpot?.onClickDown.RemoveAllListeners();

            togglePlayOption?.onClickDown.RemoveAllListeners();
            toggleBetMultiplier?.onClickDown.RemoveAllListeners();

            returnBtn?.onClickDown.RemoveAllListeners();

            SetTextValues();

            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) { return; }

            toggleReelStripSet?.onClickDown.AddListener(() =>
            {
                gm.ToggleReelStripSet();
                gm.CheckWin();
            }
            );
            toggleFeatureSelected?.onClickDown.AddListener(() =>
            {
                gm.ToggleFeatureSelected();
                gm.CheckWin();
            }
            );
            toggleRandomJackpot?.onClickDown.AddListener(() =>
            {
                gm.ToggleRandomJackpot();
                gm.CheckWin();
            }
            );

            togglePlayOption?.onClickDown.AddListener(() =>
            {
                gm.TogglePlayOption();
                gm.CheckWin();
            }
            );
            toggleBetMultiplier?.onClickDown.AddListener(() =>
            {
                gm.ToggleBetMultiplier();
                gm.CheckWin();
            }
            );
            
            returnBtn?.onClickDown.AddListener(() =>
            {
                gm.ReturnToCombiLobby();
            }
            );
        }

        private void SetReelStripSet()
        {
            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            byte reelStripSet = gm.GetReelStripSetIndex();
            valueReelStripSet.text = reelStripSet.ToString();
        }

        private void SetFeatureSelected()
        {
            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) { return; }

            switch (gm.GetFeatureFlag())
            {
                case JIXRYStateDataFlag.MAIN_GAME:
                    valueFeatureSelected.text = "NONE";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RN:
                    valueFeatureSelected.text = "GREEN";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_JP:
                    valueFeatureSelected.text = "RED";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RU:
                    valueFeatureSelected.text = "PURPLE";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNJP:
                    valueFeatureSelected.text = "GREEN RED";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNRU:
                    valueFeatureSelected.text = "GREEN PURPLE";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_JPRU:
                    valueFeatureSelected.text = "RED PURPLE";
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    valueFeatureSelected.text = "GREEN RED PURPLE";
                    break;
            }
        }

        private void SetRandomJackpot()
        {
            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) { return; }

            string str = gm.GetRandomPrizeType() switch
            {
                1 => "GRAND",
                2 => "MAJOR",
                _ => "NONE"
            };
            valueRandomJackpot.text = str;
        }

        #endregion
    }
}
