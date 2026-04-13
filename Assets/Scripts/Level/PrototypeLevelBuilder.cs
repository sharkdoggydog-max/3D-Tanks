using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Level
{
    public class PrototypeLevelBuilder : MonoBehaviour
    {
        [SerializeField] private float cellSize = 6f;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private int baseMazeWidth = 31;
        [SerializeField] private int baseMazeHeight = 21;
        [SerializeField] private int baseEnemySpawnCount = 5;
        [SerializeField] private int generationSeed = 73425;
        [SerializeField] private bool randomizeSeed;

        private readonly List<Vector3> enemySpawnPoints = new();
        private readonly List<RectInt> combatRooms = new();
        private Transform generatedRoot;
        private int currentLevel = 1;
        private int currentEnemySpawnCount;

        private RectInt entryRoom;
        private RectInt eastCombatRoom;
        private RectInt centralCombatRoom;
        private RectInt westCombatRoom;
        private RectInt objectiveRoom;
        private RectInt exitRoom;

        public Vector3 PlayerSpawnPoint { get; private set; }
        public Vector3 ObjectivePoint { get; private set; }
        public Vector3 ExitPoint { get; private set; }
        public IReadOnlyList<Vector3> EnemySpawnPoints => enemySpawnPoints;

        public void ConfigureForLevel(int level)
        {
            currentLevel = Mathf.Max(1, level);
        }

        public void BuildLevel()
        {
            enemySpawnPoints.Clear();
            combatRooms.Clear();

            if (generatedRoot != null)
            {
                Destroy(generatedRoot.gameObject);
            }

            int width = MakeOdd(Mathf.Max(21, baseMazeWidth + (currentLevel - 1) * 2));
            int height = MakeOdd(Mathf.Max(21, baseMazeHeight + ((currentLevel - 1) / 2) * 2));
            currentEnemySpawnCount = Mathf.Min(14, baseEnemySpawnCount + currentLevel - 1);
            int seed = randomizeSeed ? Random.Range(int.MinValue, int.MaxValue) : generationSeed + currentLevel * 131;

            DefineRoomLayout(width, height);

            bool[,] walls = GenerateMaze(width, height, seed);
            generatedRoot = new GameObject("GeneratedLevel").transform;
            generatedRoot.SetParent(transform, false);

            CreateFloor(width, height);
            CreateMazeWalls(walls, width, height);
            SelectMissionPointsAndSpawns(walls, width, height);
            CreateRoomPurposeProps(width, height);
        }

        private void DefineRoomLayout(int width, int height)
        {
            entryRoom = CreateRoom(3, 3, 7, 5, width, height);
            eastCombatRoom = CreateRoom(width - 11, 3, 7, 5, width, height);
            centralCombatRoom = CreateRoom(width / 2 - 4, 5, 9, 7, width, height);
            westCombatRoom = CreateRoom(5, height - 8, 9, 5, width, height);
            exitRoom = CreateRoom(width - 14, height - 8, 10, 5, width, height);
            objectiveRoom = CreateRoom(width / 2 - 5, height - 10, 11, 7, width, height);

            combatRooms.Add(eastCombatRoom);
            combatRooms.Add(centralCombatRoom);
            combatRooms.Add(westCombatRoom);
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

            CarvePurposeRooms(walls);
            return walls;
        }

        private void CarvePurposeRooms(bool[,] walls)
        {
            RectInt[] rooms =
            {
                entryRoom,
                eastCombatRoom,
                centralCombatRoom,
                westCombatRoom,
                exitRoom,
                objectiveRoom
            };

            for (int index = 0; index < rooms.Length; index++)
            {
                CarveRoom(walls, rooms[index]);
                CreateRoomDoorways(walls, rooms[index]);
            }
        }

        private void SelectMissionPointsAndSpawns(bool[,] walls, int width, int height)
        {
            List<Vector2Int> walkableCells = CollectWalkableCells(walls, width, height);
            Vector2Int playerCell = FindClosestWalkable(walkableCells, GetRoomCenterCell(entryRoom));
            Vector2Int objectiveCell = FindClosestWalkable(walkableCells, GetRoomCenterCell(objectiveRoom));
            Vector2Int exitCell = FindClosestWalkable(walkableCells, GetRoomCenterCell(exitRoom));
            int[,] distanceField = BuildDistanceField(walls, width, height, playerCell);

            PlayerSpawnPoint = CellToWorld(playerCell, width, height);
            ObjectivePoint = CellToWorld(objectiveCell, width, height);
            ExitPoint = CellToWorld(exitCell, width, height);

            walkableCells.Sort((left, right) => distanceField[right.x, right.y].CompareTo(distanceField[left.x, left.y]));

            List<Vector2Int> chosenEnemies = new();
            int playerDistanceFloor = Mathf.Max(width, height) / 4;

            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, objectiveCell + Vector2Int.left * 2, 5, 6, playerDistanceFloor);
            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, objectiveCell + Vector2Int.right * 2, 5, 6, playerDistanceFloor);
            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, GetRoomCenterCell(eastCombatRoom), 5, 7, playerDistanceFloor - 1);
            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, GetRoomCenterCell(westCombatRoom), 5, 7, playerDistanceFloor - 1);
            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, GetRoomCenterCell(centralCombatRoom), 5, 6, playerDistanceFloor - 2);
            TryAddSpawnNearAnchor(walkableCells, chosenEnemies, playerCell, exitCell + Vector2Int.left, 4, 5, playerDistanceFloor - 2);

            int requiredDistanceFromPlayer = Mathf.Max(width, height) / 3;
            int minimumSpacing = 7;

            for (int index = 0; index < walkableCells.Count && chosenEnemies.Count < currentEnemySpawnCount; index++)
            {
                Vector2Int candidate = walkableCells[index];
                int candidateDistance = distanceField[candidate.x, candidate.y];

                if (candidate == playerCell || candidate == objectiveCell || candidate == exitCell)
                {
                    continue;
                }

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

            for (int index = 0; index < walkableCells.Count && chosenEnemies.Count < currentEnemySpawnCount; index++)
            {
                Vector2Int candidate = walkableCells[index];
                if (candidate == playerCell || candidate == objectiveCell || candidate == exitCell || chosenEnemies.Contains(candidate))
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

        private void CreateRoomPurposeProps(int width, int height)
        {
            CreateRoomPad("EntryPad", GetRoomCenterWorld(entryRoom, width, height), new Vector3(cellSize * 3f, 0.16f, cellSize * 2.4f), new Color(0.2f, 0.48f, 0.24f));
            CreateGuidePillar("EntryBeaconLeft", GetRoomCenterWorld(entryRoom, width, height) + new Vector3(-cellSize * 1.1f, 0f, -cellSize * 0.7f), 1.7f, new Color(0.52f, 0.88f, 0.56f));
            CreateGuidePillar("EntryBeaconRight", GetRoomCenterWorld(entryRoom, width, height) + new Vector3(cellSize * 1.1f, 0f, -cellSize * 0.7f), 1.7f, new Color(0.52f, 0.88f, 0.56f));

            CreateRoomPad("ObjectivePad", GetRoomCenterWorld(objectiveRoom, width, height), new Vector3(cellSize * 3.5f, 0.16f, cellSize * 3f), new Color(0.46f, 0.18f, 0.16f));
            CreateCoverBlock("ObjectiveCoverNorth", GetRoomCenterWorld(objectiveRoom, width, height) + new Vector3(0f, 0f, cellSize * 1.1f), new Vector3(cellSize * 1.1f, 1.5f, cellSize * 0.7f), new Color(0.48f, 0.36f, 0.32f));
            CreateCoverBlock("ObjectiveCoverSouth", GetRoomCenterWorld(objectiveRoom, width, height) + new Vector3(0f, 0f, -cellSize * 1.1f), new Vector3(cellSize * 1.1f, 1.5f, cellSize * 0.7f), new Color(0.48f, 0.36f, 0.32f));
            CreateCoverBlock("ObjectiveCoverLeft", GetRoomCenterWorld(objectiveRoom, width, height) + new Vector3(-cellSize * 1.4f, 0f, 0f), new Vector3(cellSize * 0.7f, 1.5f, cellSize * 1f), new Color(0.48f, 0.36f, 0.32f));
            CreateCoverBlock("ObjectiveCoverRight", GetRoomCenterWorld(objectiveRoom, width, height) + new Vector3(cellSize * 1.4f, 0f, 0f), new Vector3(cellSize * 0.7f, 1.5f, cellSize * 1f), new Color(0.48f, 0.36f, 0.32f));

            CreateRoomPad("ExitPad", GetRoomCenterWorld(exitRoom, width, height), new Vector3(cellSize * 3.8f, 0.16f, cellSize * 2.8f), new Color(0.12f, 0.42f, 0.5f));
            CreateGuidePillar("ExitFrameLeft", GetRoomCenterWorld(exitRoom, width, height) + new Vector3(-cellSize * 1.3f, 0f, 0f), 2.3f, new Color(0.42f, 0.88f, 0.96f));
            CreateGuidePillar("ExitFrameRight", GetRoomCenterWorld(exitRoom, width, height) + new Vector3(cellSize * 1.3f, 0f, 0f), 2.3f, new Color(0.42f, 0.88f, 0.96f));

            for (int index = 0; index < combatRooms.Count; index++)
            {
                Vector3 roomCenter = GetRoomCenterWorld(combatRooms[index], width, height);
                CreateRoomPad($"CombatPad_{index}", roomCenter, new Vector3(cellSize * 2.8f, 0.12f, cellSize * 2.2f), new Color(0.3f, 0.28f, 0.16f));
                CreateCoverBlock($"CombatCoverA_{index}", roomCenter + new Vector3(cellSize * 0.9f, 0f, cellSize * 0.4f), new Vector3(cellSize * 0.75f, 1.3f, cellSize * 0.75f), new Color(0.42f, 0.42f, 0.38f));
                CreateCoverBlock($"CombatCoverB_{index}", roomCenter + new Vector3(-cellSize * 0.9f, 0f, -cellSize * 0.4f), new Vector3(cellSize * 0.75f, 1.3f, cellSize * 0.75f), new Color(0.42f, 0.42f, 0.38f));
            }
        }

        private void CreateRoomPad(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name;
            pad.transform.SetParent(generatedRoot, false);
            pad.transform.position = position + Vector3.up * 0.08f;
            pad.transform.localScale = scale;
            pad.GetComponent<Renderer>().material.color = color;
        }

        private void CreateGuidePillar(string name, Vector3 position, float height, Color color)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name;
            pillar.transform.SetParent(generatedRoot, false);
            pillar.transform.position = position + Vector3.up * (height * 0.5f);
            pillar.transform.localScale = new Vector3(cellSize * 0.16f, height * 0.5f, cellSize * 0.16f);
            pillar.GetComponent<Renderer>().material.color = color;
        }

        private void CreateCoverBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(generatedRoot, false);
            block.transform.position = position + Vector3.up * (scale.y * 0.5f);
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().material.color = color;
        }

        private Vector3 GetRoomCenterWorld(RectInt room, int width, int height)
        {
            return CellToWorld(GetRoomCenterCell(room), width, height);
        }

        private static Vector2Int GetRoomCenterCell(RectInt room)
        {
            return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
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

        private static bool TryAddSpawnNearAnchor(
            List<Vector2Int> walkableCells,
            List<Vector2Int> chosenCells,
            Vector2Int playerCell,
            Vector2Int anchor,
            int minimumSpacing,
            int maxAnchorDistance,
            int minimumDistanceFromPlayer)
        {
            Vector2Int bestCell = default;
            int bestAnchorDistance = int.MaxValue;
            bool found = false;

            for (int index = 0; index < walkableCells.Count; index++)
            {
                Vector2Int candidate = walkableCells[index];
                if (candidate == playerCell || chosenCells.Contains(candidate))
                {
                    continue;
                }

                int anchorDistance = Mathf.Abs(candidate.x - anchor.x) + Mathf.Abs(candidate.y - anchor.y);
                int playerDistance = Mathf.Abs(candidate.x - playerCell.x) + Mathf.Abs(candidate.y - playerCell.y);

                if (anchorDistance > maxAnchorDistance || playerDistance < minimumDistanceFromPlayer)
                {
                    continue;
                }

                if (!IsFarEnoughFromChosen(candidate, chosenCells, minimumSpacing))
                {
                    continue;
                }

                if (anchorDistance >= bestAnchorDistance)
                {
                    continue;
                }

                bestCell = candidate;
                bestAnchorDistance = anchorDistance;
                found = true;
            }

            if (!found)
            {
                return false;
            }

            chosenCells.Add(bestCell);
            return true;
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
