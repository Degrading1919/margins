using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Margins
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenuPresenter : MonoBehaviour
    {
        private enum ManagementPage
        {
            Overview,
            Team,
            Policies,
            Alerts
        }

        private const long PolicyBudgetStepCents = 25_000;

        [SerializeField] private GamePauseMenuController controller;
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private PortfolioProgressionController portfolio;

        private readonly Dictionary<VisualElement, Action> activations = new();
        private readonly List<VisualElement> focusables = new();
        private readonly List<Button> bindingButtons = new();
        private readonly List<Button> staticTitleButtons = new();
        private readonly List<Button> staticPauseButtons = new();
        private readonly List<Button> staticManagementButtons = new();
        private readonly List<Button> dynamicManagementButtons = new();

        private VisualElement root;
        private VisualElement titleView;
        private VisualElement pauseView;
        private VisualElement settingsView;
        private VisualElement managementView;
        private VisualElement generalContent;
        private VisualElement controlsContent;
        private Button titleNewBusiness;
        private Button titleLoadBusiness;
        private Button generalTab;
        private Button controlsTab;
        private Toggle fullscreenToggle;
        private SliderInt interfaceScaleSlider;
        private Label interfaceScaleValue;
        private SliderInt masterVolumeSlider;
        private Label masterVolumeValue;
        private Toggle cameraMotionToggle;
        private SliderInt sensitivitySlider;
        private Label sensitivityValue;
        private Toggle invertYToggle;
        private ScrollView bindingList;
        private Button resetBindings;
        private Button settingsBack;
        private Button settingsApply;
        private Label managementSummary;
        private Label managementStatus;
        private ScrollView managementContent;
        private Button managementOverviewTab;
        private Button managementTeamTab;
        private Button managementPolicyTab;
        private Button managementAlertsTab;
        private Button managementSave;
        private Button managementLoad;
        private Button managementResume;
        private NotificationElements titleNotification;
        private NotificationElements pauseNotification;
        private NotificationElements settingsNotification;
        private bool initialized;
        private bool managementWasVisible;
        private ManagementPage managementPage;
        private int renderedBindingRevision = -1;
        private string renderedActiveBindingKey;

        public bool IsConfigured { get; private set; }
        public VisualElement Root => root;

        private void Awake()
        {
            document ??= GetComponent<UIDocument>();
            controller ??= FindAnyObjectByType<GamePauseMenuController>();
            portfolio ??= FindAnyObjectByType<PortfolioProgressionController>();
        }

        private void OnEnable()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError(error, this);
                return;
            }

            controller.PresentationChanged += Refresh;
            if (portfolio != null)
            {
                portfolio.ManagementChanged += QueueRefresh;
                portfolio.SetToolkitManagementAvailable(true);
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.PresentationChanged -= Refresh;
            }
            if (portfolio != null)
            {
                portfolio.ManagementChanged -= QueueRefresh;
                portfolio.SetToolkitManagementAvailable(false);
            }
        }

        private void Update()
        {
            bool managementVisible = portfolio?.OwnsManagementDesk == true &&
                                     controller?.IsOpen == false;
            if (managementVisible != managementWasVisible)
            {
                Refresh();
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (controller == null || document == null ||
                document.visualTreeAsset == null || styleSheet == null)
            {
                error =
                    "Game menu presentation requires controller, UI document, visual tree, and style sheet references.";
                return false;
            }
            if (!IsConfigured || root == null)
            {
                error = "Game menu visual elements have not been initialized.";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryInitialize(out string error)
        {
            if (initialized)
            {
                error = null;
                return IsConfigured;
            }
            initialized = true;

            if (controller == null || document == null ||
                document.visualTreeAsset == null || styleSheet == null)
            {
                error =
                    "Game menu presentation requires controller, UI document, visual tree, and style sheet references.";
                return false;
            }

            root = document.rootVisualElement;
            root.styleSheets.Add(styleSheet);
            root.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);

            titleView = Require<VisualElement>("title-view");
            pauseView = Require<VisualElement>("pause-view");
            settingsView = Require<VisualElement>("settings-view");
            managementView = Require<VisualElement>("management-view");
            generalContent = Require<VisualElement>("settings-general-content");
            controlsContent = Require<VisualElement>("settings-controls-content");
            titleNewBusiness = Require<Button>("title-new-business");
            titleLoadBusiness = Require<Button>("title-load-business");
            generalTab = Require<Button>("settings-general-tab");
            controlsTab = Require<Button>("settings-controls-tab");
            fullscreenToggle = Require<Toggle>("settings-fullscreen");
            interfaceScaleSlider = Require<SliderInt>("settings-interface-scale");
            interfaceScaleValue = Require<Label>("settings-interface-scale-value");
            masterVolumeSlider = Require<SliderInt>("settings-master-volume");
            masterVolumeValue = Require<Label>("settings-master-volume-value");
            cameraMotionToggle = Require<Toggle>("settings-camera-motion");
            sensitivitySlider = Require<SliderInt>("settings-look-sensitivity");
            sensitivityValue = Require<Label>("settings-look-sensitivity-value");
            invertYToggle = Require<Toggle>("settings-invert-y");
            bindingList = Require<ScrollView>("settings-binding-list");
            resetBindings = Require<Button>("settings-reset-bindings");
            settingsBack = Require<Button>("settings-back");
            settingsApply = Require<Button>("settings-apply");
            managementSummary = Require<Label>("management-summary");
            managementStatus = Require<Label>("management-status");
            managementContent = Require<ScrollView>("management-content");
            managementOverviewTab = Require<Button>("management-overview-tab");
            managementTeamTab = Require<Button>("management-team-tab");
            managementPolicyTab = Require<Button>("management-policy-tab");
            managementAlertsTab = Require<Button>("management-alerts-tab");
            managementSave = Require<Button>("management-save");
            managementLoad = Require<Button>("management-load");
            managementResume = Require<Button>("management-resume");

            titleNotification = Notification(
                "title-notification",
                "title-notification-message",
                "title-notification-dismiss");
            pauseNotification = Notification(
                "pause-notification",
                "pause-notification-message",
                "pause-notification-dismiss");
            settingsNotification = Notification(
                "settings-notification",
                "settings-notification-message",
                "settings-notification-dismiss");

            if (HasMissingRequiredElement())
            {
                error = "Game menu visual tree is missing one or more required named elements.";
                return false;
            }

            RegisterStaticActions();
            RegisterSettingChanges();
            IsConfigured = true;
            error = null;
            return true;
        }

        private void RegisterStaticActions()
        {
            RegisterButton(titleNewBusiness, controller.RequestNewBusiness);
            RegisterButton(titleLoadBusiness, controller.RequestLoadBusiness);
            RegisterButton(Require<Button>("title-settings"), controller.OpenSettings);
            RegisterButton(Require<Button>("title-quit"), controller.QuitToDesktop);

            RegisterButton(Require<Button>("pause-resume"), controller.Resume);
            RegisterButton(Require<Button>("pause-save"), controller.SaveBusiness);
            RegisterButton(Require<Button>("pause-load"), controller.RequestLoadBusiness);
            RegisterButton(Require<Button>("pause-settings"), controller.OpenSettings);
            RegisterButton(Require<Button>("pause-title"), controller.ReturnToTitle);
            RegisterButton(Require<Button>("pause-quit"), controller.QuitToDesktop);

            RegisterButton(generalTab, () => controller.SelectSettingsTab(false));
            RegisterButton(controlsTab, () => controller.SelectSettingsTab(true));
            RegisterButton(resetBindings, controller.ResetBindingsToDefaults);
            RegisterButton(settingsBack, controller.CloseSettings);
            RegisterButton(settingsApply, controller.ApplySettings);
            RegisterButton(titleNotification.Dismiss, controller.DismissNotification);
            RegisterButton(pauseNotification.Dismiss, controller.DismissNotification);
            RegisterButton(settingsNotification.Dismiss, controller.DismissNotification);
            RegisterButton(
                managementOverviewTab,
                () => SelectManagementPage(ManagementPage.Overview));
            RegisterButton(
                managementTeamTab,
                () => SelectManagementPage(ManagementPage.Team));
            RegisterButton(
                managementPolicyTab,
                () => SelectManagementPage(ManagementPage.Policies));
            RegisterButton(
                managementAlertsTab,
                () => SelectManagementPage(ManagementPage.Alerts));
            RegisterButton(managementSave, controller.SaveBusiness);
            RegisterButton(managementLoad, controller.RequestLoadBusiness);
            RegisterButton(managementResume, ResumeManagement);

            staticTitleButtons.Add(titleNewBusiness);
            staticTitleButtons.Add(titleLoadBusiness);
            staticTitleButtons.Add(Require<Button>("title-settings"));
            staticTitleButtons.Add(Require<Button>("title-quit"));

            staticPauseButtons.Add(Require<Button>("pause-resume"));
            staticPauseButtons.Add(Require<Button>("pause-save"));
            staticPauseButtons.Add(Require<Button>("pause-load"));
            staticPauseButtons.Add(Require<Button>("pause-settings"));
            staticPauseButtons.Add(Require<Button>("pause-title"));
            staticPauseButtons.Add(Require<Button>("pause-quit"));

            staticManagementButtons.Add(managementOverviewTab);
            staticManagementButtons.Add(managementTeamTab);
            staticManagementButtons.Add(managementPolicyTab);
            staticManagementButtons.Add(managementAlertsTab);
            staticManagementButtons.Add(managementSave);
            staticManagementButtons.Add(managementLoad);
            staticManagementButtons.Add(managementResume);
        }

        private void RegisterSettingChanges()
        {
            fullscreenToggle.RegisterValueChangedCallback(
                evt => controller.SetFullscreen(evt.newValue));
            interfaceScaleSlider.RegisterValueChangedCallback(
                evt =>
                {
                    controller.SetInterfaceScale(evt.newValue / 100f);
                    interfaceScaleValue.text = $"{evt.newValue}%";
                });
            masterVolumeSlider.RegisterValueChangedCallback(
                evt =>
                {
                    controller.SetMasterVolume(evt.newValue / 100f);
                    masterVolumeValue.text = $"{evt.newValue}%";
                });
            cameraMotionToggle.RegisterValueChangedCallback(
                evt => controller.SetCameraMotion(evt.newValue));
            sensitivitySlider.RegisterValueChangedCallback(
                evt => controller.SetLookSensitivityLevel(evt.newValue));
            invertYToggle.RegisterValueChangedCallback(
                evt => controller.SetInvertY(evt.newValue));
        }

        private void Refresh()
        {
            if (!IsConfigured || controller == null)
            {
                return;
            }

            bool management = portfolio?.OwnsManagementDesk == true &&
                              !controller.IsOpen;
            managementWasVisible = management;
            root.style.display = controller.IsOpen || management
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (document.panelSettings != null)
            {
                document.panelSettings.scale =
                    GamePauseMenuController.UserInterfaceScale;
            }
            if (!controller.IsOpen && !management)
            {
                return;
            }

            bool title = !management &&
                         controller.Screen == GameMenuScreen.Title;
            bool pause = !management &&
                         controller.Screen == GameMenuScreen.Pause;
            bool settings = !management && controller.IsSettingsVisible;
            bool controls = controller.Screen == GameMenuScreen.SettingsControls;
            SetVisible(titleView, title);
            SetVisible(pauseView, pause);
            SetVisible(settingsView, settings);
            SetVisible(managementView, management);
            SetVisible(generalContent, settings && !controls);
            SetVisible(controlsContent, settings && controls);

            if (management)
            {
                RefreshManagement();
                RebuildFocusables(false, false, false, false, true);
                EnsureUsefulFocus();
                return;
            }

            titleLoadBusiness.SetEnabled(controller.CanLoadBusiness);
            Require<Button>("pause-load").SetEnabled(controller.CanLoadBusiness);
            titleNewBusiness.text =
                controller.PendingReplacement == SessionReplacementAction.NewBusiness
                    ? "Confirm New Business"
                    : "New Business";
            titleLoadBusiness.text =
                controller.PendingReplacement == SessionReplacementAction.LoadBusiness
                    ? "Confirm Load Business"
                    : "Load Business";
            Require<Button>("pause-load").text =
                controller.PendingReplacement == SessionReplacementAction.LoadBusiness
                    ? "Confirm Load Business"
                    : "Load Business";

            if (settings)
            {
                RefreshSettings(controls);
            }
            RefreshNotifications(title, pause, settings);
            RebuildFocusables(title, pause, settings, controls, false);
            EnsureUsefulFocus();
        }

        private void RefreshSettings(bool controls)
        {
            GameSettingsModel settings = controller.DraftSettings;
            if (settings == null)
            {
                return;
            }

            fullscreenToggle.SetValueWithoutNotify(settings.Fullscreen);
            interfaceScaleSlider.SetValueWithoutNotify(
                Mathf.RoundToInt(settings.InterfaceScale * 100f));
            interfaceScaleValue.text = $"{settings.InterfaceScale * 100f:0}%";
            masterVolumeSlider.SetValueWithoutNotify(
                Mathf.RoundToInt(settings.MasterVolume * 100f));
            masterVolumeValue.text = $"{settings.MasterVolume * 100f:0}%";
            cameraMotionToggle.SetValueWithoutNotify(settings.CameraMotion);
            sensitivitySlider.SetValueWithoutNotify(settings.LookSensitivityLevel);
            sensitivityValue.text =
                $"{settings.LookSensitivityLevel} · " +
                GameSettingsModel.SensitivityDescription(
                    settings.LookSensitivityLevel);
            invertYToggle.SetValueWithoutNotify(settings.InvertY);

            generalTab.EnableInClassList("settings-tab--active", !controls);
            controlsTab.EnableInClassList("settings-tab--active", controls);
            if (controls)
            {
                RefreshBindingRows();
            }
        }

        private void RefreshBindingRows()
        {
            InputBindingSettings settings = controller.BindingSettings;
            if (settings == null)
            {
                return;
            }
            if (renderedBindingRevision == settings.Revision &&
                string.Equals(
                    renderedActiveBindingKey,
                    settings.ActiveBindingKey,
                    StringComparison.Ordinal))
            {
                return;
            }

            foreach (Button button in bindingButtons)
            {
                activations.Remove(button);
            }
            bindingButtons.Clear();
            bindingList.Clear();

            foreach (PlayerBindingEntry entry in settings.GetPlayerBindings())
            {
                VisualElement row = new();
                row.AddToClassList("binding-row");
                Label name = new(entry.Label);
                name.AddToClassList("binding-name");
                Label value = new(entry.DisplayValue);
                value.AddToClassList("binding-value");
                Button rebind = new();
                rebind.AddToClassList("binding-button");
                bool active = settings.IsRebinding &&
                              string.Equals(
                                  settings.ActiveBindingKey,
                                  entry.StableKey,
                                  StringComparison.Ordinal);
                rebind.text = active
                    ? "Waiting…"
                    : entry.CanRebind
                        ? "Rebind"
                        : "Sensitivity";
                rebind.SetEnabled(
                    entry.CanRebind && (!settings.IsRebinding || active));
                RegisterButton(rebind, () => controller.BeginRebind(entry));
                bindingButtons.Add(rebind);
                row.Add(name);
                row.Add(value);
                row.Add(rebind);
                bindingList.Add(row);
            }

            renderedBindingRevision = settings.Revision;
            renderedActiveBindingKey = settings.ActiveBindingKey;
        }

        private void RefreshNotifications(bool title, bool pause, bool settings)
        {
            RefreshNotification(titleNotification, title);
            RefreshNotification(pauseNotification, pause);
            RefreshNotification(settingsNotification, settings);
        }

        private void RefreshNotification(
            NotificationElements elements,
            bool belongsToVisibleScreen)
        {
            MenuNotificationModel state = controller.Notification;
            bool visible = belongsToVisibleScreen && state.IsVisible;
            SetVisible(elements.Container, visible);
            if (!visible)
            {
                return;
            }

            elements.Message.text = state.Message;
            elements.Container.EnableInClassList(
                "notification--success",
                state.Kind == MenuNotificationKind.Success);
            elements.Container.EnableInClassList(
                "notification--error",
                state.Kind == MenuNotificationKind.Error);
            SetVisible(elements.Dismiss, state.IsPersistent);
        }

        private void RebuildFocusables(
            bool title,
            bool pause,
            bool settings,
            bool controls,
            bool management)
        {
            focusables.Clear();
            if (management)
            {
                AddEnabled(staticManagementButtons);
                AddEnabled(dynamicManagementButtons);
                return;
            }
            if (title)
            {
                AddEnabled(staticTitleButtons);
                if (controller.Notification.IsPersistent)
                {
                    focusables.Add(titleNotification.Dismiss);
                }
                return;
            }
            if (pause)
            {
                AddEnabled(staticPauseButtons);
                if (controller.Notification.IsPersistent)
                {
                    focusables.Add(pauseNotification.Dismiss);
                }
                return;
            }
            if (!settings)
            {
                return;
            }

            focusables.Add(generalTab);
            focusables.Add(controlsTab);
            if (controls)
            {
                focusables.Add(sensitivitySlider);
                focusables.Add(invertYToggle);
                AddEnabled(bindingButtons);
                focusables.Add(resetBindings);
            }
            else
            {
                focusables.Add(fullscreenToggle);
                focusables.Add(interfaceScaleSlider);
                focusables.Add(masterVolumeSlider);
                focusables.Add(cameraMotionToggle);
            }
            if (controller.Notification.IsPersistent)
            {
                focusables.Add(settingsNotification.Dismiss);
            }
            focusables.Add(settingsBack);
            focusables.Add(settingsApply);
        }

        private void EnsureUsefulFocus()
        {
            if (focusables.Count == 0)
            {
                return;
            }
            Focusable current = root.panel?.focusController?.focusedElement;
            if (current is VisualElement element && focusables.Contains(element))
            {
                return;
            }

            root.schedule.Execute(() =>
            {
                if (focusables.Count > 0)
                {
                    focusables[0].Focus();
                }
            });
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (controller == null ||
                (!controller.IsOpen && portfolio?.OwnsManagementDesk != true))
            {
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                case KeyCode.W:
                    MoveFocus(-1);
                    Consume(evt);
                    return;
                case KeyCode.DownArrow:
                case KeyCode.S:
                    MoveFocus(1);
                    Consume(evt);
                    return;
                case KeyCode.LeftArrow:
                case KeyCode.A:
                    if (AdjustFocusedControl(-1))
                    {
                        Consume(evt);
                    }
                    return;
                case KeyCode.RightArrow:
                case KeyCode.D:
                    if (AdjustFocusedControl(1))
                    {
                        Consume(evt);
                    }
                    return;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    if (ActivateFocusedControl())
                    {
                        Consume(evt);
                    }
                    return;
            }
        }

        private void MoveFocus(int direction)
        {
            if (focusables.Count == 0)
            {
                return;
            }
            VisualElement current = root.panel?.focusController?.focusedElement as VisualElement;
            int index = focusables.IndexOf(current);
            index = index < 0
                ? 0
                : (index + direction + focusables.Count) % focusables.Count;
            focusables[index].Focus();
        }

        private bool AdjustFocusedControl(int direction)
        {
            VisualElement focused = root.panel?.focusController?.focusedElement as VisualElement;
            if (focused is SliderInt slider)
            {
                int step = slider == sensitivitySlider ? 1 : 5;
                slider.value = Mathf.Clamp(
                    slider.value + direction * step,
                    slider.lowValue,
                    slider.highValue);
                return true;
            }
            if (focused is Toggle toggle)
            {
                toggle.value = direction > 0;
                return true;
            }
            if (focused == generalTab || focused == controlsTab)
            {
                controller.SelectSettingsTab(direction > 0);
                return true;
            }
            return false;
        }

        private bool ActivateFocusedControl()
        {
            VisualElement focused = root.panel?.focusController?.focusedElement as VisualElement;
            if (focused is Toggle toggle)
            {
                toggle.value = !toggle.value;
                return true;
            }
            if (focused != null && activations.TryGetValue(focused, out Action action))
            {
                action();
                return true;
            }
            return false;
        }

        private void RegisterButton(Button button, Action action)
        {
            if (button == null || action == null)
            {
                return;
            }
            button.clicked += action;
            activations[button] = action;
        }

        private void AddEnabled(IEnumerable<Button> buttons)
        {
            foreach (Button button in buttons)
            {
                if (button.enabledInHierarchy)
                {
                    focusables.Add(button);
                }
            }
        }

        private void QueueRefresh()
        {
            root?.schedule.Execute(Refresh);
        }

        private void ResumeManagement()
        {
            PortfolioProgressionSnapshot snapshot =
                portfolio?.Progression?.CreateSnapshot();
            if (snapshot == null || snapshot.locations.Count == 0)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(
                    snapshot.company.activeDetailedLocationId))
            {
                portfolio.TryVisitLocation(
                    snapshot.locations[0].locationId,
                    out _);
                return;
            }
            controller.Resume();
        }

        private void SelectManagementPage(ManagementPage selected)
        {
            managementPage = selected;
            Refresh();
        }

        private void RefreshManagement()
        {
            ClearManagementContent();
            if (portfolio?.Progression == null)
            {
                managementSummary.text = "Company state is unavailable.";
                managementStatus.text = "Management controls are not initialized.";
                managementResume.SetEnabled(false);
                return;
            }

            PortfolioProgressionSnapshot snapshot =
                portfolio.Progression.CreateSnapshot();
            managementSummary.text =
                $"DAY {snapshot.currentDay}   •   CASH {FormatCents(snapshot.cashCents)}   •   " +
                $"REPUTATION {snapshot.companyReputation}/100   •   {snapshot.locations.Count} LOCATION" +
                (snapshot.locations.Count == 1 ? string.Empty : "S");

            bool menuError = controller.Notification.IsVisible &&
                             controller.Notification.Kind ==
                             MenuNotificationKind.Error;
            managementStatus.text = controller.Notification.IsVisible
                ? controller.Notification.Message
                : portfolio.LastAction;
            managementStatus.EnableInClassList(
                "management-status--error",
                menuError ||
                (!controller.Notification.IsVisible &&
                 !portfolio.LastActionSucceeded));
            managementLoad.SetEnabled(controller.CanLoadBusiness);
            managementLoad.text =
                controller.PendingReplacement ==
                SessionReplacementAction.LoadBusiness
                    ? "Confirm Load"
                    : "Load";

            string activeLocationId = snapshot.company.activeDetailedLocationId;
            managementResume.SetEnabled(snapshot.locations.Count > 0);
            PortfolioLocationSnapshot activeLocation = snapshot.locations
                .FirstOrDefault(location => string.Equals(
                    location.locationId,
                    activeLocationId,
                    StringComparison.Ordinal));
            managementResume.text = activeLocation == null
                ? $"Enter {snapshot.locations[0].displayName}"
                : $"Return to {activeLocation.displayName}";

            managementOverviewTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Overview);
            managementTeamTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Team);
            managementPolicyTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Policies);
            managementAlertsTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Alerts);

            if (snapshot.locations.Count == 0)
            {
                AddManagementCopy(
                    managementContent,
                    "No occupied business locations are available.",
                    "management-empty");
                return;
            }

            PortfolioLocationSnapshot selected = snapshot.locations
                .FirstOrDefault(location => string.Equals(
                    location.locationId,
                    portfolio.SelectedLocationId,
                    StringComparison.Ordinal)) ?? snapshot.locations[0];
            BuildManagementLocationSelector(snapshot, selected);
            switch (managementPage)
            {
                case ManagementPage.Team:
                    BuildManagementTeam(snapshot, selected);
                    break;
                case ManagementPage.Policies:
                    BuildManagementPolicies(selected);
                    break;
                case ManagementPage.Alerts:
                    BuildManagementAlerts(selected);
                    break;
                default:
                    BuildManagementOverview(snapshot, selected);
                    break;
            }
        }

        private void BuildManagementLocationSelector(
            PortfolioProgressionSnapshot snapshot,
            PortfolioLocationSnapshot selected)
        {
            AddManagementSection("MANAGING LOCATION");
            VisualElement row = AddManagementRow(managementContent);
            foreach (PortfolioLocationSnapshot location in snapshot.locations
                         .OrderBy(value => value.displayName, StringComparer.Ordinal))
            {
                PortfolioLocationSnapshot target = location;
                AddManagementButton(
                    row,
                    target.displayName,
                    $"management-location-{target.locationId}",
                    () => portfolio.TrySelectManagementLocation(
                        target.locationId,
                        out _),
                    string.Equals(
                        target.locationId,
                        selected.locationId,
                        StringComparison.Ordinal));
            }
        }

        private void BuildManagementOverview(
            PortfolioProgressionSnapshot snapshot,
            PortfolioLocationSnapshot selected)
        {
            AddManagementSection("OPERATING STATUS");
            VisualElement location = AddManagementCard(
                selected.displayName,
                selected.operatingModel);
            AddManagementCopy(
                location,
                $"Inventory {selected.inventoryUnits}/{selected.inventoryCapacityUnits}   •   " +
                $"Service {selected.serviceQuality}/100   •   Satisfaction {selected.customerSatisfaction}/100\n" +
                $"Availability {selected.productAvailabilityBasisPoints / 100f:0.#}%   •   " +
                $"Product mix {selected.productMixBasisPoints / 100f:0.#}%   •   " +
                $"Maintenance {selected.maintenanceCondition}/100");

            string activeId = snapshot.company.activeDetailedLocationId;
            VisualElement physical = AddManagementCard(
                "Physical operation",
                string.IsNullOrWhiteSpace(activeId)
                    ? "No detailed location is active. Choose a location to enter physically."
                    : $"{LocationDisplayName(snapshot, activeId)} is active in detailed simulation.");
            if (string.IsNullOrWhiteSpace(activeId))
            {
                AddManagementButton(
                    AddManagementRow(physical),
                    $"Visit {selected.displayName}",
                    "management-visit-location",
                    () => portfolio.TryVisitLocation(
                        selected.locationId,
                        out _),
                    primary: true);
            }
            else
            {
                AddManagementButton(
                    AddManagementRow(physical),
                    "Close and leave active location",
                    "management-leave-location",
                    () => portfolio.TryLeaveVisitedLocation(out _),
                    danger: true);
            }

            AddManagementSection("OVERNIGHT");
            bool canAdvance = portfolio.Progression.CanAdvanceDelegatedDay(
                out string blocker);
            VisualElement overnight = AddManagementCard(
                $"Advance to operating day {snapshot.currentDay + 1}",
                canAdvance
                    ? "Resolve the next delegated operating day for every location, including procurement, payroll, rent, operating costs, reports, alerts, and employee progress."
                    : blocker);
            AddManagementButton(
                AddManagementRow(overnight),
                "Advance overnight",
                "management-advance-overnight",
                () => portfolio.TryAdvanceOvernight(out _),
                primary: true,
                enabled: canAdvance);

            AddManagementSection("LATEST RESULT");
            if (selected.hasLastReport && selected.lastReport != null)
            {
                PortfolioLocationReportSnapshot report = selected.lastReport;
                VisualElement reportCard = AddManagementCard(
                    $"Day {report.day} · " +
                    (report.isDetailedOperation ? "Detailed" : "Delegated"),
                    report.primaryCause);
                AddManagementCopy(
                    reportCard,
                    $"Sales {FormatCents(report.grossSalesCents)}   •   " +
                    $"Operating profit {FormatCents(report.operatingProfitCents)}   •   " +
                    $"Units {report.unitsSold}/{report.demandUnits} demand   •   " +
                    $"Alerts {report.openAlertCount}");
            }
            else
            {
                AddManagementCopy(
                    managementContent,
                    "No completed operating report exists for this location yet.",
                    "management-empty");
            }
        }

        private void BuildManagementTeam(
            PortfolioProgressionSnapshot snapshot,
            PortfolioLocationSnapshot selected)
        {
            AddManagementSection("ASSIGNMENTS & SCHEDULES");
            PortfolioEmployeeSnapshot[] assigned = snapshot.employees
                .Where(employee => string.Equals(
                    employee.assignedLocationId,
                    selected.locationId,
                    StringComparison.Ordinal))
                .OrderBy(employee => employee.role)
                .ThenBy(employee => employee.employeeId, StringComparer.Ordinal)
                .ToArray();
            if (assigned.Length == 0)
            {
                AddManagementCopy(
                    managementContent,
                    "No employees are assigned. Hire the approved cashier, stock clerk, and manager roles before delegation.",
                    "management-empty");
            }

            foreach (PortfolioEmployeeSnapshot employee in assigned)
            {
                PortfolioEmployeeSnapshot target = employee;
                VisualElement card = AddManagementCard(
                    $"{target.displayName} · {FriendlyRole(target.role)}",
                    $"{target.trait}   •   Skill {target.skill}   •   Reliability {target.reliability}   •   Morale {target.satisfaction}\n" +
                    $"Schedule {FormatSchedule(target.schedule)}   •   Focus {target.taskFocus}   •   Wage {FormatCents(target.dailyWageCents)}/day");

                AddManagementCopy(card, "Scheduled days", "management-policy-value");
                VisualElement scheduleRow = AddManagementRow(card);
                AddSchedulePreset(
                    scheduleRow,
                    target,
                    "Every day",
                    PortfolioOperationsRules.AllDaysMask,
                    "all");
                AddSchedulePreset(
                    scheduleRow,
                    target,
                    "Days 1–5",
                    0x1f,
                    "weekdays");
                AddSchedulePreset(
                    scheduleRow,
                    target,
                    "Days 6–7",
                    0x60,
                    "weekend");

                AddManagementCopy(card, "Same-day shift", "management-policy-value");
                VisualElement shiftRow = AddManagementRow(card);
                AddShiftPreset(shiftRow, target, 8 * 60, 16 * 60, "08:00–16:00", "day");
                AddShiftPreset(shiftRow, target, 12 * 60, 20 * 60, "12:00–20:00", "swing");
                AddShiftPreset(shiftRow, target, 16 * 60, 24 * 60, "16:00–24:00", "late");

                VisualElement actionRow = AddManagementRow(card);
                PortfolioTaskFocus nextFocus = (PortfolioTaskFocus)(
                    ((int)target.taskFocus + 1) %
                    Enum.GetValues(typeof(PortfolioTaskFocus)).Length);
                AddManagementButton(
                    actionRow,
                    $"Focus: {target.taskFocus}",
                    $"management-focus-{target.employeeId}",
                    () => portfolio.TrySetTaskFocus(
                        target.employeeId,
                        nextFocus,
                        out _));
                AddManagementButton(
                    actionRow,
                    $"Train · {FormatCents(PortfolioProgressionRules.TrainingCostCents)}",
                    $"management-train-{target.employeeId}",
                    () => portfolio.TryTrainEmployee(target.employeeId, out _));
                foreach (PortfolioLocationSnapshot other in snapshot.locations
                             .Where(value => !string.Equals(
                                 value.locationId,
                                 target.assignedLocationId,
                                 StringComparison.Ordinal)))
                {
                    PortfolioLocationSnapshot destination = other;
                    AddManagementButton(
                        actionRow,
                        $"Assign to {destination.displayName}",
                        $"management-assign-{target.employeeId}-{destination.locationId}",
                        () => portfolio.TryReassignEmployee(
                            target.employeeId,
                            destination.locationId,
                            out _));
                }
            }

            PortfolioCandidateDefinition[] candidates =
                PortfolioProgressionRules.Candidates
                    .Where(candidate => snapshot.employees.All(employee =>
                        !string.Equals(
                            employee.employeeId,
                            candidate.EmployeeId,
                            StringComparison.Ordinal)))
                    .ToArray();
            if (candidates.Length == 0)
            {
                return;
            }

            AddManagementSection("AVAILABLE PEOPLE");
            foreach (PortfolioCandidateDefinition candidate in candidates)
            {
                PortfolioCandidateDefinition target = candidate;
                VisualElement card = AddManagementCard(
                    $"{target.DisplayName} · {FriendlyRole(target.Role)}",
                    $"{target.Trait}   •   Skill {target.Skill}   •   Reliability {target.Reliability}   •   " +
                    $"{FormatCents(target.DailyWageCents)}/day");
                AddManagementButton(
                    AddManagementRow(card),
                    $"Hire here · {FormatCents(target.HiringCostCents)}",
                    $"management-hire-{target.EmployeeId}",
                    () => portfolio.TryHireCandidate(
                        target.EmployeeId,
                        selected.locationId,
                        out _),
                    primary: true);
            }
        }

        private void AddSchedulePreset(
            VisualElement row,
            PortfolioEmployeeSnapshot employee,
            string label,
            int mask,
            string suffix)
        {
            AddManagementButton(
                row,
                label,
                $"management-schedule-{employee.employeeId}-{suffix}",
                () =>
                {
                    PortfolioEmployeeScheduleSnapshot schedule =
                        PortfolioOperationsRules.Clone(employee.schedule);
                    schedule.scheduledDayMask = mask;
                    portfolio.TrySetEmployeeSchedule(
                        employee.employeeId,
                        schedule,
                        out _);
                },
                employee.schedule.scheduledDayMask == mask);
        }

        private void AddShiftPreset(
            VisualElement row,
            PortfolioEmployeeSnapshot employee,
            int startMinute,
            int endMinute,
            string label,
            string suffix)
        {
            AddManagementButton(
                row,
                label,
                $"management-shift-{employee.employeeId}-{suffix}",
                () =>
                {
                    PortfolioEmployeeScheduleSnapshot schedule =
                        PortfolioOperationsRules.Clone(employee.schedule);
                    schedule.shiftStartMinute = startMinute;
                    schedule.shiftEndMinute = endMinute;
                    portfolio.TrySetEmployeeSchedule(
                        employee.employeeId,
                        schedule,
                        out _);
                },
                employee.schedule.shiftStartMinute == startMinute &&
                employee.schedule.shiftEndMinute == endMinute);
        }

        private void BuildManagementPolicies(PortfolioLocationSnapshot location)
        {
            AddManagementSection("PRICING & PROCUREMENT");
            VisualElement commerce = AddManagementCard(
                location.displayName,
                "Policies remain provisional and feed the same detailed and aggregate business authorities.");
            AddManagementCopy(commerce, "Pricing", "management-policy-value");
            VisualElement pricing = AddManagementRow(commerce);
            foreach (PortfolioPricingPolicy value in
                     Enum.GetValues(typeof(PortfolioPricingPolicy)))
            {
                PortfolioPricingPolicy policy = value;
                AddManagementButton(
                    pricing,
                    policy.ToString(),
                    $"management-pricing-{policy.ToString().ToLowerInvariant()}",
                    () => portfolio.TrySetPricingPreset(
                        location.locationId,
                        policy,
                        out _),
                    location.pricingPolicy == policy);
            }
            AddManagementCopy(commerce, "Reorder target", "management-policy-value");
            VisualElement reorder = AddManagementRow(commerce);
            foreach (PortfolioReorderPolicy value in
                     Enum.GetValues(typeof(PortfolioReorderPolicy)))
            {
                PortfolioReorderPolicy policy = value;
                AddManagementButton(
                    reorder,
                    policy.ToString(),
                    $"management-reorder-{policy.ToString().ToLowerInvariant()}",
                    () => portfolio.TrySetReorderPolicy(
                        location.locationId,
                        policy,
                        out _),
                    location.reorderPolicy == policy);
            }
            AddManagementButton(
                AddManagementRow(commerce),
                "Place owner reorder",
                "management-owner-reorder",
                () => portfolio.TryPlaceManualPurchaseOrder(
                    location.locationId,
                    out _));

            PortfolioDelegationPolicySnapshot policyState =
                location.delegationPolicy;
            AddManagementSection("MANAGER AUTHORITY & BUDGET");
            VisualElement authority = AddManagementCard(
                "Delegated authority",
                "Choose what the manager may resolve without owner intervention. Alerts remain visible when authority or budget blocks action.");
            VisualElement authorityRow = AddManagementRow(authority);
            AddPolicyToggle(
                authorityRow,
                location,
                "Purchasing",
                "purchase-authority",
                policyState.managerCanPurchase,
                policy => policy.managerCanPurchase =
                    !policy.managerCanPurchase);
            AddPolicyToggle(
                authorityRow,
                location,
                "Maintenance",
                "maintenance-authority",
                policyState.managerCanAuthorizeMaintenance,
                policy => policy.managerCanAuthorizeMaintenance =
                    !policy.managerCanAuthorizeMaintenance);
            AddPolicyToggle(
                authorityRow,
                location,
                "Price adjustments",
                "pricing-authority",
                policyState.managerCanAdjustPrices,
                policy => policy.managerCanAdjustPrices =
                    !policy.managerCanAdjustPrices);

            AddManagementCopy(
                authority,
                $"Daily delegated spending limit: {FormatCents(policyState.dailySpendingLimitCents)}",
                "management-policy-value");
            VisualElement budget = AddManagementRow(authority);
            AddPolicyChange(
                budget,
                location,
                $"− {FormatCents(PolicyBudgetStepCents)}",
                "budget-decrease",
                policy => policy.dailySpendingLimitCents = Math.Max(
                    0,
                    policy.dailySpendingLimitCents - PolicyBudgetStepCents),
                enabled: policyState.dailySpendingLimitCents > 0);
            AddPolicyChange(
                budget,
                location,
                $"+ {FormatCents(PolicyBudgetStepCents)}",
                "budget-increase",
                policy => policy.dailySpendingLimitCents =
                    policy.dailySpendingLimitCents <=
                    long.MaxValue - PolicyBudgetStepCents
                        ? policy.dailySpendingLimitCents +
                          PolicyBudgetStepCents
                        : long.MaxValue,
                enabled: policyState.dailySpendingLimitCents < long.MaxValue);

            AddManagementSection("OPERATING STANDARDS");
            VisualElement standards = AddManagementCard(
                "Owner standards",
                "Standards create understandable exceptions; they do not fabricate final balance targets.");
            AddStandardControl(
                standards,
                location,
                "Service quality",
                policyState.minimumServiceQuality,
                5,
                0,
                100,
                "service",
                (policy, value) => policy.minimumServiceQuality = value,
                value => $"{value}/100");
            AddStandardControl(
                standards,
                location,
                "Product availability",
                policyState.minimumProductAvailabilityBasisPoints,
                500,
                0,
                PortfolioOperationsRules.BasisPoints,
                "availability",
                (policy, value) =>
                    policy.minimumProductAvailabilityBasisPoints = value,
                value => $"{value / 100f:0.#}%");
            AddStandardControl(
                standards,
                location,
                "Maintenance condition",
                policyState.minimumMaintenanceCondition,
                5,
                0,
                100,
                "maintenance",
                (policy, value) => policy.minimumMaintenanceCondition = value,
                value => $"{value}/100");

            AddManagementCopy(standards, "Maintenance policy", "management-policy-value");
            VisualElement maintenance = AddManagementRow(standards);
            foreach (PortfolioMaintenancePolicy value in
                     Enum.GetValues(typeof(PortfolioMaintenancePolicy)))
            {
                PortfolioMaintenancePolicy maintenancePolicy = value;
                AddPolicyChange(
                    maintenance,
                    location,
                    maintenancePolicy.ToString(),
                    $"maintenance-{maintenancePolicy.ToString().ToLowerInvariant()}",
                    policy => policy.maintenancePolicy = maintenancePolicy,
                    selected: policyState.maintenancePolicy == maintenancePolicy);
            }
        }

        private void AddPolicyToggle(
            VisualElement row,
            PortfolioLocationSnapshot location,
            string label,
            string suffix,
            bool enabledValue,
            Action<PortfolioDelegationPolicySnapshot> change)
        {
            AddPolicyChange(
                row,
                location,
                $"{label}: {(enabledValue ? "Allowed" : "Owner only")}",
                suffix,
                change,
                selected: enabledValue);
        }

        private void AddPolicyChange(
            VisualElement row,
            PortfolioLocationSnapshot location,
            string label,
            string suffix,
            Action<PortfolioDelegationPolicySnapshot> change,
            bool selected = false,
            bool enabled = true)
        {
            AddManagementButton(
                row,
                label,
                $"management-{suffix}",
                () =>
                {
                    PortfolioDelegationPolicySnapshot candidate =
                        PortfolioOperationsRules.Clone(
                            location.delegationPolicy);
                    change(candidate);
                    portfolio.TrySetDelegationPolicy(
                        location.locationId,
                        candidate,
                        out _);
                },
                selected,
                enabled: enabled);
        }

        private void AddStandardControl(
            VisualElement card,
            PortfolioLocationSnapshot location,
            string label,
            int current,
            int step,
            int minimum,
            int maximum,
            string suffix,
            Action<PortfolioDelegationPolicySnapshot, int> assign,
            Func<int, string> format)
        {
            AddManagementCopy(
                card,
                $"{label}: {format(current)}",
                "management-policy-value");
            VisualElement row = AddManagementRow(card);
            AddPolicyChange(
                row,
                location,
                $"− {format(step)}",
                $"{suffix}-decrease",
                policy => assign(
                    policy,
                    Math.Max(minimum, current - step)),
                enabled: current > minimum);
            AddPolicyChange(
                row,
                location,
                $"+ {format(step)}",
                $"{suffix}-increase",
                policy => assign(
                    policy,
                    Math.Min(maximum, current + step)),
                enabled: current < maximum);
        }

        private void BuildManagementAlerts(PortfolioLocationSnapshot location)
        {
            PortfolioOperatingAlertSnapshot[] open = location.operatingAlerts
                .Where(alert => alert.IsOpen)
                .OrderByDescending(alert => alert.severity)
                .ThenBy(alert => alert.alertId, StringComparer.Ordinal)
                .ToArray();
            AddManagementSection($"OPEN EXCEPTIONS · {open.Length}");
            if (open.Length == 0)
            {
                AddManagementCopy(
                    managementContent,
                    "No open operating exceptions. Resolved history remains below.",
                    "management-empty");
            }
            foreach (PortfolioOperatingAlertSnapshot alert in open)
            {
                PortfolioOperatingAlertSnapshot target = alert;
                VisualElement card = AddManagementCard(
                    $"{target.severity} · {target.problemTypeId}",
                    target.summary,
                    target.severity == PortfolioAlertSeverity.Critical
                        ? "management-card--critical"
                        : "management-card--warning");
                AddManagementCopy(
                    card,
                    $"Recovery: {target.recoveryAction}\n" +
                    $"Opened day {target.openedDay}   •   " +
                    (target.requiresOwnerAttention
                        ? "Owner attention required"
                        : "Manager-visible exception"));
                VisualElement actions = AddManagementRow(card);
                if (!target.acknowledged)
                {
                    AddManagementButton(
                        actions,
                        "Acknowledge",
                        $"management-ack-{target.alertId}",
                        () => portfolio.TryAcknowledgeOperatingAlert(
                            location.locationId,
                            target.alertId,
                            out _));
                }
                AddAlertRecoveryAction(actions, location, target);
            }

            if (location.maintenanceCondition < 100)
            {
                AddManagementSection("DIRECT RECOVERY");
                VisualElement maintenance = AddManagementCard(
                    "Emergency maintenance",
                    $"Current condition {location.maintenanceCondition}/100 and failure pressure {location.failurePressure}/100. " +
                    "This action uses the existing provisional emergency cost and recovery profile.",
                    "management-card--warning");
                AddManagementButton(
                    AddManagementRow(maintenance),
                    "Perform emergency maintenance",
                    "management-emergency-maintenance",
                    () => portfolio.TryPerformEmergencyMaintenance(
                        location.locationId,
                        out _),
                    primary: true);
            }

            PortfolioOperatingAlertSnapshot[] resolved = location.operatingAlerts
                .Where(alert => !alert.IsOpen)
                .OrderByDescending(alert => alert.resolvedDay)
                .ThenBy(alert => alert.alertId, StringComparer.Ordinal)
                .ToArray();
            if (resolved.Length == 0)
            {
                return;
            }
            AddManagementSection($"RESOLVED HISTORY · {resolved.Length}");
            foreach (PortfolioOperatingAlertSnapshot alert in resolved)
            {
                AddManagementCard(
                    $"Resolved day {alert.resolvedDay} · {alert.problemTypeId}",
                    alert.summary);
            }
        }

        private void AddAlertRecoveryAction(
            VisualElement row,
            PortfolioLocationSnapshot location,
            PortfolioOperatingAlertSnapshot alert)
        {
            switch (alert.problemTypeId)
            {
                case "maintenance-pressure":
                    AddManagementButton(
                        row,
                        "Emergency maintenance",
                        $"management-recover-{alert.alertId}",
                        () => portfolio.TryPerformEmergencyMaintenance(
                            location.locationId,
                            out _),
                        danger: true);
                    break;
                case "product-availability":
                case "purchasing-authority":
                    AddManagementButton(
                        row,
                        "Place owner reorder",
                        $"management-recover-{alert.alertId}",
                        () => portfolio.TryPlaceManualPurchaseOrder(
                            location.locationId,
                            out _));
                    break;
                case "service-standard":
                    AddManagementButton(
                        row,
                        "Open team controls",
                        $"management-recover-{alert.alertId}",
                        () => SelectManagementPage(ManagementPage.Team));
                    break;
                default:
                    AddManagementButton(
                        row,
                        "Open policy controls",
                        $"management-recover-{alert.alertId}",
                        () => SelectManagementPage(ManagementPage.Policies));
                    break;
            }
        }

        private void ClearManagementContent()
        {
            foreach (Button button in dynamicManagementButtons)
            {
                activations.Remove(button);
            }
            dynamicManagementButtons.Clear();
            managementContent?.Clear();
        }

        private void AddManagementSection(string text)
        {
            Label label = new(text);
            label.AddToClassList("management-section-title");
            managementContent.Add(label);
        }

        private VisualElement AddManagementCard(
            string title,
            string copy,
            string modifier = null)
        {
            VisualElement card = new();
            card.AddToClassList("management-card");
            if (!string.IsNullOrWhiteSpace(modifier))
            {
                card.AddToClassList(modifier);
            }
            Label heading = new(title);
            heading.AddToClassList("management-card-title");
            card.Add(heading);
            AddManagementCopy(card, copy);
            managementContent.Add(card);
            return card;
        }

        private static void AddManagementCopy(
            VisualElement parent,
            string text,
            string className = "management-card-copy")
        {
            Label label = new(text);
            label.AddToClassList(className);
            parent.Add(label);
        }

        private static VisualElement AddManagementRow(VisualElement parent)
        {
            VisualElement row = new();
            row.AddToClassList("management-row");
            parent.Add(row);
            return row;
        }

        private Button AddManagementButton(
            VisualElement parent,
            string text,
            string name,
            Action action,
            bool selected = false,
            bool primary = false,
            bool danger = false,
            bool enabled = true)
        {
            Button button = new()
            {
                name = name,
                text = text
            };
            button.AddToClassList("management-action");
            button.EnableInClassList("management-action--selected", selected);
            button.EnableInClassList("management-action--primary", primary);
            button.EnableInClassList("management-action--danger", danger);
            button.SetEnabled(enabled);
            RegisterButton(button, action);
            dynamicManagementButtons.Add(button);
            parent.Add(button);
            return button;
        }

        private static string LocationDisplayName(
            PortfolioProgressionSnapshot snapshot,
            string locationId)
        {
            return snapshot.locations.FirstOrDefault(location => string.Equals(
                       location.locationId,
                       locationId,
                       StringComparison.Ordinal))?.displayName ?? locationId;
        }

        private static string FormatSchedule(
            PortfolioEmployeeScheduleSnapshot schedule)
        {
            if (schedule == null)
            {
                return "Unavailable";
            }
            string days = schedule.scheduledDayMask switch
            {
                PortfolioOperationsRules.AllDaysMask => "Every day",
                0x1f => "Days 1–5",
                0x60 => "Days 6–7",
                _ => $"Custom mask {schedule.scheduledDayMask}"
            };
            return $"{days}, {FormatMinute(schedule.shiftStartMinute)}–" +
                   FormatMinute(schedule.shiftEndMinute);
        }

        private static string FormatMinute(int minute)
        {
            if (minute == PortfolioOperationsRules.MinutesPerDay)
            {
                return "24:00";
            }
            return $"{minute / 60:00}:{minute % 60:00}";
        }

        private static string FormatCents(long cents)
        {
            long absolute = cents == long.MinValue
                ? long.MaxValue
                : Math.Abs(cents);
            string value = $"${absolute / 100:N0}.{absolute % 100:00}";
            return cents < 0 ? $"-{value}" : value;
        }

        private static string FriendlyRole(PortfolioEmployeeRole role)
        {
            return role switch
            {
                PortfolioEmployeeRole.StockClerk => "Stock clerk",
                PortfolioEmployeeRole.Manager => "Manager",
                _ => "Cashier"
            };
        }

        private NotificationElements Notification(
            string container,
            string message,
            string dismiss)
        {
            return new NotificationElements(
                Require<VisualElement>(container),
                Require<Label>(message),
                Require<Button>(dismiss));
        }

        private T Require<T>(string name) where T : VisualElement
        {
            return root?.Q<T>(name);
        }

        private bool HasMissingRequiredElement()
        {
            return root == null || titleView == null || pauseView == null ||
                   settingsView == null || managementView == null ||
                   generalContent == null ||
                   controlsContent == null || titleNewBusiness == null ||
                   titleLoadBusiness == null || generalTab == null ||
                   controlsTab == null || fullscreenToggle == null ||
                   interfaceScaleSlider == null || interfaceScaleValue == null ||
                   masterVolumeSlider == null || masterVolumeValue == null ||
                   cameraMotionToggle == null || sensitivitySlider == null ||
                   sensitivityValue == null || invertYToggle == null ||
                   bindingList == null || resetBindings == null ||
                   settingsBack == null || settingsApply == null ||
                   managementSummary == null || managementStatus == null ||
                   managementContent == null ||
                   managementOverviewTab == null ||
                   managementTeamTab == null ||
                   managementPolicyTab == null ||
                   managementAlertsTab == null || managementSave == null ||
                   managementLoad == null || managementResume == null ||
                   !titleNotification.IsValid || !pauseNotification.IsValid ||
                   !settingsNotification.IsValid;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private static void Consume(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();
        }

        private readonly struct NotificationElements
        {
            public NotificationElements(
                VisualElement container,
                Label message,
                Button dismiss)
            {
                Container = container;
                Message = message;
                Dismiss = dismiss;
            }

            public VisualElement Container { get; }
            public Label Message { get; }
            public Button Dismiss { get; }
            public bool IsValid =>
                Container != null && Message != null && Dismiss != null;
        }
    }
}
