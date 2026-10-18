using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Market
{
    [Serializable]
    public class MarketItemData
    {
        public string id;
        public string itemName;
        public string categoryLabel; // Örn: "Deprem Eşyası" veya "Gereksiz Eşya"
        public bool isEssential;
        public Color itemColor = Color.cyan;
        public List<Vector2Int> defaultShape = new List<Vector2Int>();

        public MarketItemData(string id, string name, string categoryLabel, bool isEssential, Color color, Vector2Int[] shape)
        {
            this.id = id;
            this.itemName = name;
            this.categoryLabel = categoryLabel;
            this.isEssential = isEssential;
            this.itemColor = color;
            this.defaultShape = new List<Vector2Int>(shape);
            NormalizeShape(this.defaultShape);
        }

        public static void NormalizeShape(List<Vector2Int> shape)
        {
            if (shape == null || shape.Count == 0) return;
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            for (int i = 0; i < shape.Count; i++)
            {
                if (shape[i].x < minX) minX = shape[i].x;
                if (shape[i].y < minY) minY = shape[i].y;
            }
            for (int i = 0; i < shape.Count; i++)
            {
                shape[i] = new Vector2Int(shape[i].x - minX, shape[i].y - minY);
            }
        }
    }

    [Serializable]
    public class PlacedItemInstance
    {
        public string uniqueId;
        public MarketItemData data;
        public List<Vector2Int> currentShape;
        public Vector2Int gridPos;
        public int rotation = 0;
        public bool isPlacedOnGrid = false;

        public PlacedItemInstance(MarketItemData data)
        {
            this.uniqueId = Guid.NewGuid().ToString();
            this.data = data;
            this.currentShape = new List<Vector2Int>(data.defaultShape);
            MarketItemData.NormalizeShape(this.currentShape);
            this.rotation = 0;
            this.gridPos = Vector2Int.zero;
            this.isPlacedOnGrid = false;
        }

        public void RotateClockwise()
        {
            rotation = (rotation + 90) % 360;
            for (int i = 0; i < currentShape.Count; i++)
            {
                int newX = currentShape[i].y;
                int newY = -currentShape[i].x;
                currentShape[i] = new Vector2Int(newX, newY);
            }
            MarketItemData.NormalizeShape(currentShape);
        }

        public Vector2 GetShapeCenterOffset()
        {
            if (currentShape == null || currentShape.Count == 0)
                return Vector2.zero;

            int maxX = 0, maxY = 0;
            for (int i = 0; i < currentShape.Count; i++)
            {
                if (currentShape[i].x > maxX) maxX = currentShape[i].x;
                if (currentShape[i].y > maxY) maxY = currentShape[i].y;
            }
            return new Vector2(maxX / 2f, maxY / 2f);
        }

        public List<Vector2Int> GetOccupiedCells(Vector2Int rootPosition)
        {
            List<Vector2Int> cells = new List<Vector2Int>(currentShape.Count);
            for (int i = 0; i < currentShape.Count; i++)
            {
                cells.Add(rootPosition + currentShape[i]);
            }
            return cells;
        }
    }
}
