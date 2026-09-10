using System.Collections;
using System.Linq;
using System.Reflection;
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
        public IEnumerator CarriedDeliveryActionsAndRecyclingSurviveRestoreWithoutLosingStock()
        {
            yield return LoadValidationScene();
            var interaction = Object.FindAnyObjectByType<FirstStoreInteractionController>();
            var box = Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            var target = box.GetComponent<DeliveryBoxWorldInteractionTarget>();
            var mapper = Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            var bucket = Require("Mop Bucket").GetComponent<CarryableToolComponent>();
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot fresh, out string error), Is.True, error);
            Assert.That(target.TryPrimary(out error), Is.True, error);
            Camera.main.transform.rotation = Quaternion.Euler(-85f, 0f, 0f);
            Physics.SyncTransforms();
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.CurrentPromptText, Is.EqualTo("[E] Open carried delivery"));
            Assert.That(interaction.TryToggleBuildMode(out _), Is.False);
            Assert.That(bucket.TryPrimary(out _), Is.False);
            Assert.That(interaction.TryPrimaryInteraction(out error), Is.True, error);
            Assert.That(box.IsOpen, Is.True);
            Assert.That(interaction.TryCancelInteraction(out error), Is.True, error);
            Assert.That(box.IsCarried, Is.False);
            Assert.That(box.TryRecycle(out _), Is.False);
            var products = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                .Where(product => box.TryGetConfiguredProductRemaining(product, out _, out _, out _))
                .GroupBy(product => product.StableProductId).Select(group => group.First()).ToArray();
            foreach (var product in products)
            {
                while (box.InventoryComponent.Inventory.GetQuantity(box.InventoryLocationId, product.StableProductId) > 0)
                    Assert.That(box.TryRemoveOneUnit(product, out _, out _, out _, out error), Is.True, error);
            }
            var inventoryBefore = box.InventoryComponent.Inventory.CreateSnapshot();
            Assert.That(target.Prompt.FormattedText, Is.EqualTo("[E] Recycle empty box"));
            Assert.That(target.TryPrimary(out error), Is.True, error);
            Assert.That(box.gameObject.activeSelf, Is.False);
            Assert.That(box.InventoryComponent.Inventory.CreateSnapshot(), Is.EqualTo(inventoryBefore));
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot recycled, out error), Is.True, error);
            recycled = JsonUtility.FromJson<FirstStoreSnapshot>(JsonUtility.ToJson(recycled));
            Assert.That(mapper.TryRestore(fresh, out error), Is.True, error);
            Assert.That(box.gameObject.activeSelf, Is.True);
            Assert.That(box.IsRecycled, Is.False);
            Assert.That(mapper.TryRestore(recycled, out error), Is.True, error);
            Assert.That(box.IsRecycled, Is.True);
            Assert.That(box.gameObject.activeSelf, Is.False);
            Assert.That(target.TryPrimary(out _), Is.False);
        }

        [UnityTest]
        public IEnumerator BucketFirstCleaningRequiresReturnAndResolvesKitOnRestore()
        {
            yield return LoadValidationScene();
            var mop = Require("Mop Tool").GetComponent<CarryableToolComponent>();
            var bucket = Require("Mop Bucket").GetComponent<CarryableToolComponent>();
            var carrier = Object.FindAnyObjectByType<PlayerCarryableToolController>();
            var interaction = Object.FindAnyObjectByType<FirstStoreInteractionController>();
            var mapper = Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            var cleaning = Object.FindAnyObjectByType<CleaningTaskComponent>();
            var cleanTarget = Object.FindAnyObjectByType<CleaningWorldInteractionTarget>();
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(GameObject.Find("Receiving Wall Sign"), Is.Null);
            Assert.That(GameObject.Find("Receiving Floor Zone"), Is.Null);
            Assert.That(GameObject.Find("Receiving Rail"), Is.Null);
            Assert.That(Require("Stockroom Delivery Drop"), Is.Not.Null, "Retain the employee delivery fixture.");
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot fresh, out string error), Is.True, error);
            Vector3 storagePosition = bucket.transform.position;
            Assert.That(carrier.TryPickUp(mop, out _), Is.False);
            Assert.That(mop.TryPrimary(out error), Is.True, error);
            Assert.That(carrier.HeldTool, Is.SameAs(bucket));
            Assert.That(mop.transform.parent, Is.EqualTo(bucket.transform));
            Assert.That(carrier.HasCapability("clean-floor"), Is.False);
            Assert.That(cleanTarget.TryPrimary(out _), Is.False);
            Assert.That(mapper.TryGetDiskSaveBlocker(out _), Is.True);
            Assert.That(mapper.TryGetLoadRollbackBlocker(out _), Is.True);
            Assert.That(interaction.TryPrimaryInteraction(out error), Is.True, error);
            Assert.That(bucket.IsCarried, Is.False);
            Assert.That(mop.Prompt.Action, Is.EqualTo("Take mop from bucket"));
            Assert.That(mop.TryPrimary(out error), Is.True, error);
            Assert.That(carrier.HasCapability("clean-floor"), Is.True);
            while (cleaning.NeedsCleaning)
                Assert.That(cleanTarget.TryPrimary(out error), Is.True, error);
            Assert.That(Object.FindAnyObjectByType<FirstStorePromptPresenter>().CurrentObjectiveKind,
                Is.EqualTo(FirstStoreObjectiveKind.ReturnMop));
            Vector3 playerPosition = player.transform.position;
            player.transform.position += Vector3.right * 10f;
            Assert.That(carrier.TrySetDownHeldTool(out error), Is.False);
            Assert.That(error, Does.Contain("closer to the bucket"));
            Assert.That(carrier.HeldTool, Is.SameAs(mop));
            player.transform.position = playerPosition;
            Assert.That(interaction.TryCancelInteraction(out error), Is.True, error);
            Assert.That(carrier.HasHeldTool, Is.False);
            Assert.That(mop.transform.parent, Is.EqualTo(bucket.transform));
            Assert.That(mapper.TryGetDiskSaveBlocker(out _), Is.False);
            Assert.That(bucket.TryPrimary(out error), Is.True, error);
            Assert.That(carrier.TrySetDownHeldTool(out error), Is.True, error);
            Assert.That(mapper.TryRestore(fresh, out error), Is.True, error);
            Assert.That(carrier.HasHeldTool, Is.False);
            Assert.That(bucket.transform.position, Is.EqualTo(storagePosition));
            Assert.That(mop.transform.parent, Is.EqualTo(bucket.transform));
            Assert.That(bucket.HasBeenPlaced, Is.False);
        }

        [UnityTest]
        public IEnumerator ExplicitTargetsAndPresenterExposeExactlyFormattedFocusedPrompt()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStorePromptPresenter presenter =
                Object.FindAnyObjectByType<FirstStorePromptPresenter>();
            DeliveryBoxWorldInteractionTarget deliveryBoxTarget =
                Require("Mixed Starter Delivery")
                    .GetComponent<DeliveryBoxWorldInteractionTarget>();
            Assert.That(
                Require("Essential Checkout Fixture")
                    .GetComponent<CustomerCheckoutWorldInteractionTarget>(),
                Is.Not.Null);
            Assert.That(Require("World Cleaning Interaction").GetComponent<CleaningWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("World Store Operating Control").GetComponent<StoreOperatingWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("Mixed Starter Delivery").transform.Find("Delivery Content Cola Target"), Is.Not.Null);
            Assert.That(Require("fixture-shelf-cola-validation").GetComponent<ShelfFixtureWorldInteractionTarget>(), Is.Not.Null);
            Assert.That(Require("Mop Tool").GetComponent<CarryableToolComponent>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<OwnedPropertyPlacementArea>(), Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            AimAt(Camera.main, deliveryBoxTarget.transform);
            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(
                interaction.CurrentPromptText,
                Is.EqualTo("[E] Pick up delivery"));
            StringAssert.DoesNotContain("sealed container", interaction.CurrentPromptText);
            Assert.That(presenter.CurrentPromptText, Is.EqualTo(interaction.CurrentPromptText));
        }

        [UnityTest]
        public IEnumerator SpecificDeliveryProductChildOverridesWholeBoxSemanticTarget()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryBoxWorldInteractionTarget boxTarget =
                delivery.GetComponent<DeliveryBoxWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaTarget =
                delivery.transform.Find("Delivery Content Cola Target")
                    .GetComponent<DeliveryProductWorldInteractionTarget>();

            Assert.That(boxTarget.TryPrimary(out string error), Is.True, error);
            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(boxTarget.TryCancel(out error), Is.True, error);
            yield return null;
            AimAt(Camera.main, colaTarget.transform);

            Assert.That(interaction.RefreshFocus(), Is.True);
            Assert.That(interaction.FocusedTargetId, Is.EqualTo(colaTarget.StableTargetId));
            StringAssert.Contains("Take", interaction.CurrentPromptText);
        }

        [UnityTest]
        public IEnumerator FocusPromptClearsImmediatelyWhenCameraLosesTarget()
        {
            yield return LoadValidationScene();

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            DeliveryBoxWorldInteractionTarget deliveryBoxTarget =
                Require("Mixed Starter Delivery")
                    .GetComponent<DeliveryBoxWorldInteractionTarget>();
            AimAt(Camera.main, deliveryBoxTarget.transform);
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
            DeliveryBoxWorldInteractionTarget boxTarget =
                delivery.GetComponent<DeliveryBoxWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaTarget =
                delivery.transform.Find("Delivery Content Cola Target")
                    .GetComponent<DeliveryProductWorldInteractionTarget>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            ProductDefinition cola = FindProduct("prod-cola-can-355ml");
            int totalBefore = inventory.Inventory.GetTotalQuantity(cola.StableProductId);

            Assert.That(colaTarget.IsAvailable, Is.True);
            StringAssert.Contains(cola.DisplayName, colaTarget.Prompt.FormattedText);
            StringAssert.DoesNotContain("delivery sealed", colaTarget.Prompt.FormattedText);
            Assert.That(colaTarget.TryPrimary(out string sealedError), Is.False);
            StringAssert.Contains("Open the delivery", sealedError);
            Assert.That(delivery.TryGetConfiguredProductRemaining(cola, out string name, out int remaining, out string error), Is.True, error);
            Assert.That(name, Is.EqualTo(cola.DisplayName));
            Assert.That(remaining, Is.GreaterThan(0));
            int expectedRemovals = remaining;

            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(delivery.IsCarried, Is.True);
            Assert.That(colaTarget.IsAvailable, Is.False);
            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(delivery.IsOpen, Is.True);
            Assert.That(colaTarget.TryPrimary(out string carriedError), Is.False);
            StringAssert.Contains("Set the delivery box down", carriedError);
            Assert.That(boxTarget.TryCancel(out error), Is.True, error);
            Assert.That(delivery.IsCarried, Is.False);
            Assert.That(colaTarget.TryPrimary(out error), Is.True, error);
            StockingController stocking = Object.FindAnyObjectByType<StockingController>();
            Assert.That(stocking.HeldPhysicalUnit, Is.Not.Null);
            for (int index = 1; index < expectedRemovals; index++)
            {
                Assert.That(
                    delivery.TryRemoveOneUnit(
                        cola,
                        out _,
                        out _,
                        out _,
                        out error),
                    Is.True,
                    error);
            }

            Assert.That(delivery.TryGetConfiguredProductRemaining(cola, out _, out remaining, out error), Is.True, error);
            Assert.That(remaining, Is.Zero);
            Assert.That(colaTarget.TryPrimary(out string exhaustedError), Is.False);
            StringAssert.Contains("No", exhaustedError);
            Assert.That(inventory.Inventory.GetTotalQuantity(cola.StableProductId), Is.EqualTo(totalBefore));
            Assert.That(inventory.Inventory.GetQuantity("loc-held", cola.StableProductId), Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", cola.StableProductId), Is.EqualTo(expectedRemovals - 1));
        }

        [UnityTest]
        public IEnumerator ExactShelfTargetUsesHeldQuarterTurnAndInvalidTargetPreservesCola()
        {
            yield return LoadValidationScene();

            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryBoxWorldInteractionTarget boxTarget =
                delivery.GetComponent<DeliveryBoxWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaDeliveryTarget =
                delivery.transform.Find("Delivery Content Cola Target")
                    .GetComponent<DeliveryProductWorldInteractionTarget>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            ProductDefinition cola = FindProduct("prod-cola-can-355ml");
            ShelfFixtureWorldInteractionTarget validTarget =
                Require("fixture-shelf-cola-validation")
                    .GetComponent<ShelfFixtureWorldInteractionTarget>();
            ShelfFixtureWorldInteractionTarget invalidTarget =
                Require("fixture-shelf-chips-validation")
                    .GetComponent<ShelfFixtureWorldInteractionTarget>();

            Assert.That(boxTarget.TryPrimary(out string error), Is.True, error);
            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(boxTarget.TryCancel(out error), Is.True, error);
            Assert.That(colaDeliveryTarget.TryPrimary(out error), Is.True, error);
            ProductItem held = stocking.HeldPhysicalUnit;
            Assert.That(held, Is.Not.Null);
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
            Assert.That(
                held.SnappedFixture,
                Is.SameAs(Require("fixture-shelf-cola-validation").GetComponent<ShelfFixture>()));
            Assert.That(held.SnappedPointId, Is.Not.Empty);
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
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            CarryableToolComponent mop =
                Require("Mop Tool").GetComponent<CarryableToolComponent>();
            PlayerCarryableToolController carrier =
                Object.FindAnyObjectByType<PlayerCarryableToolController>();

            Assert.That(cleaning.NeedsCleaning, Is.True);
            Assert.That(cleaningTarget.IsAvailable, Is.True);
            StringAssert.DoesNotContain(
                "compatible cleaning tool",
                cleaningTarget.Prompt.FormattedText);
            Assert.That(cleaningTarget.TryPrimary(out string blocker), Is.False);
            StringAssert.Contains("Pick up", blocker);
            Assert.That(mop.StorageTool.TryPrimary(out string error), Is.True, error);
            Assert.That(carrier.TrySetDownHeldTool(out error), Is.True, error);
            Assert.That(mop.TryPrimary(out error), Is.True, error);
            Assert.That(carrier.HeldTool, Is.SameAs(mop));
            StringAssert.DoesNotContain("0/4", cleaningTarget.Prompt.FormattedText);
            for (int index = 0; index < cleaning.RequiredProgressUnits; index++)
            {
                Assert.That(cleaningTarget.TryPrimary(out error), Is.True, error);
            }
            Assert.That(cleaning.NeedsCleaning, Is.False);
            Assert.That(cleaningTarget.IsAvailable, Is.False);
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Closed));
            Assert.That(store.IsContinuousOperation, Is.False);
            Assert.That(storeTarget.IsAvailable, Is.True);
            StringAssert.DoesNotContain(
                "Stock a checkout product",
                storeTarget.Prompt.FormattedText);

            Assert.That(carrier.TrySetDownHeldTool(out error), Is.True, error);
            StockOneColaFromDelivery(out error);
            Assert.That(storeTarget.TryPrimary(out error), Is.True, error);
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
            StringAssert.Contains("Begin closing", storeTarget.Prompt.FormattedText);
        }

        [UnityTest]
        public IEnumerator NewBusinessCanOpenBeforeMerchandiseIsShelved()
        {
            yield return LoadValidationScene();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            flow.enabled = false;

            Assert.That(store.Checkout.HasSellableStock, Is.False);
            Assert.That(store.TryGetFirstOpenBlocker(out string blocker), Is.False,
                blocker);
            Assert.That(store.TryOpenStore(out string error), Is.True, error);
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
        }

        [UnityTest]
        public IEnumerator TabHudSuppressesFocusedCleaningInteractionThroughInputGate()
        {
            yield return LoadValidationScene();

            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();
            GameMenuPlayModeTests.CompleteManagementFirstShift(portfolio);
            yield return null;

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            CleaningTaskComponent cleaning =
                Require("Cleaning Task").GetComponent<CleaningTaskComponent>();
            CleaningWorldInteractionTarget cleaningTarget =
                Require("World Cleaning Interaction")
                    .GetComponent<CleaningWorldInteractionTarget>();
            Assert.That(cleaning.NeedsCleaning, Is.True);
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
            StringAssert.Contains("store", error);
        }

        [UnityTest]
        public IEnumerator DedicatedCheckoutPresentsExactItemsLocksMovementAndAlwaysClears()
        {
            yield return LoadValidationScene();

            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            FirstStorePersistenceMapperComponent mapper =
                Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            CustomerCheckoutWorldInteractionTarget checkoutTarget =
                Require("Essential Checkout Fixture")
                    .GetComponent<CustomerCheckoutWorldInteractionTarget>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            for (int step = 0; step < cleaning.RequiredProgressUnits; step++)
            {
                cleaning.TryApplyProgress(1);
            }

            StockProductFromDelivery(
                "Delivery Content Cola Target",
                "fixture-shelf-cola-validation",
                out string error);
            StockProductFromDelivery(
                "Delivery Content Chips Target",
                "fixture-shelf-chips-validation",
                out error);
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            SetPrivateField(flow, "secondsUntilNextArrival", 1_000f);
            SetPrivateField(flow, "arrivalIntervalSeconds", 1_000f);
            Assert.That(flow.TryAdmitCustomerNow(out _, out error), Is.True, error);

            float deadline = Time.realtimeSinceStartup + 12f;
            while (!flow.CanStartCheckout &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(flow.CanStartCheckout, Is.True, flow.CheckoutBlocker);

            int completedBefore = checkout.CompletedTransactionCount;
            Assert.That(
                interaction.TryBeginDedicatedCheckoutMode(
                    checkoutTarget,
                    out error),
                Is.True,
                error);
            Assert.That(interaction.IsCheckoutModeActive, Is.True);
            Assert.That(player.IsInteractionMovementLocked, Is.True);
            Assert.That(flow.ActiveCheckoutItemCount, Is.GreaterThan(0));
            foreach (string physicalUnitId in flow.ActiveCheckoutPhysicalUnitIds)
            {
                Assert.That(
                    flow.PhysicalUnits.TryGetUnit(
                        physicalUnitId,
                        out ProductItem item,
                        out error),
                    Is.True,
                    error);
                Assert.That(item.IsReservedByCustomer, Is.True);
                Assert.That(
                    item.GetComponentsInChildren<Renderer>(true)
                        .Any(renderer => renderer.enabled),
                    Is.True,
                    $"Checkout item {physicalUnitId} was not visibly presented.");
            }

            Assert.That(
                interaction.TryPrimaryInteraction(out error),
                Is.True,
                error);
            Assert.That(flow.ActiveCheckoutScannedCount, Is.EqualTo(1));
            Assert.That(
                interaction.TryCancelInteraction(out error),
                Is.True,
                error);
            Assert.That(flow.ActiveCheckoutScannedCount, Is.Zero);
            Assert.That(interaction.IsCheckoutModeActive, Is.True);
            Assert.That(checkout.HasActiveIncompleteSession, Is.True);

            while (flow.ActiveCheckoutScannedCount <
                   flow.ActiveCheckoutItemCount)
            {
                Assert.That(
                    interaction.TryPrimaryInteraction(out error),
                    Is.True,
                    error);
            }
            Assert.That(
                interaction.TryPrimaryInteraction(out error),
                Is.True,
                error);
            Assert.That(checkout.CompletedTransactionCount,
                Is.EqualTo(completedBefore + 1));
            Assert.That(interaction.IsCheckoutModeActive, Is.False);
            Assert.That(player.IsInteractionMovementLocked, Is.False);
            Assert.That(flow.HasActiveCheckout, Is.False);
            Assert.That(checkout.HasActiveIncompleteSession, Is.False);

            deadline = Time.realtimeSinceStartup + 12f;
            while (flow.HasCustomersInStore &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(flow.HasCustomersInStore, Is.False);
            StockProductFromDelivery(
                "Delivery Content Cola Target",
                "fixture-shelf-cola-validation",
                out error);
            StockProductFromDelivery(
                "Delivery Content Chips Target",
                "fixture-shelf-chips-validation",
                out error);
            Assert.That(flow.TryAdmitCustomerNow(out _, out error), Is.True, error);
            deadline = Time.realtimeSinceStartup + 25f;
            while (!flow.CanStartCheckout &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(flow.CanStartCheckout, Is.True, flow.CheckoutBlocker);
            Assert.That(
                interaction.TryBeginDedicatedCheckoutMode(
                    checkoutTarget,
                    out error),
                Is.True,
                error);
            Assert.That(
                interaction.TryCancelInteraction(out error),
                Is.True,
                error);
            Assert.That(interaction.IsCheckoutModeActive, Is.False);
            Assert.That(flow.HasActiveCheckout, Is.False);
            Assert.That(checkout.HasActiveIncompleteSession, Is.False);
            Assert.That(mapper.TryGetDiskSaveBlocker(out _), Is.False);

            Assert.That(
                checkout.TryBeginSession("stale-session-regression", out error),
                Is.True,
                error);
            Assert.That(flow.HasActiveCheckout, Is.False);
            Assert.That(checkout.HasActiveIncompleteSession, Is.True);
            Assert.That(flow.TryClearStaleCheckout(out error), Is.True, error);
            Assert.That(checkout.HasActiveIncompleteSession, Is.False);
            Assert.That(mapper.TryGetDiskSaveBlocker(out _), Is.False);
        }

        private static void StockOneColaFromDelivery(out string error)
        {
            StockProductFromDelivery(
                "Delivery Content Cola Target",
                "fixture-shelf-cola-validation",
                out error);
        }

        private static void StockProductFromDelivery(
            string deliveryTargetName,
            string shelfName,
            out string error)
        {
            DeliveryBoxComponent delivery =
                Require("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryBoxWorldInteractionTarget boxTarget =
                delivery.GetComponent<DeliveryBoxWorldInteractionTarget>();
            DeliveryProductWorldInteractionTarget colaTarget =
                delivery.transform.Find(deliveryTargetName)
                    .GetComponent<DeliveryProductWorldInteractionTarget>();
            ShelfFixtureWorldInteractionTarget shelfTarget =
                Require(shelfName)
                    .GetComponent<ShelfFixtureWorldInteractionTarget>();

            if (delivery.IsSealed)
            {
                Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
                Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            }
            if (delivery.IsCarried)
            {
                Assert.That(boxTarget.TryCancel(out error), Is.True, error);
            }
            Assert.That(colaTarget.TryPrimary(out error), Is.True, error);
            Assert.That(shelfTarget.TryPrimary(out error), Is.True, error);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
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
