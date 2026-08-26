using UnityEngine;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYMultiLanguageDisplayObject : WkMultiLanguageDisplayObject
    {
        [SerializeField] private SpriteRenderer graphic = null!;

        protected override void OnLanguageChange()
        {
            if (collection is null) return;
            string lang = language.ToString().ToLower();

            if (collection.TryGetValue(lang, out Sprite? s))
            {
                graphic.sprite = s;
            }
        }
    }
}
