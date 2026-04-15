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
            BuildBuildingSurroundings();
            BuildContainmentFences();
            BuildHazardChokepoints();
            BuildPerimeterGuardPosts();
            BuildCorridorEquipment();
            BuildQuadrantFill();
            BuildCampusGreens();
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

        private void BuildBuildingSurroundings()
        {
            var surround = new GameObject("BuildingSurroundings").transform;
            surround.SetParent(root);

            var mainLab = ResearchLayout.MainLabPosition;
            SpawnCrate(surround, mainLab + new Vector3(-5f, 0f, 0f));
            SpawnCrate(surround, mainLab + new Vector3(5f, 0f, 0f));
            SpawnBarrel(surround, mainLab + new Vector3(-5f, -2f, 0f), explosive: false);
            SpawnBarrel(surround, mainLab + new Vector3(5f, -2f, 0f), explosive: false);
            SpawnDumpster(surround, mainLab + new Vector3(-6f, 2f, 0f));
            SpawnCar(surround, mainLab + new Vector3(6f, 2f, 0f), 0f);
            SpawnCar(surround, mainLab + new Vector3(-6f, -3f, 0f), 90f);

            var quarantine = ResearchLayout.QuarantineGatePosition;
            SpawnCrate(surround, quarantine + new Vector3(-4f, -2f, 0f));
            SpawnCrate(surround, quarantine + new Vector3(4f, -2f, 0f));
            SpawnBarrel(surround, quarantine + new Vector3(-5f, 3f, 0f), explosive: true);
            SpawnBarrel(surround, quarantine + new Vector3(5f, 3f, 0f), explosive: false);
            SpawnDumpster(surround, quarantine + new Vector3(6f, -3f, 0f));
            SpawnCar(surround, quarantine + new Vector3(-6f, -3f, 0f), 180f);

            var dataVault = ResearchLayout.DataVaultPosition;
            SpawnCrate(surround, dataVault + new Vector3(0f, 5f, 0f));
            SpawnCrate(surround, dataVault + new Vector3(0f, -5f, 0f));
            SpawnBarrel(surround, dataVault + new Vector3(-3f, 5f, 0f), explosive: false);
            SpawnBarrel(surround, dataVault + new Vector3(3f, -5f, 0f), explosive: false);
            SpawnDumpster(surround, dataVault + new Vector3(-3f, -5f, 0f));
            SpawnCar(surround, dataVault + new Vector3(3f, 4f, 0f), 90f);

            var bio = ResearchLayout.BioContainmentPosition;
            SpawnCrate(surround, bio + new Vector3(0f, 5f, 0f));
            SpawnCrate(surround, bio + new Vector3(0f, -5f, 0f));
            SpawnBarrel(surround, bio + new Vector3(3f, 5f, 0f), explosive: true);
            SpawnBarrel(surround, bio + new Vector3(-3f, -5f, 0f), explosive: false);
            SpawnDumpster(surround, bio + new Vector3(3f, -5f, 0f));
            SpawnCar(surround, bio + new Vector3(-3f, 4f, 0f), 90f);

            var reactor = ResearchLayout.ReactorYardPosition;
            SpawnCrate(surround, reactor + new Vector3(-4f, -3f, 0f));
            SpawnCrate(surround, reactor + new Vector3(4f, 3f, 0f));
            SpawnBarrel(surround, reactor + new Vector3(-4f, 3f, 0f), explosive: true);
            SpawnBarrel(surround, reactor + new Vector3(4f, -3f, 0f), explosive: false);
            SpawnDumpster(surround, reactor + new Vector3(5f, 0f, 0f));
        }

        private void BuildContainmentFences()
        {
            var fences = new GameObject("ContainmentFences").transform;
            fences.SetParent(root);
            Color fenceColor = new Color(0.55f, 0.63f, 0.7f);
            Vector3 quarantine = ResearchLayout.QuarantineGatePosition;
            SpawnFence(fences, quarantine + new Vector3(-8.2f, -1.8f, 0f), quarantine + new Vector3(-5.4f, -1.8f, 0f), fenceColor);
            SpawnFence(fences, quarantine + new Vector3(5.4f, -1.8f, 0f), quarantine + new Vector3(8.2f, -1.8f, 0f), fenceColor);
            SpawnFence(fences, quarantine + new Vector3(-8.2f, -1.8f, 0f), quarantine + new Vector3(-8.2f, 4.6f, 0f), fenceColor);
            SpawnFence(fences, quarantine + new Vector3(8.2f, -1.8f, 0f), quarantine + new Vector3(8.2f, 4.6f, 0f), fenceColor);
            SpawnFence(fences, quarantine + new Vector3(-8.2f, 4.6f, 0f), quarantine + new Vector3(-5.8f, 4.6f, 0f), fenceColor);
            SpawnFence(fences, quarantine + new Vector3(5.8f, 4.6f, 0f), quarantine + new Vector3(8.2f, 4.6f, 0f), fenceColor);

            Vector3 reactor = ResearchLayout.ReactorYardPosition;
            SpawnFence(fences, reactor + new Vector3(-6.5f, 4.4f, 0f), reactor + new Vector3(-2.6f, 4.4f, 0f), fenceColor);
            SpawnFence(fences, reactor + new Vector3(2.6f, 4.4f, 0f), reactor + new Vector3(6.5f, 4.4f, 0f), fenceColor);

            Vector3 dataVault = ResearchLayout.DataVaultPosition;
            SpawnFence(fences, dataVault + new Vector3(-6.5f, 7.4f, 0f), dataVault + new Vector3(-2.4f, 7.4f, 0f), fenceColor);
            SpawnFence(fences, dataVault + new Vector3(-6.5f, -7.4f, 0f), dataVault + new Vector3(-2.4f, -7.4f, 0f), fenceColor);

            Vector3 bioContainment = ResearchLayout.BioContainmentPosition;
            SpawnFence(fences, bioContainment + new Vector3(2.4f, 7.4f, 0f), bioContainment + new Vector3(6.5f, 7.4f, 0f), fenceColor);
            SpawnFence(fences, bioContainment + new Vector3(2.4f, -7.4f, 0f), bioContainment + new Vector3(6.5f, -7.4f, 0f), fenceColor);
        }

        private void BuildHazardChokepoints()
        {
            var hazards = new GameObject("HazardChokepoints").transform;
            hazards.SetParent(root);

            Vector3[] barrelPositions =
            {
                new Vector3(-8.5f, 0f, 0f),
                new Vector3(8.5f, 0f, 0f),
                new Vector3(0f, 10.5f, 0f),
                new Vector3(0f, -10.5f, 0f),
            };

            foreach (var pos in barrelPositions)
            {
                SpawnBarrel(hazards, pos, explosive: true);
            }

            SpawnCrate(hazards, new Vector3(-4.5f, 4.2f, 0f));
            SpawnCrate(hazards, new Vector3(4.5f, -4.2f, 0f));
            SpawnRock(hazards, new Vector3(-18f, 17f, 0f), false);
            SpawnRock(hazards, new Vector3(18f, -17f, 0f), false);
        }

        private void BuildPerimeterGuardPosts()
        {
            var posts = new GameObject("GuardPosts").transform;
            posts.SetParent(root);

            SpawnGuardBooth(posts, new Vector3(-18f, 12f, 0f));
            SpawnGuardBooth(posts, new Vector3(18f, 12f, 0f));
            SpawnGuardBooth(posts, new Vector3(-18f, -12f, 0f));
            SpawnGuardBooth(posts, new Vector3(18f, -12f, 0f));
            SpawnGuardBooth(posts, new Vector3(-12f, 18f, 0f));
            SpawnGuardBooth(posts, new Vector3(12f, -18f, 0f));

            SpawnBarrel(posts, new Vector3(-16f, 12f, 0f), explosive: false);
            SpawnBarrel(posts, new Vector3(16f, 12f, 0f), explosive: false);
            SpawnBarrel(posts, new Vector3(-16f, -12f, 0f), explosive: false);
            SpawnBarrel(posts, new Vector3(16f, -12f, 0f), explosive: false);
        }

        private void BuildCorridorEquipment()
        {
            var equipment = new GameObject("CorridorEquipment").transform;
            equipment.SetParent(root);

            SpawnCrate(equipment, new Vector3(-8f, 14f, 0f));
            SpawnCrate(equipment, new Vector3(8f, 14f, 0f));
            SpawnCrate(equipment, new Vector3(-8f, -14f, 0f));
            SpawnCrate(equipment, new Vector3(8f, -14f, 0f));

            SpawnDumpster(equipment, new Vector3(-14f, 8f, 0f));
            SpawnDumpster(equipment, new Vector3(14f, -8f, 0f));

            SpawnBarrel(equipment, new Vector3(-13f, 14.5f, 0f), explosive: false);
            SpawnBarrel(equipment, new Vector3(13f, -14.5f, 0f), explosive: false);
            SpawnBarrel(equipment, new Vector3(-15f, 0f, 0f), explosive: false);
            SpawnBarrel(equipment, new Vector3(15f, 0f, 0f), explosive: false);
        }

        private void BuildQuadrantFill()
        {
            var fill = new GameObject("QuadrantFill").transform;
            fill.SetParent(root);

            SpawnCar(fill, new Vector3(-17f, 24f, 0f), 0f);
            SpawnCar(fill, new Vector3(18f, -24f, 0f), 180f);
            SpawnCar(fill, new Vector3(-24f, 15f, 0f), 90f);
            SpawnCar(fill, new Vector3(24f, -15f, 0f), 90f);

            SpawnCrate(fill, new Vector3(-22f, 22f, 0f));
            SpawnCrate(fill, new Vector3(21f, -22f, 0f));
            SpawnCrate(fill, new Vector3(-15f, 20f, 0f));
            SpawnCrate(fill, new Vector3(15f, -20f, 0f));

            SpawnDumpster(fill, new Vector3(-20f, 8f, 0f));
            SpawnDumpster(fill, new Vector3(14f, 20f, 0f));

            SpawnBarrel(fill, new Vector3(-24f, -8f, 0f), explosive: false);
            SpawnBarrel(fill, new Vector3(23f, 7f, 0f), explosive: false);

            SpawnRock(fill, new Vector3(-27f, 25f, 0f));
            SpawnRock(fill, new Vector3(27f, -25f, 0f));
            SpawnRock(fill, new Vector3(-16f, -8f, 0f));
            SpawnRock(fill, new Vector3(16f, 8f, 0f));

            SpawnTree(fill, new Vector3(-30f, 23f, 0f));
            SpawnTree(fill, new Vector3(30f, 24f, 0f));
            SpawnTree(fill, new Vector3(-30f, -24f, 0f));
            SpawnTree(fill, new Vector3(30f, -23f, 0f));
        }

        private void BuildCampusGreens()
        {
            var greens = new GameObject("CampusGreens").transform;
            greens.SetParent(root);

            Vector3[] treePositions =
            {
                new Vector3(-28f, 28f, 0f),
                new Vector3(28f, 28f, 0f),
                new Vector3(-28f, -28f, 0f),
                new Vector3(28f, -28f, 0f),
                new Vector3(-33f, 2f, 0f),
                new Vector3(33f, -2f, 0f),
            };

            foreach (Vector3 pos in treePositions)
            {
                if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.9f, 0.9f)))
                {
                    SpawnTree(greens, pos, false);
                }
            }

            Vector3[] rockPositions =
            {
                new Vector3(-30f, 30f, 0f),
                new Vector3(30f, 30f, 0f),
                new Vector3(-30f, -30f, 0f),
                new Vector3(30f, -30f, 0f),
            };

            foreach (Vector3 pos in rockPositions)
            {
                if (!IsRoad(pos) && TryPlace(pos, new Vector2(0.8f, 0.8f)))
                {
                    SpawnRock(greens, pos, false);
                }
            }
        }

        private void ScatterLabDebris()
        {
            var debris = new GameObject("LabDebris").transform;
            debris.SetParent(root);

            Vector3[] carPositions =
            {
                new Vector3(-20f, 0.8f, 0f),
                new Vector3(20f, -0.8f, 0f),
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

        private void SpawnGuardBooth(Transform parent, Vector3 pos)
        {
            SpawnBuilding(parent, pos, new Vector2(1.4f, 1.4f), 0, new Color(0.6f, 0.65f, 0.72f), "GuardBooth");
            SpawnCrate(parent, pos + new Vector3(1.2f, 0f, 0f));
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
