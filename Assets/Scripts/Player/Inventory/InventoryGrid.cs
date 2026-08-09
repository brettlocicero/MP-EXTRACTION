using System.Text;
using UnityEngine;

public class InventoryGrid
{
    readonly int width;
    readonly int height;

    readonly InventoryItem[,] grid;

    public int Width => width;
    public int Height => height;

    public InventoryGrid(int width, int height)
    {
        this.width = width;
        this.height = height;

        grid = new InventoryItem[width, height];
    }

    public bool CanPlaceItem(Vector2Int size, Vector2Int position)
    {
        // Check bounds
        if (position.x < 0 || position.y < 0)
            return false;

        if (position.x + size.x > width)
            return false;

        if (position.y + size.y > height)
            return false;

        // Check occupied cells
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[position.x + x, position.y + y] != null)
                    return false;
            }
        }

        return true;
    }

    public void PlaceItem(InventoryItem item, Vector2Int position)
    {
        item.Position = position;

        for (int x = 0; x < item.Size.x; x++)
        {
            for (int y = 0; y < item.Size.y; y++)
            {
                grid[position.x + x, position.y + y] = item;
            }
        }
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < item.Size.x; x++)
        {
            for (int y = 0; y < item.Size.y; y++)
            {
                Vector2Int cell = item.Position + new Vector2Int(x, y);

                if (IsInsideGrid(cell) && grid[cell.x, cell.y] == item)
                {
                    grid[cell.x, cell.y] = null;
                }
            }
        }
    }

    public InventoryItem GetItem(Vector2Int position)
    {
        if (!IsInsideGrid(position))
            return null;

        return grid[position.x, position.y];
    }

    public bool FindSpace(Vector2Int size, out Vector2Int position)
    {
        for (int y = 0; y <= height - size.y; y++)
        {
            for (int x = 0; x <= width - size.x; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (CanPlaceItem(size, candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        position = Vector2Int.zero;
        return false;
    }

    public bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 &&
               position.y >= 0 &&
               position.x < width &&
               position.y < height;
    }

    public void Clear()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = null;
            }
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(grid[x, y] == null ? ". " : "X ");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}