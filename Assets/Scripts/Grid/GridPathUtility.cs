using System.Collections.Generic;
using UnityEngine;

public static class GridPathUtility
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public static bool CanUnitReachExit(GridManager grid, Unit unit)
    {
        if (grid == null || unit == null)
        {
            return false;
        }

        Vector2Int unitPosition = unit.GridCoordinate;

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int neighbor = unitPosition + Directions[i];

            if (IsOutsideGrid(grid, neighbor))
            {
                return true;
            }

            if (IsCellPassable(grid, neighbor) && HasPathToExit(grid, neighbor))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanBoxReachExit(GridManager grid, Unit boxedUnit)
    {
        if (grid == null || boxedUnit == null)
        {
            return false;
        }

        Vector2Int unitPosition = boxedUnit.GridCoordinate;

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int neighbor = unitPosition + Directions[i];

            if (IsOutsideGrid(grid, neighbor))
            {
                return true;
            }

            if (IsCellPassableForBoxExposure(grid, neighbor) && HasPathToExitForBoxExposure(grid, neighbor))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasPathToExit(GridManager grid, Vector2Int start)
    {
        if (grid == null || !IsCellPassable(grid, start))
        {
            return false;
        }

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = current + Directions[i];

                if (IsOutsideGrid(grid, next))
                {
                    return true;
                }

                if (IsCellPassable(grid, next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    public static bool HasPathToExitForBoxExposure(GridManager grid, Vector2Int start)
    {
        if (grid == null || !IsCellPassableForBoxExposure(grid, start))
        {
            return false;
        }

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = current + Directions[i];

                if (IsOutsideGrid(grid, next))
                {
                    return true;
                }

                if (IsCellPassableForBoxExposure(grid, next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    public static List<Vector2Int> FindShortestExitPath(GridManager grid, Unit unit, out Vector2Int exitDirection)
    {
        exitDirection = Vector2Int.zero;

        if (grid == null || unit == null)
        {
            return new List<Vector2Int>();
        }

        Vector2Int unitPosition = unit.GridCoordinate;
        List<Vector2Int> bestPath = null;
        Vector2Int bestExitDirection = Vector2Int.zero;
        int bestLength = int.MaxValue;

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int direction = Directions[i];
            Vector2Int neighbor = unitPosition + direction;

            if (IsOutsideGrid(grid, neighbor))
            {
                if (bestLength > 0)
                {
                    bestPath = new List<Vector2Int>();
                    bestExitDirection = direction;
                    bestLength = 0;
                }

                continue;
            }

            if (!IsCellPassable(grid, neighbor))
            {
                continue;
            }

            if (!TryFindPathToExitFromCell(grid, neighbor, out List<Vector2Int> path, out Vector2Int pathExitDirection))
            {
                continue;
            }

            if (path.Count < bestLength)
            {
                bestPath = path;
                bestExitDirection = pathExitDirection;
                bestLength = path.Count;
            }
        }

        exitDirection = bestExitDirection;
        return bestPath ?? new List<Vector2Int>();
    }

    public static bool IsCellPassable(GridManager grid, Vector2Int position)
    {
        if (grid == null || IsOutsideGrid(grid, position))
        {
            return false;
        }

        return grid.HasTile(position.x, position.y)
            && !grid.HasBarrier(position.x, position.y)
            && !grid.HasUnit(position.x, position.y);
    }

    private static bool IsCellPassableForBoxExposure(GridManager grid, Vector2Int position)
    {
        if (grid == null || IsOutsideGrid(grid, position))
        {
            return false;
        }

        if (!grid.HasTile(position.x, position.y) || grid.HasBarrier(position.x, position.y))
        {
            return false;
        }

        Unit unit = grid.GetUnitAt(position.x, position.y);
        return unit == null || unit.IsBoxed;
    }

    public static bool IsOutsideGrid(GridManager grid, Vector2Int position)
    {
        return grid == null || !grid.IsInside(position.x, position.y);
    }

    public static Vector3 GridDirectionToWorldOffset(Vector2Int direction, float cellSize)
    {
        if (direction == Vector2Int.right)
        {
            return new Vector3(cellSize, 0f, 0f);
        }

        if (direction == Vector2Int.left)
        {
            return new Vector3(-cellSize, 0f, 0f);
        }

        if (direction == Vector2Int.up)
        {
            return new Vector3(0f, 0f, cellSize);
        }

        if (direction == Vector2Int.down)
        {
            return new Vector3(0f, 0f, -cellSize);
        }

        return Vector3.zero;
    }

    private static bool TryFindPathToExitFromCell(
        GridManager grid,
        Vector2Int start,
        out List<Vector2Int> path,
        out Vector2Int exitDirection)
    {
        path = null;
        exitDirection = Vector2Int.zero;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        var parent = new Dictionary<Vector2Int, Vector2Int?>();
        queue.Enqueue(start);
        parent[start] = null;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int direction = Directions[i];
                Vector2Int next = current + direction;

                if (IsOutsideGrid(grid, next))
                {
                    path = ReconstructPath(parent, start, current);
                    exitDirection = direction;
                    return true;
                }

                if (IsCellPassable(grid, next) && visited.Add(next))
                {
                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int?> parent,
        Vector2Int start,
        Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        Vector2Int? current = goal;

        while (current.HasValue && current.Value != start)
        {
            path.Add(current.Value);
            current = parent[current.Value];
        }

        path.Reverse();
        return path;
    }
}
