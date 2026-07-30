using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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
        public IEnumerator HandleFocusedEPreviewsRotatesAndConfirmsFixturePlacement()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            FixturePlacementWorldInteractionTarget handle =
                Require("Essential Checkout Fixture Placement Handle")
                    .GetComponent<FixturePlacementWorldInteractionTarget>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture")
                    .GetComponent<PlaceableFixtureComponent>();

            MoveTargetInFrontOfCamera(handle.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Press(keyboard.eKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;
            Assert.That(
                mode.IsActive,
                Is.True,
                $"Focused {interaction.FocusedTargetId}; feedback {interaction.LastFeedback}; " +
                $"enabled {interaction.IsWorldInteractionEnabled}; handle active {handle.gameObject.activeInHierarchy}; " +
                $"collider {handle.GetComponent<Collider>().enabled}; " +
                $"camera {Camera.main.transform.position} target {handle.transform.position}");

            Vector3 previewPoint = GridCellCenter(placement, 1, 1);
            Assert.That(mode.TryPreviewAtWorldPoint(previewPoint, out string error), Is.True, error);
            Assert.That(mode.PreviewPosition, Is.EqualTo(new GridPosition(1, 1)));
            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Assert.That(mode.PreviewQuarterTurns, Is.EqualTo(1));
            Set(mouse.scroll, Vector2.zero, queueEventOnly: true);
            yield return null;

            Assert.That(mode.TryPreviewAtWorldPoint(previewPoint, out error), Is.True, error);
            Assert.That(interaction.TryPrimaryInteraction(out error), Is.True, error);

            Assert.That(mode.IsActive, Is.False);
            Assert.That(placement.TryGetPlacement(fixture.StableFixtureInstanceId, out FixturePlacementSnapshot accepted), Is.True);
            Assert.That(accepted.gridPosition, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(accepted.quarterTurns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlacedFixtureMovePreviewCancelsToExactAcceptedSnapshotAndTransform()
        {
            yield return LoadValidationScene();

            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture").GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget placedTarget =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();
            PlaceFixture(placement, fixture, new GridPosition(1, 1), 1);
            Assert.That(placement.TryGetPlacement(fixture.StableFixtureInstanceId, out FixturePlacementSnapshot before), Is.True);
            Vector3 positionBefore = fixture.transform.position;
            Quaternion rotationBefore = fixture.transform.rotation;

            Assert.That(placedTarget.TryPrimary(out string error), Is.True, error);
            Assert.That(mode.IsActive, Is.True);
            Assert.That(mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 4, 3), out error), Is.True, error);
            Assert.That(mode.PreviewPosition, Is.EqualTo(new GridPosition(4, 3)));

            Press(keyboard.qKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.qKey, queueEventOnly: true);
            yield return null;

            Assert.That(mode.IsActive, Is.False);
            Assert.That(placement.TryGetPlacement(fixture.StableFixtureInstanceId, out FixturePlacementSnapshot after), Is.True);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(Vector3.Distance(fixture.transform.position, positionBefore), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(fixture.transform.rotation, rotationBefore), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator BackspaceRemovesFixtureAndHandleRemainsUsableForReplacement()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture").GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget handle =
                Require("Essential Checkout Fixture Placement Handle")
                    .GetComponent<FixturePlacementWorldInteractionTarget>();
            PlaceFixture(placement, fixture, new GridPosition(1, 1), 0);

            MoveTargetInFrontOfCamera(handle.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Press(keyboard.backspaceKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.backspaceKey, queueEventOnly: true);
            yield return null;
            Assert.That(
                placement.IsPlaced(fixture.StableFixtureInstanceId),
                Is.False,
                $"Focused {interaction.FocusedTargetId}; feedback {interaction.LastFeedback}; " +
                $"enabled {interaction.IsWorldInteractionEnabled}; handle active {handle.gameObject.activeInHierarchy}; " +
                $"collider {handle.GetComponent<Collider>().enabled}; " +
                $"camera {Camera.main.transform.position} target {handle.transform.position}");
            Assert.That(handle.IsAvailable, Is.True);

            Assert.That(handle.TryPrimary(out string error), Is.True, error);
            Assert.That(mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 2, 2), out error), Is.True, error);
            Assert.That(mode.TryConfirm(out error), Is.True, error);
            Assert.That(placement.IsPlaced(fixture.StableFixtureInstanceId), Is.True);
        }

        [UnityTest]
        public IEnumerator TabHudCancelsAndSuppressesActivePreviewWithoutAcceptedMutation()
        {
            yield return LoadValidationScene();

            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture").GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget handle =
                Require("Essential Checkout Fixture Placement Handle")
                    .GetComponent<FixturePlacementWorldInteractionTarget>();
            Assert.That(handle.TryPrimary(out string error), Is.True, error);
            Assert.That(mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 1, 1), out error), Is.True, error);
            Assert.That(mode.IsActive, Is.True);
            Assert.That(placement.IsPlaced(fixture.StableFixtureInstanceId), Is.False);

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;

            Assert.That(mode.IsActive, Is.False);
            Assert.That(placement.IsPlaced(fixture.StableFixtureInstanceId), Is.False);
            Assert.That(fixture.gameObject.activeInHierarchy, Is.False);
            Assert.That(mode.TryConfirm(out string confirmError), Is.False);
            StringAssert.Contains("Select a valid", confirmError);
        }

        [UnityTest]
        public IEnumerator OpenAndClosingRejectRequiredFixtureMoveAndBackspaceWithoutMutation()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FixturePlacementController placement =
                Require("Fixture Placement").GetComponent<FixturePlacementController>();
            StoreOperatingController store =
                Require("Store Operating Controller").GetComponent<StoreOperatingController>();
            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            StockingController stocking =
                Require("Stocking Controller").GetComponent<StockingController>();
            PlaceableFixtureComponent fixture =
                Require("Essential Checkout Fixture").GetComponent<PlaceableFixtureComponent>();
            FixturePlacementWorldInteractionTarget placedTarget =
                fixture.GetComponent<FixturePlacementWorldInteractionTarget>();
            FixturePlacementWorldInteractionTarget handle =
                Require("Essential Checkout Fixture Placement Handle")
                    .GetComponent<FixturePlacementWorldInteractionTarget>();
            PlaceFixture(placement, fixture, new GridPosition(1, 1), 0);
            ProductDefinition cola = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == "prod-cola-can-355ml");
            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    cola,
                    out ProductItem loose,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                stocking.TryPickUpLooseUnit(loose, out _, out error),
                Is.True,
                error);
            Assert.That(stocking.TryStockHeldUnit(0, out error), Is.True, error);
            Assert.That(store.TryBeginPreparation(out error), Is.True, error);
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            yield return AssertRejectedInCurrentOperatingState(
                interaction,
                placement,
                fixture,
                placedTarget,
                handle);

            Assert.That(store.TryBeginClosing(out error), Is.True, error);
            yield return AssertRejectedInCurrentOperatingState(
                interaction,
                placement,
                fixture,
                placedTarget,
                handle);
        }

        private IEnumerator AssertRejectedInCurrentOperatingState(
            FirstStoreInteractionController interaction,
            FixturePlacementController placement,
            PlaceableFixtureComponent fixture,
            FixturePlacementWorldInteractionTarget placedTarget,
            FixturePlacementWorldInteractionTarget handle)
        {
            Assert.That(placement.TryGetPlacement(fixture.StableFixtureInstanceId, out FixturePlacementSnapshot before), Is.True);
            Vector3 positionBefore = fixture.transform.position;
            Assert.That(placedTarget.TryPrimary(out string moveError), Is.False);
            StringAssert.Contains("unavailable", moveError);

            MoveTargetInFrontOfCamera(handle.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Press(keyboard.backspaceKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.backspaceKey, queueEventOnly: true);
            yield return null;

            Assert.That(placement.TryGetPlacement(fixture.StableFixtureInstanceId, out FixturePlacementSnapshot after), Is.True);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(Vector3.Distance(fixture.transform.position, positionBefore), Is.LessThan(0.001f));
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

        private static void PlaceFixture(
            FixturePlacementController placement,
            PlaceableFixtureComponent fixture,
            GridPosition position,
            int quarterTurns)
        {
            if (!placement.IsPlaced(fixture.StableFixtureInstanceId))
            {
                Assert.That(placement.TryPlace(fixture, position, quarterTurns).IsSuccess, Is.True);
            }
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

        private static void MoveTargetInFrontOfCamera(Transform target)
        {
            target.SetPositionAndRotation(
                Camera.main.transform.position + Camera.main.transform.forward * 1.5f,
                Quaternion.identity);
            Physics.SyncTransforms();
        }

        private static void AimCameraAt(Vector3 target)
        {
            Camera.main.transform.rotation = Quaternion.LookRotation(
                (target - Camera.main.transform.position).normalized,
                Vector3.up);
            Physics.SyncTransforms();
        }
    }
}
