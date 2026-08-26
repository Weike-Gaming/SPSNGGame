using System;
using System.Collections;
using UnityEngine;
using Weike.Core;
using Dreamteck.Splines;
using System.Collections.Generic;
using System.Linq;
using Weike.Common;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYSmallIngotDisplayObject : WkDisplayObject
    {
        private JIXRYReelNudgePreSpinDisplayObject _reelNudgePreSpinDisplayObject = null;

        [SerializeField] private SpriteRenderer bgBlur;

        [Header("LightBurst animatorLightBurst Ref")] // Reference the parent objs Spin, Prize, Jackpot
        [SerializeField] private Animator animatorLightBurst = null;
        [SerializeField] private SpriteRenderer lightBurst = null;
        [SerializeField] private SpriteRenderer lightBurstBack = null;
        private bool _isVisibleLightBurst = false;
        private const float LightBurstRotationSpeed = 200f;

        [Header("OrbStartEndPositions Ref")]
        [SerializeField] private Transform[] orbStartEndPositions = null; // stores Transforms of starts & ends in order of Spin, Prize, Jackpot according to heirarchy positions
        private const byte TotalOrbStartEndPositions = 6; // 1)SpinSTART 2)SpinEND 3)PrizeSTART 4)PrizeEND 5)JackpotSTART 6)JackpotEND

        [Header("Particle System")]
        [SerializeField] private ParticleSystem sprayParticle = null;
        [SerializeField] private ParticleSystem burstParticle = null;

        [Header("Ingot Ref")]
        [SerializeField] private GameObject[] spinIngotGO;
        [SerializeField] private GameObject[] prizeIngotGO;
        [SerializeField] private GameObject[] jackpotIngotGO;

        [Header("VFX")]
        [SerializeField] private GameObject[] orb;
        [SerializeField] private GameObject[] impact;
        [SerializeField] private GameObject[] dust;

        private Dictionary<int, GameObject[]> _ingotDictionary;  // Blue = 0, Green = 1, Red = 2
        public float[] animDelay = { 6f, 5f, 5.4f };

        private Color _colorPurple = new Color(212f / 255f, 0, 1);

        #region Binding
        public bool playHitFeatureAnim { get; set; }
        #endregion

        private bool _isPlaying;
        private bool _isRecover;

        protected override void Awake()
        {
            base.Awake();
            _ingotDictionary = new Dictionary<int, GameObject[]>
            {
                { 0, spinIngotGO },
                { 1, prizeIngotGO },
                { 2, jackpotIngotGO }
            };

            // Ensure animatorLightBurst are populated
            WkAssert.EnsureMsgf(animatorLightBurst is not null, "animatorLightBurst is NULL");
            WkAssert.EnsureMsgf(lightBurst is not null, "lightBurst is NULL");
            WkAssert.EnsureMsgf(lightBurstBack is not null, "lightBurstBack is NULL");

            // Get orbStartEndPositions
            WkAssert.EnsureMsgf(orbStartEndPositions is not null, "orbStartEndPositionsParent is NULL");
            WkAssert.EnsureMsgf((orbStartEndPositions.Length == TotalOrbStartEndPositions), "Mismatch in orbStartEndPositionsParent count");

            // Ensure sprayParticle is assigned
            WkAssert.EnsureMsgf(sprayParticle is not null, "sprayParticle is NULL");
            sprayParticle.Pause();

            _reelNudgePreSpinDisplayObject = FindAnyObjectByType<JIXRYReelNudgePreSpinDisplayObject>();
            WkAssert.EnsureMsgf(_reelNudgePreSpinDisplayObject is not null, "reelNudgePreSpinDisplayObject is NULL");
            _reelNudgePreSpinDisplayObject.animDuration = animDelay[0] - 0.5f; // 0.5f is buffer to stop preemptively stop spawning ingots
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            if (playHitFeatureAnim)
            {
                _isRecover = false;
                TryPlayNext();
            }
        }

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
            ResetIngotAnim();
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            wkModelCollection.AddModel(dm);

            ResetLightBackAnimations();
            SubscribeEvent();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("fg-spin")!.onRecoverState += ResetIngotAnim;
            getState("fg-spin")!.onExitState += ResetIngotAnim;
        }

        private List<Coroutine> _ingotCoroutine = new List<Coroutine>();
        
        public override void OnPIError()
        {
            base.OnPIError();

            if (_ingotCoroutine.Count > 0)
            {
                foreach (Coroutine c in _ingotCoroutine)
                {
                    WkCoroutine.instance.StopTrackedCoroutine(c);
                }
            }
        }

        private void ResetIngotAnim()
        {
            _isPlaying = false;
            _isVisibleLightBurst = false;
            _isRecover = true;

            bgBlur.enabled = false;
            lightBurst.gameObject.SetActive(false);
            lightBurstBack.gameObject.SetActive(false);

            ResetIngotGO(spinIngotGO);
            ResetIngotGO(prizeIngotGO);
            ResetIngotGO(jackpotIngotGO);
            ResetVfxGO(orb, true);
            ResetVfxGO(dust, true);
            ResetVfxGO(impact);
            ResetLightBackAnimations();
        }

        private void ResetIngotGO(GameObject[] gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                JIXRYFloatTween floatTween = go.GetComponent<JIXRYFloatTween>();
                floatTween.enabled = true;
                floatTween.ResetProperties();
                

                SplineFollower[] followers = go.GetComponents<SplineFollower>();
                followers[1].SetPercent(0f);
                followers[1].enabled = false;

                followers[0].enabled = true;
                followers[0].follow = true;
                followers[0].SetPercent(0f);

                go.SetActive(false);
            }
        }

        private void ResetVfxGO(GameObject[] gameObjects, bool haveSpline = false)
        {
            foreach (GameObject go in gameObjects)
            {
                if (haveSpline)
                {
                    SplineFollower[] followers = go.GetComponents<SplineFollower>();

                    if (followers.Length == 2)
                    {
                        followers[1].SetPercent(0f);
                        followers[1].enabled = false;

                        followers[0].enabled = true;
                        followers[0].follow = true;
                        followers[0].SetPercent(0f);
                    }
                    else
                    {
                        followers[0].SetPercent(0f);
                    }
                }

                go.SetActive(false);
            }
        }

        private void ResetLightBackAnimations()
        {
            animatorLightBurst.Play("LightBurst_Idle_Animation");
            animatorLightBurst.Play("LightBurstBack_Idle_Animation");
        }

        #region LightBurst

        /// <summary>
        /// Play 'Appear' animation & set active to LightBurst
        /// </summary>
        /// <param name="index"></param>
        private void PlayLightBurst(int index)
        {
            if (_isRecover) return;
            lightBurst.gameObject.SetActive(true);
            _isVisibleLightBurst = true;
            KeyValuePair<int, Color> tmp = StartEndIndexConverterForPositionalData(index, false);
            lightBurst.transform.position = orbStartEndPositions[tmp.Key].position;
            lightBurst.color = tmp.Value;
            animatorLightBurst.Play("LightBurst_Appear_Animation");
        }
        private void StopLightBurst(int index)
        {
            _isVisibleLightBurst = false;
            KeyValuePair<int, Color> tmp = StartEndIndexConverterForPositionalData(index, false);
            animatorLightBurst.Play("LightBurst_Disappear_Animation");
        }

        private void RotateLightBurst()
        {
            if (_isVisibleLightBurst)
            {
                lightBurst.transform.Rotate(0, 0, LightBurstRotationSpeed * Time.deltaTime);
                lightBurstBack.transform.Rotate(0, 0, LightBurstRotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Play 'Appear' animation & set active to LightBurstBack
        /// </summary>
        /// <param name="index"></param>
        private void PlayLightBurstBack(int index)
        {
            if (_isRecover) return;
            lightBurstBack.gameObject.SetActive(true);
            _isVisibleLightBurst = true;
            KeyValuePair<int, Color> tmp = StartEndIndexConverterForPositionalData(index, false);
            lightBurstBack.transform.position = orbStartEndPositions[tmp.Key].position;
            lightBurstBack.color = tmp.Value;
            animatorLightBurst.Play("LightBurstBack_Appear_Animation");
        }
        private void StopLightBurstBack(int index)
        {
            _isVisibleLightBurst = false;
            KeyValuePair<int, Color> tmp = StartEndIndexConverterForPositionalData(index, false);
            animatorLightBurst.Play("LightBurstBack_Disappear_Animation");
        }

        #endregion

        #region Subscribe Events
        private void SubscribeEvent()
        {
            for (int i = 0; i < 3; i++)
            {
                int index = i; // index 0 = Spin, 1 = Jackpot, 2 = Prize 

                // Orb reach end, close orb, play impact, play ingot anim
                SplineFollower orbFollower = orb[index].GetComponent<SplineFollower>();
                orbFollower.onEndReached += (d) => orb[index].SetActive(false);
                orbFollower.onEndReached += (d) => impact[index].SetActive(true);
                orbFollower.onEndReached += (d) => StartCoroutine(OrbSparkleDelay(index));
                orbFollower.onEndReached += (d) => StartCoroutine(SplineMotion(_ingotDictionary[index]));
                orbFollower.onEndReached += (d) => PlayParticleSpray(index, false);
                orbFollower.onEndReached += (d) => PlayLightBurst(index);

                // Get the last ingot to start idle time
                SplineFollower[] spinFollower = _ingotDictionary[index][^1].GetComponents<SplineFollower>(); // this gets last ingot
                spinFollower[0].onEndReached += (d) => { _ingotCoroutine.Add(WkCoroutine.instance.StartTrackedCoroutine(IdleFloatingTime(index), true)); };
                spinFollower[0].onEndReached += (d) => StopLightBurst(index);
                spinFollower[1].onEndReached += (d) => ResetIngotAnim(index, true);
                spinFollower[1].onEndReached += (d) => StopLightBurstBack(index);

                // Hide Ingot once ingot anim finish
                for (int j = 0; j < _ingotDictionary[index].Length; j++)
                {
                    GameObject ingot = _ingotDictionary[index][j];
                    SplineFollower[] ingotFollowers = ingot.GetComponents<SplineFollower>();
                    ingotFollowers[1].onEndReached += (d) => FinishIngotMotion(index, ingot);
                }
            }
        }
        #endregion

        #region Animation Queue
        private void TryPlayNext()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            if (_isRecover)
            {
                _isRecover = false;
                gm.pendingSmallIngotAnim.Clear();
                return;
            }

            bgBlur.enabled = true;

            if (_isPlaying) return;
            if (gm.pendingSmallIngotAnim.Count == 0) return;

            Debug.Log($"[TryPlayNext] Count before Dequeue = {gm.pendingSmallIngotAnim.Count}");

            JIXRYStateDataFlag next = gm.pendingSmallIngotAnim.Dequeue();
            _isPlaying = true;
            _reelNudgePreSpinDisplayObject.tryToSpawn = false;

            switch (next)
            {
                case JIXRYStateDataFlag.FREE_GAME_RN:
#if DEBUG
                    Debug.Log("Fly small ingot: Play LED (FREE_GAME_RN)");
#endif
                    _reelNudgePreSpinDisplayObject.animDuration = animDelay[0] - 0.5f; // 0.5f is buffer to stop preemptively stop spawning ingots

                    _reelNudgePreSpinDisplayObject.tryToSpawn = true;
                    gm.PlayLED(next);
                    PlayOrbAnim(0);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_JP:
#if DEBUG
                    Debug.Log("Fly small ingot: Play LED (FREE_GAME_JP)");
#endif
                    _reelNudgePreSpinDisplayObject.ResetThis();
                    gm.PlayLED(next);
                    PlayOrbAnim(2);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RU:
#if DEBUG 
                    Debug.Log("Fly small ingot: Play LED (FREE_GAME_RU)");
#endif
                    _reelNudgePreSpinDisplayObject.ResetThis();
                    gm.PlayLED(next);
                    PlayOrbAnim(1);
                    break;
            }
        }

        private IEnumerator WaitAnimationFinish(float delay)
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            yield return new WaitForSeconds(delay);

            _isPlaying = false;
            TryPlayNext();

            if (!_isPlaying && gm.pendingSmallIngotAnim.Count == 0)
            {
                AllAnimationPlayed();
            }
        }

        private void AllAnimationPlayed()
        {
            bgBlur.enabled = false;
#if DEBUG
            Debug.Log("Small ingot animation ended: Play LED");
#endif
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PlayLED((JIXRYStateDataFlag)gm.dataModel.GetModelDataChecked<JIXRYGameDataModel>().potFeatureGameFlag);
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.FinishPreSpinAnimation(0.3f);
        }
        #endregion

        #region Orb
        private void PlayOrbAnim(int type)
        {
            getActiveAudioManager.PlayAudio("PreSpinSparkle");

            PlayParticleSpray(type, true);
            SplineFollower orbFollower = orb[type].GetComponent<SplineFollower>();
            orbFollower.SetPercent(0f);
            orb[type].SetActive(true);

            SplineFollower dustFollower = dust[type].GetComponent<SplineFollower>();
            dustFollower.SetPercent(0f);
            dust[type].SetActive(true);

            impact[type].SetActive(false);

            StartCoroutine(WaitAnimationFinish(animDelay[type]));
        }

        /// <summary>
        /// Delay disabling of the Sparkle/Dust/Trail/whateverYouCallIt
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private IEnumerator OrbSparkleDelay(int index)
        {
            ParticleSystem.MainModule sparkle = dust[index].GetComponent<ParticleSystem>().main;
            sparkle.loop = false;

            yield return new WaitForSeconds(2f);
            dust[index].SetActive(false);
            sparkle.loop = true;
        }

        /// <summary>
        /// Play sprayParticle at some Start/End position
        /// Hardcoded stored as 1)SpinSTART 2)PrizeSTART 3)JackpotSTART 4)SpinEND 5)PrizeEND1 6)PrizeEND2 7)JackpotEND
        /// </summary>
        /// <param name="index">0, 1, 2 for Spin, Prize, Jackpot</param>
        /// <param name="isStart"></param>
        private void PlayParticleSpray(int index, bool isStart)
        {
            ParticleSystem.MainModule main = sprayParticle.main;
            // convert index & isStart to actual array index
            KeyValuePair<int, Color> tmp = StartEndIndexConverterForPositionalData(index, isStart);

            Transform pos = orbStartEndPositions[tmp.Key];
            sprayParticle.gameObject.transform.position = pos.position;
            sprayParticle.gameObject.transform.rotation = pos.rotation;
            sprayParticle.Play();
        }
        #endregion

        #region Idle Floating
        public IEnumerator IdleFloatingTime(int ingotType)
        {
            yield return new WaitForSeconds(1f);

            GameObject[] ingots = _ingotDictionary[ingotType].ToArray();
            PlayLightBurstBack(ingotType);


            for (int i = 0; i < ingots.Length; i++)
            {
                JIXRYFloatTween floatTween = ingots[i].GetComponent<JIXRYFloatTween>();
                floatTween.enabled = false;

                SplineFollower[] follower = ingots[i].GetComponents<SplineFollower>();
                follower[0].enabled = false;
                follower[1].enabled = true;

                GameObject sparkleParticle = ingots[i].GetComponentInChildren<TrailRenderer>(true).gameObject;
                sparkleParticle.SetActive(true);

                yield return new WaitForSeconds(0.1f);
            }
        }
        #endregion

        #region Ingot Spline
        private IEnumerator SplineMotion(GameObject[] ingotGO)
        {
            yield return new WaitForSeconds(0.1f);

            GameObject[] ingots = ingotGO;

            foreach (GameObject go in ingots)
            {
                go.SetActive(true);
                getActiveAudioManager.PlayAudio("IngotOut");
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void FinishIngotMotion(int index, GameObject go)
        {
            if(!burstParticle.isPlaying)
            {
                burstParticle.gameObject.transform.position = go.transform.position;
            }

            ParticleSystem.MainModule main = burstParticle.main;

            if (index==0)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(Color.blue, Color.white);
            }
            else if (index == 1)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(_colorPurple, Color.white);
            }
            else
            {
                main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.white);
            }

            burstParticle.Play();
            getActiveAudioManager.PlayAudio("IngotIn");
            go.SetActive(false);
        }

        public void ResetVfx(int type)
        {
            // Reset orb
            SplineFollower orbFollower = orb[type].GetComponent<SplineFollower>();
            orbFollower.SetPercent(0f);
            orb[type].SetActive(false);

            // Reset dust
            dust[type].SetActive(false);

            impact[type].SetActive(false);
        }

        private void ResetIngotAnim(int ingotType, bool resetTweens)
        {
            GameObject[] ingotGO = _ingotDictionary[ingotType];

            foreach (GameObject go in ingotGO)
            {
                SplineFollower[] followers = go.GetComponents<SplineFollower>();
                followers[1].enabled = false;
                followers[0].enabled = true;
                followers[0].SetPercent(0f);
                followers[1].SetPercent(0f);
                followers[0].follow = true;

                if (resetTweens)
                {
                    JIXRYFloatTween tween = go.GetComponent<JIXRYFloatTween>();
                    tween.ResetProperties();
                }

                go.SetActive(false);
            }
        }
        #endregion

        #region HELPER

        /// <summary>
        /// Converts index & isStart into element to use in orbStartEndPositions[]
        /// </summary>
        /// <param name="index"></param>
        /// <param name="isStart"></param>
        /// <returns></returns>
        private KeyValuePair<int, Color> StartEndIndexConverterForPositionalData(int index, bool isStart)
        {
            if (!isStart)
            {
                switch (index)
                {
                    case 0: // SpinEnd
                        return new KeyValuePair<int, Color>(3, new Color(5f / 255f, 0, 1));
                    case 1: // PrizeEnd
                        return new KeyValuePair<int, Color>(4, _colorPurple);
                    case 2: // JackpotEnd
                        return new KeyValuePair<int, Color>(5, new Color(1f, 0f, 0f));
                    default:
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 0: // SpinStart
                        return new KeyValuePair<int, Color>(0, new Color(5f / 255f, 0, 1)); // Teal
                    case 1: // PrizeStart
                        return new KeyValuePair<int, Color>(1, _colorPurple); // Green
                    case 2: // JackpotStart
                        return new KeyValuePair<int, Color>(2, new Color(1f, 0f, 0f)); // Red
                    default:
                        break;
                }
            }
            return new KeyValuePair<int, Color>(0, new Color(1, 1, 1));
        }

        #endregion

        private void Update()
        {
            RotateLightBurst();
        }
    }
}