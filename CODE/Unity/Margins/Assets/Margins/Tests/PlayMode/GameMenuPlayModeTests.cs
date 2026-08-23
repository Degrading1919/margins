using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Margins.Tests
{
    [Category("GameMenu")]
    public sealed class GameMenuPlayModeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private string createdSavePath;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrWhiteSpace(createdSavePath))
            {
                DeleteIfPresent(createdSavePath);
                DeleteIfPresent(createdSavePath + ".tmp");
                DeleteIfPresent(createdSavePath + ".previous");
            }
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TitleAndSettingsUseSeparatedUIToolkitPresentation()
        {
            yield return LoadValidationScene();
            GamePauseMenuController menu =
                Object.FindAnyObjectByType<GamePauseMenuController>();
            GameMenuPresenter presenter =
                Object.FindAnyObjectByType<GameMenuPresenter>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            menu.ShowTitleAtLaunch();
            yield return null;
            Assert.That(presenter.TryValidateConfiguration(out string error), Is.True, error);
            VisualElement root = presenter.Root;
            Assert.That(root.Q("menu-background-layer"), Is.Not.Null);
            Assert.That(root.Q("menu-foreground-layer"), Is.Not.Null);
            Assert.That(root.Q<IMGUIContainer>(), Is.Null);

            string[] primaryOptions =
            {
                root.Q<Button>("title-new-business").text,
                root.Q<Button>("title-load-business").text,
                root.Q<Button>("title-settings").text,
                root.Q<Button>("title-quit").text
            };
            Assert.That(primaryOptions, Is.EqualTo(new[]
            {
                "New Business",
                "Load Business",
                "Settings",
                "Quit to Desktop"
            }));
            string allText = string.Join(
                " ",
                root.Query<TextElement>().ToList().Select(element => element.text));
            Assert.That(allText, Does.Not.Contain("OWNER / OPERATOR"));
            Assert.That(allText, Does.Not.Contain("Build the business"));
            Assert.That(allText, Does.Not.Contain("FIRST-PERSON BUSINESS SIMULATION"));

            menu.OpenSettings();
            yield return null;
            Assert.That(menu.Screen, Is.EqualTo(GameMenuScreen.SettingsGeneral));
            Assert.That(
                root.Q("settings-general-content").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                root.Q("settings-controls-content").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.None));

            menu.SelectSettingsTab(true);
            yield return null;
            Assert.That(menu.Screen, Is.EqualTo(GameMenuScreen.SettingsControls));
            Assert.That(
                root.Q("settings-controls-content").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<ScrollView>("settings-binding-list").childCount, Is.GreaterThan(0));
            string[] boundActions = menu.BindingSettings.GetPlayerBindings()
                .Select(entry => entry.Action.name)
                .Distinct()
                .ToArray();
            Assert.That(boundActions, Does.Contain("Move"));
            Assert.That(boundActions, Does.Contain("Sprint"));
            Assert.That(boundActions, Does.Contain("Jump"));
            Assert.That(boundActions, Does.Contain("Interact"));
            Assert.That(boundActions, Does.Contain("BuildMode"));
            Assert.That(boundActions, Does.Contain("Cancel"));
            Assert.That(boundActions, Does.Contain("RotatePlacement"));

            VisualElement notification = root.Q("settings-notification");
            VisualElement footer = root.Q(className: "settings-footer");
            Assert.That(notification.parent, Is.SameAs(footer.parent));
            Assert.That(
                notification.parent.IndexOf(notification),
                Is.LessThan(footer.parent.IndexOf(footer)));
        }

        [UnityTest]
        public IEnumerator CompanyDeskUsesAuthoritativeSchedulesPoliciesAlertsRecoveryAndOvernight()
        {
            yield return LoadValidationScene();
            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            GameMenuPresenter presenter =
                Object.FindAnyObjectByType<GameMenuPresenter>();
            Assert.That(portfolio, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            CompleteManagementFirstShift(portfolio);
            StoreCustomerFlowController customerFlow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<InStoreEmployeeWorkController>();
            customerFlow.enabled = false;
            employeeWork.enabled = false;
            player.SetGameplayMode(false);
            portfolio.enabled = false;

            yield return null;
            yield return null;
            VisualElement root = presenter.Root;
            Assert.That(
                root.Q("management-view").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<IMGUIContainer>(), Is.Null);
            Button blockedOvernight = root.Q<Button>(
                "management-advance-overnight");
            Assert.That(blockedOvernight, Is.Not.Null);
            Assert.That(blockedOvernight.enabledInHierarchy, Is.False);
            Submit(root.Q<Button>("management-leave-location"));
            yield return null;
            Assert.That(portfolio.HasActiveDetailedSimulation, Is.False);
            Assert.That(customerFlow.enabled, Is.False);
            Assert.That(employeeWork.enabled, Is.False);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-marcus-reed",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-priya-shah",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);

            PortfolioProgressionSnapshot degraded =
                portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot degradedLocation = degraded.locations.Single();
            degradedLocation.maintenanceCondition = 20;
            degradedLocation.failurePressure = 80;
            degradedLocation.operatingAlerts.Add(
                new PortfolioOperatingAlertSnapshot
                {
                    alertId =
                        "alert-location-mile-7-market-maintenance-pressure-999",
                    problemTypeId = "maintenance-pressure",
                    severity = PortfolioAlertSeverity.Critical,
                    openedDay = degraded.currentDay,
                    resolvedDay = 0,
                    acknowledged = false,
                    requiresOwnerAttention = true,
                    summary =
                        "Maintenance condition is below the configured standard.",
                    recoveryAction =
                        "Authorize maintenance, raise the spending limit, or complete standards work."
                });
            Assert.That(
                portfolio.TryRestoreSnapshot(degraded, out error),
                Is.True,
                error);
            yield return null;

            Submit(root.Q<Button>("management-team-tab"));
            yield return null;
            Button weekdaySchedule = root.Q<Button>(
                "management-schedule-employee-elena-ruiz-weekdays");
            Assert.That(weekdaySchedule, Is.Not.Null);
            Submit(weekdaySchedule);
            yield return null;
            Assert.That(
                portfolio.Progression.Employees.Single(employee =>
                    employee.employeeId == "employee-elena-ruiz")
                    .schedule.scheduledDayMask,
                Is.EqualTo(0x1f));
            Assert.That(
                root.Q<Button>("management-shift-employee-elena-ruiz-day"),
                Is.Null,
                "Inert same-day shift-time presets must not be player-facing.");

            PortfolioProgressionSnapshot beforePromotion =
                portfolio.Progression.CreateSnapshot();
            PortfolioProgressionSnapshot promotable =
                portfolio.Progression.CreateSnapshot();
            promotable.employees.RemoveAll(employee =>
                employee.role == PortfolioEmployeeRole.Manager);
            promotable.employees.Single(employee =>
                employee.employeeId == "employee-elena-ruiz").skill = 65;
            Assert.That(
                portfolio.TryRestoreSnapshot(promotable, out error),
                Is.True,
                error);
            yield return null;
            Assert.That(
                root.Q<Button>("management-promote-employee-elena-ruiz"),
                Is.Null,
                "Staff development actions should remain behind progressive disclosure.");
            Submit(root.Q<Button>(
                "management-staff-details-employee-elena-ruiz"));
            yield return null;
            Button promote = root.Q<Button>(
                "management-promote-employee-elena-ruiz");
            Assert.That(promote, Is.Not.Null);
            Submit(promote);
            yield return null;
            Assert.That(
                portfolio.Progression.Employees.Single(employee =>
                    employee.employeeId == "employee-elena-ruiz").role,
                Is.EqualTo(PortfolioEmployeeRole.Manager));
            Assert.That(
                portfolio.TryRestoreSnapshot(beforePromotion, out error),
                Is.True,
                error);
            yield return null;

            Submit(root.Q<Button>("management-policy-tab"));
            yield return null;
            Assert.That(
                root.Q<Button>("management-pricing-authority"),
                Is.Null,
                "Manager price authority has no simulation effect and must not be player-facing.");
            long cashBeforeOrder = portfolio.Progression.CashCents;
            Assert.That(
                portfolio.TryPlaceManualPurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            PurchaseOrderSnapshot pendingOrder = portfolio.Progression
                .PurchaseOrders.Single(order => !order.IsTerminal);
            yield return null;
            Button cancelOrder = root.Q<Button>(
                $"management-cancel-order-{pendingOrder.orderId}");
            Assert.That(cancelOrder, Is.Not.Null);
            Submit(cancelOrder);
            yield return null;
            Assert.That(
                portfolio.Progression.PurchaseOrders.Single(order =>
                    order.orderId == pendingOrder.orderId).status,
                Is.EqualTo(PurchaseOrderStatus.Canceled));
            Assert.That(portfolio.Progression.CashCents,
                Is.EqualTo(cashBeforeOrder));
            long priorBudget = portfolio.Progression.Locations.Single()
                .delegationPolicy.dailySpendingLimitCents;
            Submit(root.Q<Button>("management-purchase-authority"));
            yield return null;
            Assert.That(
                portfolio.Progression.Locations.Single()
                    .delegationPolicy.managerCanPurchase,
                Is.False);
            Submit(root.Q<Button>("management-budget-increase"));
            yield return null;
            Assert.That(
                portfolio.Progression.Locations.Single()
                    .delegationPolicy.dailySpendingLimitCents,
                Is.EqualTo(priorBudget + 25_000));

            Submit(root.Q<Button>("management-alerts-tab"));
            yield return null;
            const string alertId =
                "alert-location-mile-7-market-maintenance-pressure-999";
            Button acknowledge = root.Q<Button>($"management-ack-{alertId}");
            Assert.That(acknowledge, Is.Not.Null);
            Assert.That(acknowledge.text, Is.EqualTo("Mark as seen"));
            string alertText = AllText(root.Q("management-content"));
            Assert.That(alertText, Does.Contain("Store condition is slipping"));
            Assert.That(alertText, Does.Not.Contain("maintenance-pressure"));
            Assert.That(alertText, Does.Not.Contain("OPEN EXCEPTIONS"));
            Submit(acknowledge);
            yield return null;
            Assert.That(
                portfolio.Progression.Locations.Single().operatingAlerts
                    .Single(alert => alert.alertId == alertId).acknowledged,
                Is.True);

            long cashBeforeRecovery = portfolio.Progression.CashCents;
            int conditionBeforeRecovery = portfolio.Progression.Locations
                .Single().maintenanceCondition;
            Button emergency = root.Q<Button>(
                "management-emergency-maintenance");
            Assert.That(emergency, Is.Not.Null);
            Submit(emergency);
            yield return null;
            Assert.That(portfolio.Progression.CashCents,
                Is.LessThan(cashBeforeRecovery));
            Assert.That(
                portfolio.Progression.Locations.Single().maintenanceCondition,
                Is.GreaterThan(conditionBeforeRecovery));
            Assert.That(
                root.Q<Button>("management-toggle-resolved-alerts"),
                Is.Not.Null,
                "Resolved alerts should remain available without filling the default alert view.");

            Submit(root.Q<Button>("management-overview-tab"));
            yield return null;
            int dayBefore = portfolio.Progression.CurrentDay;
            Button overnight = root.Q<Button>(
                "management-advance-overnight");
            Assert.That(overnight, Is.Not.Null);
            Assert.That(overnight.enabledInHierarchy, Is.True);
            Submit(overnight);
            yield return null;
            PortfolioProgressionSnapshot afterOvernight =
                portfolio.Progression.CreateSnapshot();
            Assert.That(afterOvernight.currentDay, Is.EqualTo(dayBefore + 1));
            Assert.That(afterOvernight.locations.Single().lastReport.day,
                Is.EqualTo(afterOvernight.currentDay));
            Assert.That(afterOvernight.locations.Single().lastReport
                .isDetailedOperation, Is.False);

            Submit(root.Q<Button>("management-locations-tab"));
            yield return null;
            Assert.That(
                root.Q("management-location-card-location-mile-7-market"),
                Is.Not.Null);
            Button lease = root.Q<Button>(
                "management-lease-location-riverbend-market");
            Assert.That(lease, Is.Not.Null);
            Submit(lease);
            yield return null;
            Assert.That(portfolio.Progression.Locations.Count, Is.EqualTo(2));

            Submit(root.Q<Button>("management-reports-tab"));
            yield return null;
            Assert.That(root.Q("management-portfolio-report"), Is.Not.Null);
            Assert.That(
                root.Q("management-location-report-location-mile-7-market"),
                Is.Not.Null);
            Assert.That(
                root.Q("management-location-report-location-riverbend-market"),
                Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OwnerPhoneGuidesStoreClosureAndRevealsDetailsOnRequest()
        {
            yield return LoadValidationScene();
            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            GameMenuPresenter presenter =
                Object.FindAnyObjectByType<GameMenuPresenter>();
            Assert.That(portfolio, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            CompleteManagementFirstShift(portfolio);
            Object.FindAnyObjectByType<StoreCustomerFlowController>().enabled = false;
            Object.FindAnyObjectByType<InStoreEmployeeWorkController>().enabled = false;
            player.SetGameplayMode(false);
            portfolio.enabled = false;
            yield return null;
            yield return null;

            VisualElement root = presenter.Root;
            Assert.That(root.Q<Label>("management-page-title").text,
                Is.EqualTo("Today"));
            Assert.That(root.Q<Button>("management-overview-tab").text,
                Is.EqualTo("Home"));
            Assert.That(root.Q<Button>("management-locations-tab").text,
                Is.EqualTo("Stores"));
            Assert.That(root.Q<Button>("management-team-tab").text,
                Is.EqualTo("Staff"));
            Assert.That(root.Q<Button>("management-policy-tab").text,
                Is.EqualTo("Operations"));

            Button endDay = root.Q<Button>("management-advance-overnight");
            Assert.That(endDay.text, Is.EqualTo("End Day 1"));
            Assert.That(endDay.enabledInHierarchy, Is.False);
            string blocker = root.Q<Label>("management-end-day-blocker").text;
            Assert.That(blocker, Does.Contain("leave the active store").IgnoreCase);
            AssertPlayerFacingLanguage(root);

            Submit(root.Q<Button>("management-leave-location"));
            yield return null;
            Assert.That(portfolio.HasActiveDetailedSimulation, Is.False);
            HireCoreTeam(portfolio);
            yield return null;

            endDay = root.Q<Button>("management-advance-overnight");
            Assert.That(endDay.enabledInHierarchy, Is.True);
            int priorDay = portfolio.Progression.CurrentDay;
            Submit(endDay);
            yield return null;
            Assert.That(portfolio.Progression.CurrentDay, Is.EqualTo(priorDay + 1));
            Assert.That(root.Q<Label>("management-status").text,
                Does.Contain("complete").IgnoreCase);

            Submit(root.Q<Button>("management-reports-tab"));
            yield return null;
            const string firstLocationId =
                PortfolioProgressionRules.FirstLocationId;
            Assert.That(
                root.Q($"management-location-report-detail-{firstLocationId}"),
                Is.Null);
            Submit(root.Q<Button>(
                $"management-report-details-{firstLocationId}"));
            yield return null;
            VisualElement reportDetail = root.Q(
                $"management-location-report-detail-{firstLocationId}");
            Assert.That(reportDetail, Is.Not.Null);
            Assert.That(AllText(reportDetail), Does.Contain("Product costs"));
            AssertPlayerFacingLanguage(root);

            Submit(root.Q<Button>("management-policy-tab"));
            yield return null;
            Assert.That(root.Q<Button>("management-service-decrease"), Is.Null);
            Submit(root.Q<Button>("management-toggle-standards"));
            yield return null;
            Assert.That(root.Q<Button>("management-service-decrease"), Is.Not.Null);

            Submit(root.Q<Button>("management-team-tab"));
            yield return null;
            Assert.That(
                root.Q<Button>("management-train-employee-elena-ruiz"),
                Is.Null);
            Submit(root.Q<Button>(
                "management-staff-details-employee-elena-ruiz"));
            yield return null;
            Assert.That(
                root.Q<Button>("management-train-employee-elena-ruiz"),
                Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OwnerPhoneLeasesAndSwitchesStoresDirectly()
        {
            yield return LoadValidationScene();
            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();
            GameMenuPresenter presenter =
                Object.FindAnyObjectByType<GameMenuPresenter>();
            Assert.That(portfolio, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            CompleteManagementFirstShift(portfolio);
            Object.FindAnyObjectByType<StoreCustomerFlowController>().enabled = false;
            Object.FindAnyObjectByType<InStoreEmployeeWorkController>().enabled = false;
            player.SetGameplayMode(false);
            portfolio.enabled = false;
            yield return null;
            yield return null;

            VisualElement root = presenter.Root;
            Submit(root.Q<Button>("management-leave-location"));
            yield return null;
            HireCoreTeam(portfolio);
            Assert.That(
                portfolio.TryAdvanceOvernight(out string overnightError),
                Is.True,
                overnightError);
            yield return null;
            Submit(root.Q<Button>("management-locations-tab"));
            yield return null;
            Submit(root.Q<Button>(
                "management-lease-location-riverbend-market"));
            yield return null;
            Assert.That(portfolio.Progression.Locations.Count, Is.EqualTo(2));

            Button riverbend = root.Q<Button>(
                "management-visit-location-riverbend-market");
            Assert.That(riverbend.text, Does.StartWith("Go to Riverbend"));
            Submit(riverbend);
            yield return null;
            yield return null;
            Assert.That(portfolio.ActiveDetailedSimulationLocationId,
                Is.EqualTo("location-riverbend-market"));
            Assert.That(player.IsGameplayMode, Is.True);

            Press(keyboard.tabKey);
            yield return null;
            Release(keyboard.tabKey);
            yield return null;
            Assert.That(
                root.Q<Label>(
                    "management-location-state-location-riverbend-market").text,
                Is.EqualTo("YOU'RE HERE"));
            Button mileSeven = root.Q<Button>(
                "management-visit-location-mile-7-market");
            Assert.That(mileSeven.text, Does.StartWith("Go to Mile 7"));
            Submit(mileSeven);
            yield return null;
            yield return null;
            Assert.That(portfolio.ActiveDetailedSimulationLocationId,
                Is.EqualTo(PortfolioProgressionRules.FirstLocationId));
            Assert.That(player.IsGameplayMode, Is.True);
        }

        [UnityTest]
        public IEnumerator NewAndLoadBusinessUseAuthoritativeStateAndKeepDiskSave()
        {
            yield return LoadValidationScene();
            FirstStoreDiskPersistenceController persistence =
                Object.FindAnyObjectByType<FirstStoreDiskPersistenceController>();
            FirstStorePersistenceMapperComponent mapper =
                Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            GamePauseMenuController menu =
                Object.FindAnyObjectByType<GamePauseMenuController>();
            Assert.That(persistence, Is.Not.Null);
            Assert.That(mapper, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);

            for (int frame = 0;
                 frame < 10 && !persistence.HasNewBusinessTemplate;
                 frame++)
            {
                yield return null;
            }
            Assert.That(persistence.HasNewBusinessTemplate, Is.True);
            Assert.That(
                mapper.TryCapture(out FirstStoreSnapshot cleanState, out string error),
                Is.True,
                error);

            string saveFileName = $"menu-test-{Guid.NewGuid():N}.json";
            SetPrivateField(persistence, "saveFileName", saveFileName);
            createdSavePath = persistence.SavePath;

            MoveCheckoutFixture(new GridPosition(1, 1), 1);
            Assert.That(
                mapper.TryCapture(out FirstStoreSnapshot savedState, out error),
                Is.True,
                error);
            Assert.That(persistence.TrySave(), Is.True, persistence.LastDiagnostic);
            Assert.That(File.Exists(createdSavePath), Is.True);

            MoveCheckoutFixture(new GridPosition(4, 3), 2);
            menu.Resume();
            menu.ReturnToTitle();
            menu.RequestNewBusiness();
            Assert.That(
                menu.PendingReplacement,
                Is.EqualTo(SessionReplacementAction.NewBusiness));
            Assert.That(File.Exists(createdSavePath), Is.True);
            menu.RequestNewBusiness();
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(
                mapper.TryCapture(out FirstStoreSnapshot newBusinessState, out error),
                Is.True,
                error);
            Assert.That(newBusinessState, Is.EqualTo(cleanState));
            Assert.That(File.Exists(createdSavePath), Is.True);

            menu.ReturnToTitle();
            menu.RequestLoadBusiness();
            Assert.That(
                menu.PendingReplacement,
                Is.EqualTo(SessionReplacementAction.LoadBusiness));
            menu.RequestLoadBusiness();
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(
                mapper.TryCapture(out FirstStoreSnapshot loadedState, out error),
                Is.True,
                error);
            Assert.That(loadedState, Is.EqualTo(savedState));
            Assert.That(File.Exists(createdSavePath), Is.True);
        }

        [UnityTest]
        public IEnumerator InteractiveRebindCanBeCancelledWithoutChangingBinding()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap player = new("Player");
            asset.AddActionMap(player);
            InputAction jump = player.AddAction(
                "Jump",
                InputActionType.Button);
            jump.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
            TestPreferences preferences = new();
            InputBindingSettings settings = new(asset, preferences);
            Assert.That(settings.TryLoad(out string error), Is.True, error);
            player.Enable();
            PlayerBindingEntry entry = settings.GetPlayerBindings().Single();
            RebindResult? result = null;

            Assert.That(
                settings.BeginInteractiveRebind(
                    entry,
                    completed => result = completed,
                    out error),
                Is.True,
                error);
            Assert.That(settings.IsRebinding, Is.True);

            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.Outcome, Is.EqualTo(RebindOutcome.Cancelled));
            Assert.That(settings.IsRebinding, Is.False);
            Assert.That(jump.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
            Assert.That(jump.enabled, Is.True);

            settings.Dispose();
            Object.Destroy(asset);
            yield return null;
        }

        private static IEnumerator LoadValidationScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;
        }

        private static void MoveCheckoutFixture(
            GridPosition position,
            int quarterTurns)
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(item =>
                    item.StableFixtureInstanceId == "fixture-checkout-essential-01");
            FixturePlacementResult result = placement.TryMove(
                fixture,
                position,
                quarterTurns);
            Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
        }

        private static void CompleteManagementFirstShift(
            PortfolioProgressionController portfolio)
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(item =>
                    item.StableFixtureInstanceId ==
                    "fixture-checkout-essential-01");
            if (!placement.IsPlaced(fixture.StableFixtureInstanceId))
            {
                FixturePlacementResult placed = placement.TryPlace(
                    fixture,
                    new GridPosition(1, 1),
                    0);
                Assert.That(placed.IsSuccess, Is.True, placed.Failure.ToString());
            }

            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            ProductDefinition cola = Resources
                .FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product =>
                    product.StableProductId == "prod-cola-can-355ml");
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
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            Assert.That(
                checkout.TryBeginSession(
                    "transaction-management-ui-001",
                    out error),
                Is.True,
                error);
            Assert.That(
                checkout.TryScan(
                    cola,
                    1,
                    out CheckoutFailure scanFailure),
                Is.True,
                scanFailure.ToString());
            Assert.That(
                checkout.TryComplete(
                    out _,
                    out CheckoutFailure completionFailure),
                Is.True,
                completionFailure.ToString());
            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out error),
                Is.True,
                error);
            Assert.That(portfolio.Progression.FirstShiftCompleted, Is.True);
        }

        private static void HireCoreTeam(
            PortfolioProgressionController portfolio)
        {
            foreach (string employeeId in new[]
                     {
                         "employee-elena-ruiz",
                         "employee-marcus-reed",
                         "employee-priya-shah"
                     })
            {
                Assert.That(
                    portfolio.TryHireCandidate(
                        employeeId,
                        PortfolioProgressionRules.FirstLocationId,
                        out string error),
                    Is.True,
                    error);
            }
        }

        private static string AllText(VisualElement element)
        {
            return string.Join(
                " ",
                element.Query<TextElement>().ToList()
                    .Select(value => value.text));
        }

        private static void AssertPlayerFacingLanguage(VisualElement root)
        {
            string text = AllText(root.Q("management-view"));
            foreach (string backendTerm in new[]
                     {
                         "detailed simulation",
                         "aggregate portfolio",
                         "procurement",
                         "COGS",
                         "due tick",
                         "problemTypeId",
                         "vertical-slice"
                     })
            {
                Assert.That(
                    text,
                    Does.Not.Contain(backendTerm).IgnoreCase,
                    $"Player-facing management copy exposed backend term '{backendTerm}'.");
            }
        }

        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null);
            NavigationSubmitEvent submit =
                NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
            submit.Dispose();
        }

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private sealed class TestPreferences : IGamePreferences
        {
            private readonly Dictionary<string, object> values = new();

            public bool HasKey(string key) => values.ContainsKey(key);
            public int GetInt(string key, int defaultValue) =>
                values.TryGetValue(key, out object value) ? (int)value : defaultValue;
            public float GetFloat(string key, float defaultValue) =>
                values.TryGetValue(key, out object value) ? (float)value : defaultValue;
            public string GetString(string key, string defaultValue) =>
                values.TryGetValue(key, out object value) ? (string)value : defaultValue;
            public void SetInt(string key, int value) => values[key] = value;
            public void SetFloat(string key, float value) => values[key] = value;
            public void SetString(string key, string value) => values[key] = value;
            public void DeleteKey(string key) => values.Remove(key);
            public void Save()
            {
            }
        }
    }
}
