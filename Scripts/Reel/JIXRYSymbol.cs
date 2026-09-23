using System.Collections.Generic;
using UnityEngine;
using Weike.Core;
using DigitalRuby.Tween;
using Weike.SlotCore;
using System;
using System.Collections;
using Weike.LobbyManagement;

namespace Weike.Games.JIXRY
{
    public class JIXRYSymbol : WkSymbol
    {
        private const int BlueScatter = 11;
        private const int RedScatter = 12;
        private const int GreenScatter = 13;
        private const int NormalIngot = 15;
        private const int PrizeMultiplierIngot = 25;
        private const int JackpotIngot = 16;

        private int ingotValueToAdd = 0;

        [SerializeField] private GameObject extraSprite;
        private SpriteRenderer extraSpriteRenderer;

        [SerializeField] private WkTexture2DFramesInfo prizeAdditionInfo;

        [Header("Sprite Collection")]
        [SerializeField] private List<Sprite> extraSpinSprite;
        [SerializeField] private List<Sprite> scatterSprite;

        [Header("VFX")]
        [SerializeField] private ParticleSystem pixieDust;
        [SerializeField] private ParticleSystem ring;
        [SerializeField] private ParticleSystem orb;
        [SerializeField] private ParticleSystem sprayDust;
        [SerializeField] private ParticleSystem burstVFX;

        private int _spinType = -1;
        private int _multiplierType = -1;
        private int _additionType = -1;
        private int _jackpotType = -1;
        private int _currentJackpotType = -1;

        private const int OffsetWidth = -30;
        private const int OffsetHeight = 0;
        
        private bool _isJpIngotAnimPlaying = false;
        private bool _isTransformed = false;

        private ITween _cachedTween;

        public bool isChangeAnim;
        private readonly AnimationCurve _curve = new AnimationCurve(new Keyframe(0f, 0.77f, 0.0172079261f, 0.0172079261f), new Keyframe(0.1333f, 1.03f, -0.00450215442f, -0.00450215442f), new Keyframe(0.2444f, 0.77f, -0.120354414f, 0.0352805071f), new Keyframe(0.4222f, 0.87f, -0.0002667665f, -0.0002667665f), new Keyframe(0.6f, 0.77f, -0.0446761064f, -7.535286E-05f), new Keyframe(0.7778f, 0.77f, 0f, 0f), new Keyframe(1.6f, 0.77f, 0f, 0f));
        #region Checking
        public bool CheckNormalIngot()
        {
            return symbolID == NormalIngot;
        }

        public bool CheckScatter()
        {
            if (symbolID == BlueScatter || symbolID == GreenScatter || symbolID == RedScatter)
            {
                return true;
            }
            else
                return false;
        }


        public bool CheckPrizeMultiplierIngot()
        {
            return symbolID == PrizeMultiplierIngot;
        }

        public bool CheckJackpotIngot()
        {
            return symbolID == JackpotIngot;
        }

        public bool CheckRedScatter()
        {
            return symbolID == RedScatter;
        }
        
        public bool CheckGreenScatter()
        {
            return symbolID == GreenScatter;
        }
        
        public bool CheckBlueScatter()
        {
            return symbolID == BlueScatter;
        }
        #endregion

        #region Set Type Data
        public void SetIngotValue(int ingotValue)
        {
            ingotValueToAdd = ingotValue;
        }

        public void SetExtraSpinType(int type)
        {
            _spinType = type;
        }

        public void SetExtraPrizeMultuplierType(int type)
        {
            _multiplierType = type;
        }

        public void SetExtraPrizeAdditionType(int type)
        {
            _additionType = type;
        }

        public void SetExtraJackpotType(int type)
        {
            _jackpotType = type;
            _isTransformed = false;
        }

        public void SetCurrentExtraJackpotType(int type)
        {
            _currentJackpotType = type;
        }

        public void SetTransformed(bool isTransformed)
        {
            _isTransformed = isTransformed;
        }
        #endregion

        public override void InitSymbol(WkSymbolProperty property)
        {
            base.InitSymbol(property);
            extraSpriteRenderer = extraSprite.GetComponent<SpriteRenderer>();
            ResetExtraAnim();
        }

        protected override void InitializeRendererCollection()
        {
            base.InitializeRendererCollection();
            rendererCollection.Add("FeatureGraphic", InstantiateRenderer("FeatureGraphic"));
            rendererCollection.Add("ScatterSpin", InstantiateRenderer("ScatterSpin"));
        }

        protected override void UpdateGraphic()
        {
            base.UpdateGraphic();
            rendererCollection["Digit"].gameObject.SetActive(false);
            rendererCollection["FeatureGraphic"].gameObject.SetActive(false);
            rendererCollection["ScatterSpin"].gameObject.SetActive(false);
            switch (symbolID)
            {
                case JackpotIngot:
                    {
                        if (!_isTransformed && _jackpotType >= 1 && _jackpotType <= 4)
                        {
                            bool isEn = WkLobbySceneManager.instance.dataModel.language == WkGameLanguage.EN;
                            AddFeatureImageInfo(isEn);
                        }
                        SetCurrentExtraJackpotType(_jackpotType);
                    }
                    break;
                case NormalIngot:
                    {
                        AddTextInfo(ingotValueToAdd, OffsetWidth, OffsetHeight);
                    }
                    break;
            }
        }


        private void Update()
        {
            if (CheckJackpotIngot())
            {
                bool isEn = WkLobbySceneManager.instance.dataModel.language == WkGameLanguage.EN;
                if (!_isJpIngotAnimPlaying)
                {
                    RefreshJackpotLocalizeSprite(isEn);
                }
            }
        }

        private void RefreshJackpotLocalizeSprite(bool isEn)
        {
            Texture2D texture = null;
            SSFrame frame = new SSFrame();
            if (_currentJackpotType < 1 || _currentJackpotType > 4)
            {
                return;
            }
            switch (_currentJackpotType)
            {
                case 1:
                    texture = GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Grand_en" : "Grand_zh")
                        .GetLastTexture();
                    break;
                case 2:
                    texture = GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Major_en" : "Major_zh")
                        .GetLastTexture();
                    break;
                case 3:
                    texture = GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Minor_en" : "Minor_zh")
                        .GetLastTexture();
                    break;
                case 4:
                    texture = GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Mini_en" : "Mini_zh")
                        .GetLastTexture();
                    break;
            }

            switch (_currentJackpotType)
            {
                case 1:
                    frame = GetSymbolDetail().GetSpriteAnimationInfo("Grand_en").GetLastFrame();
                    break;
                case 2:
                    frame = GetSymbolDetail().GetSpriteAnimationInfo("Major_en").GetLastFrame();
                    break;
                case 3:
                    frame = GetSymbolDetail().GetSpriteAnimationInfo("Minor_en").GetLastFrame();
                    break;
                case 4:
                    frame = GetSymbolDetail().GetSpriteAnimationInfo("Mini_en").GetLastFrame();
                    break;
            }

            DrawImageInfo info = new DrawImageInfo();
            info.texture = texture;
            info.frame = frame;
            info.scale = Vector2.one;
            rendererCollection["FeatureGraphic"].AddDrawInfo("Base", info);
            
            rendererCollection["FeatureGraphic"].gameObject.SetActive(true);
            rendererCollection["FeatureGraphic"].Draw();
        }

        #region Feature Image
        private void AddFeatureImageInfo(bool isEn)
        {
            Texture2D texture = GetFeatureTexture(isEn);
            SSFrame frame = GetFeatureFrame();
            DrawImageInfo info = new DrawImageInfo();
            info.texture = texture;
            info.frame = frame;
            info.scale = Vector2.one;
            rendererCollection["FeatureGraphic"].AddDrawInfo("Base", info);

            rendererCollection["FeatureGraphic"].gameObject.SetActive(true);
            rendererCollection["FeatureGraphic"].Draw();
            _isTransformed = true;
        }

        private Texture2D GetFeatureTexture(bool isEn)
        {
            switch (symbolID)
            {
                case JackpotIngot:
                    return ChangeJackpotTexture(isEn);
                default:
                    return null;
            }
        }

        private SSFrame GetFeatureFrame()
        {
            switch (symbolID)
            {
                case JackpotIngot:
                    return ChangeExtraJackpotFrame();
                default:
                    return new SSFrame();
            }
        }

        #region Spin
        private Texture2D ChangeExtraSpinTexture(bool isEn)
        {
            switch(_spinType)
            {
                case 1:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Spin1_en" : "Spin1_zh")
                        .GetLastTexture();
                case 2:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Spin2_en" : "Spin2_zh")
                        .GetLastTexture();
                case 3:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Spin3_en" : "Spin3_zh")
                        .GetLastTexture();
            }
            return null;
        }

        private SSFrame ChangeExtraSpinFrame()
        {
            switch (_spinType)
            {
                case 1:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Spin1_en").GetLastFrame();
                case 2:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Spin2_en").GetLastFrame();
                case 3:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Spin3_en").GetLastFrame();
            }

            return new SSFrame();
        }
        #endregion

        #endregion

        #region Jackpot
        private Texture2D ChangeJackpotTexture(bool isEn)
        {
            switch (_jackpotType)
            {
                case 1:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Grand_en" : "Grand_zh")
                        .GetLastTexture();
                case 2:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Major_en" : "Major_zh")
                        .GetLastTexture();
                case 3:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Minor_en" : "Minor_zh")
                        .GetLastTexture();
                case 4:
                    return GetSymbolDetail()
                        .GetSpriteAnimationInfo(isEn ? "Mini_en" : "Mini_zh")
                        .GetLastTexture();
                default:
                    return null;
            }
        }

        private SSFrame ChangeExtraJackpotFrame()
        {
            switch (_jackpotType)
            {
                case 1:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Grand_en").GetLastFrame();
                case 2:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Major_en").GetLastFrame();
                case 3:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Minor_en").GetLastFrame();
                case 4:
                    return GetSymbolDetail().GetSpriteAnimationInfo("Mini_en").GetLastFrame();
                default:
                    return new SSFrame();
            }
        }
        #endregion

        #region Scatter
        private void AddScatterImageInfo()
        {
            Texture2D texture = GetScatterTexture();
            SSFrame frame = new SSFrame(0, 0, texture.width, texture.height);
            DrawImageInfo info = new DrawImageInfo();
            info.texture = texture;
            info.frame = frame;
            info.scale = Vector2.one;
            rendererCollection["ScatterSpin"].gameObject.SetActive(true);
            rendererCollection["ScatterSpin"].AddDrawInfo("Base", info);
            rendererCollection["ScatterSpin"].Draw("UI", 15);
        }

        private Texture2D GetScatterTexture()
        {
            switch (symbolID)
            {
                case BlueScatter:
                    return scatterSprite[0].texture;
                case RedScatter:
                    return scatterSprite[1].texture;
                case GreenScatter:
                    return scatterSprite[2].texture;
                default:
                    return null;
            }
        }

        public void PlayScatterAnim(Transform spinPot, Transform jackpotPot, Transform prizePot, Action<JIXRYSymbol> onSymbolScatterComplete)
        {
            Transform target = null;

            if (symbolID == BlueScatter)
            {
                target = spinPot;
            }
            else if (symbolID == RedScatter)
            {
                target = jackpotPot;
            }
            else if (symbolID == GreenScatter)
            {
                target = prizePot;
            }
            else
            {
                onSymbolScatterComplete?.Invoke(this);
            }

            if (target == null) return;

            AddScatterImageInfo();
            PlaySpecificAnimation("Spin", -1, rendererCollection["ScatterSpin"]);

            Vector2 start = transform.position;
            Vector2 end = target.position + new Vector3(0, 1.2f);

            // ==== compute B (mid) ====
            float dirX = Mathf.Sign(end.x - start.x);
            if (Mathf.Approximately(dirX, 0f)) dirX = 1f;

            // base distance and helpful scalars
            float dist = Vector2.Distance(start, end);
            float midX = (start.x + end.x) * 0.5f;

            // push the mid point slightly *past* the middle in the travel direction
            // so it "surges" forward then drops
            float horizPush = Mathf.Clamp(dist * 0.15f, 0.25f, 1.2f);
            float bx = midX + dirX * horizPush;

            // lift the mid point above the higher of start/end to get a throw arc
            float baseLift = Mathf.Clamp(Mathf.Pow(dist, 1.0f), 0.5f, 10f);
            float by = Mathf.Max(start.y, end.y) + baseLift;

            Vector2 control = new Vector2(bx, by);

            float duration = UnityEngine.Random.Range(1.5f, 2f);

            string tweenKey = "ScatterMove_" + uniqueID;
            _cachedTween = TweenFactory.Tween(
                tweenKey,
                0f, 1f,
                duration,
                TweenScaleFunctions.CubicEaseInOut,
                t =>
                {
                    if (this == null || gameObject == null)
                        return;
                    float u = t.CurrentValue;

                    Vector2 potPosition = QuadBezier(start, control, end, u);
                    rendererCollection["ScatterSpin"].transform.position = potPosition;

                    if (u >= 0.7f)
                    {
                        float shrinkProgress = Mathf.InverseLerp(0.7f, 1f, u);
                        float scale = Mathf.Lerp(1f, 0.5f, shrinkProgress);
                        rendererCollection["ScatterSpin"].transform.localScale = new Vector2(scale, scale);
                    }
                    
                    if (u >= 0.99f)
                    {
                        PlayPotRingAnim(end);
                        rendererCollection["ScatterSpin"].gameObject.SetActive(false);
                    }
                },
                tComplete =>
                {
                    rendererCollection["ScatterSpin"].transform.position = transform.position;
                    rendererCollection["ScatterSpin"].transform.localScale = new Vector2(1, 1);
                    
                    onSymbolScatterComplete?.Invoke(this);
                }
            );
        }

        private void PlayPotRingAnim(Vector2 end)
        {
            // Prevent coroutine from being called when scene has switched
            if (!WkCoroutine.instance.gameObject.activeInHierarchy) return;

            if (!rendererCollection["ScatterSpin"].gameObject.activeSelf) return;
            WkAudioManager.instance.PlayAudio("ScatterHitPot");
            sprayDust.transform.position = end;
            sprayDust.Play();
            WkCoroutine.instance.StartTrackedCoroutine(ResetSprayDustAfterFinish());
        }

        private IEnumerator ResetSprayDustAfterFinish()
        {
            yield return new WaitUntil(() => !sprayDust.isPlaying);
            sprayDust.transform.position = transform.position;
        }
        #endregion

        #region Extra Spin
        public void PlayExtraSpinAnim(Transform freegameCounter)
        {
            WkAudioManager.instance.PlayAudio("SpinIngotFly");
            rendererCollection["FeatureGraphic"].gameObject.SetActive(false);
            ShowExtraAnim();

            Transform tf = extraSprite.transform;
            Transform parent = tf.parent;

            Vector2 A = new Vector2(0, 0.25f);
            Vector3 targetWorld = freegameCounter.position;
            Vector3 targetLocal3 = parent != null ? parent.InverseTransformPoint(targetWorld) : targetWorld;
            Vector2 C = new Vector2(targetLocal3.x, targetLocal3.y);

            float dist = Vector2.Distance(A, C);
            Vector2 control = new Vector2(
                (A.x + C.x) * 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f),
                Mathf.Max(A.y, C.y) + Mathf.Clamp(dist * 0.5f, 1f, 4f)
            );

            float duration = 1.05f;
            Vector3 startScale = tf.localScale;
            Vector3 endScale = startScale * 0.5f;

            _cachedTween = TweenFactory.Tween(
                $"ExtraSpin_{GetInstanceID()}",
                0f, 1f,
                duration,
                TweenScaleFunctions.CubicEaseIn,
                t =>
                {
                    float u = t.CurrentValue;
                    Vector2 pos = QuadBezier(A, control, C, u);
                    tf.localPosition = pos;
                    pixieDust.transform.localPosition = pos;

                    tf.localScale = Vector3.LerpUnclamped(startScale, endScale, Mathf.SmoothStep(0f, 1f, u));
                },
                _ =>
                {
                    WkAudioManager.instance.PlayAudio("SpinIngotCounter");
                    PlayBurst();
                    ResetExtraAnim();
                    extraSpriteRenderer.enabled = false;
                }
            );
        }

        private void PlayBurst()
        {
            burstVFX.transform.position = extraSprite.transform.position;
            burstVFX.Play();
            WkCoroutine.instance.StartTrackedCoroutine(ResetBurstAfterFinish());
        }

        private IEnumerator ResetBurstAfterFinish()
        {
            yield return new WaitUntil(() => !burstVFX.isPlaying);
            burstVFX.transform.position = transform.position;
        }
        #endregion

        #region Prize Multiplier
        public void PlayPrizeMultiplierAnim(Vector3 target, float animSpeed, System.Action onComplete)
        {
            Vector2 start = transform.position;
            ShowExtraAnim();
            StopAnimation();
            if (target == transform.position)
            {
                onComplete?.Invoke();
                return;
            }

            _cachedTween = TweenFactory.Tween(
                "PrizeMultiplierTween" + uniqueID,
                0f, 1f,
                animSpeed,
                TweenScaleFunctions.CubicEaseIn,
                t => {
                    extraSprite.transform.position = Vector2.Lerp(start, target, t.CurrentValue);
                    pixieDust.gameObject.transform.position = Vector2.Lerp(start, target, t.CurrentValue);
                },
                t => {
                    orb.Stop();
                    orb.Clear();
                    pixieDust.Stop();

                    ring.transform.position = target;
                    ring.Play();

                    

                    WkCoroutine.instance.StartTrackedCoroutine(DelayThenComplete(animSpeed, onComplete));
                });
        }
        #endregion

        #region Prize Addition
        public void PlayPrizeAdditionAnim(Vector3 target, float animSpeed, System.Action onComplete)
        {
            Vector2 start = transform.position;
            ShowExtraAnim();
            WkAudioManager.instance.PlayAudio("IngotZap");

            if (target == transform.position)
            {
                onComplete?.Invoke();
                return;
            }

            _cachedTween = TweenFactory.Tween(
                "PrizeAdditionTween" + uniqueID,
                0f, 1f,
                animSpeed,
                TweenScaleFunctions.CubicEaseIn,
                t => {
                    extraSprite.transform.position = Vector2.Lerp(start, target, t.CurrentValue);
                    pixieDust.gameObject.transform.position = Vector2.Lerp(start, target, t.CurrentValue);
                },
                t => {
                    orb.Stop();
                    orb.Clear();
                    pixieDust.Stop();

                    ring.transform.position = target;               
                    ring.Play();

                    

                    WkCoroutine.instance.StartTrackedCoroutine(DelayThenComplete(animSpeed, onComplete));
                });
        }
        #endregion
        private IEnumerator DelayThenComplete(float animSpeed, Action onComplete)
        {
            yield return new WaitForSeconds(0.3f * animSpeed);
            ResetExtraAnim();
            onComplete?.Invoke();
        }
        public void StartIngotTransform(int ingotValue)
        {
            Vector3 start = rendererCollection["FeatureGraphic"].gameObject.transform.localScale;
            Vector3 peak = start * 1.3f;

            _cachedTween = TweenFactory.Tween(
                "BounceScaleUp_" + uniqueID,
                0f, 1f,
                0.16f,
                TweenScaleFunctions.SineEaseOut,
                t => {
                    rendererCollection["FeatureGraphic"].transform.localScale = Vector3.Lerp(start, peak, t.CurrentValue);
                },
                t => {
                    _cachedTween = TweenFactory.Tween(
                        "BounceScaleDown_" + uniqueID,
                        0f, 1f,
                        0.16f,
                        TweenScaleFunctions.SineEaseIn,
                        t2 => {
                            rendererCollection["FeatureGraphic"].transform.localScale = Vector3.Lerp(peak, start, t2.CurrentValue);
                        },
                        t2 => {
                            WkCoroutine.instance.StartTrackedCoroutine(DelayTransform(ingotValue));
                        });
                });
        }

        private IEnumerator DelayTransform(int ingotValue)
        {
            yield return new WaitForSeconds(0.3f);
            WkAudioManager.instance.PlayAudio("IngotAward");
            IngotValueTransform(ingotValue);
        }

        public void IngotValueTransform(int ingotValue)
        {
            SetIngotValue(ingotValue);
            AddTextInfo(ingotValueToAdd, OffsetWidth, OffsetHeight);
        }

        private void ShowExtraAnim()
        {
            pixieDust.Play();
            extraSpriteRenderer.enabled = true;
            switch (symbolID)
            {
                case PrizeMultiplierIngot:
                    extraSpriteRenderer.enabled = false;
                    orb.Play();
                    break;
            }
        }

        private void ResetExtraAnim()
        {
            pixieDust.Stop();
            orb.Stop();

            extraSprite.gameObject.transform.position = transform.position + new Vector3(0, 0.25f);
            extraSprite.gameObject.transform.localScale = transform.localScale * 0.8f;
            extraSpriteRenderer.enabled = false;
            ring.transform.position = transform.position;
            pixieDust.gameObject.transform.position = transform.position;
        }

        #region Animation

        /// <summary>
        /// Used by Reel.OnNudgeComplete to play a fast win animation
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="key"></param>
        /// <param name="callBack"></param>
        public void PlayNudgeAnimation(float duration, string key, Action callBack = null)
        {
            if (CheckScatter())
            {
                callBack = () => PlaySpecificAnimation("Loop", -1, rendererCollection["Symbol"]);
            }

            base.PlayGameSpecificAnimation(duration, key, callBack);
        }

        protected override void PlayGameSpecificAnimation(float duration, string key, Action callBack = null)
        {
            if (CheckScatter())
            {
                callBack = () => PlaySpecificAnimation("Loop", -1, rendererCollection["Symbol"]);
            }

            base.PlayGameSpecificAnimation(duration, key, callBack);
        }

        public void PlayLandingAnim()
        {
            if (CheckNormalIngot() ||  CheckJackpotIngot() || CheckScatter())
            {
                PlaySpecificAnimation("Landing", 1, rendererCollection["Symbol"], () => PlaySpecificAnimation("Loop", -1, rendererCollection["Symbol"]));
            }                 
        }

        public void PlayAwardAnimation()
        {
            StopGameSpecificAnimation();
            bool isEn = WkLobbySceneManager.instance.dataModel.language == WkGameLanguage.EN;
            if (CheckJackpotIngot())
            {
                _isJpIngotAnimPlaying = true;
                WkAudioManager.instance.PlayAudio("IngotAward");
                PlaySpecificAnimation("Award", 1, rendererCollection["Symbol"], () => PlaySpecificAnimation("Loop", -1, rendererCollection["Symbol"]));
                if (_jackpotType == 1)
                {
                    PlaySpecificAnimation(isEn ? "Grand_en" : "Grand_zh", 1, rendererCollection["FeatureGraphic"], () => _isJpIngotAnimPlaying = false);
                }
                else if (_jackpotType == 2)
                {
                    PlaySpecificAnimation(isEn ? "Major_en" : "Major_zh", 1, rendererCollection["FeatureGraphic"], () => _isJpIngotAnimPlaying = false);
                }
                else if (_jackpotType == 3)
                {
                    PlaySpecificAnimation(isEn ? "Minor_en" : "Minor_zh", 1, rendererCollection["FeatureGraphic"], () => _isJpIngotAnimPlaying = false);
                }
                else if (_jackpotType == 4)
                {
                    PlaySpecificAnimation(isEn ? "Mini_en" : "Mini_zh", 1, rendererCollection["FeatureGraphic"], () => _isJpIngotAnimPlaying = false);
                }
            }        
        }

        public void PlayLoopAnimation()
        {
            if (CheckNormalIngot() || CheckScatter())
            {
                PlaySpecificAnimation("Loop", -1, rendererCollection["Symbol"]);
            }             
        }

        public override void StopAnimation()
        {
            base.StopAnimation();            
            WkAnimator.instance.StopTaskByKey($"{uniqueID}_Win");
        }

        public override void StopGameSpecificAnimation()
        {
            base.StopGameSpecificAnimation();
            WkAnimator.instance.StopTaskByKey($"{uniqueID}_Landing");
            WkAnimator.instance.StopTaskByKey($"{uniqueID}_Loop");
        }
        #endregion

        public void StopAllTween()
        {
            if (CheckPrizeMultiplierIngot())
            {
                if (_cachedTween != null)
                {
                    TweenFactory.RemoveTween(_cachedTween, TweenStopBehavior.DoNotModify);
                }              
            }

            ResetExtraAnim();
        }

        #region Override
        public override void ShowSymbol()
        {
            foreach(var dict in rendererCollection)
            {
                if (dict.Key.Equals("FeatureGraphic")) continue;
                dict.Value.gameObject.SetActive(true);
            }
        }
        #endregion

        #region Helper
        //Control Point for Curve
        private static Vector2 MakeArcControl(Vector2 p0, Vector2 p1, float arcFactor)
        {
            Vector2 d = p1 - p0;
            float len = d.magnitude;
            if (len <= 1e-5f) return (p0 + p1) * 0.5f;
            Vector2 n = new Vector2(-d.y, d.x).normalized; // 2D normal
            return (p0 + p1) * 0.5f + n * (arcFactor * len);
        }

        // Quadratic Bezier
        private static Vector2 QuadBezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float s = 1f - t;
            return s * s * a + 2f * s * t * c + t * t * b;
        }
        #endregion

        #region History
        public void StartIngotTransformWithoutAnimation(int ingotValue)
        {
            IngotValueTransform(ingotValue);
        }
        #endregion

        public void SetSymbolSize(float size)
        {
            if(size == 0.77f)
            {
                isChangeAnim = true;
            }
            else if (size == 1f)
            {
                isChangeAnim = false;
            }
            normalSize = new Vector3(size, size, size);
            SetGraphicsSize();
        }

        protected override void PlayNormalWinAnimation(float duration)
        {
            if (isChangeAnim)
            {
                int num = 2;
                float duration2 = duration / (float)num;
                WkAnimationKeyFrame<Vector3>[] collection = new WkAnimationKeyFrame<Vector3>[1]
                {
                    new WkAnimationKeyFrame<Vector3>(Vector3.one, duration2, _curve)
                };
                Stack<WkAnimationKeyFrame<Vector3>> keyFrameCollection = new Stack<WkAnimationKeyFrame<Vector3>>(collection);
                WkAnimationClip<Vector3> clip = new WkAnimationClip<Vector3>(num, keyFrameCollection);
                WkLocalScaleWithAnimationCurveAnimatorTask task = new WkLocalScaleWithAnimationCurveAnimatorTask(uniqueID, clip, rendererWrapper);
                CallAnimatorPlayAnimation(task);
            }
            else
            {
                base.PlayNormalWinAnimation(duration);
            }
        }
    }
}