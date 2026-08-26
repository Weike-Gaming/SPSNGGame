using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    enum SpawnStyle
    {
        Shower = 0,     // Randomised spray of ingots to 5x3 destinations.
        Interval,       // Set spray of X num of ingots every Y seconds to set number of destinations.
        Sweep,          // Stream of ingots moving to a destination point that follows a spline path.
        SweepGrid,      // Modified Interval that shoots single ingot to each 5x3 destinations.
        CenterLineX3,   // Modified Interval that shoots single ingot to center row from left to right 3 times.
        AltCenterLineX3,// Modified CenterLineX3 to have additional effects before & after
        AltCenterLineX3WithOrb,// Modified CenterLineX3 to have additional effects before & after & starts with orb anim
    }

    /// <summary>
    /// JIXRY Small Ingot Display Object will call this script to run when supposed to play the "SPIN" pre-spin animation.
    /// </summary>
    public class JIXRYReelNudgePreSpinDisplayObject : WkDisplayObject
    {
        [Serializable]
        class Ingot
        {
            // Set once in PostInitialized
            public GameObject wrapper;      // Parent of the 3 children below
            public GameObject ingot;        // Child, holds ingot sprite
            public GameObject trail;        // Child, trail for the ingot
            public GameObject impactStart;  // Child, particle explosion effect
            public GameObject impactEnd;    // child, particle explosion effect
            public GameObject impactEndDelayed;    // child, particle explosion effect

            public int index;               // index in _ingotPool array

            // (Re)Set in TryToSpawn.SetUpIngot
            public float aliveElapsed;
            public float aliveDuration;
            public float fadeElapsed;
            public Vector3 startPosition;
            public Transform destination;
            public bool hasReachedDestination;  // false from TryToSpawn till reach destination, then true
        }

        private JIXRYSmallIngotDisplayObject _controller = null; // Game Object that has animationTime & toggle this script to play

        private bool _postInitialized = false;      // bool Check to accidental prevent early spawning by Update()
        private float _elapsedTimeTotal = 0f;       // Timer for TotalDuration
        private const float IngotLingerDuration = 0f; // How long the ingot's sub game objects remain active after hasReachedDestination
        private float _modifiedIngotLingerDuration = 0f;
        private const float IngotFadeDuration = 0.4f;  // How long the ingot plays it's fade out
        private float _modifiedIngotFadeDuration = 0f;

        private bool _styleInitialized = false;

        #region Menu Button Control
        [Header("")]
        [Header("MENU SETTINGS")]
        [SerializeField] private bool allowMenuOnDemoNudge = false; // Let menu be seen on Demo > Nudge
        [SerializeField] private GameObject parentToDemoOptions = null;
        private bool _menuEnabled = false;
        [SerializeField] private UnityEngine.UI.Button styleToggle;
        [SerializeField] private TextMeshProUGUI styleToggleText;
        [SerializeField] private UnityEngine.UI.Button yesButton; // Aka "Set to 1 FreeGame"
        [SerializeField] private UnityEngine.UI.Button noButton; // Aka "Normal Game"
        #endregion

        [Header("")]
        [Header("OBJECT POOL SETTINGS")]
        [Tooltip("Doesn't dynamically change on runtime.")]
        [SerializeField] private int poolSize = 30;
        [Tooltip("Parent for instantiated ingots.")]
        [SerializeField] private GameObject parentIngots = null;
        [Tooltip("Prefab to instantiate.")]
        [SerializeField] private GameObject ingotPrefab = null;
        private int _poolIndex = 0;                                  // Circular index used in _ingotPool for identifing which ingot to "spawn"
        private Ingot[] _ingotPool;                                  // Array where all the ingots are stored & accessed
        private List<Ingot> _activeIngots = new List<Ingot>();       // List of all active ingots

        [Header("")]
        [Header("INGOT SPAWNING SETTINGS")]
        [Tooltip("Primary control for spawning ingots.")]
        public bool tryToSpawn = false;         // Also controlled by JIXRYSmallIngotDisplayObject when to start/stop the pre-spin animation
        [Tooltip("0: Shower for animDuration w/ AnimationCurve\n1: Pairs into reels")]
        [SerializeField] private SpawnStyle spawnStyle = SpawnStyle.Shower;
        public float animDuration = 5f;         // Set by JIXRYSmallIngotDisplayObject.Awake. Used as a reference to RN PreSpin duration.
        
        #region Shower Style Variables
        [Header("Shower Settings")]
        [Tooltip("Control ingot spawn rate over course of \"animDuration\"")]
        [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Constant(0, 1, 5f); // Bursts per second over normalised time
        [Tooltip("Min amount of ingots to spawn per interval.\nAffected by \"spawnRateCurve\"")]
        [SerializeField] int spawnMin = 1;
        [Tooltip("Max amount of ingots to spawn per interval.\nAffected by \"spawnRateCurve\"")]
        [SerializeField] int spawnMax = 3;
        private float _spawnAccumulator = 0f;   // Accumulator for probability‑based spawning
        [Tooltip("Override \"animDuration\" to allow constant ingot spawning.\nEffectively locks duration to Zero.")]
        [SerializeField] private bool infiniteSpawn= false;
        private bool _playPresetSfxOnce = false;
        private const bool ShowerUseAltDestination = false;
        #endregion

        #region Interval Style Variables
        [Header("Interval Settings")]
        [Tooltip("Limit how many destinations to use out of parentDestinations.children.")]
        [SerializeField] int numOfDestinationsToUse = 5;
        [Tooltip("Time delay between each interval.")]
        [SerializeField] float delayBetweenPairs = 0.2f;
        [Tooltip("How many to spawn per interval.")]
        [SerializeField] int numToSpawnPerInterval = 2;
        private float _localAnimDuration;       // Local copy of animDuration to modify for reel border timing
        private int _pairsSpawned;              // Number of pairs already spawned
        private int _totalPairs;                // Total number of pairs to spawn (destinations.Length / 2)
        private float _nextSpawnTime;           // Time (in seconds) when the next pair should spawn
        private const bool IntervalUseAltDestination = false;
        #endregion

        #region Sweep Grid Style Variables
        [Header("Sweeping Grid Settings.")]
        [Tooltip("Time delay between each interval.")]
        [SerializeField] private float delayBetweenIngot = 0.2f;
        private int _sweepGridIngotSpawned;
        private const bool SweepGridUseAltDestination = true;
        #endregion

        #region Sweep Style Variables
        [Header("Sweeping Settings")]
        [Tooltip("ParticleSystem Emission Rate. Default 10.")]
        [SerializeField] private float emissionRate = 10f;
        [SerializeField] private ParticleSystem sweepStyleParticleSystem = null;         // Spawn the particles (which will be ingots)

        [Tooltip("Follower.")]
        [SerializeField] private SplineFollower splineFollower = null;
        [Tooltip("Follower use speed or time to follow.")]
        [SerializeField] private bool useTimeNotSpeed = true;
        [Tooltip("Follower speed.")]
        [SerializeField] private float splineFollowerSpeed = 10f;
        [Tooltip("Follower time/duration. Defaults to \"animDuration\".")]
        [SerializeField] private float splineFollowerDuration = 5f;
        #endregion

        #region CenterLineX3 Style Variables
        private const bool CenterLineUseAltDestination = false;
        private int _centerLineIngotSpawned = 0;
        private int _centerLineDestinationIndex = 0;
        #endregion

        #region AltCenterLineX3 Style Variables
        private const bool AltCenterLineUseAltDestination = false;
        private int _altCenterLineIngotSpawned = 0;
        private int _altCenterLineDestinationIndex = 0;
        [SerializeField] private Animator animatorLightBurst = null;
        [SerializeField] private SpriteRenderer lightBurst = null;
        [SerializeField] private SpriteRenderer lightBurstBack = null;
        private bool _triggerLightBurst = false;         // Trigger the light burst GO
        private const float LightBurstRotationSpeed = 200f;
        private bool _isVisibleLightBurst = false;      // Check for if the GO is playing animation
        private const float DurationLight = 0.7f;       // LightBurst animation will play for 2x this amount due to how it's coded
        private float _elapsedLightLinger = 0f;         // Timer that, when > Duration, calls Stop()
        private bool _isVisibleLightBurstBack = false;  // Check for if the GO is playing animation
        private float _elapsedLightBackLinger = 0f;
        private bool _lightBurstSfxPlayed = false;

        [SerializeField] private SplineFollower followerOrb = null;
        [SerializeField] private SplineFollower followerOrbTrail = null;
        private bool _orbReachedEnd = false;

        #endregion

        [Header("")]
        [Header("INGOT MOVEMENT SETTINGS")]
        [SerializeField] private Transform startPos;                // Position of pot
        [Tooltip("Destinations are set up as children of this as:\n[5 6 7 8 9]\n[0 1 2 3 4]\n[10 11 12 13 14]\nUsed by Shower style.")]
        [SerializeField] private Transform parentDestinations;      // Parent Positions where ingots can end - Currently set in unity to be 5x3
        private Transform[] _destinations;
        [Tooltip("Destinations are set up as children of this as:\n[5 6 0 7 1]\n[10 8 2 11 9]\n[3 12 4 13 14]\nUsed by SweepGrid style.")]
        [SerializeField] private Transform parentDestinationsSweepGrid;      // Parent Positions where ingots can end - Currently set in unity to be 5x3
        private Transform[] _destinationsSweep;
        private const float CurveStrength = 1f;
        [SerializeField] private float minTravelTime = 0.5f;        // Min time for ingots to travel
        [SerializeField] private float maxTravelTime = 0.5f;        // Max time for ingots to travel

        [Header("")]
        [Header("CURVE SETTINGS")]
        [SerializeField] private float curveHeight = 2f;
        [SerializeField] private float curveOffset = 0.5f;

        [Header("")]
        [Header("REEL BORDER SETTINGS")]
        [Tooltip("Whether to use reel borders during the animation. If false, reel borders will be disabled and won't be turned on at all.")]
        [SerializeField] private bool useReelBorder = true;
        [Tooltip("Parent for reel borders.")]
        [SerializeField] private Transform parentReelBorder;
        private const int ReelBorderCount = 5;
        private GameObject[] _reelBorders;
        private bool[] _reelBordersActive;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            _controller = FindObjectOfType<JIXRYSmallIngotDisplayObject>();

            // Set Menu Buttons
            AddMenuListeners();
            DisableMenu();

            // Instantiate poolSize copies of the prefab
            _ingotPool = new Ingot[poolSize];
            for (int i = 0; i < poolSize; ++i)
            {
                GameObject newIngotWrapper = Instantiate(ingotPrefab, parentIngots.transform);
                newIngotWrapper.name = $"Ingot_{i:00}";
                newIngotWrapper.SetActive(false);

                _ingotPool[i] = new Ingot();
                _ingotPool[i].wrapper = newIngotWrapper;
                _ingotPool[i].index = i;

                // Get grandchildren by name
                foreach (Transform grandchild in newIngotWrapper.transform)
                {
                    if (grandchild.name.Contains("Sprite"))
                        _ingotPool[i].ingot = grandchild.gameObject;
                    else if (grandchild.name.Contains("Trail"))
                        _ingotPool[i].trail = grandchild.gameObject;
                    else if (grandchild.name.Contains("ImpactStart"))
                        _ingotPool[i].impactStart = grandchild.gameObject;
                    else if (grandchild.name.Contains("ImpactEnd"))
                        _ingotPool[i].impactEnd = grandchild.gameObject;
                    else if (grandchild.name.Contains("ImpactDelayed"))
                        _ingotPool[i].impactEndDelayed = grandchild.gameObject;
                    else
                        Debug.LogWarning($"Found grandchild of IngotPool which doesn't belong. GO name is: {grandchild.name}.");
                }
            }

            // Initialize _destinations
            int childCountDestinations = parentDestinations.transform.childCount;
            _destinations = new Transform[childCountDestinations];
            for (int i = 0; i < childCountDestinations; ++i)
            {
                _destinations[i] = parentDestinations.transform.GetChild(i);
            }

            // Initialize _destinationsSweep
            childCountDestinations = parentDestinationsSweepGrid.transform.childCount;
            _destinationsSweep = new Transform[childCountDestinations];
            for (int i = 0; i < childCountDestinations; ++i)
            {
                _destinationsSweep[i] = parentDestinationsSweepGrid.transform.GetChild(i);
            }

            // Initialize _reelBorders
            int childCountReelBorder = parentReelBorder.transform.childCount;
            _reelBorders = new GameObject[ReelBorderCount];
            _reelBordersActive = new bool[ReelBorderCount];
            for (int i = 0; i < childCountReelBorder; ++i)
            {
                _reelBorders[i] = parentReelBorder.transform.GetChild(i).gameObject;
                SetReelBorder(i, false);

                // Disable reel borders if not using them.
                if (!useReelBorder)
                    _reelBorders[i].SetActive(false);
            }

            // Set up listener for Sweep Style splineFollower
            SubscribeEvent();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            wkModelCollection.AddModel(dm);
            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("idle")!.onExitState += DisableMenu;
            getState("fg-spin")!.onRecoverState += ResetToDefault;
            getState("fg-spin")!.onExitState += ResetToDefault;

            _postInitialized = true;
        }

        #region Menu Button Control
        private void DisableMenu()
        {
            _menuEnabled = false;
            parentToDemoOptions.SetActive(false);
        }

        public void TryToEnableMenu()
        {
            if (!allowMenuOnDemoNudge) return;
            if (_menuEnabled) return;
            _menuEnabled = true;
            parentToDemoOptions.SetActive(true);
        }

        private void AddMenuListeners()
        {
            if (styleToggle != null)
            {
                styleToggleText.text = $"{spawnStyle}";

                styleToggle.onClick.AddListener(() =>
                {
                    // Cycle to the next spawn style
                    int nextStyle = ((int)spawnStyle + 1) % Enum.GetValues(typeof(SpawnStyle)).Length;
                    spawnStyle = (SpawnStyle)nextStyle;
                    styleToggleText.text = $"{spawnStyle}";

                    // Change anim duration if using AltCenterLineX3
                    if (spawnStyle == SpawnStyle.AltCenterLineX3WithOrb)
                        _controller.animDelay[0] = 6f;
                    else
                        _controller.animDelay[0] = 5f;
                });
            }

            if (yesButton != null)
            {
                yesButton.onClick.AddListener(() =>
                {
                    JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
                    gm.modifiedInitialFreeGameCount = 1;
                    DisableMenu();
                });
            }

            if (noButton != null)
            {
                noButton.onClick.AddListener(() =>
                {
                    DisableMenu();
                });
            }
        }
        #endregion

        private void Update()
        {
            if (!_postInitialized) return;

            UpdateActiveObjects();
            UpdateLightBurstAnimation();

            if (tryToSpawn)
            {
                switch (spawnStyle)
                {
                    case SpawnStyle.Shower:
                        StyleShower();
                        break;
                    case SpawnStyle.Interval:
                        StyleInterval();
                        break;
                    case SpawnStyle.Sweep:
                        StyleSweep();
                        break;
                    case SpawnStyle.SweepGrid:
                        StyleSweepGrid();
                        break;
                    case SpawnStyle.CenterLineX3:
                        StyleCenterLineX3();
                        break;
                    case SpawnStyle.AltCenterLineX3:
                        StyleAltCenterLineX3();
                        break;
                    case SpawnStyle.AltCenterLineX3WithOrb:
                        StyleAltCenterLineX3WithOrb();
                        break;
                }
            }
        }

        #region Style AltCenterLineX3
        private void StyleAltCenterLineX3WithOrb()
        {
            if (_orbReachedEnd)
                _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;
                _localAnimDuration = 0.9f * (animDuration - followerOrb.followDuration); // Leave 10% of time at the end for reel border animation
                _nextSpawnTime = 0f; // Spawn the first pair immediately
                _altCenterLineIngotSpawned = 0;
                _altCenterLineDestinationIndex = 0;
                _modifiedIngotLingerDuration = 0.7f;
                _modifiedIngotFadeDuration = 0.1f;
                ResetLightAnimations();

                followerOrb.gameObject.SetActive(true);
                followerOrb.SetPercent(0f);
                followerOrb.enabled = true;
                followerOrbTrail.gameObject.SetActive(true);
                followerOrbTrail.SetPercent(0f);
                followerOrbTrail.enabled = true;
                _orbReachedEnd = false;
            }

            if (_orbReachedEnd)
            {
                while (_altCenterLineIngotSpawned < 15 && _elapsedTimeTotal >= _nextSpawnTime)
                {
                    // Trigger LightBurst
                    if (_altCenterLineIngotSpawned == 0)
                        TriggerLightBurst(_destinations[0]);
                    if (_altCenterLineIngotSpawned == 14)
                        TriggerLightBurst(_destinations[4]);

                    // TryToSpawn numToSpawnPerInterval ingots, with delayBetweenPairs between them, alive duration based on animDuration and total pairs, to the same destination
                    TryToSpawn(1, delayBetweenIngot, _localAnimDuration / 15, _altCenterLineDestinationIndex, AltCenterLineUseAltDestination);
                    _altCenterLineIngotSpawned++;
                    _altCenterLineDestinationIndex = (++_altCenterLineDestinationIndex > 4) ? 0 : _altCenterLineDestinationIndex; // use _destination 0 to 5
                    _nextSpawnTime += _localAnimDuration / 15;
                }
            }
        }

        #region LightBurst

        /// <summary>
        /// Use _triggerLightBurst to trigger the animation
        /// </summary>
        /// <param name="pos"></param>
        private void TriggerLightBurst(Transform pos)
        {
            _triggerLightBurst = true;
            lightBurst.transform.position = pos.position;
            lightBurstBack.transform.position = pos.position;
        }

        /// <summary>
        /// Wait for _triggerLightBurst to be set, then play the light burst animation
        /// </summary>
        private void UpdateLightBurstAnimation()
        {
            // Force reset animation
            if (_triggerLightBurst && (_isVisibleLightBurst || _isVisibleLightBurstBack))
            {
                ResetLightAnimations();
            }

            // Initialise
            if (_triggerLightBurst && !_isVisibleLightBurst && !_isVisibleLightBurstBack)
            {
                _triggerLightBurst = false;
                _isVisibleLightBurst = true;
                _elapsedLightLinger = 0f;
                _elapsedLightBackLinger = 0f;
                _lightBurstSfxPlayed = false;

                PlayLightBurst();
            }

            // Play LightBurst then LightBurstBack
            if (!_triggerLightBurst && (_isVisibleLightBurst || _isVisibleLightBurstBack))
            {
                lightBurst.transform.Rotate(0, 0, LightBurstRotationSpeed * Time.deltaTime);
                lightBurstBack.transform.Rotate(0, 0, LightBurstRotationSpeed * Time.deltaTime);

                if (_isVisibleLightBurst)
                {
                    _elapsedLightLinger += Time.deltaTime;
                    if (_elapsedLightLinger > DurationLight)
                    {
                        _isVisibleLightBurst = false;
                        StopLightBurst();

                        //_isVisibleLightBurstBack = true;
                        //PlayLightBurstBack();
                    }
                }

                if (_isVisibleLightBurstBack && _elapsedLightLinger > DurationLight)
                {
                    _elapsedLightBackLinger += Time.deltaTime;
                    if (_elapsedLightBackLinger > DurationLight)
                    {
                        _isVisibleLightBurstBack = false;
                        //StopLightBurstBack();
                    }
                }
            }
        }

        /// <summary>
        /// Play 'Appear' animation & set active to LightBurst
        /// </summary>
        /// <param name="index"></param>
        private void PlayLightBurst()
        {
            lightBurst.gameObject.SetActive(true);
            animatorLightBurst.Play("LightBurst_Appear_Animation");

            if (!_lightBurstSfxPlayed)
            {
                _lightBurstSfxPlayed = true;
                getActiveAudioManager.PlayAudio("IngotIn");
            }
        }
        private void StopLightBurst()
        {
            animatorLightBurst.Play("LightBurst_Disappear_Animation");
        }

        /// <summary>
        /// Play 'Appear' animation & set active to LightBurstBack
        /// </summary>
        /// <param name="index"></param>
        private void PlayLightBurstBack()
        {
            lightBurstBack.gameObject.SetActive(true);
            animatorLightBurst.Play("LightBurstBack_Appear_Animation");
        }
        private void StopLightBurstBack()
        {
            _isVisibleLightBurst = false;
            animatorLightBurst.Play("LightBurstBack_Disappear_Animation");
        }

        /// <summary>
        /// Disable both LightBurst & LightBurstBack
        /// </summary>
        private void ResetLightAnimations()
        {
            animatorLightBurst.Play("LightBurst_Idle_Animation");
            _isVisibleLightBurst = false;
            _elapsedLightLinger = 0f;

            animatorLightBurst.Play("LightBurstBack_Idle_Animation");
            _isVisibleLightBurstBack = false;
            _elapsedLightBackLinger = 0f;
        }
        #endregion
        
        private void StyleAltCenterLineX3()
        {
            _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;
                _localAnimDuration = 0.9f * animDuration; // Leave 10% of time at the end for reel border animation
                _nextSpawnTime = 0f; // Spawn the first pair immediately
                _altCenterLineIngotSpawned = 0;
                _altCenterLineDestinationIndex = 0;
                _modifiedIngotFadeDuration = 1.0f;
                ResetLightAnimations();
            }

            while (_altCenterLineIngotSpawned < 15 && _elapsedTimeTotal >= _nextSpawnTime)
            {
                // Trigger LightBurst
                if (_altCenterLineIngotSpawned == 0)
                    TriggerLightBurst(_destinations[0]);
                if (_altCenterLineIngotSpawned == 14)
                    TriggerLightBurst(_destinations[4]);

                // TryToSpawn numToSpawnPerInterval ingots, with delayBetweenPairs between them, alive duration based on animDuration and total pairs, to the same destination
                TryToSpawn(1, delayBetweenIngot, _localAnimDuration / 15, _altCenterLineDestinationIndex, AltCenterLineUseAltDestination);
                _altCenterLineIngotSpawned++;
                _altCenterLineDestinationIndex = (++_altCenterLineDestinationIndex > 4) ? 0 : _altCenterLineDestinationIndex; // use _destination 0 to 5
                _nextSpawnTime += _localAnimDuration / 15;
            }
        }
        #endregion

        private void StyleCenterLineX3()
        {
            _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;
                _localAnimDuration = 0.9f * animDuration; // Leave 10% of time at the end for reel border animation
                _nextSpawnTime = 0f; // Spawn the first pair immediately
                _centerLineIngotSpawned = 0;
                _centerLineDestinationIndex = 0;
            }

            while (_centerLineIngotSpawned < 15 && _elapsedTimeTotal >= _nextSpawnTime)
            {
                // TryToSpawn numToSpawnPerInterval ingots, with delayBetweenPairs between them, alive duration based on animDuration and total pairs, to the same destination
                TryToSpawn(1, delayBetweenIngot, _localAnimDuration / 15, _centerLineDestinationIndex, CenterLineUseAltDestination);
                _centerLineIngotSpawned++;
                _centerLineDestinationIndex = (++_centerLineDestinationIndex > 4) ? 0 : _centerLineDestinationIndex; // use _destination 0 to 5
                _nextSpawnTime += _localAnimDuration / 15;
            }
        }

        /// <summary>
        /// Uses modified _destinations with StyleInterval spawning
        /// </summary>
        private void StyleSweepGrid()
        {
            _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;
                _localAnimDuration = 0.9f * animDuration; // Leave 10% of time at the end for reel border animation
                _nextSpawnTime = 0f; // Spawn the first pair immediately
                _sweepGridIngotSpawned = 0;
            }

            // Spawn pairs as long as we haven't finished and the next spawn time has been reached
            // Enable reel border for the corresponding reel when its pair starts spawning
            while (_sweepGridIngotSpawned < 15 && _elapsedTimeTotal >= _nextSpawnTime)
            {
                // TryToSpawn numToSpawnPerInterval ingots, with delayBetweenPairs between them, alive duration based on animDuration and total pairs, to the same destination
                TryToSpawn(1, delayBetweenIngot, _localAnimDuration / 15, _sweepGridIngotSpawned, SweepGridUseAltDestination);
                SetReelBorder(_sweepGridIngotSpawned, true);
                _sweepGridIngotSpawned++;
                _nextSpawnTime += _localAnimDuration / 15;
            }
        }

        #region Style Sweep
        private void StyleSweep()
        {
            _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;

                sweepStyleParticleSystem.Play();
                ParticleSystem.EmissionModule emission = sweepStyleParticleSystem.emission;
                emission.rateOverTime = emissionRate;
                splineFollower.SetPercent(0f);
                splineFollower.enabled = true;

                PlayShowerPresetSFX();
            }

            // Disabled by listener. Set in SubscribteEvent()
        }

        private void SubscribeEvent()
        {
            splineFollower.onEndReached += (d) =>
            {
                ParticleSystem.EmissionModule emission = sweepStyleParticleSystem.emission;
                emission.rateOverTime = 0f;
            };
            followerOrb.onEndReached += (d) =>
            {
                followerOrb.gameObject.SetActive(false);
                followerOrbTrail.gameObject.SetActive(false);
                _orbReachedEnd = true;
            };
        }

        void OnParticleTrigger()
        {
            if (spawnStyle == SpawnStyle.Sweep)
            {
                // List to hold the particles that just entered the trigger
                List<ParticleSystem.Particle> enterParticles = new List<ParticleSystem.Particle>();
                int numEnter = sweepStyleParticleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);

                for (int i = 0; i < numEnter; i++)
                {
                    ParticleSystem.Particle p = enterParticles[i];

                    // Kill the particle by setting its remaining lifetime to a negative value
                    p.remainingLifetime = -1f;

                    // Play audio
                    getActiveAudioManager.PlayAudio("IngotIn", 0, 0.7f);
                }

                // Apply the modified particles back to the system
                sweepStyleParticleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);
            }
        }
        #endregion

        /// <summary>
        /// Spawns ingots in pairs, each pair targeting two consecutive destinations.
        /// Pairs are spawned at regular intervals over the animation duration.
        /// </summary>
        private void StyleInterval()
        {
            _elapsedTimeTotal += Time.deltaTime;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Initialize spawning parameters when we first enter this style
            if (!_styleInitialized)
            {
                _styleInitialized = true;

                _totalPairs = numOfDestinationsToUse;
                if (_totalPairs == 0)
                {
                    Debug.LogWarning("Not enough destinations to form a single pair.");
                    return;
                }

                _localAnimDuration = 0.8f * animDuration; // Leave 10% of time at the end for reel border animation
                _nextSpawnTime = 0f; // Spawn the first pair immediately
                _pairsSpawned = 0;
            }

            // Spawn pairs as long as we haven't finished and the next spawn time has been reached
            // Enable reel border for the corresponding reel when its pair starts spawning
            while (_pairsSpawned < _totalPairs && _elapsedTimeTotal >= _nextSpawnTime)
            {
                // TryToSpawn numToSpawnPerInterval ingots, with delayBetweenPairs between them, alive duration based on animDuration and total pairs, to the same destination
                TryToSpawn(numToSpawnPerInterval, delayBetweenPairs, _localAnimDuration / _totalPairs, _pairsSpawned, IntervalUseAltDestination);
                SetReelBorder(_pairsSpawned, true);
                _pairsSpawned++;
                _nextSpawnTime += _localAnimDuration / _totalPairs;
            }
        }

        /// <summary>
        /// Spawns ingots over time based on an animation curve and elapsed time, accumulating spawn events and
        /// triggering them at defined intervals during the animation duration.
        /// </summary>
        /// <remarks>Uses a spawn rate curve to control the frequency of ingot spawning and ensures the
        /// number of ingots spawned per event is randomized within specified bounds.</remarks>
        private void StyleShower()
        {
            _elapsedTimeTotal += Time.deltaTime;
            if (infiniteSpawn) _elapsedTimeTotal = 0f;

            // Stop spawning if we've exceeded the animation duration
            if (_elapsedTimeTotal > animDuration)
            {
                // Turn off all reel borders at the end of the animation
                for (int i = 0; i < ReelBorderCount; i++)
                    SetReelBorder(i, false);
                return;
            }

            // Turn on all reel borders for the duration of the animation
            for (int i = 0; i < ReelBorderCount; i++)
                SetReelBorder(i, true);

            float t = Mathf.Clamp01(_elapsedTimeTotal / animDuration);
            float rate = spawnRateCurve.Evaluate(t);
            rate = Mathf.Max(0f, rate);               // prevent negative rates

            _spawnAccumulator += rate * Time.deltaTime;

            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                int numToSpawn = UnityEngine.Random.Range(spawnMin, spawnMax + 1);
                TryToSpawn(numToSpawn, ShowerUseAltDestination);
            }
        }

        #region Pseudo Ingot Particle System

        /// <summary>
        /// (Re)Set ingot data. Called by TryToSpawn.
        /// </summary>
        private IEnumerator SetupIngot(int index, float aliveDuration, int endPointToUse, float delay, bool useAltDestination)
        {
            if (index < 0 || index >= poolSize || _ingotPool[index] == null)
                yield return null;

            if (endPointToUse < 0 || endPointToUse >= _destinations.Length || _destinations[endPointToUse] == null)
                yield return null;

            if (delay > 0)
                yield return new WaitForSeconds(delay);

            Ingot ingot = _ingotPool[index];

            // Reset ingot state
            ingot.aliveElapsed = 0;
            ingot.aliveDuration = aliveDuration;
            ingot.fadeElapsed = 0;
            ingot.startPosition = startPos.position;
            ingot.destination = (useAltDestination) ? _destinationsSweep[endPointToUse] : _destinations[endPointToUse];
            ingot.hasReachedDestination = false;

            // Activate and position at start
            if (ingot.wrapper != null)
            {
                ingot.wrapper.transform.position = startPos.position;
                ingot.wrapper.SetActive(true);
                ingot.ingot.SetActive(true);
                ingot.ingot.transform.localScale = Vector3.one;
                ingot.trail.SetActive(true);
                ingot.impactStart.SetActive(true);
                ingot.impactEnd.SetActive(false);
                ingot.impactEndDelayed.SetActive(false);
            }

            _activeIngots.Add(ingot);
        }

        /// <summary>
        /// Overloaded version of TryToSpawn to specifically be used by SpwanStyle1.
        /// </summary>
        /// <param name="numToSpawn"></param>
        /// <param name="delayPerSpawn"></param>
        /// <param name="aliveDuration"></param>
        /// <param name="endPointToUse"></param>
        private void TryToSpawn(int numToSpawn, float delayPerSpawn, float aliveDuration, int endPointToUse, bool useAltDestination)
        {
            for (int i = 0; i < numToSpawn && _activeIngots.Count < poolSize; i++)
            {
                // Find an available ingot
                int attempts = 0;
                while (attempts < poolSize)
                {
                    if (!_activeIngots.Contains(_ingotPool[_poolIndex]))
                    {
                        WkCoroutine.instance.StartTrackedCoroutine(SetupIngot(_poolIndex, aliveDuration, endPointToUse, i * delayPerSpawn, useAltDestination));
                        break;
                    }

                    _poolIndex = (_poolIndex + 1) % poolSize;
                    attempts++;
                }

                _poolIndex = (_poolIndex + 1) % poolSize;
            }
        }

        /// <summary>
        /// Attempt to spawn "numToSpawn" amount of ingots
        /// </summary>
        private void TryToSpawn(int numToSpawn, bool useAltDestination)
        {
            for (int i = 0; i < numToSpawn && _activeIngots.Count < poolSize; i++)
            {
                // Find an available ingot
                int attempts = 0;
                while (attempts < poolSize)
                {
                    if (!_activeIngots.Contains(_ingotPool[_poolIndex]))
                    {
                        float aliveDuration = UnityEngine.Random.Range(minTravelTime, maxTravelTime);
                        int endPointToUse = UnityEngine.Random.Range(0, _destinations.Length);
                        WkCoroutine.instance.StartTrackedCoroutine(SetupIngot(_poolIndex, aliveDuration, endPointToUse, 0f, false));
                        break;
                    }

                    _poolIndex = (_poolIndex + 1) % poolSize;
                    attempts++;
                }

                _poolIndex = (_poolIndex + 1) % poolSize;
            }
        }

        /// <summary>
        /// Update the positions of ingotWrappers in _activeIngots list.
        /// </summary>
        private void UpdateActiveObjects()
        {
            for (int i = _activeIngots.Count - 1; i >= 0; i--)
            {
                Ingot ingot = _activeIngots[i];

                if (ingot == null || ingot.wrapper == null || ingot.destination == null)
                {
                    _activeIngots.RemoveAt(i);
                    continue;
                }

                // If ingot has already reached its destination, skip position update
                if (ingot.hasReachedDestination)
                    continue;

                ingot.aliveElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(ingot.aliveElapsed / ingot.aliveDuration);

                if (t >= 1f)
                {
                    if (!ingot.hasReachedDestination)
                    {
                        // Reached destination
                        ingot.hasReachedDestination = true;
                        ingot.wrapper.transform.position = ingot.destination.position;

                        // Deactivate and remove from active list
                        _activeIngots.RemoveAt(i);
                        WkCoroutine.instance.StartTrackedCoroutine(SetIngotInactive(ingot));
                    }
                    continue;
                }

                // Calculate curved movement using Bézier curve
                Vector3 curvedPosition = CalculateCurvedPosition(
                    ingot.startPosition, ingot.destination.position, t, curveOffset, curveHeight
                );

                ingot.wrapper.transform.position = curvedPosition;
            }
        }

        /// <summary>
        /// Let ingot linger after turning off ingot sprite so that impact & trail can play finish
        /// </summary>
        /// <returns></returns>
        private IEnumerator SetIngotInactive(Ingot ingot)
        {
            ingot.impactEnd.SetActive(true);

            yield return new WaitForSeconds(_modifiedIngotLingerDuration);

            float calcMultiplier = -0.3f * _activeIngots.Count + 0.98f;
            float volMultiplier = calcMultiplier < 0.3f ? 0.3f : calcMultiplier;

            if (spawnStyle == SpawnStyle.Shower)
                PlayShowerPresetSFX();
            else if (spawnStyle == SpawnStyle.AltCenterLineX3WithOrb)
                getActiveAudioManager.PlayAudio("IngotIn");
            else
                getActiveAudioManager.PlayAudio("IngotIn", 0, volMultiplier);

            float fadeDuration = _modifiedIngotFadeDuration;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                ingot.ingot.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }

            ingot.impactEndDelayed.SetActive(true);
            yield return new WaitForSeconds(_modifiedIngotLingerDuration);

            // Final cleanup
            ingot.ingot.SetActive(false);
            ingot.ingot.transform.localScale = Vector3.zero;
            ingot.impactStart.SetActive(false);
            ingot.impactEnd.SetActive(false);
            ingot.impactEndDelayed.SetActive(false);
            ingot.trail.SetActive(true);   // Re‑enable trail (original behaviour)
            ingot.wrapper.SetActive(false);

            // Remove from active list
            _activeIngots.Remove(ingot);
        }

        /// <summary>
        /// Calculate position along a curved path using quadratic Bézier curve
        /// </summary>
        private Vector3 CalculateCurvedPosition(
            Vector3 start, Vector3 end, float t, float curveOffset, float curveHeight)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            // Handle vertical movement case
            Vector3 direction = (end - start);
            if (direction.sqrMagnitude < 0.001f) return start; // Too close

            direction.Normalize();

            // Choose a stable up vector
            Vector3 upReference = (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.999f)
                ? Vector3.forward
                : Vector3.up;

            Vector3 perpendicular = Vector3.Cross(direction, upReference).normalized;
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);

            // Xontrol point for true quadratic Bézier
            float curveDirection = 1f;
            Vector3 controlPoint = midPoint +
                (perpendicular * CurveStrength * curveDirection * curveOffset) +
                (Vector3.up * curveHeight);

            // Standard quadratic Bézier with fixed control point
            float u = 1 - t;
            return (u * u) * start +
                   (2 * u * t) * controlPoint +
                   (t * t) * end;
        }

        /// <summary>
        /// Try to set Reel Border by reelIndex to "toEnable".
        /// NOTE: Currently will do nothing due to early return.
        /// </summary>
        /// <param name="reelIndex"></param>
        private void SetReelBorder(int reelIndex, bool toEnable)
        {
            // Early return if not using reel borders at all
            if (!useReelBorder)
                return;

            // Return if out of bounds
            if (reelIndex < 0 || reelIndex >= ReelBorderCount)
                return;

            // Return if already set
            if (_reelBordersActive[reelIndex] == toEnable && _postInitialized)
                return;

            _reelBordersActive[reelIndex] = toEnable;
            _reelBorders[reelIndex].SetActive(toEnable);
        }

        /// <summary>
        /// Disable all ingotWrappers and clears _activeIngots list.
        /// </summary>
        public void ClearAllIngots()
        {
            if (_ingotPool is null || _ingotPool.Length < poolSize) return;
            foreach (Ingot ingot in _ingotPool)
            {
                if (ingot != null && ingot.wrapper != null)
                {
                    ingot.wrapper.SetActive(false);
                    ingot.ingot.SetActive(false);
                    ingot.ingot.transform.localScale = Vector3.one;
                    ingot.trail.SetActive(false);
                    ingot.impactStart.SetActive(false);
                    ingot.impactEnd.SetActive(false);
                    ingot.impactEndDelayed.SetActive(false);
                }
            }
            _activeIngots.Clear();
        }

        private void PlayShowerPresetSFX()
        {
            if (_playPresetSfxOnce) return;
            _playPresetSfxOnce = true;
            getActiveAudioManager.PlayAudioUnique("ReelNudgePreSpin_ShowerSFX");            
        }

        #endregion

        protected override void OnAllowedEnable()
        {
            ClearAllIngots();
        }

        public override void OnPIError()
        {
            base.OnPIError();
            ResetThis();
        }

        protected override void ResetToDefault()
        {
            ResetThis();
        }

        public void ResetThis()
        {
            ClearAllIngots();
            tryToSpawn = false;
            _styleInitialized = false;
            _modifiedIngotFadeDuration = IngotFadeDuration;
            _modifiedIngotLingerDuration = IngotLingerDuration;

            // Reset Shower Style Variables
            _poolIndex = 0;
            _elapsedTimeTotal = 0;
            _spawnAccumulator = 0f;
            infiniteSpawn= false;

            // Reset Interval Style Variables
            _pairsSpawned = 0;
            _nextSpawnTime = 0f;

            // Reset Sweep Style Variables
            sweepStyleParticleSystem.Stop();
            ParticleSystem.EmissionModule emission = sweepStyleParticleSystem.emission;
            emission.rateOverTime = 0f;
            splineFollower.enabled = false;
            splineFollower.SetPercent(0f);

            // Reset CenterLineX3 Style Variables
            _centerLineIngotSpawned = 0;
            _centerLineDestinationIndex = 0;

            // Reset AltCenterLineX3 Style Variables
            _altCenterLineIngotSpawned = 0;
            _altCenterLineDestinationIndex = 0;
            ResetLightAnimations();

            followerOrb.gameObject.SetActive(false);
            followerOrb.enabled = false;
            followerOrb.SetPercent(0f);
            followerOrbTrail.gameObject.SetActive(false);
            followerOrbTrail.enabled = false;
            followerOrbTrail.SetPercent(0f);
            _orbReachedEnd = false;
            _lightBurstSfxPlayed = false;

            // Reset ReelBorders
            for (int i = 0; i < ReelBorderCount; i++)
                SetReelBorder(i, false);

            StopAllCoroutines();
        }
    }
}