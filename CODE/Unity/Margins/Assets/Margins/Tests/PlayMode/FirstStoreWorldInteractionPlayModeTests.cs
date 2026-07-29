using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    [Category("FirstStoreWorldInteractions")]
    public sealed class FirstStoreWorldInteractionPlayModeTests : InputTestFixture
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
        public IEnumerator ExplicitTargetsAndPresenterExposeExactlyFormattedFocusedPrompt()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStorePromptPresenter presenter =
                Object.FindAnyObjectByType<FirstStorePromptPresenter>();
            DeliveryOpenWorldInteractionTarget deliveryOpen =
                Require("Mixed Starter Delivery")
                    .GetComponent<DeliveryOpenWorldInteractionTarget>();
            Assert.That(Require("World Checkout Interaction").GetComponent<StagedCheckoutWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("World Cleaning Interaction").GetComponent<CleaningWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("World Store Operating Control").GetComponent<StoreOperatingWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("Mixed Starter Delivery").transform.Find("Delivery Content Cola Target"), Is.Not.Null);
            Assert.That(Require("fixture-shelf-cola-validation").GetComponentsInChildren<ShelfSnapWorldInteractionTarget>(), Is.Not.Empty);
            Assert.That(presenter, Is.Not.Null);

            AimAt(Camera.main, deliveryOpen.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.CurrentPromptText, Is.EqualTo("[E] Open delivery"));
            Assert.That(presenter.CurrentPromptText, Is.EqualTo("[E] Open delivery"));
        }

        [UnityTest]
        public IEnumerator FocusPromptClearsImmediatelyWhenCameraLosesTarget()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            DeliveryOpenWorldInteractionTarget deliveryOpen =
                Require("Mixed Starter Delivery")
                    .GetComponent<DeliveryOpenWorldInteractionTarget>();
            AimAt(Camera.main, deliveryOpen.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.CurrentPromptText, Is.Not.Empty);

            Camera.main.transform.rotation = Quaternion.Euler(-85f, 0f, 0f);
            Physics.SyncTransforms();
            Assert.That(interaction.RefreshFocus(), Is.False);
            Assert.That(interaction.CurrentPromptText, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DeliveryTargetsRejectSealedAndExhaustedRequestsWithoutChangingTotalInventory()
        {
            yield return LoadValidationScene();

            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryOpenWorldInteractionTarget openTarget =
                delivery.GetComponent<DeliveryOpenWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaTarget =
                delivery.transform.Find("Delivery Content Cola Target")
                    .GetComponent<DeliveryProductWorldInteractionTarget>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            ProductDefinition cola = FindProduct("prod-cola-can-355ml");
            int totalBefore = inventory.Inventory.GetTotalQuantity(cola.StableProductId);

            Assert.That(colaTarget.IsAvailable, Is.True);
            StringAssert.Contains("delivery sealed", colaTarget.Prompt.FormattedText);
            StringAssert.Contains(cola.DisplayName, colaTarget.Prompt.FormattedText);
            Assert.That(colaTarget.TryPrimary(out string sealedError), Is.False);
            StringAssert.Contains("Open the delivery", sealedError);
            Assert.That(delivery.TryGetConfiguredProductRemaining(cola, out string name, out int remaining, out string error), Is.True, error);
            Assert.That(name, Is.EqualTo(cola.DisplayName));
            Assert.That(remaining, Is.GreaterThan(0));
            int expectedRemovals = remaining;

            Assert.That(openTarget.TryPrimary(out error), Is.True, error);
            for (int index = 0; index < expectedRemovals; index++)
            {
                Assert.That(colaTarget.TryPrimary(out error), Is.True, error);
            }

            Assert.That(delivery.TryGetConfiguredProductRemaining(cola, out _, out remaining, out error), Is.True, error);
            Assert.That(remaining, Is.Zero);
            Assert.That(colaTarget.TryPrimary(out string exhaustedError), Is.False);
            StringAssert.Contains("No", exhaustedError);
            Assert.That(inventory.Inventory.GetTotalQuantity(cola.StableProductId), Is.EqualTo(totalBefore));
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", cola.StableProductId), Is.EqualTo(expectedRemovals));
        }

        [UnityTest]
        public IEnumerator ExactShelfTargetUsesHeldQuarterTurnAndInvalidTargetPreservesCola()
        {
            yield return LoadValidationScene();

            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryOpenWorldInteractionTarget openTarget =
                delivery.GetComponent<DeliveryOpenWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaDeliveryTarget =
                delivery.transform.Find("Delivery Content Cola Target")
                    .GetComponent<DeliveryProductWorldInteractionTarget>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            ProductDefinition cola = FindProduct("prod-cola-can-355ml");
            ShelfSnapWorldInteractionTarget validTarget =
                Require("fixture-shelf-cola-validation")
                    .GetComponentsInChildren<ShelfSnapWorldInteractionTarget>()
                    .First();
            ShelfSnapWorldInteractionTarget invalidTarget =
                Require("fixture-shelf-chips-validation")
                    .GetComponentsInChildren<ShelfSnapWorldInteractionTarget>()
                    .First();

            Assert.That(openTarget.TryPrimary(out string error), Is.True, error);
            Assert.That(colaDeliveryTarget.TryPrimary(out error), Is.True, error);
            Assert.That(stocking.TryPickUpLooseUnit(cola, out ProductItem held, out error), Is.True, error);
            Assert.That(held.AdjustQuarterTurns(1), Is.True);
            int totalBefore = inventory.Inventory.GetTotalQuantity(cola.StableProductId);
            int physicalBefore = stocking.PhysicalUnits.VisibleUnitCount;

            Assert.That(invalidTarget.TryPrimary(out string invalidError), Is.False);
            StringAssert.Contains("does not accept", invalidError);
            Assert.That(stocking.HeldPhysicalUnit, Is.SameAs(held));
            Assert.That(held.IsHeld, Is.True);
            Assert.That(held.QuarterTurns, Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetTotalQuantity(cola.StableProductId), Is.EqualTo(totalBefore));
            Assert.That(stocking.PhysicalUnits.VisibleUnitCount, Is.EqualTo(physicalBefore));

            Assert.That(validTarget.TryPrimary(out error), Is.True, error);
            Assert.That(held.IsHeld, Is.False);
            Assert.That(held.SnappedFixture, Is.SameAs(validTarget.ShelfFixture));
            Assert.That(held.SnappedPointId, Is.EqualTo(validTarget.SnapPointId));
            Assert.That(held.QuarterTurns, Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetTotalQuantity(cola.StableProductId), Is.EqualTo(totalBefore));
            Assert.That(stocking.PhysicalUnits.VisibleUnitCount, Is.EqualTo(physicalBefore));
        }

        [UnityTest]
        public IEnumerator CleaningAndOpeningTargetsPresentProgressCompletionAndPlayerFacingBlocker()
        {
            yield return LoadValidationScene();

            CleaningTaskComponent cleaning =
                Require("Cleaning Task").GetComponent<CleaningTaskComponent>();
            CleaningWorldInteractionTarget cleaningTarget =
                Require("World Cleaning Interaction")
                    .GetComponent<CleaningWorldInteractionTarget>();
            StoreOperatingWorldInteractionTarget storeTarget =
                Require("World Store Operating Control")
                    .GetComponent<StoreOperatingWorldInteractionTarget>();

            StringAssert.Contains("0/4", cleaningTarget.Prompt.FormattedText);
            for (int index = 0; index < cleaning.RequiredProgressUnits; index++)
            {
                Assert.That(cleaningTarget.TryPrimary(out string error), Is.True, error);
            }
            StringAssert.Contains("already complete", cleaningTarget.Prompt.FormattedText);
            Assert.That(cleaningTarget.TryPrimary(out string alreadyCompleteError), Is.True, alreadyCompleteError);
            StringAssert.Contains("already complete", cleaningTarget.Prompt.FormattedText);

            Assert.That(storeTarget.TryPrimary(out string preparationError), Is.True, preparationError);
            Assert.That(storeTarget.TryPrimary(out string blocker), Is.False);
            StringAssert.Contains("Place the required fixture", blocker);
            StringAssert.DoesNotContain("fixture-", blocker);
            StringAssert.DoesNotContain("Preparing", blocker);
        }

        [UnityTest]
        public IEnumerator TabHudSuppressesFocusedCleaningInteractionThroughInputGate()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            CleaningTaskComponent cleaning =
                Require("Cleaning Task").GetComponent<CleaningTaskComponent>();
            CleaningWorldInteractionTarget cleaningTarget =
                Require("World Cleaning Interaction")
                    .GetComponent<CleaningWorldInteractionTarget>();
            AimAt(Camera.main, cleaningTarget.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.CurrentPromptText, Is.Not.Empty);
            int before = cleaning.CompletedProgressUnits;

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Press(keyboard.eKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;

            Assert.That(interaction.IsWorldInteractionEnabled, Is.False);
            Assert.That(interaction.CurrentPromptText, Is.Empty);
            Assert.That(cleaning.CompletedProgressUnits, Is.EqualTo(before));
            Assert.That(interaction.TryPrimaryInteraction(out string error), Is.False);
            StringAssert.Contains("HUD", error);
        }

        private static IEnumerator LoadValidationScene()
        {
            yield return SceneManager.LoadSceneAsync("FirstStoreValidation", LoadSceneMode.Single);
            yield return null;
            Assert.That(Object.FindAnyObjectByType<FirstStoreInteractionController>(), Is.Not.Null);
        }

        private static GameObject Require(string objectName)
        {
            GameObject result = GameObject.Find(objectName);
            Assert.That(result, Is.Not.Null, objectName);
            return result;
        }

        private static ProductDefinition FindProduct(string productId)
        {
            ProductDefinition result = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == productId);
            Assert.That(result, Is.Not.Null, productId);
            return result;
        }

        private static void AimAt(Camera camera, Transform target)
        {
            target.SetPositionAndRotation(
                camera.transform.position + camera.transform.forward * 1.5f,
                Quaternion.identity);
            Physics.SyncTransforms();
        }
    }
}
