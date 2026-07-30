using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace Margins.Tests
{
    [Category("FirstStoreAdapters")]
    public sealed class FirstStoreInputPlayModeTests : InputTestFixture
    {
        private const string ColaProductId = "prod-cola-can-355ml";

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
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator EscapeMenuPausesAndReliablyResumesGameplay()
        {
            yield return LoadValidationScene();

            GamePauseMenuController menu =
                Object.FindAnyObjectByType<GamePauseMenuController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(player.IsGameplayMode, Is.True);

            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(GamePauseMenuController.IsAnyMenuOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.IsGameplayMode, Is.False);

            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(GamePauseMenuController.IsAnyMenuOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(player.IsGameplayMode, Is.True);
        }

        [UnityTest]
        public IEnumerator GameplayStartsLockedAndTabTogglesManagementMode()
        {
            yield return LoadValidationScene();

            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            FirstStoreValidationController validation =
                Object.FindAnyObjectByType<FirstStoreValidationController>();
            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();

            Assert.That(player.IsGameplayMode, Is.True);
            Assert.That(validation.IsHudModeActive, Is.False);
            Assert.That(portfolio.OwnsManagementDesk, Is.False);
            Assert.That(
                player.RequestedCursorLockState,
                Is.EqualTo(CursorLockMode.Locked));
            Assert.That(player.IsGameplayInputActive, Is.True);

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;

            Assert.That(player.IsGameplayMode, Is.False);
            Assert.That(validation.IsHudModeActive, Is.False);
            Assert.That(portfolio.OwnsManagementDesk, Is.True);
            Assert.That(
                player.RequestedCursorLockState,
                Is.EqualTo(CursorLockMode.None));
            Assert.That(player.IsGameplayInputActive, Is.False);

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;

            Assert.That(player.IsGameplayMode, Is.True);
            Assert.That(validation.IsHudModeActive, Is.False);
            Assert.That(portfolio.OwnsManagementDesk, Is.False);
            Assert.That(
                player.RequestedCursorLockState,
                Is.EqualTo(CursorLockMode.Locked));
            Assert.That(player.IsGameplayInputActive, Is.True);
        }

        [UnityTest]
        public IEnumerator LockedCursorMouseInputRotatesPlayerAndCamera()
        {
            yield return LoadValidationScene();

            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            Camera camera = Camera.main;
            Quaternion playerBefore = player.transform.rotation;
            Quaternion cameraBefore = camera.transform.localRotation;

            Set(
                mouse.delta,
                new Vector2(30f, -20f),
                queueEventOnly: true);
            yield return null;

            Assert.That(player.IsGameplayInputActive, Is.True);
            Assert.That(
                Quaternion.Angle(playerBefore, player.transform.rotation),
                Is.GreaterThan(0.1f));
            Assert.That(
                Quaternion.Angle(cameraBefore, camera.transform.localRotation),
                Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator RaycastPickupSelectsExactTargetAndSynchronizesLocations()
        {
            yield return LoadValidationScene();

            ProductItem[] looseUnits = RemoveLooseColaUnits(2);
            ProductItem decoy = looseUnits[0];
            ProductItem targeted = looseUnits[1];
            Camera camera = Camera.main;
            AimAt(camera, targeted, decoy);

            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();

            Assert.That(
                interaction.TryPickUpTargetedUnit(
                    out ProductItem selected,
                    out string error),
                Is.True,
                error);
            Assert.That(selected, Is.SameAs(targeted));
            Assert.That(interaction.HeldProduct, Is.SameAs(targeted));
            Assert.That(targeted.IsHeld, Is.True);
            Assert.That(decoy.IsHeld, Is.False);
            Assert.That(
                stocking.PhysicalUnits.IsAtLocation(targeted, "loc-held"),
                Is.True);
            Assert.That(
                stocking.PhysicalUnits.IsAtLocation(decoy, "loc-loose"),
                Is.True);
            Assert.That(
                inventory.Inventory.GetQuantity("loc-held", ColaProductId),
                Is.EqualTo(1));
            Assert.That(
                inventory.Inventory.GetQuantity("loc-loose", ColaProductId),
                Is.EqualTo(1));
            Assert.That(
                inventory.Inventory.GetTotalQuantity(ColaProductId),
                Is.EqualTo(4));
            Assert.That(stocking.PhysicalUnits.VisibleUnitCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ScrollDirectionsAreOppositeAndQuarterTurnsWrap()
        {
            yield return LoadValidationScene();

            ProductItem held = PickUpOneTargetedCola();
            Quaternion initialRotation = held.transform.localRotation;

            Set(mouse.scroll, new Vector2(0f, -120f), queueEventOnly: true);
            yield return null;
            Assert.That(held.QuarterTurns, Is.EqualTo(3));
            Assert.That(
                Quaternion.Angle(initialRotation, held.transform.localRotation),
                Is.GreaterThan(89f));

            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Assert.That(held.QuarterTurns, Is.Zero);
            Assert.That(
                Quaternion.Angle(initialRotation, held.transform.localRotation),
                Is.LessThan(0.1f));

            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Assert.That(held.QuarterTurns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HudModeSuppressesPickupStockingAndRotation()
        {
            yield return LoadValidationScene();

            ProductItem loose = RemoveLooseColaUnits(1).Single();
            Camera camera = Camera.main;
            AimAt(camera, loose);
            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;

            Press(keyboard.eKey, queueEventOnly: true);
            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;

            Assert.That(interaction.HeldProduct, Is.Null);
            Assert.That(loose.IsHeld, Is.False);
            Assert.That(loose.QuarterTurns, Is.Zero);
            Assert.That(
                inventory.Inventory.GetQuantity("loc-loose", ColaProductId),
                Is.EqualTo(1));
            Assert.That(
                inventory.Inventory.GetQuantity("loc-held", ColaProductId),
                Is.Zero);

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Assert.That(
                interaction.TryPickUpTargetedUnit(out ProductItem held, out string error),
                Is.True,
                error);

            Press(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.tabKey, queueEventOnly: true);
            yield return null;
            Press(keyboard.eKey, queueEventOnly: true);
            Set(mouse.scroll, new Vector2(0f, -120f), queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;

            Assert.That(interaction.HeldProduct, Is.SameAs(held));
            Assert.That(held.IsHeld, Is.True);
            Assert.That(held.QuarterTurns, Is.Zero);
            Assert.That(
                inventory.Inventory.GetQuantity("loc-held", ColaProductId),
                Is.EqualTo(1));
            Assert.That(
                inventory.Inventory.GetQuantity("loc-shelf-cola", ColaProductId),
                Is.Zero);
        }

        [UnityTest]
        public IEnumerator EStocksAcceptedRotationAndPreservesConservation()
        {
            yield return LoadValidationScene();

            ProductItem held = PickUpOneTargetedCola();
            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            int totalBefore = inventory.Inventory.GetTotalQuantity(ColaProductId);
            int visibleBefore = stocking.PhysicalUnits.VisibleUnitCount;

            Set(mouse.scroll, new Vector2(0f, 120f), queueEventOnly: true);
            yield return null;
            Assert.That(held.QuarterTurns, Is.EqualTo(1));

            ShelfFixtureWorldInteractionTarget shelfTarget = Object
                .FindObjectsByType<ShelfFixtureWorldInteractionTarget>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .First(target => target.gameObject.name ==
                                 "fixture-shelf-cola-validation");
            AimAt(Camera.main, shelfTarget.transform);

            Press(keyboard.eKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;

            Assert.That(interaction.HeldProduct, Is.Null);
            Assert.That(held.IsHeld, Is.False);
            Assert.That(held.IsSnapped, Is.True);
            Assert.That(held.QuarterTurns, Is.EqualTo(1));
            Assert.That(
                stocking.PhysicalUnits.IsAtLocation(
                    held,
                    "loc-shelf-cola"),
                Is.True);
            Assert.That(
                inventory.Inventory.GetQuantity("loc-held", ColaProductId),
                Is.Zero);
            Assert.That(
                inventory.Inventory.GetQuantity("loc-shelf-cola", ColaProductId),
                Is.EqualTo(1));
            Assert.That(
                inventory.Inventory.GetTotalQuantity(ColaProductId),
                Is.EqualTo(totalBefore));
            Assert.That(
                stocking.PhysicalUnits.VisibleUnitCount,
                Is.EqualTo(visibleBefore));
        }

        [UnityTest]
        public IEnumerator ContinuousWorldPathStocksVisibleFixturesProcessesItemsAndKeepsOperating()
        {
            yield return LoadValidationScene();

            FirstStoreExperienceController experience =
                Object.FindAnyObjectByType<FirstStoreExperienceController>();
            Assert.That(experience, Is.Not.Null);
            Assert.That(experience.TryValidateConfiguration(out string error), Is.True, error);
            Assert.That(GameObject.Find("First Store Presentation"), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Light>(FindObjectsInactive.Include).Length, Is.GreaterThanOrEqualTo(9));

            FixturePlacementController placement =
                GameObject.Find("Fixture Placement").GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent fixture =
                GameObject.Find("Essential Checkout Fixture").GetComponent<PlaceableFixtureComponent>();
            Assert.That(placement.IsPlaced(fixture.StableFixtureInstanceId), Is.True);

            DeliveryBoxComponent delivery =
                GameObject.Find("Mixed Starter Delivery").GetComponent<DeliveryBoxComponent>();
            DeliveryBoxWorldInteractionTarget boxTarget =
                delivery.GetComponent<DeliveryBoxWorldInteractionTarget>();
            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(delivery.IsCarried, Is.True);
            Assert.That(boxTarget.TryPrimary(out error), Is.True, error);
            Assert.That(delivery.IsOpen, Is.True);
            Assert.That(boxTarget.TryCancel(out error), Is.True, error);

            ShelfFixtureWorldInteractionTarget[] shelfTargets =
                Object.FindObjectsByType<ShelfFixtureWorldInteractionTarget>(
                    FindObjectsInactive.Exclude);
            StockingController stocking = Object.FindAnyObjectByType<StockingController>();
            DeliveryProductWorldInteractionTarget[] productTargets =
                delivery.GetComponentsInChildren<DeliveryProductWorldInteractionTarget>();
            for (int round = 0; round < 2; round++)
            {
                foreach (DeliveryProductWorldInteractionTarget productTarget in productTargets)
                {
                    Assert.That(productTarget.TryPrimary(out error), Is.True, error);
                    Assert.That(stocking.HeldPhysicalUnit, Is.Not.Null);
                    Assert.That(
                        stocking.HeldPhysicalUnit.Definition,
                        Is.SameAs(productTarget.ProductDefinition));
                    ShelfFixtureWorldInteractionTarget matchingShelf = shelfTargets
                        .Single(target => target.gameObject.name.Contains(
                            productTarget.ProductDefinition.StableProductId.Contains("cola")
                                ? "cola"
                                : "chips"));
                    Assert.That(matchingShelf.TryPrimary(out error), Is.True, error);
                }
            }

            StagedCheckoutWorldInteractionTarget checkoutTarget =
                GameObject.Find("World Checkout Interaction")
                    .GetComponent<StagedCheckoutWorldInteractionTarget>();
            StagedCheckoutInteractionComponent stagedCheckout =
                Object.FindAnyObjectByType<StagedCheckoutInteractionComponent>();
            CheckoutProductWorldInteractionTarget[] checkoutProducts =
                Object.FindObjectsByType<CheckoutProductWorldInteractionTarget>(
                    FindObjectsInactive.Exclude);
            int customerCount = 0;
            while (!stagedCheckout.AllBasketsComplete && customerCount < 3)
            {
                Assert.That(checkoutTarget.TryPrimary(out error), Is.True, error);
                while (stagedCheckout.NextAction == StagedCheckoutPrimaryAction.Scan)
                {
                    CheckoutProductWorldInteractionTarget visible =
                        checkoutProducts.Single(target => target.IsAvailable);
                    Assert.That(visible.TryPrimary(out error), Is.True, error);
                }
                Assert.That(checkoutTarget.TryPrimary(out error), Is.True, error);
                customerCount++;
            }
            Assert.That(stagedCheckout.AllBasketsComplete, Is.True);
            Assert.That(customerCount, Is.EqualTo(3));
            yield return null;

            CleaningWorldInteractionTarget cleaningTarget =
                GameObject.Find("World Cleaning Interaction")
                    .GetComponent<CleaningWorldInteractionTarget>();
            CleaningTaskComponent cleaning =
                GameObject.Find("Cleaning Task").GetComponent<CleaningTaskComponent>();
            Assert.That(cleaning.NeedsCleaning, Is.True);
            while (cleaning.NeedsCleaning)
            {
                Assert.That(cleaningTarget.TryPrimary(out error), Is.True, error);
            }

            StoreOperatingController store =
                GameObject.Find("Store Operating Controller")
                    .GetComponent<StoreOperatingController>();
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
            Assert.That(store.ResultTotals, Is.Null);
            Assert.That(store.CurrentTotals.transactionCount, Is.EqualTo(3));
            Assert.That(store.CurrentTotals.unitsSold, Is.EqualTo(4));
            Assert.That(store.CurrentTotals.grossSalesCents, Is.EqualTo(696));
        }

        private static IEnumerator LoadValidationScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            Assert.That(
                Object.FindAnyObjectByType<FirstStoreInteractionController>(),
                Is.Not.Null);
        }

        private static ProductItem[] RemoveLooseColaUnits(int count)
        {
            ProductDefinition cola = Resources
                .FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == ColaProductId);
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            Assert.That(
                delivery.TryOpen(out _, out string error),
                Is.True,
                error);

            ProductItem[] result = new ProductItem[count];
            for (int index = 0; index < count; index++)
            {
                Assert.That(
                    delivery.TryRemoveOneUnit(
                        cola,
                        out result[index],
                        out _,
                        out _,
                        out error),
                    Is.True,
                    error);
            }

            return result;
        }

        private static ProductItem PickUpOneTargetedCola()
        {
            ProductItem loose = RemoveLooseColaUnits(1).Single();
            AimAt(Camera.main, loose);
            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            Assert.That(
                interaction.TryPickUpTargetedUnit(
                    out ProductItem held,
                    out string error),
                Is.True,
                error);
            return held;
        }

        private static void AimAt(
            Camera camera,
            ProductItem target,
            params ProductItem[] otherUnits)
        {
            Freeze(target);
            target.transform.SetPositionAndRotation(
                camera.transform.position + camera.transform.forward * 1.5f,
                Quaternion.identity);

            for (int index = 0; index < otherUnits.Length; index++)
            {
                Freeze(otherUnits[index]);
                otherUnits[index].transform.SetPositionAndRotation(
                    camera.transform.position +
                    camera.transform.right * (1.5f + index),
                    Quaternion.identity);
            }

            Physics.SyncTransforms();
        }

        private static void AimAt(Camera camera, Transform target)
        {
            target.SetPositionAndRotation(
                camera.transform.position + camera.transform.forward * 1.5f,
                Quaternion.identity);
            Physics.SyncTransforms();
        }

        private static void Freeze(ProductItem item)
        {
            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
