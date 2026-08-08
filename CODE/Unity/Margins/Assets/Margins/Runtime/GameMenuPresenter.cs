using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Margins
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenuPresenter : MonoBehaviour
    {
        [SerializeField] private GamePauseMenuController controller;
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet styleSheet;

        private readonly Dictionary<VisualElement, Action> activations = new();
        private readonly List<VisualElement> focusables = new();
        private readonly List<Button> bindingButtons = new();
        private readonly List<Button> staticTitleButtons = new();
        private readonly List<Button> staticPauseButtons = new();

        private VisualElement root;
        private VisualElement titleView;
        private VisualElement pauseView;
        private VisualElement settingsView;
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
        private NotificationElements titleNotification;
        private NotificationElements pauseNotification;
        private NotificationElements settingsNotification;
        private bool initialized;
        private int renderedBindingRevision = -1;
        private string renderedActiveBindingKey;

        public bool IsConfigured { get; private set; }
        public VisualElement Root => root;

        private void Awake()
        {
            document ??= GetComponent<UIDocument>();
            controller ??= FindAnyObjectByType<GamePauseMenuController>();
        }

        private void OnEnable()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError(error, this);
                return;
            }

            controller.PresentationChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.PresentationChanged -= Refresh;
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

            root.style.display = controller.IsOpen
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (document.panelSettings != null)
            {
                document.panelSettings.scale =
                    GamePauseMenuController.UserInterfaceScale;
            }
            if (!controller.IsOpen)
            {
                return;
            }

            bool title = controller.Screen == GameMenuScreen.Title;
            bool pause = controller.Screen == GameMenuScreen.Pause;
            bool settings = controller.IsSettingsVisible;
            bool controls = controller.Screen == GameMenuScreen.SettingsControls;
            SetVisible(titleView, title);
            SetVisible(pauseView, pause);
            SetVisible(settingsView, settings);
            SetVisible(generalContent, settings && !controls);
            SetVisible(controlsContent, settings && controls);

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
            RebuildFocusables(title, pause, settings, controls);
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
            bool controls)
        {
            focusables.Clear();
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
            if (controller == null || !controller.IsOpen)
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
                   settingsView == null || generalContent == null ||
                   controlsContent == null || titleNewBusiness == null ||
                   titleLoadBusiness == null || generalTab == null ||
                   controlsTab == null || fullscreenToggle == null ||
                   interfaceScaleSlider == null || interfaceScaleValue == null ||
                   masterVolumeSlider == null || masterVolumeValue == null ||
                   cameraMotionToggle == null || sensitivitySlider == null ||
                   sensitivityValue == null || invertYToggle == null ||
                   bindingList == null || resetBindings == null ||
                   settingsBack == null || settingsApply == null ||
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
