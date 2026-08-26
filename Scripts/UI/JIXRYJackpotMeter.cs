using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYJackpotMeter : WkDisplayObject
    {
        [SerializeField] private string key;
        private Dictionary<string, WkAdvanceRenderer> _rendererCollection = new Dictionary<string, WkAdvanceRenderer>();
        [SerializeField] private TextAsset[] jsonCollection = null!;
        [SerializeField] private Texture2D[] textureCollection = null!;
        private List<SSFrame> _framesCollection = null!;
        private int[] _totalSpriteInSS = null!;
        private List<string> _animationKeyCollection = new List<string>();

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
            StopAllAnimation();
        }

        protected override void Awake()
        {
            base.Awake();
            ProcessJson();
            SetUpRenderer();           
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            string[] recoverStates = { "idle", "spin", "jp-announcement", "fg-transition", "fg-init", "fg-spin", "fg-session-end", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot" };

            foreach (string state in recoverStates)
            {
                getState(state)!.onRecoverState += () => PlayMeterAnimation(-1, _rendererCollection["JackpotMeter"]);
            }
        }

        #region Initialize Renderer
        private void SetUpRenderer()
        {
            _rendererCollection = new Dictionary<string, WkAdvanceRenderer>()
            {
                {"JackpotMeter", InstantiateRenderer("JackpotMeter")}
            };

            _rendererCollection["JackpotMeter"].SetSortingOrder(10);

            UpdateGraphic("JackpotMeter");
        }

        private WkAdvanceRenderer InstantiateRenderer(string name)
        {
            GameObject go = new GameObject();
            WkAdvanceRenderer renderer = go.AddComponent<WkAdvanceRenderer>();
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.name = $"{name}_Renderer";
            return renderer;
        }

        protected void UpdateGraphic(string graphic)
        {
            Texture2D texture = GetLastTexture();
            SSFrame frame = GetLastFrame();
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

        private Texture2D GetLastTexture()
        {
            WkAssert.EnsureMsgf(textureCollection.Length > 0, "Get Last Texture Fail, collection is less than 0");
            return textureCollection[textureCollection.Length - 1];
        }

        private SSFrame GetLastFrame()
        {
            WkAssert.EnsureMsgf(_framesCollection.Count > 0, "Get Last Frame Fail, collection is less than 0");
            return _framesCollection[_framesCollection.Count - 1];
        }
        #endregion

        #region Animation
        private void PlayMeterAnimation(int loopCount, WkAdvanceRenderer renderer, Action callback = null)
        {
            renderer.Draw();
            const int keyFrameAmount = 1;
            string animationKey = key;
            ReadOnlySpan<Texture2D> collection = textureCollection;
            ReadOnlySpan<SSFrame> frames = _framesCollection.ToArray();
            ReadOnlySpan<int> totalSpriteInSS = _totalSpriteInSS;
            int start = 0;
            int end = frames.Length;
            int framePerSecond = 24;
            float animationDuration = (float)frames.Length / (float)framePerSecond;
            WkAnimationKeyFrame<int>[] array = new WkAnimationKeyFrame<int>[keyFrameAmount]
            {
                new WkAnimationKeyFrame<int>(start, end, animationDuration)
            };

            Stack<WkAnimationKeyFrame<int>> keyFrameCollection = new Stack<WkAnimationKeyFrame<int>>(array);
            WkAnimationClip<int> clip = new WkAnimationClip<int>(loopCount, keyFrameCollection);
            WkSpriteAnimatorTask spriteTask = new WkSpriteAnimatorTask(key, "Base", renderer, collection.ToArray(), totalSpriteInSS.ToArray(), frames, clip, callback);
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
        }
        #endregion

        #region Deserialize Json
        private void ProcessJson()
        {
            _framesCollection = new List<SSFrame>();
            _totalSpriteInSS = new int[jsonCollection.Length];
            for (int i = 0; i < jsonCollection.Length; i++)
            {
                int count = 0;
                TextAsset json = jsonCollection[i] ?? throw new Exception();

                JObject loadedJson = JObject.Parse(json.text);
                if (!WkAssert.EnsureMsgf(loadedJson, "Failed to load JSON Obj")) return;

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
                    _framesCollection.Add(ssFrame);
                    count++;
                }
                _totalSpriteInSS[i] = count;
            }
        }       
        #endregion
    }
}
