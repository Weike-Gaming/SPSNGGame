using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    class PotLevelAnimationData
    {
        public Texture2D[] Textures;
        public List<SSFrame> Frames;
        public int[] TotalSpritesPerTexture;
    }

    public class JIXRYPotFeatureDisplayObject : WkMultiLanguageDisplayObject
    {
        [SerializeField] private string potType;
        [SerializeField] private Transform potCoinPosition;
        [SerializeField] private SpriteRenderer awardedRenderer;
        private SpriteRenderer _potSpriteRenderer;

        private Dictionary<string, WkAdvanceRenderer> _rendererCollection = new Dictionary<string, WkAdvanceRenderer>();
        private Dictionary<int, PotLevelAnimationData> _potCoinLevelData;
        private Dictionary<int, PotLevelAnimationData> _coinBurstLevelData;

        #region PotCoin
        private const int PotCoinMaxLevel = 5;
        [Header("PotCoin")]
        [SerializeField] private Texture2D[] potCoinTextureCollection = null!;
        [SerializeField] private TextAsset[] potCoinJsonCollection = null!;
        private List<SSFrame> _potCoinFramesCollection = null!;
        private int[] _potCoinTotalSpriteInSS = null!;

        [Header("PotCoin05")]
        [SerializeField] private Texture2D[] potCoin05TextureCollection = null!;
        [SerializeField] private TextAsset[] potCoin05JsonCollection = null!;
        private List<SSFrame> _potCoin05FramesCollection = null!;
        private int[] _potCoin05TotalSpriteInSS = null!;

        [Header("PotCoin15")]
        [SerializeField] private Texture2D[] potCoin15TextureCollection = null!;
        [SerializeField] private TextAsset[] potCoin15JsonCollection = null!;
        private List<SSFrame> _potCoin15FramesCollection = null!;
        private int[] _potCoin15TotalSpriteInSS = null!;

        [Header("PotCoin25")]
        [SerializeField] private Texture2D[] potCoin25TextureCollection = null!;
        [SerializeField] private TextAsset[] potCoin25JsonCollection = null!;
        private List<SSFrame> _potCoin25FramesCollection = null!;
        private int[] _potCoin25TotalSpriteInSS = null!;

        [Header("PotCoin35")]
        [SerializeField] private Texture2D[] potCoin35TextureCollection = null!;
        [SerializeField] private TextAsset[] potCoin35JsonCollection = null!;
        private List<SSFrame> _potCoin35FramesCollection = null!;
        private int[] _potCoin35TotalSpriteInSS = null!;
        #endregion

        #region Effect
        [Header("CoinBurst")]
        [SerializeField] private Texture2D[] coinBurstTextureCollection = null!;
        [SerializeField] private TextAsset[] coinBurstJsonCollection = null!;
        private List<SSFrame> _coinBurstFramesCollection = null!;
        private int[] _coinBurstTotalSpriteInSS = null!;

        [Header("CoinBurst05")]
        [SerializeField] private Texture2D[] coinBurst05TextureCollection = null!;
        [SerializeField] private TextAsset[] coinBurst05JsonCollection = null!;
        private List<SSFrame> _coinBurst05FramesCollection = null!;
        private int[] _coinBurst05TotalSpriteInSS = null!;

        [Header("CoinBurst15")]
        [SerializeField] private Texture2D[] coinBurst15TextureCollection = null!;
        [SerializeField] private TextAsset[] coinBurst15JsonCollection = null!;
        private List<SSFrame> _coinBurst15FramesCollection = null!;
        private int[] _coinBurst15TotalSpriteInSS = null!;

        [Header("CoinBurst25")]
        [SerializeField] private Texture2D[] coinBurst25TextureCollection = null!;
        [SerializeField] private TextAsset[] coinBurst25JsonCollection = null!;
        private List<SSFrame> _coinBurst25FramesCollection = null!;
        private int[] _coinBurst25TotalSpriteInSS = null!;

        [Header("CoinBurst35")]
        [SerializeField] private Texture2D[] coinBurst35TextureCollection = null!;
        [SerializeField] private TextAsset[] coinBurst35JsonCollection = null!;
        private List<SSFrame> _coinBurst35FramesCollection = null!;
        private int[] _coinBurst35TotalSpriteInSS = null!;

        [Header("PotShine")]
        [SerializeField] private Texture2D[] potShineTextureCollection = null!;
        [SerializeField] private TextAsset[] potShineJsonCollection = null!;
        private List<SSFrame> _potShineFramesCollection = null!;
        private int[] _potShineTotalSpriteInSS = null!;

        [Header("Light")]      
        [SerializeField] private Texture2D[] lightTextureCollection = null!;
        [SerializeField] private TextAsset[] lightJsonCollection = null!;
        private List<SSFrame> _lightFramesCollection = null!;
        private int[] _lightTotalSpriteInSS = null!;

        [Header("Sparkle04")]
        [SerializeField] private Texture2D[] sparkle04TextureCollection = null!;
        [SerializeField] private TextAsset[] sparkle04JsonCollection = null!;
        private List<SSFrame> _sparkle04FramesCollection = null!;
        private int[] _sparkle04TotalSpriteInSS = null!;

        [Header("Sparkle5")]
        [SerializeField] private Texture2D[] sparkle5TextureCollection = null!;
        [SerializeField] private TextAsset[] sparkle5JsonCollection = null!;
        private List<SSFrame> _sparkle5FramesCollection = null!;
        private int[] _sparkle5TotalSpriteInSS = null!;
        #endregion

        [Header("Text Shine")]
        [SerializeField] private SpriteRenderer textRenderer = null!;
        [SerializeField] private List<Material> textShineMaterial = null!;

        [Header("FinalGlow")]
        [SerializeField] private Texture2D[] glowCollection = null!;
        [SerializeField] private TextAsset[] glowJsonCollection = null!;
        private List<SSFrame> _glowCollectionFramesCollection = null!;
        private int[] _glowCollectionTotalSpriteInSS = null!;

        private List<string> _animationKeyCollection = new List<string>();
        private int _previousScatterLevel = 0;

        private static readonly int[] ScatterThresholds = { 3, 6, 9, 12, 15 };
        private const int PotCoinSortingOrder = 10;
        private const int EffectSortingOrder = 101;
        private const int LightSortingOrder = 12;
        private readonly int _potShineStrength = Shader.PropertyToID("_Strength");
        #region Binding
        public bool playFeatureTriggerCoinAnim { get; set; }
        public bool blueCoinInAnim { get; set; }
        public bool redCoinInAnim { get; set; }
        public bool greenCoinInAnim { get; set; }
        public bool triggerFinalAnim { get; set; }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            //Pot Coin
            (_potCoinFramesCollection, _potCoinTotalSpriteInSS) = ProcessJsonCollection(potCoinJsonCollection);
            (_potCoin05FramesCollection, _potCoin05TotalSpriteInSS) = ProcessJsonCollection(potCoin05JsonCollection);
            (_potCoin15FramesCollection, _potCoin15TotalSpriteInSS) = ProcessJsonCollection(potCoin15JsonCollection);
            (_potCoin25FramesCollection, _potCoin25TotalSpriteInSS) = ProcessJsonCollection(potCoin25JsonCollection);
            (_potCoin35FramesCollection, _potCoin35TotalSpriteInSS) = ProcessJsonCollection(potCoin35JsonCollection);

            //Coin Burst
            (_coinBurstFramesCollection, _coinBurstTotalSpriteInSS) = ProcessJsonCollection(coinBurstJsonCollection);
            (_coinBurst05FramesCollection, _coinBurst05TotalSpriteInSS) = ProcessJsonCollection(coinBurst05JsonCollection);
            (_coinBurst15FramesCollection, _coinBurst15TotalSpriteInSS) = ProcessJsonCollection(coinBurst15JsonCollection);
            (_coinBurst25FramesCollection, _coinBurst25TotalSpriteInSS) = ProcessJsonCollection(coinBurst25JsonCollection);
            (_coinBurst35FramesCollection, _coinBurst35TotalSpriteInSS) = ProcessJsonCollection(coinBurst35JsonCollection);

            //Light
            (_lightFramesCollection, _lightTotalSpriteInSS) = ProcessJsonCollection(lightJsonCollection);

            //Sparkle
            (_sparkle04FramesCollection, _sparkle04TotalSpriteInSS) = ProcessJsonCollection(sparkle04JsonCollection);
            (_sparkle5FramesCollection, _sparkle5TotalSpriteInSS) = ProcessJsonCollection(sparkle5JsonCollection);
            (_glowCollectionFramesCollection, _glowCollectionTotalSpriteInSS) = ProcessJsonCollection(glowJsonCollection);

            //Pot Shine
            (_potShineFramesCollection, _potShineTotalSpriteInSS) = ProcessJsonCollection(potShineJsonCollection);

            _potSpriteRenderer = GetComponent<SpriteRenderer>();
            _potCoinLevelData = BuildLevelAnimationDictionary(potCoinTextureCollection, _potCoinFramesCollection, _potCoinTotalSpriteInSS, 2);
            _coinBurstLevelData = BuildLevelAnimationDictionary(coinBurstTextureCollection, _coinBurstFramesCollection, _coinBurstTotalSpriteInSS, 4);

            SetUpRenderer();
        }

        protected override void OnAllowedEnable()
        {
            HandleMgRecovery();
        }

        protected override void ResetToDefault()
        {
            awardedRenderer.enabled = false;
            StopAllAnimation();
            string[] effect = { "Light", "PotShine", "CoinBurst", "Sparkle" };
            SetRendererVisible(effect, false);
        }

        public override void OnPIError()
        {
            base.OnPIError();
            StopAllAnimation();
        }
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            wkModelCollection.AddModel(dm);

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("fg-init")!.onEnterState += ToggleAwarded;
            getState("fg-feature-animation")!.onEnterState += () => WkCoroutine.instance.StartTrackedCoroutine(HandleFgFeatureCoroutine());
            getState("fg-session-end")!.onEnterState += ResetPotShine;
            getState("spin")!.onExitState += ()=> CheckThreshold();

            string[] mgRecoverState = { "idle", "spin", "jp-announcement", "jp-session-end" };
            foreach (string state in mgRecoverState)
            {
                getState(state).onRecoverState += HandleMgRecovery;
            }

            string[] fgRecoverAwardState = { "fg-init", "fg-end", "fg-spin", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot", "fg-end-panel"};
            foreach (string state in fgRecoverAwardState)
            {
                getState(state).onRecoverState += ToggleAwarded;
            }

            string[] fgRecoverState = { "fg-transition", "fg-init", "fg-end", "fg-spin", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot", "fg-end-panel"};
            foreach (string state in fgRecoverState)
            {
                getState(state).onRecoverState += HandleFgRecovery;
            }            
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();

            if (playFeatureTriggerCoinAnim)
            {
                PlayFeatureTriggerAnim();
            }
            if(blueCoinInAnim)
            {
                PlayCoinInAnimation("ES_CoinBurst");
            }
            if(redCoinInAnim)
            {
                PlayCoinInAnimation("JP_CoinBurst");
            }
            if(greenCoinInAnim)
            {
                PlayCoinInAnimation("EP_CoinBurst");
            }
            if(triggerFinalAnim)
            {
                PlayTriggerFinalAnimation();
            }
        }

        protected override void OnLanguageChange()
        {
            textRenderer.material = language == WkGameLanguage.EN ? textShineMaterial[0] : textShineMaterial[1];

            if (awardedRenderer.enabled == true)
            {
                awardedRenderer.material = language == WkGameLanguage.EN ? textShineMaterial[2] : textShineMaterial[3];
            }
        }

        private IEnumerator HandleFgFeatureCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            ToggleAwarded();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            HandleMgRecovery();
        }

        #region Recovery
        private void HandleMgRecovery()
        {
            if (IsAwarded()) return;

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();

            StopAllAnimation();

            _previousScatterLevel = GetScatterLevel(true);
            _rendererCollection["Light"].gameObject.SetActive(false);
            _rendererCollection["PotCoin"].gameObject.SetActive(true);
            if (_previousScatterLevel < PotCoinMaxLevel)
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = Vector2.zero;
            else
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -0.49f);
            _potSpriteRenderer.material.SetFloat(_potShineStrength, 0);
            UpdateGraphic("PotCoin", GetScatterLevel(true));

            gm.RecoverPotCoinLevel();
        }

        private void HandleFgRecovery()
        {
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;

            if (feature.ToString().Contains(potType))
            {
                _rendererCollection["PotCoin"].gameObject.SetActive(true);
                _rendererCollection["PotShine"].gameObject.SetActive(true);
                awardedRenderer.enabled = true;
                _potSpriteRenderer.material.SetFloat(_potShineStrength, 1);

                _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -0.49f);
                UpdateGraphic("PotCoin", PotCoinMaxLevel);
                UpdateGraphic("PotShine");
                PlayEffectAnimation("PotShine", -1, _rendererCollection["PotShine"]);
            }
            else
            {
                _potSpriteRenderer.material.SetFloat(_potShineStrength, 0);
                awardedRenderer.enabled = false;
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = Vector2.zero;
                UpdateGraphic("PotCoin", GetScatterLevel(true));
            }
        }
        #endregion

        #region Threshold
        private void CheckThreshold()
        {
            if (IsAwarded()) return;

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            bool isHitFeature = dm.upcomingPotFeatureGameFlag != dm.previousPotFeatureGameFlag;

            // if featuer hit only play the trigger anim
            if (isHitFeature)
            {
                return;
            }

            int newLevel = GetScatterLevel(false);

            if (newLevel > _previousScatterLevel)
            {
                StopAllAnimation();
                _previousScatterLevel = newLevel;

                string[] effect = { "Light", "PotCoin", "CoinBurst", "Sparkle" };
                SetRendererVisible(effect, true);

                PlayCoinPotAnimation(potType, newLevel, 1, _rendererCollection["PotCoin"]);
                PlayEffectAnimation("Light", 1, _rendererCollection["Light"], () => _rendererCollection["Light"].gameObject.SetActive(false));
                PlayEffectAnimation("CoinBurst", 1, _rendererCollection["CoinBurst"], () => _rendererCollection["CoinBurst"].gameObject.SetActive(false));
                PlayEffectAnimation("Sparkle", 1, _rendererCollection["Sparkle"], () => _rendererCollection["Sparkle"].gameObject.SetActive(false));
                getActiveAudioManager.PlayAudio("PotIncrement");
            }
            else if (GetScatterLevel(false) == 0)
            {
                UpdateGraphic("PotCoin", 0);
            }
        }

        private void ToggleAwarded()
        {
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PotCoinFeatureAwarded(potType);

            if (gm.GetUpcomingGameType().Contains(potType))
            {
                _previousScatterLevel = 0;
            }           
        }

        private void ResetPotShine()
        {
            if (IsAwarded())
            {
                _rendererCollection["PotShine"].gameObject.SetActive(false);
                _potSpriteRenderer.material.SetFloat(_potShineStrength, 0); // Stop shine
                UpdateGraphic("PotCoin", 0);
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = Vector2.zero;
                awardedRenderer.enabled = false;
            }           
        }

        private bool IsAwarded()
        {
            return awardedRenderer.enabled;
        }

        private void PlayFeatureTriggerAnim()
        {
            StopAllAnimation();

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            Debug.Log("Pot Play Feature Trigger Anim");
            List<JIXRYStateDataFlag> newParts = gm.GetNewFeatureList();
            bool toPlayAnim = false;
            bool isRecovery = false;

            if (newParts.Count > 0)
            {
                // Get min level
                int minLevel = int.MaxValue;
                foreach (JIXRYStateDataFlag part in newParts)
                {
                    int level = GetScatterLevelForPotType(part.ToString(), isRecovery);
                    if (level < minLevel)
                        minLevel = level;
                }

                int scatterLevel = GetScatterLevel(isRecovery);
                // Check if self needs to play animation and sfx
                foreach (JIXRYStateDataFlag part in newParts)
                {
                    string newFeature = part.ToString();

                    if (newFeature.Contains(potType))
                    {
                        string[] effect = { "Light", "PotCoin", "CoinBurst", "Sparkle" };
                        SetRendererVisible(effect, true);
                        getActiveAudioManager.PlayAudioUnique("PotTrigger");
                        // Only grow if lv < 5
                        if (scatterLevel < PotCoinMaxLevel)
                        {
                            getActiveAudioManager.PlayAudioUnique($"ScatterHitPot");
                            bool isLowest = scatterLevel <= minLevel;
                            PlayCoinPotTriggerAnimation(potType, _previousScatterLevel, 1, _rendererCollection["PotCoin"], isLowest, gm.FinishPotAnimation);
                            toPlayAnim = true;
                        }

                        PlayCoinBurstTrigger(_previousScatterLevel, 1, () => _rendererCollection["CoinBurst"].gameObject.SetActive(false));
                        PlayEffectAnimation("PotShine", -1, _rendererCollection["PotShine"]);
                        PlayEffectAnimation("Light", 1, _rendererCollection["Light"], () => _rendererCollection["Light"].gameObject.SetActive(false));
                        PlayEffectAnimation("Sparkle", 1, _rendererCollection["Sparkle"], () => _rendererCollection["Sparkle"].gameObject.SetActive(false));
                        _potSpriteRenderer.material.SetFloat(_potShineStrength, 1); // Play shine
                        awardedRenderer.enabled = gm.GetUpcomingGameType().Contains(potType);
                        awardedRenderer.material = language == WkGameLanguage.EN ? textShineMaterial[2] : textShineMaterial[3];
                    }
                }

                if (!toPlayAnim)
                {
                    WkCoroutine.instance.StartTrackedCoroutine(DelayTrigger());
                }
            }
        }

        private IEnumerator DelayTrigger()
        {
            yield return new WaitForEndOfFrame();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            gm.FinishPotAnimation();
        }

        private void PlayTriggerFinalAnimation()
        {
            StopAllAnimation();

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            List<JIXRYStateDataFlag> newParts = gm.GetNewFeatureList();
            foreach (JIXRYStateDataFlag part in newParts)
            {
                string newFeature = part.ToString();

                if (newFeature.Contains(potType))
                {
                    _rendererCollection["PotCoin"].Draw();
                    string animationKey = $"{potType}_potCoinFinalTrigger";

                    ReadOnlySpan<Texture2D> collection = null;
                    ReadOnlySpan<SSFrame> frames = ReadOnlySpan<SSFrame>.Empty;
                    ReadOnlySpan<int> totalSpriteInSS = _lightTotalSpriteInSS;


                    collection = glowCollection;
                    frames = _glowCollectionFramesCollection.ToArray();
                    totalSpriteInSS = _glowCollectionTotalSpriteInSS;

                    _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -0.49f);
                    PlayAnimation(frames, 1, animationKey, _rendererCollection["PotCoin"], collection, totalSpriteInSS, () => Enable_PotShine());

                    PlayCoinBurstTrigger(_previousScatterLevel, 1, () => _rendererCollection["CoinBurst"].gameObject.SetActive(false));
                    PlayEffectAnimation("PotShine", -1, _rendererCollection["PotShine"]);
                    PlayEffectAnimation("Light", 1, _rendererCollection["Light"], () => _rendererCollection["Light"].gameObject.SetActive(false));
                    PlayEffectAnimation("Sparkle", 1, _rendererCollection["Sparkle"], () => _rendererCollection["Sparkle"].gameObject.SetActive(false));
                    _potSpriteRenderer.material.SetFloat(_potShineStrength, 1); // Play shine
                    awardedRenderer.enabled = gm.GetUpcomingGameType().Contains(potType);
                    awardedRenderer.material = language == WkGameLanguage.EN ? textShineMaterial[2] : textShineMaterial[3];
                    PlayPotGlowDelayedSfx();
                }
            }
            WkCoroutine.instance.StartTrackedCoroutine(PlayFeatureTriggerSound());
        }

        private void SetRendererVisible(string[] type, bool visible)
        {
            foreach (string renderer in type)
            {
                _rendererCollection[renderer].gameObject.SetActive(visible);
            }          
        }

        private IEnumerator PlayFeatureTriggerSound()
        {
            yield return new WaitForSeconds(1.5f);
            getActiveAudioManager.PlayAudioUnique("FeatureTrigger");
        }

        private void Enable_PotShine()
        {
            _rendererCollection["PotShine"].gameObject.SetActive(true);
        }

        #region Get Values
        private int GetScatterLevelForPotType(string potTypeStr, bool isRecovery)
        {
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (potTypeStr.Contains("LW")) return (isRecovery) ? dm.savedBluePotScatter : dm.tempBluePotScatter;
            if (potTypeStr.Contains("JP")) return (isRecovery) ? dm.savedRedPotScatter : dm.tempRedPotScatter;
            if (potTypeStr.Contains("LB")) return (isRecovery) ? dm.savedGreenPotScatter : dm.tempGreenPotScatter;
            return 0;
        }

        private int GetScatterLevel(bool isRecovery)
        {
            int value = isRecovery ? GetSavedScatterValue() : GetTempScatterValue();

            for (int i = 0; i < ScatterThresholds.Length; i++)
            {
                if (value <= ScatterThresholds[i])
                    return i;
            }

            return ScatterThresholds.Length;
        }

        private int GetSavedScatterValue()
        {
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            int value = 0;
            
            if (potType == "RN")
            {
                value = dm.savedBluePotScatter;
            }
            else if (potType == "JP")
            {
                value = dm.savedRedPotScatter;
            }
            else if (potType == "RU")
            {
                value = dm.savedGreenPotScatter;
            }

            return value;
        }

        private int GetTempScatterValue()
        {
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            int value = 0;

            if (potType == "RN")
            {
                value = dm.tempBluePotScatter;
            }
            else if (potType == "JP")
            {
                value = dm.tempRedPotScatter;
            }
            else if (potType == "RU")
            {
                value = dm.tempGreenPotScatter;
            }

            return value;
        }
        #endregion

        #endregion

        #region Initialize Renderer
        private void SetUpRenderer()
        {
            _rendererCollection = new Dictionary<string, WkAdvanceRenderer>()
            {
                {"PotCoin", InstantiateRenderer("PotCoin")},
                {"CoinBurst", InstantiateRenderer("CoinBurst")},
                {"PotShine", InstantiateRenderer("PotShine")},
                {"Light", InstantiateRenderer("Light")},
                {"Sparkle", InstantiateRenderer("Sparkle")}
            };

            _rendererCollection["PotShine"].gameObject.transform.localPosition = new Vector2(0.2f, -1.4f);
            _rendererCollection["CoinBurst"].gameObject.transform.localPosition = new Vector2(0, -2.53f);
            _rendererCollection["Sparkle"].gameObject.transform.localPosition = new Vector2(0, -2f);

            _rendererCollection["PotCoin"].SetSortingOrder(PotCoinSortingOrder);
            _rendererCollection["Light"].SetSortingOrder(LightSortingOrder);
            _rendererCollection["CoinBurst"].SetSortingOrder(EffectSortingOrder);
            _rendererCollection["PotShine"].SetSortingOrder(EffectSortingOrder);           
            _rendererCollection["Sparkle"].SetSortingOrder(EffectSortingOrder);

            UpdateGraphic("PotCoin", 0);
            UpdateGraphic("CoinBurst");
            UpdateGraphic("PotShine");
            UpdateGraphic("Light");
            UpdateGraphic("Sparkle");

            string[] effect = { "Light", "PotShine", "CoinBurst", "Sparkle" };
            SetRendererVisible(effect, false);
            _potSpriteRenderer.material.SetFloat(_potShineStrength, 0);
        }

        private WkAdvanceRenderer InstantiateRenderer(string name)
        {
            GameObject go = new GameObject();
            WkAdvanceRenderer renderer = go.AddComponent<WkAdvanceRenderer>();
            go.transform.parent = potCoinPosition.transform;
            go.transform.localPosition = Vector3.zero;
            go.name = $"{name}_Renderer";
            return renderer;
        }

        private void UpdateGraphic(string graphic, int level=1)
        {
            Texture2D texture = null;
            SSFrame frame = new SSFrame();

            if(graphic == "PotCoin")
            {
                if (level == 0)
                {
                    texture = GetFirstTexture(graphic);
                    frame = GetFirstFrame(graphic);
                }
                else
                {
                    texture = GetLastTextureForLevel(level);
                    frame = GetLastFrameForLevel(level);
                }
            }
            else
            {
                texture = GetFirstTexture(graphic);
                frame = GetFirstFrame(graphic);
            }

            DrawImageInfo info = new DrawImageInfo();
            info.texture = texture;
            info.frame = frame;
            info.scale = Vector2.one;

            AddDrawInfoAndDraw(graphic, info);
        }

        private void AddDrawInfoAndDraw(string key, IDrawInfo info)
        {
            _rendererCollection[key].AddDrawInfo("Base", info);
            _rendererCollection[key].Draw();
        }

        private Texture2D GetLastTextureForLevel(int level)
        {
            if (!_potCoinLevelData.TryGetValue(level, out PotLevelAnimationData data))
            {
                WkAssert.EnsureMsgf(false, $"Invalid level {level}: No texture data found.");
                return null;
            }

            if (data.Textures == null || data.Textures.Length == 0)
            {
                WkAssert.EnsureMsgf(false, $"No textures found for level {level}.");
                return null;
            }

            return data.Textures[data.Textures.Length - 1];
        }

        private SSFrame GetLastFrameForLevel(int level)
        {
            if (!_potCoinLevelData.TryGetValue(level, out PotLevelAnimationData data))
            {
                WkAssert.EnsureMsgf(false, $"Invalid level {level}: No frame data found.");
                return new SSFrame();
            }

            if (data.Frames == null || data.Frames.Count == 0)
            {
                WkAssert.EnsureMsgf(false, $"No frames found for level {level}.");
                return new SSFrame();
            }

            return data.Frames[data.Frames.Count - 1];
        }

        private Texture2D GetFirstTexture(string graphic)
        {
            switch (graphic)
            {
                case "PotCoin":
                    _potCoinLevelData.TryGetValue(1, out PotLevelAnimationData potCoindata);
                    return potCoindata.Textures[0];
                case "CoinBurst":
                    _coinBurstLevelData.TryGetValue(1, out PotLevelAnimationData coinBurstdata);
                    return coinBurstdata.Textures[0];
                case "Sparkle":
                    return _previousScatterLevel < PotCoinMaxLevel ? sparkle04TextureCollection[0] : sparkle5TextureCollection[0];
                case "PotShine":
                    return potShineTextureCollection[0];
                case "Light":
                    return lightTextureCollection[0];               
                default:
                    return null;
            }          
        }

        private SSFrame GetFirstFrame(string graphic)
        {
            switch (graphic)
            {
                case "PotCoin":
                    _potCoinLevelData.TryGetValue(1, out PotLevelAnimationData potCoindata);
                    return potCoindata.Frames[0];
                case "CoinBurst":
                    _coinBurstLevelData.TryGetValue(1, out PotLevelAnimationData coinBurstdata);
                    return coinBurstdata.Frames[0];
                case "Sparkle":
                    return _previousScatterLevel < PotCoinMaxLevel ? _sparkle04FramesCollection[0] : _sparkle5FramesCollection[0];
                case "PotShine":
                    return _potShineFramesCollection[0];
                case "Light":
                    return _lightFramesCollection[0];              
                default:
                    return new SSFrame();
            }           
        }

        private void ClearAllDrawInfo()
        {
            foreach (WkAdvanceRenderer ar in _rendererCollection.Values)
            {
                ar.ClearAllDrawInfo();
            }
        }
        #endregion

        #region Animation

        #region Normal Increment Anim
        private void PlayCoinPotAnimation(string key, int level, int loopCount, WkAdvanceRenderer renderer, Action callback = null)
        {
            if (!_potCoinLevelData.TryGetValue(level, out PotLevelAnimationData data))
                return;

            renderer.Draw();
            string animationKey = $"{potType}_{key}_sparkle";

            ReadOnlySpan<Texture2D> collection = data.Textures;
            ReadOnlySpan<SSFrame> frames = data.Frames.ToArray();
            ReadOnlySpan<int> totalSpriteInSS = data.TotalSpritesPerTexture;
            if(level == PotCoinMaxLevel)
            {
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -0.49f);
            }
            PlayAnimation(frames, loopCount, animationKey, renderer, collection, totalSpriteInSS, callback);
        }

        private void PlayEffectAnimation(string key, int loopCount, WkAdvanceRenderer renderer, Action callback = null)
        {
            renderer.Draw();
            string animationKey = $"{potType}_{key}";

            ReadOnlySpan<Texture2D> collection = null;
            ReadOnlySpan<SSFrame> frames = ReadOnlySpan<SSFrame>.Empty;
            ReadOnlySpan<int> totalSpriteInSS = _lightTotalSpriteInSS;

            if (key == "Light")
            {
                UpdateGraphic("Light");
                collection = lightTextureCollection;
                frames = _lightFramesCollection.ToArray();
                totalSpriteInSS = _lightTotalSpriteInSS;
            }
            else if (key == "CoinBurst")
            {
                UpdateGraphic("CoinBurst");
                _coinBurstLevelData.TryGetValue(_previousScatterLevel, out PotLevelAnimationData data);
                collection = data.Textures;
                frames = data.Frames.ToArray();
                totalSpriteInSS = data.TotalSpritesPerTexture;
            }
            else if (key == "Sparkle")
            {
                UpdateGraphic("Sparkle");
                if (_previousScatterLevel == PotCoinMaxLevel)
                {
                    collection = sparkle5TextureCollection;
                    frames = _sparkle5FramesCollection.ToArray();
                    totalSpriteInSS = _sparkle5TotalSpriteInSS;
                }
                else
                {
                    collection = sparkle04TextureCollection;
                    frames = _sparkle04FramesCollection.ToArray();
                    totalSpriteInSS = _sparkle04TotalSpriteInSS;
                }
            }
            else if (key == "PotShine")
            {
                UpdateGraphic("PotShine");
                collection = potShineTextureCollection;
                frames = _potShineFramesCollection.ToArray();
                totalSpriteInSS = _potShineTotalSpriteInSS;
            }

            PlayAnimation(frames, loopCount, animationKey, renderer, collection, totalSpriteInSS, callback);
        }

        private void PlayCoinInAnimation(string key)
        {
            if (!key.Contains(potType)) return;

            _rendererCollection["CoinBurst"].Draw();
            _rendererCollection["CoinBurst"].gameObject.SetActive(true);
            UpdateGraphic("CoinBurst");

            ReadOnlySpan<Texture2D> collection = null;
            ReadOnlySpan<SSFrame> frames = ReadOnlySpan<SSFrame>.Empty;
            ReadOnlySpan<int> totalSpriteInSS = _lightTotalSpriteInSS;
            
            if (_previousScatterLevel == 0)
            {
                _coinBurstLevelData.TryGetValue(1, out PotLevelAnimationData data);
                collection = data.Textures;
                frames = data.Frames.ToArray();
                totalSpriteInSS = data.TotalSpritesPerTexture;
            }
            else
            {
                _coinBurstLevelData.TryGetValue(_previousScatterLevel, out PotLevelAnimationData data);
                collection = data.Textures;
                frames = data.Frames.ToArray();
                totalSpriteInSS = data.TotalSpritesPerTexture;
            }           
            PlayAnimation(frames, 1, key, _rendererCollection["CoinBurst"], collection, totalSpriteInSS, () => _rendererCollection["CoinBurst"].gameObject.SetActive(false));
        }
        #endregion

        #region Special Increment Anim
        private void PlayCoinPotTriggerAnimation(string key, int level, int loopCount, WkAdvanceRenderer renderer, bool isLowest, Action callback = null)
        {
            renderer.Draw();
            string animationKey = $"{potType}_{key}_potCoinTrigger";

            ReadOnlySpan<Texture2D> collection;
            ReadOnlySpan<SSFrame> frames;
            ReadOnlySpan<int> totalSpriteInSS;
            _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -0.49f);
            if (level==0)
            {
                collection = potCoin05TextureCollection;
                frames = _potCoin05FramesCollection.ToArray();
                totalSpriteInSS = _potCoin05TotalSpriteInSS;
            }
            else if (level==1)
            {
                collection = potCoin15TextureCollection;
                frames = _potCoin15FramesCollection.ToArray();
                totalSpriteInSS = _potCoin15TotalSpriteInSS;
            }
            else if (level == 2)
            {
                collection = potCoin25TextureCollection;
                frames = _potCoin25FramesCollection.ToArray();
                totalSpriteInSS = _potCoin25TotalSpriteInSS;
            }
            else if (level == 3)
            {
                collection = potCoin35TextureCollection;
                frames = _potCoin35FramesCollection.ToArray();
                totalSpriteInSS = _potCoin35TotalSpriteInSS;
            }
            else if (level == 4)
            {
                _potCoinLevelData.TryGetValue(5, out PotLevelAnimationData data);
                collection = data.Textures;
                frames = data.Frames.ToArray();
                totalSpriteInSS = data.TotalSpritesPerTexture;
            }
            else
            {
                Debug.LogWarning($"Level {level} is not a valid level.");
                return;
            }

            PlayPotAnimationWithSound(frames, loopCount, animationKey, renderer, collection, totalSpriteInSS, level, isLowest, callback);
        }

        private void PlayCoinBurstTrigger(int level, int loopCount, Action callback = null)
        {
            WkAdvanceRenderer renderer = _rendererCollection["CoinBurst"];
            renderer.Draw();
            // Ensure the renderer is active before starting the animation
            renderer.gameObject.SetActive(false);
            renderer.gameObject.SetActive(true);
            string animationKey = $"{potType}_CoinBurstTrigger";

            ReadOnlySpan<Texture2D> collection = null;
            ReadOnlySpan<SSFrame> frames = ReadOnlySpan<SSFrame>.Empty;
            ReadOnlySpan<int> totalSpriteInSS = _lightTotalSpriteInSS;

            if (level == 0)
            {
                collection = coinBurst05TextureCollection;
                frames = _coinBurst05FramesCollection.ToArray();
                totalSpriteInSS = _coinBurst05TotalSpriteInSS;
            }
            else if (level == 1)
            {
                collection = coinBurst15TextureCollection;
                frames = _coinBurst15FramesCollection.ToArray();
                totalSpriteInSS = _coinBurst15TotalSpriteInSS;
            }
            else if (level == 2)
            {
                collection = coinBurst25TextureCollection;
                frames = _coinBurst25FramesCollection.ToArray();
                totalSpriteInSS = _coinBurst25TotalSpriteInSS;
            }
            else if (level == 3)
            {
                collection = coinBurst35TextureCollection;
                frames = _coinBurst35FramesCollection.ToArray();
                totalSpriteInSS = _coinBurst35TotalSpriteInSS;
            }
            else if (level == 4 || level == PotCoinMaxLevel)
            {
                _coinBurstLevelData.TryGetValue(5, out PotLevelAnimationData data);
                collection = data.Textures;
                frames = data.Frames.ToArray();
                totalSpriteInSS = data.TotalSpritesPerTexture;
            }

            PlayAnimation(frames, loopCount, animationKey, renderer, collection, totalSpriteInSS, callback);
        }
        #endregion

        private IEnumerator PlayPotIncreaseSfx(float delay)
        {
            yield return new WaitForSeconds(delay);
            WkAudioManager.instance.PlayAudioUnique("ScatterHitPot");
        }

        private void PlayPotGlowDelayedSfx()
        {
            WkAudioManager.instance.PlayAudioUnique("PotTriggerDelayed");
        }

        /// <summary>
        /// Modified PlayAnimation() to start multiple coroutines to play "ScatterHitPot" whenever pot finishes a set of pot increasing animation
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="loopCount"></param>
        /// <param name="animationKey"></param>
        /// <param name="renderer"></param>
        /// <param name="collection"></param>
        /// <param name="totalSpriteInSS"></param>
        /// <param name="callback"></param>
        private void PlayPotAnimationWithSound(ReadOnlySpan<SSFrame> frames, int loopCount, string animationKey, WkAdvanceRenderer renderer,
                                    ReadOnlySpan<Texture2D> collection, ReadOnlySpan<int> totalSpriteInSS, int numOfPotIncreaseAnimations, bool isLowest, Action callback = null)
        {
            const int keyFrameAmount = 1;

            int start = 0;
            int end = frames.Length;
            int framePerSecond = 24;
            float animationDuration = (float)end / framePerSecond;
            WkAnimationKeyFrame<int>[] array = new WkAnimationKeyFrame<int>[keyFrameAmount]
            {
                new WkAnimationKeyFrame<int>(start, end, animationDuration)
            };

            Stack<WkAnimationKeyFrame<int>> keyFrameCollection = new Stack<WkAnimationKeyFrame<int>>(array);
            WkAnimationClip<int> clip = new WkAnimationClip<int>(loopCount, keyFrameCollection);

            WkSpriteAnimatorTask spriteTask = new WkSpriteAnimatorTask(animationKey, "Base", renderer, collection.ToArray(), totalSpriteInSS.ToArray(), frames, clip, callback);

            CallAnimatorPlayAnimation(spriteTask);
            _animationKeyCollection.Add(animationKey);

            if (!isLowest) return;

            // Start "numOfInterval" coroutines to play "ScatterHitPot" SFX at even interval after the first pot increase animation
            int numOfInterval = PotCoinMaxLevel - numOfPotIncreaseAnimations;
            float playSfxInterval = animationDuration / numOfInterval;
            for (int i = 0; i < numOfInterval;  i++)
            {
                WkCoroutine.instance.StartTrackedCoroutine(PlayPotIncreaseSfx(i * playSfxInterval));
            }
        }

        /// <summary>
        /// Generic PlayAnimation.
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="loopCount"></param>
        /// <param name="animationKey"></param>
        /// <param name="renderer"></param>
        /// <param name="collection"></param>
        /// <param name="totalSpriteInSS"></param>
        /// <param name="callback"></param>
        private void PlayAnimation(ReadOnlySpan<SSFrame> frames, int loopCount, string animationKey, WkAdvanceRenderer renderer, 
                                    ReadOnlySpan<Texture2D> collection, ReadOnlySpan<int> totalSpriteInSS, Action callback = null)
        {
            const int keyFrameAmount = 1;

            int start = 0;
            int end = frames.Length;
            int framePerSecond = 24;
            float animationDuration = (float)end / framePerSecond;
            WkAnimationKeyFrame<int>[] array = new WkAnimationKeyFrame<int>[keyFrameAmount]
            {
                new WkAnimationKeyFrame<int>(start, end, animationDuration)
            };

            Stack<WkAnimationKeyFrame<int>> keyFrameCollection = new Stack<WkAnimationKeyFrame<int>>(array);
            WkAnimationClip<int> clip = new WkAnimationClip<int>(loopCount, keyFrameCollection);

            WkSpriteAnimatorTask spriteTask = new WkSpriteAnimatorTask(animationKey, "Base", renderer, collection.ToArray(), totalSpriteInSS.ToArray(), frames, clip, callback);

            CallAnimatorPlayAnimation(spriteTask);
            _animationKeyCollection.Add(animationKey);
        }

        private void CallAnimatorPlayAnimation(IWkAnimatorTask task)
        {
            WkAnimator animator = WkAnimator.instance!;
            WkAssert.EnsureMsgf(animator, "Missing animator.");
            animator.Play(task);
        }

        private void StopAllAnimation()
        {
            WkAnimator animator = WkAnimator.instance;
            if (animator is null) return;

            foreach (string s in _animationKeyCollection)
            {
                animator.StopTaskByKey(s);
            }

            _animationKeyCollection.Clear();

            string[] otherAnims = { "CoinBurst", "Light", "Sparkle" };
            foreach (string t in otherAnims)
            {
                if (_rendererCollection.TryGetValue(t, out WkAdvanceRenderer renderer))
                    renderer.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Deserialize Json & Build Anim Data Dictionary
        private (List<SSFrame> frames, int[] totalSprites) ProcessJsonCollection(TextAsset[] jsonCollection)
        {
            List<SSFrame> framesCollection = new List<SSFrame>();
            int[] totalSpritesInSS = new int[jsonCollection.Length];

            for (int i = 0; i < jsonCollection.Length; i++)
            {
                int count = 0;
                TextAsset json = jsonCollection[i] ?? throw new Exception();

                JObject loadedJson = JObject.Parse(json.text);
                if (!WkAssert.EnsureMsgf(loadedJson, "Failed to load JSON Obj"))
                    return (framesCollection, totalSpritesInSS);

                loadedJson.TryGetValue("frames", out JToken? allFramesToken);
                JObject allFrames = allFramesToken as JObject ?? throw new NullReferenceException();

                foreach (JProperty frame in allFrames.Children<JProperty>())
                {
                    JProperty? frameDetail = frame.Value.First as JProperty;
                    if (frameDetail?.Value is not JObject frameObj) continue;

                    float x = (float)frameObj.GetValue("x")!;
                    float y = (float)frameObj.GetValue("y")!;
                    float w = (float)frameObj.GetValue("w")!;
                    float h = (float)frameObj.GetValue("h")!;

                    SSFrame ssFrame = new(x, y, w, h);
                    framesCollection.Add(ssFrame);
                    count++;
                }

                totalSpritesInSS[i] = count;
            }

            return (framesCollection, totalSpritesInSS);
        }

        private Dictionary<int, PotLevelAnimationData> BuildLevelAnimationDictionary(Texture2D[] textureCollection, List<SSFrame> framesCollection, int[] totalSpritesInSS, int texturesPerLevel)
        {
            Dictionary<int, PotLevelAnimationData> levelDataDict = new Dictionary<int, PotLevelAnimationData>();

            int levelCount = totalSpritesInSS.Length / texturesPerLevel;
            int frameIndex = 0;

            for (int level = 1; level <= levelCount; level++)
            {
                int baseIndex = (level - 1) * texturesPerLevel;

                // Extract textures for this level
                Texture2D[] levelTextures = new Texture2D[texturesPerLevel];
                Array.Copy(textureCollection, baseIndex, levelTextures, 0, texturesPerLevel);

                // Extract total sprites per texture
                int[] totalSpritesPerTexture = new int[texturesPerLevel];
                Array.Copy(totalSpritesInSS, baseIndex, totalSpritesPerTexture, 0, texturesPerLevel);

                // Collect the frames for this level
                int totalFrameCount = totalSpritesPerTexture.Sum();
                List<SSFrame> levelFrames = new List<SSFrame>(totalFrameCount);

                foreach (int spriteCount in totalSpritesPerTexture)
                {
                    for (int i = 0; i < spriteCount; i++)
                    {
                        levelFrames.Add(framesCollection[frameIndex++]);
                    }
                }

                // Create data object
                PotLevelAnimationData data = new PotLevelAnimationData
                {
                    Textures = levelTextures,
                    Frames = levelFrames,
                    TotalSpritesPerTexture = totalSpritesPerTexture
                };

                levelDataDict[level] = data;
            }

            return levelDataDict;
        }
        #endregion
    }
}