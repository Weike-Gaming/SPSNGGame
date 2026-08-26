using DigitalRuby.Tween;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYLobbyOdoMeterFixAmountDO : WkTransformCustomFont
    {
        [Header("Currency")]
        [SerializeField] private Texture2D currencyTextures = null!;
        [SerializeField] private TextAsset currencyTexturesSS = null!;
        [SerializeField] private Texture2D naTexture = null!;

        [SerializeField] private Texture2D genericCurrencyTexture = null!;
        [SerializeField] private TextAsset genericCurrencyTextureSS = null!;

        [SerializeField] private Vector2 currencyOffset;
        [SerializeField] private bool fixSize = false;
        [SerializeField] private float fixScale = 1;

        [SerializeField] private Vector2 maxSize = Vector2.one;
        [SerializeField] private GameObject symbolPrefab;

        [Header("Debug")]
        [SerializeField] private Color debugColor;

        [SerializeField] private int prizeIndex;

        [SerializeField] private Vector2 genericScale = Vector2.one;
        private readonly Dictionary<string, SSFrame> _cacheCurrencyTexture = new();
        private readonly Dictionary<char, SSFrame> _cacheGenericCurrencyFrames = new();
        private List<IWkRenderProperty> _symbolsAreas = new List<IWkRenderProperty>();

        private const float Duration = 3f;

        /// <summary>
        /// Current credit
        /// </summary>
        protected ulong amount;
        protected readonly List<Transform> transforms = new List<Transform>();
        private float _accumulatedWidth;
        private bool _isGenericCurrency = false;
        #region Binding Properties

        /// <summary>
        /// Currency from Binding
        /// </summary>
        private string? _currency = "USD";

        #endregion

        public JIXRYLobbyOdoMeterFixAmountDO()
        {
            isLobby = true;
        }

        #region Unity Interface

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            ProcessJson();
        }

        #endregion

        protected override void ClearCharacters()
        {
            base.ClearCharacters();
            _accumulatedWidth = 0;
            DestroyAllChild();
        }

        /// <summary>
        /// Clear everything, and regenerate digit
        /// </summary>
        public void SetUpAndGenerateDigits(ulong denomValue = 1, bool isFirst = false)
        {
            _accumulatedWidth = 0f;
            ClearCharacters();
            transform.localPosition = new Vector3(transform.localPosition.x, 0.75f, transform.localPosition.z);
            Vector3 initialPos = transform.localPosition;
            Vector3 target = new Vector3(transform.localPosition.x, -0.25f, transform.position.z);
            amount = (ulong)GetBasePrize() * denomValue;
            GenerateCharacters();
            if (!isFirst)
            {
                TweenFactory.Tween($"ToggleEnter{prizeIndex}",
                0f,
                1f,
                Duration,
                TweenScaleFunctions.QuadraticEaseOut,
                (alpha) =>
                {
                    float x = initialPos.x;
                    float y = WkEasing.EaseOutQuad(initialPos.y, target.y, alpha.CurrentValue);
                    Vector3 newPos = new(x, y, 0f);
                    transform.localPosition = newPos;
                });
            }
        }

        private void GenerateGenericCurrency()
        {
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);
            ReadOnlySpan<char> currency = machInfo.genericSymbol.AsSpan();

            if (currency.Length <= 2)
            {
                GenerateEmptySymbol();
            }

            if (currency.Length <= 1)
            {
                GenerateEmptySymbol();
            }

            if (currency.Length <= 0)
            {
                GenerateEmptySymbol();
            }

            foreach (char c in currency)
            {
                if (_cacheGenericCurrencyFrames.TryGetValue(c, out SSFrame currencyFrame))
                {
                    GenerateSymbol(genericCurrencyTexture, currencyFrame, genericScale);
                }
            }
        }

        private void GenerateEmptySymbol()
        {
            GameObject go = Instantiate(symbolPrefab, transform);
            WkRenderer renderer = go.GetComponent<WkRenderer>();
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = Vector3.zero;

            _symbolsAreas.Add(renderer);
            transforms.Add(go.transform);
        }

        private long GetBasePrize()
        {
            int gameIndex = WkLobbySceneManager.instance.GetHighestGame();
            long denomValue = WkLobbySceneManager.instance.GetGameDenomValue(gameIndex) <= 0 ? 1 : WkLobbySceneManager.instance.GetGameDenomValue(gameIndex);
            return (machineContext?.platformInterface?.GetStdProgressiveAmount(prizeIndex) ?? 0) / denomValue;
        }

        public void ToggleOffDigits()
        {
            transform.localPosition = new Vector3(transform.localPosition.x, -0.25f, transform.localPosition.z);
            Vector3 initialPos = transform.localPosition;
            Vector3 target = new Vector3(transform.localPosition.x, -1.25f, transform.position.z);
            TweenFactory.Tween($"ToggleLeave{prizeIndex}",
                0f,
                1f,
                Duration,
                TweenScaleFunctions.QuadraticEaseOut,
                (alpha) =>
                {
                    float x = initialPos.x;
                    float y = WkEasing.EaseOutQuad(initialPos.y, target.y, alpha.CurrentValue);
                    Vector3 newPos = new(x, y, 0f);
                    transform.localPosition = newPos;
                });
        }

        /// <summary>
        /// Keep digit in the center
        /// </summary>
        private void CenterAlignment()
        {
            float width = maxSize.x;
            float space = (width - _accumulatedWidth) / 2;
            float x = (width / 2) - space;
            Vector2 newPos = Vector2.zero;
            if (transforms.Count <= 1)
            {
                return;
            }

            Vector2 currencyScale = scale;

            if (_isGenericCurrency)
            {
                currencyScale = genericScale;
                currencyScale.x *= scale.x;
            }

            // digits
            for (int i = 3; i < transforms.Count; i++)
            {
                x -= _symbolsAreas[i].area.x * scale.x / 2;
                x -= i > 3 ? _symbolsAreas[i - 1].area.x * scale.x / 2 : 0;
                Vector3 pos = transforms[i].transform.localPosition;
                pos.x = x / pixelPerUnits;
                transforms[i].transform.localPosition = pos;
            }

            // currency
            for (int i = 2; i >= 0; i--)
            {
                x -= _symbolsAreas[i].area.x * currencyScale.x / 2;
                if (i == 2)
                {
                    x -= _symbolsAreas[i + 1].area.x * scale.x / 2;
                }
                else
                {
                    x -= _symbolsAreas[i + 1].area.x * currencyScale.x / 2;
                }

                newPos.x = x / pixelPerUnits;
                transforms[i].transform.localPosition = newPos + currencyOffset;
            }
        }

        /// <summary>
        /// Generate Currency symbol and Filler symbol
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="frame"></param>
        private void GenerateSymbol(Texture2D texture, SSFrame frame, Vector2 setScale)
        {
            GameObject go = Instantiate(symbolPrefab, transform);
            WkRenderer renderer = go.GetComponent<WkRenderer>();
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = setScale;
            renderer.CreateMaterial(texture, frame, 1);
            renderer.setSortingLayer = gameLayer;
            renderer.setLayerOrder = gameLayerOrder;
            renderer.SetMaskInteraction = maskInteraction;
            renderer.Draw();
            _accumulatedWidth += frame.w * setScale.x;
            _symbolsAreas.Add(renderer);

            transforms.Add(go.transform);
        }

        /// <summary>
        /// Calculate Size for digit
        /// </summary>
        private void CalculateSize()
        {
            if (cacheH <= 0 || cacheW <= 0)
            {
                ProcessJson();
            };

            WkLobbySceneManager? lobby = WkLobbySceneManager.instance;
            _currency = lobby is null ? "USD" : lobby.dataModel.currency;

            string money = WkCoreCurrencyUtils.GetCurrencyInStringNoSymbol(_currency!, amount);
            ReadOnlySpan<char> chars = money.AsSpan();

            float width = 0;

            if (WkCoreCurrencyUtils.ShowCurrencyOnGameDisplay(_currency!))
            {
                if (_cacheCurrencyTexture.TryGetValue(_currency!, out SSFrame currencyFrame))
                {
                    width += currencyFrame.w;
                }
                else
                {
                    MachInfoWrapper machInfo = new();
                    MachInfoWrapper.PlExtGetMachInfo(machInfo);
                    ReadOnlySpan<char> currency = machInfo.genericSymbol.AsSpan();

                    float height = 0;

                    if (_cacheGenericCurrencyFrames.TryGetValue('W', out SSFrame ssframe))
                    {
                        genericScale.x = cacheW / ssframe.w;
                    }

                    foreach (char c in currency)
                    {
                        if (_cacheGenericCurrencyFrames.TryGetValue(c, out SSFrame f))
                        {
                            width += f.w * genericScale.x;
                            height = f.h;
                        }
                    }

                    genericScale.y = maxSize.y / height;
                }
            }

            for (int i = chars.Length - 1; i >= 0; i--)
            {
                char c = chars[i];
                if ((c == ',' || c == '.' || c == ' ') && cachedFrames.TryGetValue(c, out SSFrame fillerFrame))
                {
                    width += fillerFrame.w;
                    continue;
                }

                width += cacheW;
            }

            float maxWidth = maxSize.x;

            if (maxWidth < width)
            {
                scale.x = maxWidth / width;
            }

            if (fixSize)
            {
                scale.x = fixScale;
            }

            float maxHeight = maxSize.y;
            scale.y = maxHeight / cacheH;
        }

        /// <summary>
        /// Process Json Files
        /// </summary>
        protected override void ProcessJson()
        {
            JObject loadedJson;
            JObject allFrames;

            if (currencyTextures is null || spriteSheet is null) return;

            float currencySourceHeight = currencyTextures.height;
            float digitSourceHeight = spriteSheet.height;
            loadedJson = JObject.Parse(currencyTexturesSS.text);
            if (!WkAssert.EnsureMsgf(loadedJson, "Failed to load JSON Obj")) return;

            loadedJson.TryGetValue("frames", out JToken? currencyAllFramesToken);

            allFrames = currencyAllFramesToken as JObject ?? throw new NullReferenceException();

            foreach (JProperty frame in allFrames.Children<JProperty>())
            {
                JProperty? frameDetail = frame.Value.First as JProperty;

                if (frameDetail?.Value is not JObject frameObj) continue;

                float x = (float)(frameObj.GetValue("x") ?? throw new NullReferenceException());
                float y = (float)(frameObj.GetValue("y") ?? throw new NullReferenceException());
                float w = (float)(frameObj.GetValue("w") ?? throw new NullReferenceException());
                float h = (float)(frameObj.GetValue("h") ?? throw new NullReferenceException());

                float finalY = -y + currencySourceHeight - h;
                SSFrame ssFrame = new(x, finalY, w, h);
                _cacheCurrencyTexture.TryAdd(frame.Name, ssFrame);
            }

            if (spriteSheetJson is null) return;

            loadedJson = JObject.Parse(spriteSheetJson.text);
            if (!WkAssert.EnsureMsgf(loadedJson, "Failed to load JSON Obj")) return;

            loadedJson.TryGetValue("frames", out JToken? allFramesToken);

            allFrames = allFramesToken as JObject ?? throw new NullReferenceException();

            foreach (JProperty frame in allFrames.Children<JProperty>())
            {
                JProperty? frameDetail = frame.Value.First as JProperty;

                if (frameDetail?.Value is not JObject frameObj) continue;

                float x = (float)(frameObj.GetValue("x") ?? throw new NullReferenceException());
                float y = (float)(frameObj.GetValue("y") ?? throw new NullReferenceException());
                float w = (float)(frameObj.GetValue("w") ?? throw new NullReferenceException());
                float h = (float)(frameObj.GetValue("h") ?? throw new NullReferenceException());

                if (cacheW <= 0f)
                {
                    cacheW = w;
                }

                if (cacheH <= 0f)
                {
                    cacheH = h;
                }

                float finalY = -y + currencySourceHeight - h;
                SSFrame ssFrame = new(x, finalY, w, h);
                cachedFrames.TryAdd(frame.Name.ToCharArray(0, 1)[0], ssFrame);
            }

            {
                float genericSourceHeight = genericCurrencyTexture.height;
                loadedJson = JObject.Parse(genericCurrencyTextureSS.text);
                if (!WkAssert.EnsureMsgf(loadedJson, "Failed to load JSON Obj")) return;
                loadedJson.TryGetValue("frames", out JToken? digitAllFramesToken);
                allFrames = digitAllFramesToken as JObject ?? throw new NullReferenceException();
                foreach (JProperty frame in allFrames.Children<JProperty>())
                {
                    JProperty? frameDetail = frame.Value.First as JProperty;
                    if (frameDetail?.Value is not JObject frameObj) continue;
                    float x = (float)(frameObj.GetValue("x") ?? throw new NullReferenceException());
                    float y = (float)(frameObj.GetValue("y") ?? throw new NullReferenceException());
                    float w = (float)(frameObj.GetValue("w") ?? throw new NullReferenceException());
                    float h = (float)(frameObj.GetValue("h") ?? throw new NullReferenceException());

                    if (cacheW <= 0f)
                    {
                        cacheW = w;
                    }

                    if (cacheH <= 0f)
                    {
                        cacheH = h;
                    }
                    float finalY = -y + genericSourceHeight - h;
                    SSFrame ssFrame = new(x, finalY, w, h);
                    _cacheGenericCurrencyFrames.TryAdd(frame.Name.ToCharArray(0, 1)[0], ssFrame);
                }
            }
        }

        ///<inheritdoc/>
        protected override void OnAllowedEnable()
        {
            ClearCharacters();
            transform.localPosition = new Vector3(transform.localPosition.x, -0.25f, transform.localPosition.z);
            if (machineContext?.platformInterface is null)
            {
                return;
            }
            WkPlatformInterface platform = machineContext.platformInterface;
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;
            Credits outCredit = new();
            bool noCredit = platform.GetCredit(outCredit) <= 0;
            if (!platform.IsStdProgressiveAmountReady() || lobby is null)
            {
                return;
            }

            if (noCredit)
            {
                amount = (ulong)GetBasePrize() * lobby.dataModel.targetDenomInLobby;
            }
            else
            {
                amount = (ulong)platform!.GetStdProgressiveAmount(prizeIndex);
            }
 
            CalculateSize();
            GenerateCharacters();
        }

        ///<inheritdoc/>
        protected override void ResetToDefault()
        {
            amount = 0;
            ClearCharacters();
            TweenFactory.Clear();
        }

        /// <summary>
        /// Clean up
        /// </summary>
        private void DestroyAllChild()
        {
            foreach (Transform childTransform in transform)
            {
                GameObject child = childTransform.gameObject;
                Destroy(child.gameObject);
            }
            transforms.Clear();
            _symbolsAreas = new List<IWkRenderProperty>();
        }

        public override void SetText(string newText)
        {
            
        }

        protected override void GenerateCharacters()
        {
            CalculateSize();

            if (amount <= 0)
            {
                float x = naTexture.texelSize.x;
                float y = naTexture.texelSize.y;
                float w = naTexture.width;
                float h = naTexture.height;
                SSFrame frame = new SSFrame(x, y, w, h);
                GenerateSymbol(naTexture, frame, scale);
                CenterAlignment();
                return;
            }
    
            string money = WkCoreCurrencyUtils.GetCurrencyInStringNoSymbol(_currency!, amount);
            ReadOnlySpan<char> chars = money.AsSpan();
            if (_cacheCurrencyTexture.TryGetValue(_currency!, out SSFrame currencyFrame))
            {
                GenerateSymbol(currencyTextures, currencyFrame, scale);
                GenerateEmptySymbol(); // fill up the index
                GenerateEmptySymbol(); // fill up the index
                _isGenericCurrency = false;
            }
            else
            {                
                GenerateGenericCurrency();
                _isGenericCurrency = true;
            }

            for (int i = chars.Length - 1; i >= 0; i--)
            {
                char c = chars[i];
                if ((c == ',' || c == '.') && cachedFrames.TryGetValue(c, out SSFrame fillerFrame))
                {
                    GenerateSymbol(spriteSheet, fillerFrame, scale);
                    continue;
                }

                WkRenderer item = CreateCharacter(c, Vector3.zero, scale);
                if (item is null) continue;

                _accumulatedWidth += cacheW * scale.x;
                _symbolsAreas.Add(item);
                transforms.Add(item.transform);
            }
            CenterAlignment();
        }
    }
}
