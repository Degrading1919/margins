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
        public void FloorRayAndInverseGridMappingUseFloorDivision()
        {
            PlacementRig rig = CreateRig();
            Assert.That(rig.Mode.TryBegin(rig.Fixture, out string error), Is.True, error);
            StringAssert.Contains("Q cancels", rig.Mode.Prompt.FormattedText);

            Ray ray = new(new Vector3(1.99f, 2f, 2.01f), Vector3.down);
            Assert.That(rig.Mode.TryRefreshPreview(ray, out error), Is.True, error);
            Assert.That(rig.Mode.PreviewPosition, Is.EqualTo(new GridPosition(1, 2)));

            rig.Origin.SetPositionAndRotation(
                new Vector3(4f, 0f, 3f),
                Quaternion.Euler(0f, 90f, 0f));
            Vector3 localPoint = new(1.99f, 0f, 2.01f);
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

            Assert.That(rig.Mode.TryPreviewAtWorldPoint(new Vector3(1.2f, 0f, 1.2f), out error), Is.True, error);
            Assert.That(rig.Controller.PlacedCount, Is.Zero);
            Assert.That(rig.Fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.Valid));

            Assert.That(rig.Mode.TryPreviewAtWorldPoint(new Vector3(9f, 0f, 1f), out error), Is.False);
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
                rig.Mode.TryPreviewAtWorldPoint(new Vector3(1.2f, 0f, 1.2f), out error),
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
            Assert.That(rig.Mode.TryPreviewAtWorldPoint(new Vector3(4.2f, 0f, 3.2f), out error), Is.True, error);
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
                rig.Mode.TryPreviewAtWorldPoint(new Vector3(4.2f, 0f, 3.2f), out error),
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
            Assert.That(rig.Mode.TryPreviewAtWorldPoint(new Vector3(2.2f, 0f, 3.2f), out error), Is.True, error);
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
            Assert.That(rig.Mode.TryPreviewAtWorldPoint(new Vector3(3.1f, 0f, 1.1f), out error), Is.True, error);
            Assert.That(rig.Mode.TryConfirm(out error), Is.True, error);
            Assert.That(rig.Controller.TryGetPlacement("fixture-test-01", out FixturePlacementSnapshot placement), Is.True);
            Assert.That(placement.gridPosition, Is.EqualTo(new GridPosition(3, 1)));
        }

        private PlacementRig CreateRig()
        {
            Transform origin = CreateGameObject("Grid Origin").transform;
            PlaceableFixtureComponent fixture = CreateFixture();
            FixturePlacementController controller = CreateController(origin, fixture);
            BoxCollider floor = CreateGameObject("Placement Floor").AddComponent<BoxCollider>();
            floor.center = new Vector3(3f, -0.05f, 3f);
            floor.size = new Vector3(6f, 0.1f, 6f);
            FirstStoreFixturePlacementModeController mode = CreateMode(controller, floor);
            return new PlacementRig(origin, fixture, controller, mode);
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
            serialized.FindProperty("gridWidthCells").intValue = 6;
            serialized.FindProperty("gridDepthCells").intValue = 6;
            serialized.FindProperty("cellSize").floatValue = 1f;
            SerializedProperty fixtures = serialized.FindProperty("fixtures");
            fixtures.arraySize = 1;
            fixtures.GetArrayElementAtIndex(0).objectReferenceValue = fixture;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(controller.TryInitialize(out string error), Is.True, error);
            return controller;
        }

        private FirstStoreFixturePlacementModeController CreateMode(
            FixturePlacementController controller,
            Collider floor)
        {
            FirstStoreFixturePlacementModeController mode =
                CreateGameObject("Placement Mode")
                    .AddComponent<FirstStoreFixturePlacementModeController>();
            SerializedObject serialized = new(mode);
            serialized.FindProperty("stableTargetId").stringValue = "target-placement-mode";
            serialized.FindProperty("fixturePlacement").objectReferenceValue = controller;
            serialized.FindProperty("placementFloor").objectReferenceValue = floor;
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
                FirstStoreFixturePlacementModeController mode)
            {
                Origin = origin;
                Fixture = fixture;
                Controller = controller;
                Mode = mode;
            }

            public Transform Origin { get; }
            public PlaceableFixtureComponent Fixture { get; }
            public FixturePlacementController Controller { get; }
            public FirstStoreFixturePlacementModeController Mode { get; }
        }
    }
}
