using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    [Category("FirstStoreFixturePlacement")]
    public sealed class FirstStoreFixturePlacementPlayModeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator BuildModeInputScopesWholeFixtureSelectionAndQCommitsRotation()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget target =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();

            Assert.That(mode.IsBuildModeActive, Is.False);
            Assert.That(target.IsAvailable, Is.False);
            yield return Tap(keyboard.bKey);
            Assert.That(mode.IsBuildModeActive, Is.True);
            Assert.That(target.IsAvailable, Is.True);

            AimCameraAtFixture(target.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.FocusedTargetId, Is.EqualTo(target.StableTargetId));
            yield return Tap(keyboard.eKey);
            Assert.That(mode.IsActive, Is.True, interaction.LastFeedback);

            Vector3 exteriorCell = GridCellCenter(placement, 1, 1);
            AimCameraDownAt(exteriorCell);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(mode.HasPreview, Is.True);
            Assert.That(mode.PreviewResult.IsSuccess, Is.True, mode.PreviewReason);

            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Set(mouse.scroll, Vector2.zero, queueEventOnly: true);
            yield return null;
            Assert.That(mode.PreviewQuarterTurns, Is.EqualTo(1));

            yield return Tap(keyboard.qKey);
            Assert.That(mode.IsActive, Is.False);
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot accepted),
                Is.True);
            Assert.That(accepted.gridPosition, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(accepted.quarterTurns, Is.EqualTo(1));

            yield return Tap(keyboard.bKey);
            Assert.That(mode.IsBuildModeActive, Is.False);
            Assert.That(target.IsAvailable, Is.False);
        }

        [UnityTest]
        public IEnumerator ExitingBuildModeRollsBackAnActiveMoveExactly()
        {
            yield return LoadValidationScene();

            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget target =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot before),
                Is.True);
            Vector3 positionBefore = fixture.transform.position;
            Quaternion rotationBefore = fixture.transform.rotation;

            Assert.That(mode.TrySetBuildMode(true, out string error), Is.True, error);
            Assert.That(target.TryPrimary(out error), Is.True, error);
            Assert.That(
                mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 1, 1), out error),
                Is.True,
                error);
            Assert.That(fixture.transform.position, Is.Not.EqualTo(positionBefore));

            yield return Tap(keyboard.bKey);

            Assert.That(mode.IsBuildModeActive, Is.False);
            Assert.That(mode.IsActive, Is.False);
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot after),
                Is.True);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(Vector3.Distance(fixture.transform.position, positionBefore), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(fixture.transform.rotation, rotationBefore), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ExteriorPropertyIsUsableButBuildingStructureRejectsPreview()
        {
            yield return LoadValidationScene();

            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget target =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();
            Collider leftWall = Require("Left Wall").GetComponent<Collider>();

            Assert.That(mode.TrySetBuildMode(true, out string error), Is.True, error);
            Assert.That(target.TryPrimary(out error), Is.True, error);
            Assert.That(
                mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 1, 1), out error),
                Is.True,
                error);

            Vector3 wallPoint = leftWall.bounds.center;
            wallPoint.y = 0f;
            Assert.That(mode.TryPreviewAtWorldPoint(wallPoint, out error), Is.False);
            Assert.That(
                mode.PreviewResult.Failure,
                Is.EqualTo(FixturePlacementFailure.StructuralCollision));
            StringAssert.Contains("collides", error);
        }

        [UnityTest]
        public IEnumerator InvalidQCancelAndOwnedPropertyExitBothRestoreAcceptedLayout()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget target =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();
            Collider leftWall = Require("Left Wall").GetComponent<Collider>();
            OwnedPropertyPlacementArea property =
                Object.FindAnyObjectByType<OwnedPropertyPlacementArea>();
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot accepted),
                Is.True);

            Assert.That(mode.TrySetBuildMode(true, out string error), Is.True, error);
            Assert.That(target.TryPrimary(out error), Is.True, error);
            Vector3 wallPoint = leftWall.bounds.center;
            wallPoint.y = 0f;
            AimCameraDownAt(wallPoint);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(mode.PreviewResult.IsSuccess, Is.False);

            yield return Tap(keyboard.qKey);
            Assert.That(mode.IsActive, Is.False);
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot afterQ),
                Is.True);
            Assert.That(afterQ, Is.EqualTo(accepted));

            Assert.That(target.TryPrimary(out error), Is.True, error);
            Assert.That(
                mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 2, 1), out error),
                Is.True,
                error);
            property.Player.position = new Vector3(30f, 1f, 30f);
            Assert.That(mode.RefreshOwnedPropertyPresence(), Is.False);
            Assert.That(mode.IsBuildModeActive, Is.False);
            Assert.That(mode.IsActive, Is.False);
            Assert.That(
                placement.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot afterExit),
                Is.True);
            Assert.That(afterExit, Is.EqualTo(accepted));
        }

        [UnityTest]
        public IEnumerator FourStableWholeObjectsOwnTheirInteractionAndDependentPoints()
        {
            yield return LoadValidationScene();

            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            (string Name, string Id)[] fixtures =
            {
                ("Essential Checkout Fixture", "fixture-checkout-essential-01"),
                ("fixture-shelf-cola-validation", "fixture-shelf-cola-validation"),
                ("fixture-shelf-chips-validation", "fixture-shelf-chips-validation"),
                ("Stockroom Delivery Drop", "fixture-delivery-drop-01")
            };

            Assert.That(GameObject.Find("Essential Checkout Fixture Placement Handle"), Is.Null);
            Assert.That(placement.PlacedCount, Is.EqualTo(fixtures.Length));
            foreach ((string objectName, string fixtureId) in fixtures)
            {
                GameObject root = Require(objectName);
                PlaceableFixtureComponent fixture =
                    root.GetComponent<PlaceableFixtureComponent>();
                FixturePlacementWorldInteractionTarget target =
                    root.GetComponent<FixturePlacementWorldInteractionTarget>();
                Assert.That(fixture.StableFixtureInstanceId, Is.EqualTo(fixtureId));
                Assert.That(target, Is.Not.Null, objectName);
                Assert.That(target.IsAvailable, Is.False);
                Assert.That(placement.IsPlaced(fixtureId), Is.True);
            }

            Assert.That(mode.TrySetBuildMode(true, out string error), Is.True, error);
            Assert.That(
                fixtures.Select(item => Require(item.Name)
                    .GetComponent<FixturePlacementWorldInteractionTarget>())
                    .All(target => target.IsAvailable),
                Is.True);

            Transform checkout = Require("Essential Checkout Fixture").transform;
            Transform colaShelf = Require("fixture-shelf-cola-validation").transform;
            Transform chipsShelf = Require("fixture-shelf-chips-validation").transform;
            Transform deliveryDrop = Require("Stockroom Delivery Drop").transform;
            Assert.That(Require("Cashier Work Point").transform.IsChildOf(checkout), Is.True);
            Assert.That(Require("Customer Queue 1").transform.IsChildOf(checkout), Is.True);
            Assert.That(Require("Customer Browse Cola").transform.IsChildOf(colaShelf), Is.True);
            Assert.That(Require("Customer Browse Chips").transform.IsChildOf(chipsShelf), Is.True);
            Assert.That(Require("Stockroom Delivery Setdown Point").transform.IsChildOf(deliveryDrop), Is.True);
        }

        [UnityTest]
        public IEnumerator LegacyGridLoadPreservesCheckoutIdentityAndSeedsNewMovables()
        {
            yield return LoadValidationScene();

            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent checkout =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();
            FixtureLayout legacy = new(8, 6);
            FixturePlacementResult legacyPlacement = legacy.TryPlace(
                checkout.StableFixtureInstanceId,
                new GridPosition(6, 1),
                checkout.Footprint,
                0);
            Assert.That(legacyPlacement.IsSuccess, Is.True, legacyPlacement.Failure.ToString());

            Assert.That(placement.TryApplyRestoredLayout(legacy, out string error), Is.True, error);
            Assert.That(placement.PlacedCount, Is.EqualTo(4));
            Assert.That(
                placement.TryGetPlacement(
                    "fixture-checkout-essential-01",
                    out FixturePlacementSnapshot migratedCheckout),
                Is.True);
            Assert.That(migratedCheckout.gridPosition, Is.EqualTo(new GridPosition(14, 20)));
            Assert.That(placement.IsPlaced("fixture-shelf-cola-validation"), Is.True);
            Assert.That(placement.IsPlaced("fixture-shelf-chips-validation"), Is.True);
            Assert.That(placement.IsPlaced("fixture-delivery-drop-01"), Is.True);
        }

        private IEnumerator Tap(KeyControl key)
        {
            Press(key, queueEventOnly: true);
            yield return null;
            Release(key, queueEventOnly: true);
            yield return null;
        }

        private static IEnumerator LoadValidationScene()
        {
            yield return SceneManager.LoadSceneAsync("FirstStoreValidation", LoadSceneMode.Single);
            yield return null;
        }

        private static GameObject Require(string objectName)
        {
            GameObject result = GameObject.Find(objectName);
            Assert.That(result, Is.Not.Null, objectName);
            return result;
        }

        private static Vector3 GridCellCenter(
            FixturePlacementController placement,
            int x,
            int z)
        {
            return placement.GridOrigin.TransformPoint(new Vector3(
                (x + 0.5f) * placement.CellSize,
                0f,
                (z + 0.5f) * placement.CellSize));
        }

        private static void AimCameraAtFixture(Transform target)
        {
            Collider collider = target.GetComponentsInChildren<Collider>(true)
                .Where(candidate => candidate.enabled && !candidate.isTrigger)
                .OrderByDescending(candidate => candidate.bounds.size.sqrMagnitude)
                .First();
            Vector3 center = collider.bounds.center;
            Vector3 direction = center - Camera.main.transform.position;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = target.forward;
            }
            direction.Normalize();
            DisablePlayerMotionButKeepGameplayInput();
            Camera.main.transform.SetPositionAndRotation(
                center - direction * (collider.bounds.extents.magnitude + 0.5f),
                Quaternion.LookRotation(direction, Vector3.up));
            Physics.SyncTransforms();
        }

        private static void AimCameraDownAt(Vector3 point)
        {
            DisablePlayerMotionButKeepGameplayInput();
            Camera.main.transform.SetPositionAndRotation(
                point + Vector3.up * 3f,
                Quaternion.LookRotation(Vector3.down, Vector3.forward));
            Physics.SyncTransforms();
        }

        private static void DisablePlayerMotionButKeepGameplayInput()
        {
            FirstPersonController controller =
                Object.FindAnyObjectByType<FirstPersonController>();
            if (controller == null)
            {
                return;
            }

            controller.enabled = false;
            controller.SetGameplayMode(true);
        }
    }
}
