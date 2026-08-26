using System.Collections.Generic;
using UnityEngine;

namespace Weike.Games.JIXRY
{
    public class JIXRYFeatureTextController : MonoBehaviour
    {
        [SerializeField] private GameObject spinText;
        [SerializeField] private GameObject jackpotText;
        [SerializeField] private GameObject prizeText;
        [SerializeField] private float verticalSpacing = 0.3f;
        [SerializeField] private bool useFixedHeight = false;
        [SerializeField] private float fixedItemHeight = 1.0f;

        public void UpdateVerticalBox()
        {
            // Ordered children
            List<GameObject> ordered = new List<GameObject> { spinText, jackpotText, prizeText };

            // Collect active ones
            List<GameObject> activeItems = new List<GameObject>();
            for (int i = 0; i < ordered.Count; i++)
            {
                GameObject go = ordered[i];
                if (go != null && go.activeSelf)
                {
                    activeItems.Add(go);
                }
            }

            if (activeItems.Count == 0)
            {
                return;
            }

            // Measure heights
            List<float> heights = new List<float>(activeItems.Count);
            for (int i = 0; i < activeItems.Count; i++)
            {
                GameObject item = activeItems[i];
                float itemHeight = GetItemHeight(item);
                heights.Add(itemHeight);
            }

            // Decide on a uniform item height for all
            float itemHeightToUse = fixedItemHeight;
            if (!useFixedHeight)
            {
                // Use the tallest active item
                itemHeightToUse = 0f;
                for (int i = 0; i < heights.Count; i++)
                {
                    if (heights[i] > itemHeightToUse)
                    {
                        itemHeightToUse = heights[i];
                    }
                }
            }

            // Calculate total stack height
            float totalHeight = (itemHeightToUse * activeItems.Count) + verticalSpacing * (activeItems.Count - 1);

            // Position items centered around parent
            float cursorFromTop = totalHeight * 0.5f;
            for (int i = 0; i < activeItems.Count; i++)
            {
                GameObject go = activeItems[i];
                float yCenter = cursorFromTop - (itemHeightToUse * 0.5f);

                Vector3 lp = go.transform.localPosition;
                go.transform.localPosition = new Vector3(0f, yCenter, lp.z);

                cursorFromTop -= (itemHeightToUse + verticalSpacing);
            }
        }

        private float GetItemHeight(GameObject go)
        {
            if (useFixedHeight)
            {
                return fixedItemHeight;
            }

            // Try to measure using Renderer
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }

                float parentScaleY = transform.lossyScale.y != 0f ? transform.lossyScale.y : 1f;
                return b.size.y / parentScaleY;
            }

            // Try UI RectTransform
            RectTransform rt = go.GetComponentInChildren<RectTransform>();
            if (rt != null)
            {
                float scaleY = rt.lossyScale.y != 0f ? rt.lossyScale.y : 1f;
                return rt.rect.height / scaleY;
            }

            // Fallback
            return fixedItemHeight;
        }
    }
}