using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    [Category("FirstStoreFixturePlacement")]
    public sealed class FirstStoreFixturePlacementModeEditModeTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void FixturePlacementGridUsesApprovedSixInchMeterNativeContract()
        {
            Assert.That(FixturePlacementGrid.MetersPerUnityUnit, Is.EqualTo(1f));
            Assert.That(
                FixturePlacementGrid.PlacementIncrementMeters,
                Is.EqualTo(0.1524f).Within(0.000001f));
            Assert.That(FixturePlacementGrid.CellsToCoverMeters(0.3048f), Is.EqualTo(2));
            Assert.That(
                FixturePlacementGrid.CellsToMeters(2),
                Is.EqualTo(0.3048f).Within(0.000001f));
        }

        [Test]
        public void FloorRayAndInverseGridMappingUseFloorDivision()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            StringAssert.Contains("[Q]", rig.Mode.Prompt.FormattedText);
            StringAssert.DoesNotContain("mouse wheel rotates", rig.Mode.Prompt.FormattedText);

            float cellSize = rig.Controller.CellSize;
            Ray ray = new(
                new Vector3(1.99f * cellSize, 2f, 2.01f * cellSize),
                Vector3.down);
            Assert.That(rig.Mode.TryRefreshPreview(ray, out error), Is.True, error);
            Assert.That(rig.Mode.PreviewPosition, Is.EqualTo(new GridPosition(1, 2)));

            rig.Origin.SetPositionAndRotation(
                new Vector3(4f, 0f, 3f),
                Quaternion.Euler(0f, 90f, 0f));
            Vector3 localPoint = new(
                1.99f * cellSize,
                0f,
                2.01f * cellSize);
            Assert.That(
                rig.Mode.TryGetGridPosition(rig.Origin.TransformPoint(localPoint), out GridPosition mapped, out error),
                Is.True,
                error);
            Assert.That(mapped, Is.EqualTo(new GridPosition(1, 2)));
        }

        [Test]
        public void ValidAndInvalidPreviewDoNotMutateLayout()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);

            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(
                    GridCellPoint(rig, 1, 1),
                    out error),
                Is.True,
                error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.Valid));

            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(
                    GridCellPoint(rig, 41, 1),
                    out error),
                Is.False);
            StringAssert.Contains("outside", error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.Invalid));
        }

        [Test]
        public void LosingPlacementFloorRayClearsConfirmablePreviewWithoutMutation()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 1, 1), out error),
                Is.True,
                error);
            Assert.That(rig.Mode.HasPreview, Is.True);

            Assert.That(
                rig.Mode.TryRefreshPreview(
                    new Ray(new Vector3(1f, 2f, 1f), Vector3.up),
                    out error),
                Is.False);
            StringAssert.Contains("Aim at", error);
            Assert.That(rig.Mode.HasPreview, Is.False);
            Assert.That(rig.Mode.TryConfirm(out error), Is.False);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.Invalid));
        }

        [Test]
        public void MoveCancelRestoresExactAcceptedPlacement()
        {
            PlacementRig rig = CreateRig();
            Assert.That(
                rig.Controller.TryPlace(rig.Fixture, new GridPosition(1, 1), 1).IsSuccess,
                Is.True);
            Vector3 acceptedPosition = rig.Fixture.transform.position;
            Quaternion acceptedRotation = rig.Fixture.transform.rotation;

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 4, 3), out error),
                Is.True,
                error);
            Assert.That(rig.Mode.AdjustQuarterTurns(1, out error), Is.True, error);
            Assert.That(rig.Mode.TryCancel(out error), Is.True, error);

            Assert.That(rig.Controller.TryGetPlacement("fixture-test-01", out FixturePlacementSnapshot placement), Is.True);
            Assert.That(placement.gridPosition, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(placement.quarterTurns, Is.EqualTo(1));
            Assert.That(rig.Fixture.transform.position, Is.EqualTo(acceptedPosition));
            Assert.That(rig.Fixture.transform.rotation, Is.EqualTo(acceptedRotation));
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.None));
        }

        [Test]
        public void RestoreResetDiscardsPreviewAndReappliesAuthoritativePlacement()
        {
            PlacementRig rig = CreateRig();
            Assert.That(
                rig.Controller.TryPlace(rig.Fixture, new GridPosition(1, 1), 1).IsSuccess,
                Is.True);
            Vector3 acceptedPosition = rig.Fixture.transform.position;
            Quaternion acceptedRotation = rig.Fixture.transform.rotation;

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 4, 3), out error),
                Is.True,
                error);
            Assert.That(rig.Fixture.transform.position, Is.Not.EqualTo(acceptedPosition));

            rig.Mode.ResetTransientStateAfterRestore();

            Assert.That(rig.Mode.IsActive, Is.False);
            Assert.That(rig.Fixture.transform.position, Is.EqualTo(acceptedPosition));
            Assert.That(rig.Fixture.transform.rotation, Is.EqualTo(acceptedRotation));
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.None));
            Assert.That(rig.Mode.TryConfirm(out error), Is.False);
        }

        [Test]
        public void ConfirmCommitsOnlyThePreviewedPlacement()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 2, 3), out error),
                Is.True,
                error);
            Assert.That(rig.Mode.AdjustQuarterTurns(1, out error), Is.True, error);
            Assert.That(rig.Mode.TryConfirm(out error), Is.True, error);

            Assert.That(rig.Mode.IsActive, Is.False);
            Assert.That(rig.Controller.TryGetPlacement("fixture-test-01", out FixturePlacementSnapshot placement), Is.True);
            Assert.That(placement.gridPosition, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(placement.quarterTurns, Is.EqualTo(1));
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.None));
        }

        [Test]
        public void RemovalLeavesFixtureAvailableForExplicitRePlacement()
        {
            PlacementRig rig = CreateRig();
            Assert.That(
                rig.Controller.TryPlace(rig.Fixture, new GridPosition(0, 0), 0).IsSuccess,
                Is.True);
            Assert.That(rig.Mode.TryRemoveFixture(rig.Fixture, out string error), Is.True, error);
            Assert.That(rig.Controller.IsPlaced("fixture-test-01"), Is.False);
            Assert.That(rig.Fixture.gameObject.activeSelf, Is.False);

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 3, 1), out error),
                Is.True,
                error);
            Assert.That(rig.Mode.TryConfirm(out error), Is.True, error);
            Assert.That(rig.Controller.TryGetPlacement("fixture-test-01", out FixturePlacementSnapshot placement), Is.True);
            Assert.That(placement.gridPosition, Is.EqualTo(new GridPosition(3, 1)));
        }

        [Test]
        public void FixtureSelectionRequiresBuildMode()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TrySetBuildMode(false, out string error), Is.True, error);

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out error), Is.False);
            StringAssert.Contains("Build Mode", error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);

            Assert.That(rig.Mode.TrySetBuildMode(true, out error), Is.True, error);
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out error), Is.True, error);
        }

        [Test]
        public void LeavingOwnedPropertyExitsBuildModeAndRestoresAcceptedPlacement()
        {
            PlacementRig rig = CreateRig();
            Assert.That(
                rig.Controller.TryPlace(rig.Fixture, new GridPosition(1, 1), 0).IsSuccess,
                Is.True);
            Vector3 accepted = rig.Fixture.transform.position;
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(GridCellPoint(rig, 4, 3), out error),
                Is.True,
                error);
            Assert.That(rig.Fixture.transform.position, Is.Not.EqualTo(accepted));

            rig.Player.position = new Vector3(20f, 1f, 20f);
            Assert.That(rig.Mode.RefreshOwnedPropertyPresence(), Is.False);

            Assert.That(rig.Mode.IsBuildModeActive, Is.False);
            Assert.That(rig.Mode.IsActive, Is.False);
            Assert.That(rig.Fixture.transform.position, Is.EqualTo(accepted));
            Assert.That(
                rig.Controller.TryGetPlacement("fixture-test-01", out FixturePlacementSnapshot placement),
                Is.True);
            Assert.That(placement.gridPosition, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void PreviewRejectsFixtureWhoseFootprintLosesOwnedPropertySupport()
        {
            PlacementRig rig = CreateRig();
            rig.Bounds.size = new Vector3(2f, 4f, 2f);
            Physics.SyncTransforms();

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(new Vector3(4.2f, 0f, 1.2f), out error),
                Is.False);
            Assert.That(
                rig.Mode.PreviewResult.Failure,
                Is.EqualTo(FixturePlacementFailure.InvalidSupport));
            StringAssert.Contains("supported owned property", error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
        }

        [Test]
        public void RotatedRectangularFootprintUsesGridAlignedOwnedPropertyExtents()
        {
            PlacementRig rig = CreateRig();
            GridPosition position = new(10, 10);
            rig.Fixture.ApplyPreview(position, 1, rig.Origin, true);
            Vector3 center = rig.Fixture.transform.position;
            rig.Bounds.center = new Vector3(center.x, 1.5f, center.z);
            rig.Bounds.size = new Vector3(1f, 4f, 0.2f);
            Physics.SyncTransforms();

            Assert.That(
                rig.PropertyArea.TryValidateFixturePlacement(
                    rig.Fixture,
                    rig.Origin,
                    1,
                    out FixturePlacementFailure failure,
                    out string error),
                Is.False);
            Assert.That(failure, Is.EqualTo(FixturePlacementFailure.InvalidSupport));
            StringAssert.Contains("supported owned property", error);
        }

        [Test]
        public void PreviewRejectsCollisionWithConfiguredStructure()
        {
            PlacementRig rig = CreateRig();
            GameObject obstacleObject = CreateGameObject("Structural Obstacle");
            obstacleObject.transform.position = new Vector3(3f, 0.5f, 2.5f);
            BoxCollider obstacle = obstacleObject.AddComponent<BoxCollider>();
            obstacle.size = new Vector3(0.5f, 1f, 0.5f);
            SerializedObject areaSerialized = new(rig.PropertyArea);
            SerializedProperty obstacles = areaSerialized.FindProperty("structuralObstacles");
            obstacles.arraySize = 1;
            obstacles.GetArrayElementAtIndex(0).objectReferenceValue = obstacle;
            areaSerialized.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();

            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            Assert.That(
                rig.Mode.TryPreviewAtWorldPoint(
                    new Vector3(3f, 0f, 2.5f),
                    out error),
                Is.False);
            Assert.That(
                rig.Mode.PreviewResult.Failure,
                Is.EqualTo(FixturePlacementFailure.StructuralCollision));
            StringAssert.Contains("collides", error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
        }

        [Test]
        public void HalfMeterLegacyLayoutMigratesStableIdentityToNearestSixInchCenter()
        {
            Transform origin = CreateGameObject("Migrated Grid Origin").transform;
            origin.SetPositionAndRotation(
                new Vector3(3f, 0f, -2f),
                Quaternion.Euler(0f, 90f, 0f));
            PlaceableFixtureComponent fixture = CreateFixture();
            FixturePlacementController controller = CreateLegacyController(
                origin,
                fixture,
                10,
                10,
                FixturePlacementGrid.LegacyPlacementIncrementMeters,
                Vector3.zero);

            FixtureLayout legacy = new(10, 10);
            Assert.That(
                legacy.TryPlace(
                    fixture.StableFixtureInstanceId,
                    new GridPosition(3, 4),
                    new GridFootprint(2, 1),
                    1).IsSuccess,
                Is.True);

            Assert.That(controller.TryApplyRestoredLayout(legacy, out string error), Is.True, error);
            Assert.That(
                controller.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot first),
                Is.True);
            Assert.That(first.fixtureInstanceId, Is.EqualTo("fixture-test-01"));
            Assert.That(first.unrotatedFootprint, Is.EqualTo(fixture.Footprint));
            Assert.That(first.quarterTurns, Is.EqualTo(1));

            Vector3 exactLegacyCenter = new(1.75f, 0f, 2.5f);
            Vector3 migratedLocalCenter = origin.InverseTransformPoint(
                fixture.transform.position);
            Assert.That(
                Mathf.Abs(migratedLocalCenter.x - exactLegacyCenter.x),
                Is.LessThanOrEqualTo(
                    FixturePlacementGrid.PlacementIncrementMeters * 0.5f + 0.0001f));
            Assert.That(
                Mathf.Abs(migratedLocalCenter.z - exactLegacyCenter.z),
                Is.LessThanOrEqualTo(
                    FixturePlacementGrid.PlacementIncrementMeters * 0.5f + 0.0001f));

            Vector3 firstWorldPosition = fixture.transform.position;
            Assert.That(controller.TryApplyRestoredLayout(legacy, out error), Is.True, error);
            Assert.That(
                controller.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot second),
                Is.True);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(fixture.transform.position, Is.EqualTo(firstWorldPosition));
        }

        private PlacementRig CreateRig()
        {
            Transform origin = CreateGameObject("Grid Origin").transform;
            PlaceableFixtureComponent fixture = CreateFixture();
            FixturePlacementController controller = CreateController(origin, fixture);
            BoxCollider floor = CreateGameObject("Placement Floor").AddComponent<BoxCollider>();
            floor.center = new Vector3(3f, -0.05f, 3f);
            floor.size = new Vector3(6f, 0.1f, 6f);
            Transform player = CreateGameObject("Player").transform;
            player.position = new Vector3(1f, 1f, 1f);
            BoxCollider bounds = CreateGameObject("Owned Property Bounds")
                .AddComponent<BoxCollider>();
            bounds.center = new Vector3(3f, 1.5f, 3f);
            bounds.size = new Vector3(6f, 4f, 6f);
            bounds.isTrigger = true;
            OwnedPropertyPlacementArea propertyArea =
                CreateGameObject("Owned Property").AddComponent<OwnedPropertyPlacementArea>();
            SerializedObject areaSerialized = new(propertyArea);
            areaSerialized.FindProperty("ownedPropertyBounds").objectReferenceValue = bounds;
            areaSerialized.FindProperty("placementSurface").objectReferenceValue = floor;
            areaSerialized.FindProperty("player").objectReferenceValue = player;
            areaSerialized.FindProperty("structuralObstacles").arraySize = 0;
            areaSerialized.ApplyModifiedPropertiesWithoutUndo();
            FirstStoreFixturePlacementModeController mode = CreateMode(
                controller,
                floor,
                propertyArea);
            Assert.That(mode.TrySetBuildMode(true, out string error), Is.True, error);
            return new PlacementRig(
                origin,
                fixture,
                controller,
                mode,
                player,
                propertyArea,
                bounds);
        }

        private PlaceableFixtureComponent CreateFixture()
        {
            PlaceableFixtureComponent fixture = CreateGameObject("Fixture")
                .AddComponent<PlaceableFixtureComponent>();
            SerializedObject serialized = new(fixture);
            serialized.FindProperty("stableFixtureInstanceId").stringValue = "fixture-test-01";
            serialized.FindProperty("footprintWidthCells").intValue = 2;
            serialized.FindProperty("footprintDepthCells").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BoxCollider collider = fixture.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(2f, 1f, 1f);
            return fixture;
        }

        private FixturePlacementController CreateController(
            Transform origin,
            PlaceableFixtureComponent fixture)
        {
            FixturePlacementController controller = CreateGameObject("Placement Controller")
                .AddComponent<FixturePlacementController>();
            SerializedObject serialized = new(controller);
            serialized.FindProperty("gridOrigin").objectReferenceValue = origin;
            serialized.FindProperty("gridWidthCells").intValue = 40;
            serialized.FindProperty("gridDepthCells").intValue = 40;
            SerializedProperty fixtures = serialized.FindProperty("fixtures");
            fixtures.arraySize = 1;
            fixtures.GetArrayElementAtIndex(0).objectReferenceValue = fixture;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(controller.TryInitialize(out string error), Is.True, error);
            return controller;
        }

        private FixturePlacementController CreateLegacyController(
            Transform origin,
            PlaceableFixtureComponent fixture,
            int legacyWidth,
            int legacyDepth,
            float legacyCellSizeMeters,
            Vector3 legacyOriginOffsetMeters)
        {
            FixturePlacementController controller = CreateGameObject(
                    "Legacy Placement Controller")
                .AddComponent<FixturePlacementController>();
            SerializedObject serialized = new(controller);
            serialized.FindProperty("gridOrigin").objectReferenceValue = origin;
            serialized.FindProperty("gridWidthCells").intValue = 40;
            serialized.FindProperty("gridDepthCells").intValue = 40;
            SerializedProperty fixtures = serialized.FindProperty("fixtures");
            fixtures.arraySize = 1;
            fixtures.GetArrayElementAtIndex(0).objectReferenceValue = fixture;
            SerializedProperty legacyGrids = serialized.FindProperty(
                "legacyGridConfigurations");
            legacyGrids.arraySize = 1;
            SerializedProperty legacy = legacyGrids.GetArrayElementAtIndex(0);
            legacy.FindPropertyRelative("gridWidthCells").intValue = legacyWidth;
            legacy.FindPropertyRelative("gridDepthCells").intValue = legacyDepth;
            legacy.FindPropertyRelative("cellSizeMeters").floatValue =
                legacyCellSizeMeters;
            legacy.FindPropertyRelative("originOffsetMeters").vector3Value =
                legacyOriginOffsetMeters;
            SerializedProperty identifiers = legacy.FindPropertyRelative(
                "fixtureInstanceIds");
            identifiers.arraySize = 1;
            identifiers.GetArrayElementAtIndex(0).stringValue =
                fixture.StableFixtureInstanceId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(controller.TryInitialize(out string error), Is.True, error);
            return controller;
        }

        private static Vector3 GridCellPoint(PlacementRig rig, int x, int z)
        {
            return rig.Origin.TransformPoint(new Vector3(
                (x + 0.5f) * rig.Controller.CellSize,
                0f,
                (z + 0.5f) * rig.Controller.CellSize));
        }

        private FirstStoreFixturePlacementModeController CreateMode(
            FixturePlacementController controller,
            Collider floor,
            OwnedPropertyPlacementArea propertyArea)
        {
            FirstStoreFixturePlacementModeController mode =
                CreateGameObject("Placement Mode")
                    .AddComponent<FirstStoreFixturePlacementModeController>();
            SerializedObject serialized = new(mode);
            serialized.FindProperty("stableTargetId").stringValue = "target-placement-mode";
            serialized.FindProperty("fixturePlacement").objectReferenceValue = controller;
            serialized.FindProperty("placementFloor").objectReferenceValue = floor;
            serialized.FindProperty("propertyArea").objectReferenceValue = propertyArea;
            serialized.FindProperty("maximumRayDistance").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return mode;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class PlacementRig
        {
            public PlacementRig(
                Transform origin,
                PlaceableFixtureComponent fixture,
                FixturePlacementController controller,
                FirstStoreFixturePlacementModeController mode,
                Transform player,
                OwnedPropertyPlacementArea propertyArea,
                BoxCollider bounds)
            {
                Origin = origin;
                Fixture = fixture;
                Controller = controller;
                Mode = mode;
                Player = player;
                PropertyArea = propertyArea;
                Bounds = bounds;
            }

            public Transform Origin { get; }
            public PlaceableFixtureComponent Fixture { get; }
            public FixturePlacementController Controller { get; }
            public FirstStoreFixturePlacementModeController Mode { get; }
            public Transform Player { get; }
            public OwnedPropertyPlacementArea PropertyArea { get; }
            public BoxCollider Bounds { get; }
        }
    }
}
