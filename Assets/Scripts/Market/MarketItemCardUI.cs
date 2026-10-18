using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace GameJam.Market
{
    public class MarketItemCardUI : MonoBehaviour, IPointerDownHandler
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descText;
        public Transform previewContainer;
        public Image cardBackground;

        public PlacedItemInstance PoolItem { get; private set; }
        private Action<PlacedItemInstance> onStartDragCallback;

        public void Setup(PlacedItemInstance itemInstance, Action<PlacedItemInstance> onStartDrag)
        {
            this.PoolItem = itemInstance;
            this.onStartDragCallback = onStartDrag;

            if (titleText != null)
            {
                titleText.text = itemInstance.data.itemName;
            }

            if (descText != null)
            {
                string tagColor = itemInstance.data.isEssential ? "#5cd679" : "#ff6b6b";
                descText.text = $"<color={tagColor}>{itemInstance.data.categoryLabel}</color> ({itemInstance.currentShape.Count} birim)";
            }

            RenderShapePreview();
            RefreshState();
        }

        public void RefreshState(PlacedItemInstance currentlyDragging = null)
        {
            if (PoolItem == null) return;

            bool isUnavailable = PoolItem.isPlacedOnGrid || (currentlyDragging == PoolItem);
            gameObject.SetActive(!isUnavailable);
        }

        public void RenderShapePreview()
        {
            if (previewContainer == null || PoolItem == null) return;

            for (int i = previewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(previewContainer.GetChild(i).gameObject);
            }

            int maxX = 0, maxY = 0;
            foreach (var cell in PoolItem.currentShape)
            {
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }

            int maxDim = Mathf.Max(maxX + 1, maxY + 1);
            float blockSize = maxDim >= 4 ? 10.5f : 13f;
            float spacing = 1.5f;
            float totalW = (maxX + 1) * (blockSize + spacing);
            float totalH = (maxY + 1) * (blockSize + spacing);

            foreach (var cell in PoolItem.currentShape)
            {
                GameObject block = new GameObject($"Block_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
                block.transform.SetParent(previewContainer, false);

                RectTransform rt = block.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(blockSize, blockSize);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                float posX = (cell.x * (blockSize + spacing)) - (totalW / 2f) + (blockSize / 2f);
                float posY = (cell.y * (blockSize + spacing)) - (totalH / 2f) + (blockSize / 2f);
                rt.anchoredPosition = new Vector2(posX, posY);

                Image img = block.GetComponent<Image>();
                img.color = PoolItem.data.itemColor;
                img.raycastTarget = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && PoolItem != null && !PoolItem.isPlacedOnGrid)
            {
                onStartDragCallback?.Invoke(PoolItem);
            }
        }
    }
}
