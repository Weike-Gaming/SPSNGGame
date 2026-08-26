using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYPreAnimDisplayObject : WkDisplayObject
    {
        [SerializeField] private PlayableDirector preAnimTimeline;
        [SerializeField] private SignalAsset whooshSignal;
        [SerializeField] private SignalAsset goldCoinSignal;
        [SerializeField] private SignalAsset elephantSignal;
        private SignalReceiver _receiver;

        #region Binding
        public bool extremeBigWinPreSpinAnim { get; set; }
        #endregion

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
            StopTimeline();
        }

        protected override void Awake()
        {
            base.Awake();
            _receiver = GetComponent<SignalReceiver>();

            // WHOOSH
            {
                UnityEvent evt = new UnityEvent();
                evt.AddListener(() => PlayAudio("WhooshPreSpin"));
                _receiver.AddReaction(whooshSignal, evt);
            }

            // GONG
            {
                UnityEvent evt = new UnityEvent();
                evt.AddListener(() => PlayAudio("GoldCoinPreSpin"));
                _receiver.AddReaction(goldCoinSignal, evt);
            }

            // ELEPHANT
            {
                UnityEvent evt = new UnityEvent();
                evt.AddListener(() => PlayAudio("ElephantPreSpin"));
                _receiver.AddReaction(elephantSignal, evt);
            }
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            wkModelCollection.AddModel(dm);

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("fg-spin")!.onRecoverState += StopTimeline;
            getState("spin")!.onRecoverState += StopTimeline;
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            if(extremeBigWinPreSpinAnim)
            {
                PlayPreAnim();
            }
        }

        private void PlayPreAnim()
        {
            preAnimTimeline.time = 0;
            preAnimTimeline.Play();
        }

        private void PlayAudio(string key)
        {
            getActiveAudioManager.PlayAudio(key);
        }

        private void StopTimeline()
        {
            preAnimTimeline.time = preAnimTimeline.duration;
            preAnimTimeline.Evaluate();
            preAnimTimeline.Pause();
        }
    }
}
