using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Level
{
    public class PrototypeLevelBuilder : MonoBehaviour
    {
        [SerializeField] private float cellSize = 6f;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private int mazeWidth = 41;
        [SerializeField] private int mazeHeight = 25;
        [SerializeField] private int enemySpawnCount = 10;
        [SerializeField] private int generationSeed = 73425;
        [SerializeField] private bool randomizeSeed;

        private readonly List<Vector3> enemySpawnPoints = new();
        private Transform generatedRoot;

        public Vector3 PlayerSpawnPoint { get; private set; }
        public IReadOnlyList<Vector3> EnemySpawnPoints => enemySpawnPoints;

        public void BuildLevel()
        {
            enemySpawnPoints.Clear();

            if (generatedRoot != null)
            {
                Destroy(generatedRoot.gameObject);
            }

            int width = MakeOdd(Mathf.Max(21, mazeWidth));
            int height = MakeOdd(Mathf.Max(21, mazeHeight));
            int seed = randomizeSeed ? Random.Range(int.MinValue, int.MaxValue) : generationSeed;

            bool[,] walls = GenerateMaze(width, height, seed);
            generatedRoot = new GameObject("GeneratedLevel").transform;
            generatedRoot.SetParent(transform, false);

            CreateFloor(width, height);
            CreateMazeWalls(walls, width, height);
            SelectSpawnPoints(walls, width, height);
        }

        private bool[,] GenerateMaze(int width, int height, int seed)
        {
            bool[,] walls = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    walls[x, y] = true;
                }
            }

            System.Random random = new(seed);
            Stack<Vector2Int> stack = new();
            Vector2Int start = new(1, 1);

            walls[start.x, start.y] = false;
            stack.Push(start);

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };

            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();
                ShuffleDirections(directions, random);

                bool carved = false;
                for (int index = 0; index < directions.Length; index++)
                {
                    Vector2Int next = current + directions[index] * 2;
                    if (!IsCarvable(next, width, height, walls))
                    {
                        continue;
                    }

                    Vector2Int between = current + directions[index];
                    walls[between.x, between.y] = false;
                    walls[next.x, next.y] = false;
                    stack.Push(next);
                    carved = true;
                    break;
                }

                if (!carved)
                {
                    stack.Pop();
                }
            }

            CarveCombatRooms(walls, width, height);
            return walls;
        }

        private void CarveCombatRooms(bool[,] walls, int width, int height)
        {
            RectInt[] rooms =
            {
                CreateRoom(3, 3, 7, 5, width, height),
                CreateRoom(width - 11, 3, 7, 5, width, height),
                CreateRoom(width / 2 - 4, 5, 9, 7, width, height),
                CreateRoom(5, height - 8, 9, 5, width, height),
                CreateRoom(width - 14, height - 8, 10, 5, width, height),
                CreateRoom(width / 2 - 5, height - 10, 11, 7, width, height)
            };

            for (int index = 0; index < rooms.Length; index++)
            {
                CarveRoom(walls, rooms[index]);
                CreateRoomDoorways(walls, rooms[index]);
            }
        }

        private void SelectSpawnPoints(bool[,] walls, int width, int height)
        {
            List<Vector2Int> walkableCells = CollectWalkableCells(walls, width, height);
            Vector2Int playerCell = FindClosestWalkable(walkableCells, new Vector2Int(1, 1));
            int[,] distanceField = BuildDistanceField(walls, width, height, playerCell);

            PlayerSpawnPoint = CellToWorld(playerCell, width, height);

            walkableCells.Sort((left, right) => distanceField[right.x, right.y].CompareTo(distanceField[left.x, left.y]));

            List<Vector2Int> chosenEnemies = new();
            int requiredDistanceFromPlayer = Mathf.Max(width, height) / 3;
            int minimumSpacing = 7;

            for (int index = 0; index < walkableCells.Count && chosenEnemies.Count < enemySpawnCount; index++)
            {
                Vector2Int candidate = walkableCells[index];
                int candidateDistance = distanceField[candidate.x, candidate.y];

                if (candidateDistance < requiredDistanceFromPlayer)
                {
                    continue;
                }

                if (!IsFarEnoughFromChosen(candidate, chosenEnemies, minimumSpacing))
                {
                    continue;
                }

                chosenEnemies.Add(candidate);
            }

            for (int index = 0; index < walkableCells.Count && chosenEnemies.Count < enemySpawnCount; index++)
            {
                Vector2Int candidate = walkableCells[index];
                if (candidate == playerCell || chosenEnemies.Contains(candidate))
                {
                    continue;
                }

                if (!IsFarEnoughFromChosen(candidate, chosenEnemies, 4))
                {
                    continue;
                }

                chosenEnemies.Add(candidate);
            }

            for (int index = 0; index < chosenEnemies.Count; index++)
            {
                enemySpawnPoints.Add(CellToWorld(chosenEnemies[index], width, height));
            }
        }

        private void CreateFloor(int width, int height)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(generatedRoot, false);
            floor.transform.localScale = new Vector3(width * cellSize, 1f, height * cellSize);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.GetComponent<Renderer>().material.color = new Color(0.22f, 0.25f, 0.2f);
        }

        private void CreateMazeWalls(bool[,] walls, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!walls[x, y])
                    {
                        continue;
                    }

                    CreateWall(CellToWorld(new Vector2Int(x, y), width, height) - Vector3.up * 0.1f);
                }
            }
        }

        private void CreateWall(Vector3 position)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(generatedRoot, false);
            wall.transform.position = position + Vector3.up * (wallHeight * 0.5f);
            wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
            wall.GetComponent<Renderer>().material.color = new Color(0.45f, 0.47f, 0.5f);
        }

        private Vector3 CellToWorld(Vector2Int cell, int width, int height)
        {
            Vector3 originOffset = new(-(width - 1) * cellSize * 0.5f, 0f, (height - 1) * cellSize * 0.5f);
            return originOffset + new Vector3(cell.x * cellSize, 0.1f, -cell.y * cellSize);
        }

        private static RectInt CreateRoom(int x, int y, int width, int height, int maxWidth, int maxHeight)
        {
            int clampedX = Mathf.Clamp(x, 1, maxWidth - 3);
            int clampedY = Mathf.Clamp(y, 1, maxHeight - 3);
            int clampedWidth = Mathf.Clamp(width, 3, maxWidth - clampedX - 1);
            int clampedHeight = Mathf.Clamp(height, 3, maxHeight - clampedY - 1);
            return new RectInt(clampedX, clampedY, clampedWidth, clampedHeight);
        }

        private static void CarveRoom(bool[,] walls, RectInt room)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    walls[x, y] = false;
                }
            }
        }

        private static void CreateRoomDoorways(bool[,] walls, RectInt room)
        {
            int midX = room.xMin + room.width / 2;
            int midY = room.yMin + room.height / 2;

            walls[Mathf.Clamp(midX, 1, walls.GetLength(0) - 2), Mathf.Clamp(room.yMin - 1, 1, walls.GetLength(1) - 2)] = false;
            walls[Mathf.Clamp(midX, 1, walls.GetLength(0) - 2), Mathf.Clamp(room.yMax, 1, walls.GetLength(1) - 2)] = false;
            walls[Mathf.Clamp(room.xMin - 1, 1, walls.GetLength(0) - 2), Mathf.Clamp(midY, 1, walls.GetLength(1) - 2)] = false;
            walls[Mathf.Clamp(room.xMax, 1, walls.GetLength(0) - 2), Mathf.Clamp(midY, 1, walls.GetLength(1) - 2)] = false;
        }

        private static List<Vector2Int> CollectWalkableCells(bool[,] walls, int width, int height)
        {
            List<Vector2Int> walkableCells = new();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (!walls[x, y])
                    {
                        walkableCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return walkableCells;
        }

        private static Vector2Int FindClosestWalkable(List<Vector2Int> walkableCells, Vector2Int target)
        {
            Vector2Int bestCell = walkableCells[0];
            int bestDistance = int.MaxValue;

            for (int index = 0; index < walkableCells.Count; index++)
            {
                int distance = Mathf.Abs(walkableCells[index].x - target.x) + Mathf.Abs(walkableCells[index].y - target.y);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestCell = walkableCells[index];
            }

            return bestCell;
        }

        private static int[,] BuildDistanceField(bool[,] walls, int width, int height, Vector2Int start)
        {
            int[,] distances = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    distances[x, y] = -1;
                }
            }

            Queue<Vector2Int> queue = new();
            queue.Enqueue(start);
            distances[start.x, start.y] = 0;

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                for (int index = 0; index < directions.Length; index++)
                {
                    Vector2Int next = current + directions[index];
                    if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                    {
                        continue;
                    }

                    if (walls[next.x, next.y] || distances[next.x, next.y] >= 0)
                    {
                        continue;
                    }

                    distances[next.x, next.y] = distances[current.x, current.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static bool IsFarEnoughFromChosen(Vector2Int candidate, List<Vector2Int> chosenCells, int minimumDistance)
        {
            for (int index = 0; index < chosenCells.Count; index++)
            {
                if (Mathf.Abs(candidate.x - chosenCells[index].x) + Mathf.Abs(candidate.y - chosenCells[index].y) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ShuffleDirections(Vector2Int[] directions, System.Random random)
        {
            for (int index = directions.Length - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (directions[index], directions[swapIndex]) = (directions[swapIndex], directions[index]);
            }
        }

        private static bool IsCarvable(Vector2Int cell, int width, int height, bool[,] walls)
        {
            return cell.x > 0 &&
                   cell.x < width - 1 &&
                   cell.y > 0 &&
                   cell.y < height - 1 &&
                   walls[cell.x, cell.y];
        }

        private static int MakeOdd(int value)
        {
            return value % 2 == 0 ? value + 1 : value;
        }
    }
}
