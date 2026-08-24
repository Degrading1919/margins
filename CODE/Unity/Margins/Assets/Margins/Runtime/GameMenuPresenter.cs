using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Margins
{
    internal static class PortfolioPlayerLabels
    {
        public static string TaskFocus(PortfolioTaskFocus focus)
        {
            return focus switch
            {
                PortfolioTaskFocus.Service => "Helping customers",
                PortfolioTaskFocus.Inventory => "Stocking shelves",
                PortfolioTaskFocus.Standards => "Cleaning and upkeep",
                _ => "Balanced"
            };
        }

        public static string PricingPolicy(PortfolioPricingPolicy policy)
        {
            return policy switch
            {
                PortfolioPricingPolicy.Value => "Lower prices",
                PortfolioPricingPolicy.Premium => "Higher margins",
                _ => "Balanced"
            };
        }

        public static string ReorderPolicy(PortfolioReorderPolicy policy)
        {
            return policy switch
            {
                PortfolioReorderPolicy.Lean => "Keep less back stock",
                PortfolioReorderPolicy.Resilient => "Keep extra back stock",
                _ => "Balanced"
            };
        }

        public static string MaintenancePolicy(
            PortfolioMaintenancePolicy policy)
        {
            return policy switch
            {
                PortfolioMaintenancePolicy.Deferred => "Repair when needed",
                PortfolioMaintenancePolicy.Preventive => "Prevent problems",
                _ => "Routine upkeep"
            };
        }
    }

    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenuPresenter : MonoBehaviour
    {
        private enum ManagementPage
        {
            Overview,
            Team,
            Policies,
            Alerts,
            Locations,
            Reports
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
        private readonly List<Button> staticSetupButtons = new();
        private readonly List<Button> staticPauseButtons = new();
        private readonly List<Button> managementTabButtons = new();
        private readonly List<Button> managementFooterButtons = new();
        private readonly List<Button> dynamicManagementButtons = new();
        private readonly HashSet<string> expandedStaff = new(StringComparer.Ordinal);
        private readonly HashSet<string> expandedReports = new(StringComparer.Ordinal);

        private VisualElement root;
        private VisualElement titleView;
        private VisualElement newBusinessSetupView;
        private VisualElement pauseView;
        private VisualElement settingsView;
        private VisualElement managementView;
        private VisualElement generalContent;
        private VisualElement controlsContent;
        private Button titleNewBusiness;
        private Button titleLoadBusiness;
        private TextField setupBusinessName;
        private DropdownField setupLogo;
        private IntegerField setupSeed;
        private TextField setupPrimaryColor;
        private TextField setupSecondaryColor;
        private DropdownField setupDifficulty;
        private Button setupBack;
        private Button setupStart;
        private Button generalTab;
        private Button controlsTab;
        private Toggle fullscreenToggle;
        private SliderInt interfaceScaleSlider;
        private Label interfaceScaleValue;
        private SliderInt masterVolumeSlider;
        private Label masterVolumeValue;
        private Toggle cameraMotionToggle;
        private Button sensitivityDecrease;
        private Button sensitivityIncrease;
        private Label sensitivityValue;
        private Toggle invertYToggle;
        private ScrollView bindingList;
        private Button resetBindings;
        private Button settingsBack;
        private Button settingsApply;
        private Label managementSummary;
        private Label managementStatus;
        private Label managementPageTitle;
        private Label managementPageHelp;
        private ScrollView managementContent;
        private Button managementOverviewTab;
        private Button managementTeamTab;
        private Button managementPolicyTab;
        private Button managementAlertsTab;
        private Button managementLocationsTab;
        private Button managementReportsTab;
        private Button managementResume;
        private NotificationElements titleNotification;
        private NotificationElements setupNotification;
        private NotificationElements pauseNotification;
        private NotificationElements settingsNotification;
        private bool initialized;
        private bool managementWasVisible;
        private bool operatingStandardsExpanded;
        private bool resolvedAlertsExpanded;
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
            newBusinessSetupView =
                Require<VisualElement>("new-business-setup-view");
            pauseView = Require<VisualElement>("pause-view");
            settingsView = Require<VisualElement>("settings-view");
            managementView = Require<VisualElement>("management-view");
            generalContent = Require<VisualElement>("settings-general-content");
            controlsContent = Require<VisualElement>("settings-controls-content");
            titleNewBusiness = Require<Button>("title-new-business");
            titleLoadBusiness = Require<Button>("title-load-business");
            setupBusinessName = Require<TextField>("setup-business-name");
            setupLogo = Require<DropdownField>("setup-logo");
            setupSeed = Require<IntegerField>("setup-seed");
            setupPrimaryColor = Require<TextField>("setup-primary-color");
            setupSecondaryColor = Require<TextField>("setup-secondary-color");
            setupDifficulty = Require<DropdownField>("setup-difficulty");
            setupBack = Require<Button>("setup-back");
            setupStart = Require<Button>("setup-start");
            generalTab = Require<Button>("settings-general-tab");
            controlsTab = Require<Button>("settings-controls-tab");
            fullscreenToggle = Require<Toggle>("settings-fullscreen");
            interfaceScaleSlider = Require<SliderInt>("settings-interface-scale");
            interfaceScaleValue = Require<Label>("settings-interface-scale-value");
            masterVolumeSlider = Require<SliderInt>("settings-master-volume");
            masterVolumeValue = Require<Label>("settings-master-volume-value");
            cameraMotionToggle = Require<Toggle>("settings-camera-motion");
            sensitivityDecrease =
                Require<Button>("settings-look-sensitivity-decrease");
            sensitivityIncrease =
                Require<Button>("settings-look-sensitivity-increase");
            sensitivityValue = Require<Label>("settings-look-sensitivity-value");
            invertYToggle = Require<Toggle>("settings-invert-y");
            bindingList = Require<ScrollView>("settings-binding-list");
            resetBindings = Require<Button>("settings-reset-bindings");
            settingsBack = Require<Button>("settings-back");
            settingsApply = Require<Button>("settings-apply");
            managementSummary = Require<Label>("management-summary");
            managementStatus = Require<Label>("management-status");
            managementPageTitle = Require<Label>("management-page-title");
            managementPageHelp = Require<Label>("management-page-help");
            managementContent = Require<ScrollView>("management-content");
            managementOverviewTab = Require<Button>("management-overview-tab");
            managementTeamTab = Require<Button>("management-team-tab");
            managementPolicyTab = Require<Button>("management-policy-tab");
            managementAlertsTab = Require<Button>("management-alerts-tab");
            managementLocationsTab = Require<Button>("management-locations-tab");
            managementReportsTab = Require<Button>("management-reports-tab");
            managementResume = Require<Button>("management-resume");

            titleNotification = Notification(
                "title-notification",
                "title-notification-message",
                "title-notification-dismiss");
            setupNotification = Notification(
                "setup-notification",
                "setup-notification-message",
                "setup-notification-dismiss");
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

            RegisterButton(setupBack, controller.CancelNewBusinessSetup);
            RegisterButton(setupStart, controller.StartConfiguredNewBusiness);

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
            RegisterButton(setupNotification.Dismiss, controller.DismissNotification);
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
            RegisterButton(
                managementLocationsTab,
                () => SelectManagementPage(ManagementPage.Locations));
            RegisterButton(
                managementReportsTab,
                () => SelectManagementPage(ManagementPage.Reports));
            RegisterButton(managementResume, ResumeManagement);

            staticTitleButtons.Add(titleNewBusiness);
            staticTitleButtons.Add(titleLoadBusiness);
            staticTitleButtons.Add(Require<Button>("title-settings"));
            staticTitleButtons.Add(Require<Button>("title-quit"));

            staticSetupButtons.Add(setupBack);
            staticSetupButtons.Add(setupStart);

            staticPauseButtons.Add(Require<Button>("pause-resume"));
            staticPauseButtons.Add(Require<Button>("pause-save"));
            staticPauseButtons.Add(Require<Button>("pause-load"));
            staticPauseButtons.Add(Require<Button>("pause-settings"));
            staticPauseButtons.Add(Require<Button>("pause-title"));
            staticPauseButtons.Add(Require<Button>("pause-quit"));

            managementTabButtons.Add(managementOverviewTab);
            managementTabButtons.Add(managementLocationsTab);
            managementTabButtons.Add(managementTeamTab);
            managementTabButtons.Add(managementPolicyTab);
            managementTabButtons.Add(managementReportsTab);
            managementTabButtons.Add(managementAlertsTab);
            managementFooterButtons.Add(managementResume);
        }

        private void RegisterSettingChanges()
        {
            setupBusinessName.maxLength = 32;
            setupLogo.choices = new List<string>
            {
                "Placeholder mark A",
                "Placeholder mark B",
                "Placeholder mark C"
            };
            setupDifficulty.choices = new List<string>
            {
                "Purpose: forgiving growth-focused play",
                "Purpose: intended challenging-but-recoverable play",
                "Purpose: harsher simulation",
                "Purpose: sandbox experimentation & construction"
            };
            setupBusinessName.RegisterValueChangedCallback(
                evt => controller.SetNewBusinessName(evt.newValue));
            setupPrimaryColor.RegisterValueChangedCallback(
                evt => controller.SetNewBusinessPrimaryColor(evt.newValue));
            setupSecondaryColor.RegisterValueChangedCallback(
                evt => controller.SetNewBusinessSecondaryColor(evt.newValue));
            setupSeed.RegisterValueChangedCallback(
                evt => controller.SetNewBusinessSeed(evt.newValue));
            setupLogo.RegisterValueChangedCallback(evt =>
            {
                int index = setupLogo.choices.IndexOf(evt.newValue);
                if (index >= 0 &&
                    index < PortfolioStartupProfileRules.LogoSelectionIds.Length)
                {
                    controller.SetNewBusinessLogoSelection(
                        PortfolioStartupProfileRules.LogoSelectionIds[index]);
                }
            });
            setupDifficulty.RegisterValueChangedCallback(evt =>
            {
                int index = setupDifficulty.choices.IndexOf(evt.newValue);
                if (index >= 0 && index <
                    PortfolioStartupProfileRules.DifficultyPurposeIds.Length)
                {
                    controller.SetNewBusinessDifficultyPurpose(
                        PortfolioStartupProfileRules.DifficultyPurposeIds[index]);
                }
            });
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
            RegisterButton(sensitivityDecrease, () =>
                AdjustSensitivity(-1));
            RegisterButton(sensitivityIncrease, () =>
                AdjustSensitivity(1));
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
            bool setup = !management &&
                         controller.Screen == GameMenuScreen.NewBusinessSetup;
            bool pause = !management &&
                         controller.Screen == GameMenuScreen.Pause;
            bool settings = !management && controller.IsSettingsVisible;
            bool controls = controller.Screen == GameMenuScreen.SettingsControls;
            SetVisible(titleView, title);
            SetVisible(newBusinessSetupView, setup);
            SetVisible(pauseView, pause);
            SetVisible(settingsView, settings);
            SetVisible(managementView, management);
            SetVisible(generalContent, settings && !controls);
            SetVisible(controlsContent, settings && controls);

            if (management)
            {
                RefreshManagement();
                RebuildFocusables(false, false, false, false, false, true);
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

            if (setup)
            {
                RefreshNewBusinessSetup();
            }

            if (settings)
            {
                RefreshSettings(controls);
            }
            RefreshNotifications(title, setup, pause, settings);
            RebuildFocusables(title, setup, pause, settings, controls, false);
            EnsureUsefulFocus();
        }

        private void RefreshNewBusinessSetup()
        {
            NewBusinessSetupData setup = controller.NewBusinessSetup;
            if (setup == null)
            {
                return;
            }

            setupBusinessName.SetValueWithoutNotify(setup.businessName);
            setupPrimaryColor.SetValueWithoutNotify(setup.primaryColorHex);
            setupSecondaryColor.SetValueWithoutNotify(setup.secondaryColorHex);
            setupSeed.SetValueWithoutNotify(setup.seed);
            int logoIndex = Array.IndexOf(
                PortfolioStartupProfileRules.LogoSelectionIds,
                setup.logoSelectionId);
            setupLogo.index = Mathf.Max(0, logoIndex);
            int difficultyIndex = Array.IndexOf(
                PortfolioStartupProfileRules.DifficultyPurposeIds,
                setup.difficultyPurposeId);
            setupDifficulty.index = Mathf.Max(0, difficultyIndex);
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

        private void RefreshNotifications(
            bool title,
            bool setup,
            bool pause,
            bool settings)
        {
            RefreshNotification(titleNotification, title);
            RefreshNotification(setupNotification, setup);
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
            bool setup,
            bool pause,
            bool settings,
            bool controls,
            bool management)
        {
            focusables.Clear();
            if (management)
            {
                AddEnabled(managementTabButtons);
                AddEnabled(dynamicManagementButtons);
                AddEnabled(managementFooterButtons);
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
            if (setup)
            {
                focusables.Add(setupBusinessName);
                focusables.Add(setupLogo);
                focusables.Add(setupSeed);
                focusables.Add(setupPrimaryColor);
                focusables.Add(setupSecondaryColor);
                focusables.Add(setupDifficulty);
                AddEnabled(staticSetupButtons);
                if (controller.Notification.IsPersistent)
                {
                    focusables.Add(setupNotification.Dismiss);
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
                focusables.Add(sensitivityDecrease);
                focusables.Add(sensitivityIncrease);
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
                int step = 5;
                slider.value = Mathf.Clamp(
                    slider.value + direction * step,
                    slider.lowValue,
                    slider.highValue);
                return true;
            }
            if (focused == sensitivityDecrease ||
                focused == sensitivityIncrease)
            {
                AdjustSensitivity(direction);
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
            if (focused is Button managementTab)
            {
                int tabIndex = managementTabButtons.IndexOf(managementTab);
                if (tabIndex >= 0)
                {
                    int nextIndex = (tabIndex + direction +
                                     managementTabButtons.Count) %
                                    managementTabButtons.Count;
                    Button next = managementTabButtons[nextIndex];
                    next.Focus();
                    if (activations.TryGetValue(next, out Action action))
                    {
                        action();
                    }
                    return true;
                }
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

        private void AdjustSensitivity(int direction)
        {
            int current = controller?.DraftSettings?.LookSensitivityLevel ?? 5;
            controller?.SetLookSensitivityLevel(current + Math.Sign(direction));
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
            int openAlertCount = snapshot.locations.Sum(location =>
                location.operatingAlerts.Count(alert => alert.IsOpen));
            managementSummary.text =
                $"Day {snapshot.currentDay}   •   {FormatCents(snapshot.cashCents)} cash   •   " +
                $"{snapshot.locations.Count} store" +
                (snapshot.locations.Count == 1 ? string.Empty : "s") +
                (openAlertCount == 0
                    ? string.Empty
                    : $"   •   {openAlertCount} need attention");
            managementAlertsTab.text = openAlertCount == 0
                ? "Alerts"
                : $"Alerts ({openAlertCount})";

            bool menuError = controller.Notification.IsVisible &&
                             controller.Notification.Kind ==
                             MenuNotificationKind.Error;
            managementStatus.text = PlayerFacingText(
                controller.Notification.IsVisible
                ? controller.Notification.Message
                : portfolio.LastAction);
            managementStatus.EnableInClassList(
                "management-status--error",
                menuError ||
                (!controller.Notification.IsVisible &&
                 !portfolio.LastActionSucceeded));
            string activeLocationId = snapshot.company.activeDetailedLocationId;
            managementResume.SetEnabled(snapshot.locations.Count > 0);
            PortfolioLocationSnapshot activeLocation = snapshot.locations
                .FirstOrDefault(location => string.Equals(
                    location.locationId,
                    activeLocationId,
                    StringComparison.Ordinal));
            managementResume.text = activeLocation == null
                ? $"Go to {snapshot.locations[0].displayName}"
                : $"Back to {activeLocation.displayName}";

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
            managementLocationsTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Locations);
            managementReportsTab.EnableInClassList(
                "management-tab--active",
                managementPage == ManagementPage.Reports);

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
            ConfigureManagementPage(selected);
            if (managementPage != ManagementPage.Locations &&
                managementPage != ManagementPage.Reports)
            {
                BuildManagementLocationSelector(snapshot, selected);
            }
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
                case ManagementPage.Locations:
                    BuildManagementLocations(snapshot);
                    break;
                case ManagementPage.Reports:
                    BuildManagementReports(snapshot);
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
            AddManagementSection("VIEWING");
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

        private void ConfigureManagementPage(
            PortfolioLocationSnapshot selected)
        {
            switch (managementPage)
            {
                case ManagementPage.Team:
                    managementPageTitle.text = "Staff";
                    managementPageHelp.text =
                        $"Set schedules and responsibilities for {selected.displayName}. Open a person for training, promotion, or transfers.";
                    break;
                case ManagementPage.Policies:
                    managementPageTitle.text = "Operations";
                    managementPageHelp.text =
                        $"Decide how {selected.displayName} prices, orders stock, and handles routine problems.";
                    break;
                case ManagementPage.Alerts:
                    managementPageTitle.text = "Alerts";
                    managementPageHelp.text =
                        $"Problems at {selected.displayName}, with the next useful action beside each one.";
                    break;
                case ManagementPage.Locations:
                    managementPageTitle.text = "Stores";
                    managementPageHelp.text =
                        "Choose where to work, manage an existing store, or lease the next available location.";
                    break;
                case ManagementPage.Reports:
                    managementPageTitle.text = "Results";
                    managementPageHelp.text =
                        "Start with the headline result. Open a store breakdown only when you need the detail.";
                    break;
                default:
                    managementPageTitle.text = "Today";
                    managementPageHelp.text =
                        "See what needs attention, choose where to work, and end the day when every store is ready.";
                    break;
            }
        }

        private void BuildManagementOverview(
            PortfolioProgressionSnapshot snapshot,
            PortfolioLocationSnapshot selected)
        {
            var openAlerts = snapshot.locations
                .SelectMany(location => location.operatingAlerts
                    .Where(alert => alert.IsOpen)
                    .Select(alert => new { Location = location, Alert = alert }))
                .OrderByDescending(value => value.Alert.severity)
                .ThenBy(value => value.Location.displayName, StringComparer.Ordinal)
                .ThenBy(value => value.Alert.alertId, StringComparer.Ordinal)
                .ToArray();
            AddManagementSection("NEEDS ATTENTION");
            if (openAlerts.Length == 0)
            {
                AddManagementCard(
                    "Nothing urgent",
                    "None of your stores has an open problem. You can keep working or review the latest results.");
            }
            else
            {
                PortfolioOperatingAlertSnapshot first = openAlerts[0].Alert;
                PortfolioLocationSnapshot alertLocation = openAlerts[0].Location;
                VisualElement attention = AddManagementCard(
                    $"{alertLocation.displayName} · {AlertTitle(first)}",
                    PlayerFacingText(first.summary),
                    first.severity == PortfolioAlertSeverity.Critical
                        ? "management-card--critical"
                        : "management-card--warning");
                AddManagementCopy(
                    attention,
                    openAlerts.Length == 1
                        ? "1 open alert across your stores."
                        : $"{openAlerts.Length} open alerts across your stores.");
                AddManagementButton(
                    AddManagementRow(attention),
                    "Review alerts",
                    "management-open-alerts",
                    () =>
                    {
                        portfolio.TrySelectManagementLocation(
                            alertLocation.locationId,
                            out _);
                        SelectManagementPage(ManagementPage.Alerts);
                    },
                    primary: true);
            }

            AddManagementSection("STORE AT A GLANCE");
            VisualElement location = AddManagementCard(
                selected.displayName,
                $"Stock {selected.inventoryUnits} of {selected.inventoryCapacityUnits} units   •   " +
                $"Service {HealthLabel(selected.serviceQuality)}   •   " +
                $"Store condition {HealthLabel(selected.maintenanceCondition)}");
            AddManagementCopy(
                location,
                $"Customer satisfaction {HealthLabel(selected.customerSatisfaction)}   •   " +
                $"Products available {selected.productAvailabilityBasisPoints / 100f:0.#}%");

            string activeId = portfolio.ActiveDetailedSimulationLocationId;
            bool detailedSimulationActive =
                portfolio.HasActiveDetailedSimulation;
            VisualElement physical = AddManagementCard(
                "Where you're working",
                !detailedSimulationActive
                    ? "You're managing remotely. Choose a store whenever you want to work there in person."
                    : $"You're currently at {LocationDisplayName(snapshot, activeId)}. The store keeps running while this phone is open.");
            if (!detailedSimulationActive)
            {
                AddManagementButton(
                    AddManagementRow(physical),
                    $"Go to {selected.displayName}",
                    "management-visit-location",
                    () => portfolio.TryVisitLocation(
                        selected.locationId,
                        out _),
                    primary: true);
            }
            else if (string.Equals(
                         activeId,
                         selected.locationId,
                         StringComparison.Ordinal))
            {
                AddManagementButton(
                    AddManagementRow(physical),
                    $"Back to {selected.displayName}",
                    "management-back-to-active-location",
                    ResumeManagement,
                    primary: true);
            }
            else
            {
                VisualElement physicalActions = AddManagementRow(physical);
                AddManagementButton(
                    physicalActions,
                    $"Go to {selected.displayName}",
                    "management-switch-location",
                    () => portfolio.TryVisitLocation(
                        selected.locationId,
                        out _),
                    primary: true);
                AddManagementButton(
                    physicalActions,
                    $"Leave {LocationDisplayName(snapshot, activeId)}",
                    "management-leave-active-location",
                    () => portfolio.TryLeaveVisitedLocation(out _),
                    danger: true);
            }

            AddManagementSection("END THE DAY");
            bool canAdvance = portfolio.CanAdvanceOvernight(
                out string blocker);
            VisualElement overnight = AddManagementCard(
                canAdvance
                    ? $"Ready to finish Day {snapshot.currentDay}"
                    : $"Day {snapshot.currentDay} is still in progress",
                canAdvance
                    ? "Finish today for every store. Staff, stock orders, rent, costs, reports, and alerts will update together."
                    : PlayerFacingText(blocker));
            if (!canAdvance)
            {
                AddNamedManagementCopy(
                    overnight,
                    "Finish or leave the active store first. Your in-store work will be saved before the day can move forward.",
                    "management-end-day-blocker",
                    "management-blocker");
            }
            VisualElement overnightActions = AddManagementRow(overnight);
            if (!canAdvance && detailedSimulationActive)
            {
                AddManagementButton(
                    overnightActions,
                    $"Leave {LocationDisplayName(snapshot, activeId)}",
                    "management-leave-location",
                    () => portfolio.TryLeaveVisitedLocation(out _));
            }
            AddManagementButton(
                overnightActions,
                $"End Day {snapshot.currentDay}",
                "management-advance-overnight",
                () => portfolio.TryAdvanceOvernight(out _),
                primary: true,
                enabled: canAdvance);

            AddManagementSection("LATEST RESULT");
            if (selected.hasLastReport && selected.lastReport != null)
            {
                PortfolioLocationReportSnapshot report = selected.lastReport;
                VisualElement reportCard = AddManagementCard(
                    $"Day {report.day} · {ReportModeLabel(report)}",
                    report.primaryCause);
                AddManagementCopy(
                    reportCard,
                    $"Sales {FormatCents(report.grossSalesCents)}   •   " +
                    $"Profit from operations {FormatCents(report.operatingProfitCents)}");
                AddManagementButton(
                    AddManagementRow(reportCard),
                    "See full report",
                    "management-open-reports",
                    () => SelectManagementPage(ManagementPage.Reports));
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
            AddManagementSection($"STAFF AT {selected.displayName.ToUpperInvariant()}");
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
                    "No one works here yet. Hire staff below before asking this store to run without you.",
                    "management-empty");
            }

            foreach (PortfolioEmployeeSnapshot employee in assigned)
            {
                PortfolioEmployeeSnapshot target = employee;
                VisualElement card = AddManagementCard(
                    $"{target.displayName} · {FriendlyRole(target.role)}",
                    $"Works {FormatScheduledDays(target.schedule)}   •   " +
                    $"Focus: {PortfolioPlayerLabels.TaskFocus(target.taskFocus)}   •   " +
                    $"Pay: {FormatCents(target.dailyWageCents)} each day worked");

                AddManagementCopy(card, "Work days", "management-policy-value");
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

                VisualElement actionRow = AddManagementRow(card);
                PortfolioTaskFocus nextFocus = (PortfolioTaskFocus)(
                    ((int)target.taskFocus + 1) %
                    Enum.GetValues(typeof(PortfolioTaskFocus)).Length);
                AddManagementButton(
                    actionRow,
                    $"Change focus to " +
                    $"{PortfolioPlayerLabels.TaskFocus(nextFocus)}",
                    $"management-focus-{target.employeeId}",
                    () => portfolio.TrySetTaskFocus(
                        target.employeeId,
                        nextFocus,
                        out _));
                bool detailsExpanded = expandedStaff.Contains(target.employeeId);
                AddManagementButton(
                    actionRow,
                    detailsExpanded ? "Hide details" : "More actions",
                    $"management-staff-details-{target.employeeId}",
                    () => ToggleExpanded(expandedStaff, target.employeeId));
                if (!detailsExpanded)
                {
                    continue;
                }

                VisualElement details = AddManagementDetail(card);
                AddManagementCopy(
                    details,
                    $"{target.trait}   •   Skill {target.skill}/100   •   " +
                    $"Reliability {target.reliability}/100   •   Morale {target.satisfaction}/100");
                VisualElement detailActions = AddManagementRow(details);
                AddManagementButton(
                    detailActions,
                    $"Train · {FormatCents(PortfolioProgressionRules.TrainingCostCents)}",
                    $"management-train-{target.employeeId}",
                    () => portfolio.TryTrainEmployee(target.employeeId, out _));
                if (target.role != PortfolioEmployeeRole.Manager)
                {
                    AddManagementButton(
                        detailActions,
                        $"Promote to manager · {FormatCents(PortfolioProgressionRules.PromotionCostCents)}",
                        $"management-promote-{target.employeeId}",
                        () => portfolio.TryPromoteEmployeeToManager(
                            target.employeeId,
                            out _));
                }
                foreach (PortfolioLocationSnapshot other in snapshot.locations
                             .Where(value => !string.Equals(
                                 value.locationId,
                                 target.assignedLocationId,
                                 StringComparison.Ordinal)))
                {
                    PortfolioLocationSnapshot destination = other;
                    AddManagementButton(
                        detailActions,
                        $"Move to {destination.displayName}",
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

            AddManagementSection("PEOPLE AVAILABLE TO HIRE");
            foreach (PortfolioCandidateDefinition candidate in candidates)
            {
                PortfolioCandidateDefinition target = candidate;
                VisualElement card = AddManagementCard(
                    $"{target.DisplayName} · {FriendlyRole(target.Role)}",
                    $"{target.Trait}   •   Skill {target.Skill}/100   •   " +
                    $"Reliability {target.Reliability}/100   •   {FormatCents(target.DailyWageCents)} each day worked");
                AddManagementButton(
                    AddManagementRow(card),
                    $"Hire for {selected.displayName} · {FormatCents(target.HiringCostCents)}",
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

        private void BuildManagementPolicies(PortfolioLocationSnapshot location)
        {
            AddManagementSection("PRICES & STOCK");
            VisualElement commerce = AddManagementCard(
                location.displayName,
                "Set the store's general price position and how much backup stock it should keep.");
            AddManagementCopy(commerce, "Price approach", "management-policy-value");
            VisualElement pricing = AddManagementRow(commerce);
            foreach (PortfolioPricingPolicy value in
                     Enum.GetValues(typeof(PortfolioPricingPolicy)))
            {
                PortfolioPricingPolicy policy = value;
                AddManagementButton(
                    pricing,
                    PortfolioPlayerLabels.PricingPolicy(policy),
                    $"management-pricing-{policy.ToString().ToLowerInvariant()}",
                    () => portfolio.TrySetPricingPreset(
                        location.locationId,
                        policy,
                        out _),
                    location.pricingPolicy == policy);
            }
            AddManagementCopy(commerce, "Stock approach", "management-policy-value");
            VisualElement reorder = AddManagementRow(commerce);
            foreach (PortfolioReorderPolicy value in
                     Enum.GetValues(typeof(PortfolioReorderPolicy)))
            {
                PortfolioReorderPolicy policy = value;
                AddManagementButton(
                    reorder,
                    PortfolioPlayerLabels.ReorderPolicy(policy),
                    $"management-reorder-{policy.ToString().ToLowerInvariant()}",
                    () => portfolio.TrySetReorderPolicy(
                        location.locationId,
                        policy,
                        out _),
                    location.reorderPolicy == policy);
            }
            AddManagementButton(
                AddManagementRow(commerce),
                "Order stock now",
                "management-owner-reorder",
                () => portfolio.TryPlaceManualPurchaseOrder(
                    location.locationId,
                    out _));

            PurchaseOrderSnapshot activeOrder = portfolio.Progression
                .PurchaseOrders.FirstOrDefault(order =>
                    !order.IsTerminal && string.Equals(
                        order.locationId,
                        location.locationId,
                        StringComparison.Ordinal));
            if (activeOrder != null)
            {
                VisualElement order = AddManagementCard(
                    "Stock order",
                    PurchaseOrderMessage(activeOrder.status));
                if (activeOrder.status == PurchaseOrderStatus.Pending)
                {
                    string orderId = activeOrder.orderId;
                    AddManagementButton(
                        AddManagementRow(order),
                        "Cancel order and refund payment",
                        $"management-cancel-order-{orderId}",
                        () => portfolio.TryCancelPurchaseOrder(
                            orderId,
                            out _),
                        danger: true);
                }
            }

            PortfolioDelegationPolicySnapshot policyState =
                location.delegationPolicy;
            AddManagementSection("WHAT YOUR MANAGER CAN HANDLE");
            VisualElement authority = AddManagementCard(
                "Manager decisions",
                "Allow routine decisions here. If a manager lacks permission or money, the phone will ask you instead.");
            VisualElement authorityRow = AddManagementRow(authority);
            AddPolicyToggle(
                authorityRow,
                location,
                "Buy stock",
                "purchase-authority",
                policyState.managerCanPurchase,
                policy => policy.managerCanPurchase =
                    !policy.managerCanPurchase);
            AddPolicyToggle(
                authorityRow,
                location,
                "Approve repairs",
                "maintenance-authority",
                policyState.managerCanAuthorizeMaintenance,
                policy => policy.managerCanAuthorizeMaintenance =
                    !policy.managerCanAuthorizeMaintenance);
            AddManagementCopy(
                authority,
                $"Manager can spend up to {FormatCents(policyState.dailySpendingLimitCents)} per day",
                "management-policy-value");
            VisualElement budget = AddManagementRow(authority);
            AddPolicyChange(
                budget,
                location,
                $"Lower to {FormatCents(Math.Max(0, policyState.dailySpendingLimitCents - PolicyBudgetStepCents))}",
                "budget-decrease",
                policy => policy.dailySpendingLimitCents = Math.Max(
                    0,
                    policy.dailySpendingLimitCents - PolicyBudgetStepCents),
                enabled: policyState.dailySpendingLimitCents > 0);
            AddPolicyChange(
                budget,
                location,
                $"Raise to {FormatCents(policyState.dailySpendingLimitCents <= long.MaxValue - PolicyBudgetStepCents ? policyState.dailySpendingLimitCents + PolicyBudgetStepCents : long.MaxValue)}",
                "budget-increase",
                policy => policy.dailySpendingLimitCents =
                    policy.dailySpendingLimitCents <=
                    long.MaxValue - PolicyBudgetStepCents
                        ? policy.dailySpendingLimitCents +
                          PolicyBudgetStepCents
                        : long.MaxValue,
                enabled: policyState.dailySpendingLimitCents < long.MaxValue);

            AddManagementSection("STORE STANDARDS");
            VisualElement standards = AddManagementCard(
                "When should the phone alert you?",
                $"Current targets: service {policyState.minimumServiceQuality}/100, " +
                $"products available {policyState.minimumProductAvailabilityBasisPoints / 100f:0.#}%, " +
                $"store condition {policyState.minimumMaintenanceCondition}/100.");
            AddManagementButton(
                AddManagementRow(standards),
                operatingStandardsExpanded ? "Hide standards" : "Adjust standards",
                "management-toggle-standards",
                () =>
                {
                    operatingStandardsExpanded = !operatingStandardsExpanded;
                    Refresh();
                });
            if (!operatingStandardsExpanded)
            {
                return;
            }

            VisualElement standardDetails = AddManagementDetail(standards);
            AddStandardControl(
                standardDetails,
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
                standardDetails,
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
                standardDetails,
                location,
                "Maintenance condition",
                policyState.minimumMaintenanceCondition,
                5,
                0,
                100,
                "maintenance",
                (policy, value) => policy.minimumMaintenanceCondition = value,
                value => $"{value}/100");

            AddManagementCopy(standardDetails, "Repair approach", "management-policy-value");
            VisualElement maintenance = AddManagementRow(standardDetails);
            foreach (PortfolioMaintenancePolicy value in
                     Enum.GetValues(typeof(PortfolioMaintenancePolicy)))
            {
                PortfolioMaintenancePolicy maintenancePolicy = value;
                AddPolicyChange(
                    maintenance,
                    location,
                    PortfolioPlayerLabels.MaintenancePolicy(maintenancePolicy),
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
                $"{label}: {(enabledValue ? "Manager can decide" : "Ask me")}",
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
                $"Lower to {format(Math.Max(minimum, current - step))}",
                $"{suffix}-decrease",
                policy => assign(
                    policy,
                    Math.Max(minimum, current - step)),
                enabled: current > minimum);
            AddPolicyChange(
                row,
                location,
                $"Raise to {format(Math.Min(maximum, current + step))}",
                $"{suffix}-increase",
                policy => assign(
                    policy,
                    Math.Min(maximum, current + step)),
                enabled: current < maximum);
        }

        private void BuildManagementLocations(
            PortfolioProgressionSnapshot snapshot)
        {
            AddManagementSection("YOUR STORES");
            string activeLocationId = portfolio.ActiveDetailedSimulationLocationId;
            foreach (PortfolioLocationSnapshot location in snapshot.locations
                         .OrderBy(value =>
                             value.displayName,
                             StringComparer.Ordinal))
            {
                PortfolioLocationSnapshot target = location;
                PortfolioCommercialPropertySnapshot property = snapshot.company
                    .properties.FirstOrDefault(value => string.Equals(
                        value.propertyId,
                        target.propertyId,
                        StringComparison.Ordinal));
                int staff = snapshot.employees.Count(employee => string.Equals(
                    employee.assignedLocationId,
                    target.locationId,
                    StringComparison.Ordinal));
                VisualElement card = AddManagementCard(
                    target.displayName,
                    $"{target.districtName}   •   {FriendlyTenure(property)}\n" +
                    $"{target.marketSummary}\n" +
                    $"{staff} staff   •   Stock {target.inventoryUnits}/{target.inventoryCapacityUnits}   •   " +
                    $"{FormatCents(target.dailyRentCents)} rent each day");
                card.name = $"management-location-card-{target.locationId}";
                bool isActive = string.Equals(
                    activeLocationId,
                    target.locationId,
                    StringComparison.Ordinal);
                AddNamedManagementCopy(
                    card,
                    isActive ? "YOU'RE HERE" : "AVAILABLE TO VISIT",
                    $"management-location-state-{target.locationId}",
                    "management-card-kicker");
                VisualElement actions = AddManagementRow(card);
                AddManagementButton(
                    actions,
                    "Manage this store",
                    $"management-manage-location-{target.locationId}",
                    () =>
                    {
                        portfolio.TrySelectManagementLocation(
                            target.locationId,
                            out _);
                        SelectManagementPage(ManagementPage.Overview);
                    });
                if (isActive)
                {
                    AddManagementButton(
                        actions,
                        $"Back to {target.displayName}",
                        $"management-visit-{target.locationId}",
                        ResumeManagement,
                        primary: true);
                }
                else
                {
                    AddManagementButton(
                        actions,
                        $"Go to {target.displayName}",
                        $"management-visit-{target.locationId}",
                        () => portfolio.TryVisitLocation(
                            target.locationId,
                            out _),
                        primary: true);
                }
            }

            PortfolioLocationDefinition[] available =
                PortfolioProgressionRules.ExpansionOptions
                    .Where(option => snapshot.locations.All(location =>
                        !string.Equals(
                            location.locationId,
                            option.LocationId,
                            StringComparison.Ordinal)))
                    .OrderBy(option =>
                        option.DisplayName,
                        StringComparer.Ordinal)
                    .ToArray();
            AddManagementSection("GROW THE BUSINESS");
            if (available.Length == 0)
            {
                AddManagementCopy(
                    managementContent,
                    "There are no more storefronts available to lease right now.",
                    "management-empty");
                return;
            }

            foreach (PortfolioLocationDefinition option in available)
            {
                PortfolioLocationDefinition target = option;
                long committed = checked(
                    target.LeaseCostCents +
                    target.OpeningInventoryCostCents);
                VisualElement card = AddManagementCard(
                    target.DisplayName,
                    $"{target.DistrictName}\n{target.MarketSummary}\n" +
                    $"Expected demand {target.BaseDemandUnits} units a day   •   " +
                    $"{CompetitionLabel(target.CompetitionIndex)} competition   •   " +
                    $"{FormatCents(target.DailyRentCents)} rent each day\n" +
                    $"Up-front total {FormatCents(committed)}, including opening stock");
                AddManagementButton(
                    AddManagementRow(card),
                    $"Lease {target.DisplayName} · {FormatCents(committed)}",
                    $"management-lease-{target.LocationId}",
                    () => portfolio.TryLeaseLocation(
                        target.LocationId,
                        out _),
                    primary: true);
            }
        }

        private void BuildManagementReports(
            PortfolioProgressionSnapshot snapshot)
        {
            PortfolioConsolidatedReportSnapshot consolidated =
                portfolio.Progression.CreateConsolidatedReport();
            AddManagementSection("COMPANY AT A GLANCE");
            VisualElement portfolioReport = AddManagementCard(
                $"Through Day {consolidated.day}",
                $"Cash {FormatCents(consolidated.cashCents)}   •   " +
                $"Total sales {FormatCents(consolidated.lifetimeGrossSalesCents)}   •   " +
                $"Profit from operations {FormatCents(consolidated.lifetimeOperatingProfitCents)}\n" +
                $"Company reputation {snapshot.companyReputation}/100   •   " +
                $"{consolidated.locationCount} store" +
                (consolidated.locationCount == 1 ? string.Empty : "s") +
                $"   •   {consolidated.leasedPropertyCount} leased properties   •   " +
                $"{consolidated.ownedPropertyCount} owned   •   {consolidated.openAlertCount} open alerts");
            portfolioReport.name = "management-portfolio-report";

            AddManagementSection("STORE RESULTS");
            foreach (PortfolioLocationSnapshot location in snapshot.locations
                         .OrderBy(value =>
                             value.displayName,
                             StringComparer.Ordinal))
            {
                if (!location.hasLastReport || location.lastReport == null)
                {
                    VisualElement emptyReport = AddManagementCard(
                        location.displayName,
                        "No completed operating report exists for this location yet.");
                    emptyReport.name =
                        $"management-location-report-{location.locationId}";
                    continue;
                }

                PortfolioLocationReportSnapshot report = location.lastReport;
                VisualElement locationReport = AddManagementCard(
                    $"{location.displayName} · Day {report.day}",
                    $"{ReportModeLabel(report)}\n" +
                    $"Sales {FormatCents(report.grossSalesCents)}   •   " +
                    $"Profit from operations {FormatCents(report.operatingProfitCents)}\n" +
                    report.primaryCause);
                locationReport.name =
                    $"management-location-report-{location.locationId}";
                bool expanded = expandedReports.Contains(location.locationId);
                AddManagementButton(
                    AddManagementRow(locationReport),
                    expanded ? "Hide breakdown" : "Show breakdown",
                    $"management-report-details-{location.locationId}",
                    () => ToggleExpanded(
                        expandedReports,
                        location.locationId));
                if (!expanded)
                {
                    continue;
                }

                VisualElement detail = AddManagementDetail(locationReport);
                detail.name =
                    $"management-location-report-detail-{location.locationId}";
                AddManagementCopy(
                    detail,
                    $"Product costs {FormatCents(report.costOfGoodsSoldCents)}   •   " +
                    $"Staff pay {FormatCents(report.payrollCents)}   •   " +
                    $"Rent {FormatCents(report.rentCents)}   •   " +
                    $"Cash change {FormatCents(report.cashChangeCents)}\n" +
                    $"Sold {report.unitsSold} of {report.demandUnits} requested units   •   " +
                    $"Missed {report.lostDemandUnits} sales   •   " +
                    $"Ended with {report.endingInventoryUnits} units in stock\n" +
                    $"Service {report.serviceQuality}/100   •   " +
                    $"Customer satisfaction {report.customerSatisfaction}/100   •   " +
                    $"Products available {report.productAvailabilityBasisPoints / 100f:0.#}%   •   " +
                    $"Product range match {report.productMixBasisPoints / 100f:0.#}%\n" +
                    $"Store condition {report.maintenanceCondition}/100   •   " +
                    $"Failure risk {report.failurePressure}/100");
            }
        }

        private void BuildManagementAlerts(PortfolioLocationSnapshot location)
        {
            PortfolioOperatingAlertSnapshot[] open = location.operatingAlerts
                .Where(alert => alert.IsOpen)
                .OrderByDescending(alert => alert.severity)
                .ThenBy(alert => alert.alertId, StringComparer.Ordinal)
                .ToArray();
            AddManagementSection($"NEEDS ATTENTION · {open.Length}");
            if (open.Length == 0)
            {
                AddManagementCopy(
                    managementContent,
                    "Nothing at this store needs your attention right now.",
                    "management-empty");
            }
            foreach (PortfolioOperatingAlertSnapshot alert in open)
            {
                PortfolioOperatingAlertSnapshot target = alert;
                VisualElement card = AddManagementCard(
                    $"{FriendlyAlertSeverity(target.severity)} · {AlertTitle(target)}",
                    PlayerFacingText(target.summary),
                    target.severity == PortfolioAlertSeverity.Critical
                        ? "management-card--critical"
                        : "management-card--warning");
                AddManagementCopy(
                    card,
                    $"What you can do: {PlayerFacingText(target.recoveryAction)}\n" +
                    $"Open since Day {target.openedDay}   •   " +
                    (target.requiresOwnerAttention
                        ? "Waiting for you"
                        : "Your manager can see this"));
                VisualElement actions = AddManagementRow(card);
                if (!target.acknowledged)
                {
                    AddManagementButton(
                        actions,
                        "Mark as seen",
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
                AddManagementSection("REPAIR OPTION");
                VisualElement maintenance = AddManagementCard(
                    "Repair the store now",
                    $"Store condition is {location.maintenanceCondition}/100. Paying for an immediate repair improves it now and reduces the chance of another failure.",
                    "management-card--warning");
                AddManagementButton(
                    AddManagementRow(maintenance),
                    "Pay for immediate repair",
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
            AddManagementSection("PAST ALERTS");
            VisualElement history = AddManagementCard(
                $"{resolved.Length} resolved alert" +
                (resolved.Length == 1 ? string.Empty : "s"),
                "Past alerts are kept for reference and do not need action.");
            AddManagementButton(
                AddManagementRow(history),
                resolvedAlertsExpanded ? "Hide past alerts" : "Show past alerts",
                "management-toggle-resolved-alerts",
                () =>
                {
                    resolvedAlertsExpanded = !resolvedAlertsExpanded;
                    Refresh();
                });
            if (!resolvedAlertsExpanded)
            {
                return;
            }
            foreach (PortfolioOperatingAlertSnapshot alert in resolved)
            {
                AddManagementCard(
                    $"Resolved Day {alert.resolvedDay} · {AlertTitle(alert)}",
                    PlayerFacingText(alert.summary));
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
                        "Repair store now",
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
                        "Order stock",
                        $"management-recover-{alert.alertId}",
                        () => portfolio.TryPlaceManualPurchaseOrder(
                            location.locationId,
                            out _));
                    break;
                case "service-standard":
                    AddManagementButton(
                        row,
                        "Review staff",
                        $"management-recover-{alert.alertId}",
                        () => SelectManagementPage(ManagementPage.Team));
                    break;
                default:
                    AddManagementButton(
                        row,
                        "Review operations",
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

        private static void AddNamedManagementCopy(
            VisualElement parent,
            string text,
            string name,
            string className = "management-card-copy")
        {
            Label label = new(text)
            {
                name = name
            };
            label.AddToClassList(className);
            parent.Add(label);
        }

        private static VisualElement AddManagementDetail(
            VisualElement parent)
        {
            VisualElement detail = new();
            detail.AddToClassList("management-detail");
            parent.Add(detail);
            return detail;
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

        private void ToggleExpanded(
            HashSet<string> expanded,
            string key)
        {
            if (!expanded.Add(key))
            {
                expanded.Remove(key);
            }
            Refresh();
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

        private static string FormatScheduledDays(
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
                _ => string.Join(
                    ", ",
                    Enumerable.Range(1, 7)
                        .Where(day =>
                            (schedule.scheduledDayMask & (1 << (day - 1))) != 0)
                        .Select(day => $"Day {day}"))
            };
            return string.IsNullOrWhiteSpace(days) ? "No scheduled days" : days;
        }

        private static string FriendlyTenure(
            PortfolioCommercialPropertySnapshot property)
        {
            if (property == null)
            {
                return "Property details unavailable";
            }
            return property.tenure == PortfolioPropertyTenure.Owned
                ? "Owned property"
                : "Leased storefront";
        }

        private static string CompetitionLabel(int competition)
        {
            return competition >= 67
                ? "High"
                : competition >= 34
                    ? "Moderate"
                    : "Low";
        }

        private static string HealthLabel(int value)
        {
            string label = value >= 80
                ? "Good"
                : value >= 60
                    ? "Watch"
                    : "Needs attention";
            return $"{label} ({value}/100)";
        }

        private static string ReportModeLabel(
            PortfolioLocationReportSnapshot report)
        {
            return report.isDetailedOperation
                ? "Run while you were there"
                : "Run by your staff";
        }

        private static string PurchaseOrderMessage(PurchaseOrderStatus status)
        {
            return status switch
            {
                PurchaseOrderStatus.Pending =>
                    "Waiting for the supplier. You can still cancel for a refund.",
                PurchaseOrderStatus.Fulfilled =>
                    "The supplier has packed this order for delivery.",
                PurchaseOrderStatus.Delivered =>
                    "Delivered at the store. Receive the products there.",
                PurchaseOrderStatus.PartiallyReceived =>
                    "Some products were received. Finish receiving the rest at the store.",
                PurchaseOrderStatus.Canceled => "This order was cancelled.",
                _ => "This order has been received and stocked."
            };
        }

        private static string FriendlyAlertSeverity(
            PortfolioAlertSeverity severity)
        {
            return severity switch
            {
                PortfolioAlertSeverity.Critical => "Urgent",
                PortfolioAlertSeverity.Warning => "Warning",
                _ => "Heads-up"
            };
        }

        private static string AlertTitle(PortfolioOperatingAlertSnapshot alert)
        {
            return alert.problemTypeId switch
            {
                "maintenance-pressure" => "Store condition is slipping",
                "product-availability" => "Products are running out",
                "purchasing-authority" => "Manager cannot order stock",
                "spending-limit" => "Manager needs a larger budget",
                "service-standard" => "Customer service is below target",
                _ => "Store needs a decision"
            };
        }

        private static string PlayerFacingText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Ready.";
            }
            if (text.StartsWith(
                    "Receive every unit in ",
                    StringComparison.Ordinal) &&
                text.EndsWith(
                    " before leaving this detailed location.",
                    StringComparison.Ordinal))
            {
                return "Receive every product in the delivered stock order before leaving this store.";
            }

            return text
                .Replace("Live operation reconciled:", "Store totals updated:")
                .Replace("COGS", "product costs")
                .Replace(
                    "Complete the hands-on first shift before delegating.",
                    "Finish your first day in the store before ending it from the phone.")
                .Replace(
                    "Leave the active detailed location before advancing delegated simulation.",
                    "Leave the active store before ending the day.")
                .Replace(
                    "Prove one delegated operating day before signing a second lease.",
                    "Finish one day with staff running the first store before signing a second lease.")
                .Replace(
                    "The selected location is not an occupied business in this portfolio.",
                    "That store is not open in your company.")
                .Replace(
                    "A valid persistent portfolio location is required.",
                    "Choose one of your open stores.")
                .Replace(" for the next operating day.", " for tomorrow.")
                .Replace(
                    " scheduled to operate while you are absent.",
                    " scheduled for tomorrow.")
                .Replace("scheduled payroll", "staff pay")
                .Replace("base operating costs", "other operating costs")
                .Replace("protected reserve", "minimum cash reserve")
                .Replace("the owner's operating standard", "the target you set")
                .Replace("the configured standard", "the target you set")
                .Replace("Inventory reached its reorder point", "Stock is running low")
                .Replace("purchasing authority", "permission to buy stock")
                .Replace("The delegated spending limit", "The manager's spending limit")
                .Replace("configured reorder", "stock order")
                .Replace("maintenance response", "repair")
                .Replace("intervene as owner", "handle it yourself")
                .Replace("inventory staffing focus", "stocking focus")
                .Replace("detailed location", "store")
                .Replace("detailed operation", "in-store work")
                .Replace("Company progression", "Company information")
                .Replace("Company procurement", "Stock ordering")
                .Replace("Purchase order location", "Store")
                .Replace(
                    "Current inventory already meets the configured reorder target.",
                    "This store already has enough stock for its current setting.");
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
            return root == null || titleView == null ||
                   newBusinessSetupView == null || pauseView == null ||
                   settingsView == null || managementView == null ||
                   generalContent == null ||
                   controlsContent == null || titleNewBusiness == null ||
                   titleLoadBusiness == null || setupBusinessName == null ||
                   setupLogo == null || setupSeed == null ||
                   setupPrimaryColor == null || setupSecondaryColor == null ||
                   setupDifficulty == null || setupBack == null ||
                   setupStart == null || generalTab == null ||
                   controlsTab == null || fullscreenToggle == null ||
                   interfaceScaleSlider == null || interfaceScaleValue == null ||
                   masterVolumeSlider == null || masterVolumeValue == null ||
                   cameraMotionToggle == null || sensitivityDecrease == null ||
                   sensitivityIncrease == null ||
                   sensitivityValue == null || invertYToggle == null ||
                   bindingList == null || resetBindings == null ||
                   settingsBack == null || settingsApply == null ||
                   managementSummary == null || managementStatus == null ||
                   managementPageTitle == null || managementPageHelp == null ||
                   managementContent == null ||
                   managementOverviewTab == null ||
                   managementTeamTab == null ||
                   managementPolicyTab == null ||
                   managementAlertsTab == null ||
                   managementLocationsTab == null ||
                   managementReportsTab == null || managementResume == null ||
                   !titleNotification.IsValid || !setupNotification.IsValid ||
                   !pauseNotification.IsValid ||
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
