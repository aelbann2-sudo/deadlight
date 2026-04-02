using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Level.MapBuilders
{
    public class ResearchBuilder : MapBuilderBase
    {
        protected override string GetMapName() => "ResearchComplex";

        protected override void BuildLayout()
        {
            ReserveCoreLots();
            BuildMainStructures();
            BuildContainmentFences();
            BuildHazardChokepoints();
            ScatterLabDebris();
        }

        private void ReserveCoreLots()
        {
            ReserveSpace(ResearchLayout.MainLabPosition, new Vector2(12f, 7f));
            ReserveSpace(ResearchLayout.QuarantineGatePosition, new Vector2(10f, 5f));
            ReserveSpace(ResearchLayout.DataVaultPosition, new Vector2(7f, 10f));
            ReserveSpace(ResearchLayout.BioContainmentPosition, new Vector2(7f, 10f));
            ReserveSpace(ResearchLayout.ReactorYardPosition, new Vector2(9f, 6f));
        }

        private void BuildMainStructures()
        {
            var structures = new GameObject("Structures").transform;
            structures.SetParent(root);

            SpawnBuilding(structures, ResearchLayout.MainLabPosition, new Vector2(8f, 4.2f), 2, new Color(0.72f, 0.82f, 0.95f), "MainLab");
            SpawnBuilding(structures, ResearchLayout.QuarantineGatePosition, new Vector2(7f, 3.6f), 0, new Color(0.78f, 0.86f, 0.95f), "QuarantineGate");
            SpawnBuilding(structures, ResearchLayout.DataVaultPosition, new Vector2(4.6f, 7.2f), 1, new Color(0.7f, 0.75f, 0.85f), "DataVault");
            SpawnBuilding(structures, ResearchLayout.BioContainmentPosition, new Vector2(4.6f, 7.2f), 1, new Color(0.7f, 0.78f, 0.84f), "BioContainment");
            SpawnBuilding(structures, ResearchLayout.ReactorYardPosition, new Vector2(6.8f, 4f), 0, new Color(0.75f, 0.82f, 0.88f), "ReactorControl");

            SpawnLabDoor(structures, ResearchLayout.MainLabPosition + new Vector3(0f, 2.5f, 0f));
            SpawnLabDoor(structures, ResearchLayout.MainLabPosition + new Vector3(-2f, -2.5f, 0f));
            SpawnLabDoor(structures, ResearchLayout.MainLabPosition + new Vector3(2f, -2.5f, 0f));
        }

        private void BuildContainmentFences()
        {
            var fences = new GameObject("ContainmentFences").transform;
            fences.SetParent(root);
            Color fenceColor = new Color(0.55f, 0.63f, 0.7f);

            SpawnFence(fences, new Vector3(-30f, 12f, 0f), new Vector3(-9f, 12f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(9f, 12f, 0f), new Vector3(30f, 12f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(-30f, -12f, 0f), new Vector3(-9f, -12f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(9f, -12f, 0f), new Vector3(30f, -12f, 0f), fenceColor);

            SpawnFence(fences, new Vector3(-12f, 30f, 0f), new Vector3(-12f, 9f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(-12f, -9f, 0f), new Vector3(-12f, -30f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(12f, 30f, 0f), new Vector3(12f, 9f, 0f), fenceColor);
            SpawnFence(fences, new Vector3(12f, -9f, 0f), new Vector3(12f, -30f, 0f), fenceColor);
        }

        private void BuildHazardChokepoints()
        {
            var hazards = new GameObject("HazardChokepoints").transform;
            hazards.SetParent(root);

            Vector3[] barrelPositions =
            {
                new Vector3(-7.5f, 0f, 0f),
                new Vector3(7.5f, 0f, 0f),
                new Vector3(0f, 7.5f, 0f),
                new Vector3(0f, -7.5f, 0f),
                new Vector3(-18f, 18f, 0f),
                new Vector3(18f, 18f, 0f),
                new Vector3(-18f, -18f, 0f),
                new Vector3(18f, -18f, 0f)
            };

            foreach (var pos in barrelPositions)
            {
                SpawnBarrel(hazards, pos, explosive: true);
            }

            SpawnCrate(hazards, new Vector3(-4f, 4.5f, 0f));
            SpawnCrate(hazards, new Vector3(4f, -4.5f, 0f));
            SpawnCrate(hazards, new Vector3(-20f, 4f, 0f));
            SpawnCrate(hazards, new Vector3(20f, -4f, 0f));
            SpawnDumpster(hazards, new Vector3(-24f, 12f, 0f));
            SpawnDumpster(hazards, new Vector3(24f, -12f, 0f));
        }

        private void ScatterLabDebris()
        {
            var debris = new GameObject("LabDebris").transform;
            debris.SetParent(root);

            Vector3[] carPositions =
            {
                new Vector3(-20f, 0.8f, 0f),
                new Vector3(20f, -0.8f, 0f),
                new Vector3(0f, 20f, 0f),
                new Vector3(0f, -20f, 0f)
            };

            foreach (var pos in carPositions)
            {
                SpawnCar(debris, pos, Mathf.Abs(pos.x) > Mathf.Abs(pos.y) ? 0f : 90f);
            }

            Vector3[] rockPositions =
            {
                new Vector3(-31f, 30f, 0f),
                new Vector3(31f, 29f, 0f),
                new Vector3(-31f, -30f, 0f),
                new Vector3(31f, -29f, 0f),
                new Vector3(-28f, 4f, 0f),
                new Vector3(28f, -4f, 0f)
            };

            foreach (var pos in rockPositions)
            {
                SpawnRock(debris, pos, false);
            }

            SpawnTree(debris, new Vector3(-33f, 10f, 0f), false);
            SpawnTree(debris, new Vector3(33f, -10f, 0f), false);
        }

        private void SpawnLabDoor(Transform parent, Vector3 pos)
        {
            var door = new GameObject("LabDoor");
            door.transform.SetParent(parent);
            door.transform.position = pos;
            door.transform.localScale = new Vector3(0.7f, 0.9f, 1f);

            var sr = door.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            sr.color = new Color(0.36f, 0.5f, 0.7f);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
        }
    }
}
