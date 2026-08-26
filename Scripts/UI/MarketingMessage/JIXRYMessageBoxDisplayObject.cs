using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.Core;
using Weike.LobbyManagement;

namespace Weike.Games.JIXRY
{
    public class JIXRYMessageBoxDisplayObject : WkDisplayObject
    {
        [SerializeField] private bool isFreeGame;
        [SerializeField] private GameObject enGameMessageBox;
        [SerializeField] private GameObject zhGameMessageBox;
        [SerializeField] private Sprite[] enGameMessageCollection;
        [SerializeField] private Sprite[] zhGameMessageCollection;

        [SerializeField] private GameObject coinLeftEffect;
        [SerializeField] private GameObject coinRightEffect;
        [SerializeField] private GameObject mask;
        private Animator _coinLeftAnimator;
        private Animator _coinRightAnimator;
        private Animator _maskAnimator;

        private SpriteRenderer _enSpriteRenderer;
        private SpriteRenderer _zhSpriteRenderer;
        private int _currentSpriteIndex = 0;
        private bool _isAnimationRunning = false;
        private readonly Vector2 _initialMaskPos = new Vector2(0f, 0.9f);
        private const float MessageDelay = 20f;

        #region Binding
        public WkGameLanguage language { get; set; } = WkGameLanguage.EN;
        #endregion
        #region WkDisplayObject Interface

        protected override void Awake()
        {
            base.Awake();
            _enSpriteRenderer = enGameMessageBox.GetComponent<SpriteRenderer>();
            _zhSpriteRenderer = zhGameMessageBox.GetComponent<SpriteRenderer>();
            _maskAnimator = mask.GetComponent<Animator>();
            _coinLeftAnimator = coinLeftEffect.GetComponent<Animator>();
            _coinRightAnimator = coinRightEffect.GetComponent<Animator>();
        }
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;
            if (lobby is null || lobby.dataModel is null)
            {
                return;
            }
            wkModelCollection.AddModel(lobby.dataModel);
            ResetSprite();
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            SetLanguage();
        }

        protected override void OnAllowedEnable()
        {
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;
            if (lobby is null)
            {
                return;
            }
            language = lobby.dataModel.language;
            _currentSpriteIndex = 0;
            Sprite[] activeCollection = language == WkGameLanguage.EN ? enGameMessageCollection : zhGameMessageCollection;
            UpdateSpriteIndex(activeCollection.Length);
            SetLanguage();
        }

        protected override void ResetToDefault()
        {
            _isAnimationRunning = false;
            _currentSpriteIndex = 0;
            Sprite[] activeCollection = language == WkGameLanguage.EN ? enGameMessageCollection : zhGameMessageCollection;
            UpdateSpriteIndex(activeCollection.Length);
            ResetSprite();
            ResetAnimator();
        }
        #endregion

        #region Unity Interface
        private void Update()
        {
            if (machineContext is null)
            {
                return;
            }
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            UpdateMessageBoxVisibility(machineContext.platformInterface.IsMachineIdSet());
        }
        #endregion

        private void SetLanguage()
        {
            if (owningPlayerController?.owner is not JIXRYGameManager { gameObject: { activeInHierarchy: bool isActive } })
            {
                return;
            }
            if (!isActive)
            {
                return;
            }
            UpdateLanguage();
        }

        public void UpdateLanguage()
        {
            _enSpriteRenderer.enabled = (language == WkGameLanguage.EN);
            _zhSpriteRenderer.enabled = (language == WkGameLanguage.ZH);
            Sprite[] activeCollection = isFreeGame ? GetFreeGameSprite() : GetMainGameSprite();

            if (language == WkGameLanguage.EN)
            {
                _enSpriteRenderer.sprite = activeCollection[_currentSpriteIndex];
            }
            else
            {
                _zhSpriteRenderer.sprite = activeCollection[_currentSpriteIndex];
            }
            StartSpriteLoop();
        }

        private Sprite[] GetFreeGameSprite()
        {
            Sprite[] activeCollection = language == WkGameLanguage.EN ? enGameMessageCollection : zhGameMessageCollection;

            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            List<int> indices = new List<int> { 0 };

            string gameType = gm.GetCurrentGameType();

            // Add "[Ingot] pays left to right on 2 or more reels!"
            indices.Add(0);

            // Remaining indices are as follows
            // 1: Any [RU coin] appearing may trigger the Reel Upgrade bonus
            // 2: Any [JP coin] appearing may trigger the Jackpot bonus
            // 3: Any [RN coin] appearing may trigger the Reel Nudge bonus
            // 4: [Grand], [Major], [Minor], and [Mini] jackpots are added to Reel 5
            // 5: Prize multipliers are added to Reels 3

            // Add Feature specific messages
            if (gameType.Contains("RN") && gameType.Contains("JP") && gameType.Contains("RU"))
            {
                indices.Add(4);
                indices.Add(5);
            }
            else if (gameType.Contains("RN") && gameType.Contains("JP"))
            {
                indices.Add(1);
                indices.Add(4);
            }
            else if(gameType.Contains("JP") && gameType.Contains("RU"))
            {
                indices.Add(3);
                indices.Add(4);
                indices.Add(5);
            }
            else if (gameType.Contains("RN") && gameType.Contains("RU"))
            {
                indices.Add(2);
                indices.Add(5);
            }
            else if (gameType.Contains("RN"))
            {
                indices.Add(1);
                indices.Add(2);
            }
            else if (gameType.Contains("RU"))
            {
                indices.Add(2);
                indices.Add(3);
                indices.Add(5);
            }
            else if (gameType.Contains("JP"))
            {
                indices.Add(1);
                indices.Add(3);
                indices.Add(4);
            }


            return indices.Select(i => activeCollection[i]).ToArray();
        }

        private Sprite[] GetMainGameSprite()
        {
            Sprite[] activeCollection = language == WkGameLanguage.EN ? enGameMessageCollection : zhGameMessageCollection;
            return activeCollection;
        }

        public void ResetSprite()
        {
            _enSpriteRenderer.sprite = null;
            _zhSpriteRenderer.sprite = null;
        }

        private void UpdateMessageBoxVisibility(bool isActive = true)
        {
            enGameMessageBox.SetActive(isActive);
            zhGameMessageBox.SetActive(isActive);
        }

        private void StartSpriteLoop()
        {
            WkCoroutine.instance.StartTrackedCoroutine(SpriteLoopCoroutine());
        }

        private IEnumerator SpriteLoopCoroutine()
        {
            while (!_isAnimationRunning && gameObject.activeInHierarchy)
            {
                Sprite[] activeCollection = isFreeGame ? GetFreeGameSprite() : GetMainGameSprite();
                _isAnimationRunning = true;
                _enSpriteRenderer.enabled = false;
                _zhSpriteRenderer.enabled = false;

                if (_currentSpriteIndex >= activeCollection.Length)
                {
                    _currentSpriteIndex = 0;
                }

                yield return PlayCoinAnimation(activeCollection[_currentSpriteIndex]);
                UpdateSpriteIndex(activeCollection.Length);
            }
        }

        private IEnumerator PlayCoinAnimation(Sprite newSprite)
        {
            _enSpriteRenderer.enabled = (language == WkGameLanguage.EN);
            _zhSpriteRenderer.enabled = (language == WkGameLanguage.ZH);
            _maskAnimator.Update(0);
            _maskAnimator.Rebind();
            yield return new WaitForSeconds(0.1f);
            _coinLeftAnimator.Update(0);
            _coinLeftAnimator.Rebind();
            _coinLeftAnimator.enabled = true;
            _coinRightAnimator.Update(0);
            _coinRightAnimator.Rebind();
            _coinRightAnimator.enabled = true;
            yield return new WaitForSeconds(0.1f);
            _maskAnimator.enabled = true;
            _maskAnimator.SetTrigger("Play");

            if (language == WkGameLanguage.EN)
            {
                _enSpriteRenderer.sprite = newSprite;
            }
            else
            {
                _zhSpriteRenderer.sprite = newSprite;
            }
            yield return new WaitForSeconds(MessageDelay);
            _isAnimationRunning = false;
        }

        private void ResetAnimator()
        {
            _maskAnimator.Update(0);
            _maskAnimator.Rebind();
            _maskAnimator.enabled = false;
            _coinLeftAnimator.Update(0);
            _coinLeftAnimator.Rebind();
            _coinLeftAnimator.enabled = false;
            _coinRightAnimator.Update(0);
            _coinRightAnimator.Rebind();
            _coinRightAnimator.enabled = false;
            mask.transform.localScale = _initialMaskPos;
        }

        private int UpdateSpriteIndex(int size)
        {
            if (machineContext is null)
            {
                return _currentSpriteIndex;
            }
            if (machineContext.platformInterface!.ShowProgressiveMeters())
            {
                _currentSpriteIndex++;
                if (_currentSpriteIndex >= size)
                {
                    _currentSpriteIndex = 0;
                }
            }
            else
            {
                for (int i = 0; i < size; ++i)
                {
                    _currentSpriteIndex++;
                    if (_currentSpriteIndex >= size)
                    {
                        _currentSpriteIndex = 0;
                    }
                }
            }
            return _currentSpriteIndex;
        }
    }
}