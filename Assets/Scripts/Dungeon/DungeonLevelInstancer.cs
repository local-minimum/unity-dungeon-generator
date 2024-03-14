using ProcDungeon.Experimental;
using ProcDungeon.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace ProcDungeon.World
{
    public class DungeonLevelInstancer : Singleton<DungeonLevelInstancer>
    {
        [SerializeField]
        Transform generatedLevel;

        [SerializeField]
        Transform generatedMap;

        [SerializeField]
        GameObject basePositionPrefab;

        public PlayerController PlayerController;

        void CleanUpBefore()
        {
            if (generatedLevel != null)
            {
                foreach (Transform t in generatedLevel)
                {
                    Destroy(t.gameObject);
                }
            }
            if (generatedMap != null)
            {
                foreach (Transform t in generatedMap)
                {
                    Destroy(t.gameObject);
                }
            }

        }

        public void InstaniateLevel(DungeonGrid dungeonGrid)
        {
            CleanUpBefore();

            foreach (var (coordinates, position) in dungeonGrid.GridPositions)
            {
                SpawnPosition(dungeonGrid, coordinates, position);
            }

            SpawnPlayer(dungeonGrid);

            PrepareMap(dungeonGrid);
            LevelMapCamera.instance.AdjustView();
            dungeonGrid.VisitPosition(dungeonGrid.PlayerPosition, dungeonGrid.PlayerLookDirection);

        }

        private void SpawnPlayer(DungeonGrid grid)
        {
            Debug.Log($"Player spawns at {grid.PlayerPosition}");
            PlayerController.DungeonGrid = grid;
            PlayerController.Teleport(grid.PlayerPosition, grid.PlayerLookDirection);
        }

        GameObject SpawnPositionRoot() => basePositionPrefab != null ? Instantiate(basePositionPrefab, generatedLevel) : new GameObject();

        private void SpawnPosition(DungeonGrid grid, Vector2Int coordinates, GridPosition position)
        {
            var spawner = WorldFeatureSpawner.instance;
            var root = SpawnPositionRoot();

            root.name = position.FullId(coordinates);
            root.transform.position = grid.LocalWorldPosition(coordinates);

            // Floor
            var floor = spawner.SpawnFloor(position.CategoryID, root.transform);
            floor.name = "Floor";
            floor.transform.localPosition = Vector3.zero;

            // Walls
            foreach (var wallDirection in position.WallDirections())
            {
                var wallPosition = WallPosition.From(coordinates, wallDirection, grid.LevelSetting.tileSize, grid.LevelSetting.tileSize * 0.5f);
                var wall = spawner.SpawnWall(position.CategoryID, root.transform);
                wall.name = $"Wall Direction {wallDirection}";
                wall.transform.position = wallPosition.Position;
                wall.transform.rotation = wallPosition.Rotation;
            }

            // Doors
            if (position.Feature == GridMapFeature.Door)
            {
                var door = spawner.SpawnDoor(position.CategoryID, root.transform);
                door.name = "Door";
                door.transform.localPosition = Vector3.zero;
                door.transform.rotation = position.Door.DirectionFromRoom.AsQuaternion();
                door.dungeonDoor = position.Door;
            }

            // Keys
            foreach (var keyInfo in position.Keys)
            {
                var key = spawner.SpawnKey(keyInfo.Id, root.transform);
                key.Key = keyInfo;
                key.transform.localPosition = Vector3.zero;
                key.name = $"Key {keyInfo.Id}";
            }
        }

        void PrepareMap(DungeonGrid grid)
        {
            var lookup = MapTilesCollection.instance;

            foreach (var (coordinates, position) in grid.GridPositions)
            {
                if (position.Category == GridMapCategory.Hub || position.Category == GridMapCategory.None) continue;

                var groundId = position.WallID;
                var mapTile = lookup.GetTileInstance(groundId);
                if (mapTile == null)
                {
                    Debug.LogWarning($"Failed to create map tile with ID {groundId} at {coordinates}");
                    continue;
                }

                mapTile.transform.SetParent(generatedMap);
                mapTile.transform.position = grid.LocalWorldPosition(coordinates, -0.2f);
                mapTile.name = $"GroundMap {groundId} {coordinates}";
                position.SetMapTile(mapTile);

                if (position.Feature != GridMapFeature.None)
                {
                    var featureTile = lookup.GetTileInstance(position.MapTileFeatureId);
                    if (featureTile != null)
                    {
                        featureTile.transform.SetParent(generatedMap);
                        featureTile.transform.position = grid.LocalWorldPosition(coordinates, -0.1f);
                        featureTile.name = $"MapFeature {position.MapTileFeatureId} {coordinates}";
                        position.SetFeatureTile(featureTile);
                    }
                    else
                    {
                        Debug.LogWarning($"Missing map tile {position.MapTileFeatureId} at {coordinates}");
                    }
                }

            }
        }

    }
}
