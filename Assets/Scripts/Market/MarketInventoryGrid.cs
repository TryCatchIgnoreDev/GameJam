using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Market
{
    public class MarketInventoryGrid
    {
        public const int GRID_SIZE = 6;
        private PlacedItemInstance[,] cells = new PlacedItemInstance[GRID_SIZE, GRID_SIZE];
        private List<PlacedItemInstance> placedItems = new List<PlacedItemInstance>();

        public IReadOnlyList<PlacedItemInstance> PlacedItems => placedItems;

        public bool IsWithinBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < GRID_SIZE && pos.y >= 0 && pos.y < GRID_SIZE;
        }

        public PlacedItemInstance GetItemAt(Vector2Int pos)
        {
            if (!IsWithinBounds(pos)) return null;
            return cells[pos.x, pos.y];
        }

        public bool CanPlace(PlacedItemInstance item, Vector2Int rootPos, PlacedItemInstance ignoreItem = null)
        {
            if (item == null || item.currentShape == null || item.currentShape.Count == 0)
                return false;

            List<Vector2Int> occupied = item.GetOccupiedCells(rootPos);

            for (int i = 0; i < occupied.Count; i++)
            {
                Vector2Int cell = occupied[i];

                // Sınır kontrolü
                if (!IsWithinBounds(cell))
                    return false;

                // Doluluk kontrolü
                PlacedItemInstance existing = cells[cell.x, cell.y];
                if (existing != null && existing != ignoreItem)
                    return false;
            }

            return true;
        }

        public bool Place(PlacedItemInstance item, Vector2Int rootPos)
        {
            if (!CanPlace(item, rootPos))
                return false;

            item.gridPos = rootPos;
            List<Vector2Int> occupied = item.GetOccupiedCells(rootPos);

            for (int i = 0; i < occupied.Count; i++)
            {
                Vector2Int cell = occupied[i];
                cells[cell.x, cell.y] = item;
            }

            if (!placedItems.Contains(item))
            {
                placedItems.Add(item);
            }

            return true;
        }

        public bool Remove(PlacedItemInstance item)
        {
            if (item == null || !placedItems.Contains(item))
                return false;

            List<Vector2Int> occupied = item.GetOccupiedCells(item.gridPos);
            for (int i = 0; i < occupied.Count; i++)
            {
                Vector2Int cell = occupied[i];
                if (IsWithinBounds(cell) && cells[cell.x, cell.y] == item)
                {
                    cells[cell.x, cell.y] = null;
                }
            }

            placedItems.Remove(item);
            return true;
        }

        public void Clear()
        {
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    cells[x, y] = null;
                }
            }
            placedItems.Clear();
        }
    }
}
