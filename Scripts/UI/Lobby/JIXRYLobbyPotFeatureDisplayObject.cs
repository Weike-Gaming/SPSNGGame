using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    class LobbyPotLevelAnimationData
    {
        public Texture2D[] Textures;
        public List<SSFrame> Frames;
        public int[] TotalSpritesPerTexture;
    }

    public class JIXRYLobbyPotFeatureDisplayObject : WkMultiLanguageDisplayObject
    {
        [SerializeField] private string potType;
        [SerializeField] private Transform potCoinPosition;

        [Header("PotCoin")]
        private const int PotCoinMaxLevel = 5;
        [SerializeField] private Texture2D[] potCoinTextureCollection = null!;
        [SerializeField] private TextAsset[] potCoinJsonCollection = null!;
        private List<SSFrame> _potCoinFramesCollection = null!;
        private int[] _potCoinTotalSpriteInSS = null!;

        [Header("Text Shine")]
        [SerializeField] private SpriteRenderer textRenderer = null!;
        [SerializeField] private List<Material> textShineMaterial = null!;

        private Dictionary<string, WkAdvanceRenderer> _rendererCollection = new Dictionary<string, WkAdvanceRenderer>();
        private Dictionary<int, PotLevelAnimationData> _potCoinLevelData;

        private static readonly int[] ScatterThresholds = { 3, 6, 9, 12, 15 };
        private const int PotCoinSortingOrder = 10;

        protected override void Awake()
        {
            base.Awake();
            (_potCoinFramesCollection, _potCoinTotalSpriteInSS) = ProcessJsonCollection(potCoinJsonCollection);
            _potCoinLevelData = BuildLevelAnimationDictionary(potCoinTextureCollection, _potCoinFramesCollection, _potCoinTotalSpriteInSS, 2);

            SetUpRenderer();
        }

        private void Update()
        {
            WkLobbyGameModel dm = WkLobbySceneManager.instance?.dataModel;
            if (dm is null) return;
            language = dm.language;
            OnLanguageChange();
        }
        protected override void OnLanguageChange()
        {
            textRenderer.material = language == WkGameLanguage.EN ? textShineMaterial[0] : textShineMaterial[1];
        }

        protected override void OnAllowedEnable()
        {
            base.OnAllowedEnable();
            UpdatePotLevel();
            PlayLED();
        }

        private void UpdatePotLevel()
        {
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;

            if (lobby is null)
            {
                return;
            }

            int potLevel = 0;

            switch(potType)
            {
                case "RN":
                    potLevel = GetScatterLevel(lobby.dataModel.customData[0]);
                    UpdateGraphic(potLevel);
                    break;
                case "JP":
                    potLevel = GetScatterLevel(lobby.dataModel.customData[1]);
                    UpdateGraphic(potLevel);
                    break;
                case "RU":
                    potLevel = GetScatterLevel(lobby.dataModel.customData[2]);
                    UpdateGraphic(potLevel);
                    break;
            }
        }

        private void SetUpRenderer()
        {
            _rendererCollection = new Dictionary<string, WkAdvanceRenderer>()
            {
                {"PotCoin", InstantiateRenderer("PotCoin")}
            };

            _rendererCollection["PotCoin"].SetSortingOrder(PotCoinSortingOrder);

            UpdateGraphic(0);
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

        protected void UpdateGraphic(int level)
        {
            Texture2D texture = null;
            SSFrame frame = new SSFrame();

            if (level == 0)
            {
                texture = GetFirstTexture();
                frame = GetFirstFrame();
            }
            else
            {
                texture = GetLastTextureForLevel(level);
                frame = GetLastFrameForLevel(level);
            }

            DrawImageInfo info = new DrawImageInfo();
            info.texture = texture;
            info.frame = frame;
            info.scale = Vector2.one;

            AddDrawInfoAndDraw("PotCoin", info);
            if (level < PotCoinMaxLevel)
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = Vector2.zero;
            else
                _rendererCollection["PotCoin"].gameObject.transform.localPosition = new Vector2(0, -1.49f);

        }

        private void AddDrawInfoAndDraw(string key, IDrawInfo info)
        {
            _rendererCollection[key].AddDrawInfo("Base", info);
            _rendererCollection[key].Draw();
        }

        public Texture2D GetLastTextureForLevel(int level)
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

        public SSFrame GetLastFrameForLevel(int level)
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

        public Texture2D GetFirstTexture()
        {
            _potCoinLevelData.TryGetValue(1, out PotLevelAnimationData data);
            return data.Textures[0];
        }

        public SSFrame GetFirstFrame()
        {
            _potCoinLevelData.TryGetValue(1, out PotLevelAnimationData data);
            return data.Frames[0];
        }

        private int GetScatterLevel(int value)
        {
            for (int i = 0; i < ScatterThresholds.Length; i++)
            {
                if (value <= ScatterThresholds[i])
                    return i;
            }

            return ScatterThresholds.Length;
        }

        #region play led
        private void PlayLED()
        {
#if DEBUG
            Debug.Log("Play LED at lobby: staticColor");
#endif
            byte[] staticColor = new byte[] { 80, 0, 0 };
            WkMachineInstance.machineContext?.platformInterface?.GamePlayLedStaticColor(staticColor);
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
