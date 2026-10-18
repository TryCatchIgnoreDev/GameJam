using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace GameJam.Market
{
    public class MarketGridCellUI : MonoBehaviour, IPointerDownHandler
    {
        public Vector2Int gridCoord;
        public Image backgroundImage;
        public Image itemFillImage;
        public TMPro.TextMeshProUGUI labelText;

        public event Action<Vector2Int, PointerEventData> OnCellPointerDown;

        private readonly Color defaultBgColor = new Color(0.18f, 0.20f, 0.23f, 1f);

        public void Init(Vector2Int coord)
        {
            gridCoord = coord;
            ResetVisual();
        }

        public void ResetVisual()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultBgColor;
            }

            if (itemFillImage != null)
            {
                itemFillImage.color = Color.clear;
                itemFillImage.gameObject.SetActive(false);
            }

            if (labelText != null)
            {
                labelText.text = "";
                labelText.gameObject.SetActive(false);
            }
        }

        public void SetOccupied(Color itemColor, string symbol = "")
        {
            if (itemFillImage != null)
            {
                itemFillImage.gameObject.SetActive(true);
                itemFillImage.color = itemColor;
            }

            if (labelText != null)
            {
                if (!string.IsNullOrEmpty(symbol))
                {
                    labelText.gameObject.SetActive(true);
                    labelText.text = symbol;
                }
                else
                {
                    labelText.gameObject.SetActive(false);
                }
            }
        }

        public void SetHighlight(Color highlightColor)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlightColor;
            }
        }

        public void ClearHighlight()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultBgColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnCellPointerDown?.Invoke(gridCoord, eventData);
            }
        }
    }
}
