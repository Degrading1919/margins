using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    /// <summary>
    /// Owns menu state and routes player choices to settings, input, and
    /// persistence authorities. GameMenuPresenter owns runtime presentation.
    /// </summary>
    public sealed class GamePauseMenuController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private FirstStoreDiskPersistenceController persistence;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, Min(0.1f)] private float notificationDuration = 3.5f;

        private readonly GameMenuStateModel menuState = new();
        private readonly MenuNotificationModel notification = new();
        private IGamePreferences preferences;
        private GameSettingsModel appliedSettings;
        private GameSettingsModel draftSettings;
        private InputBindingSettings bindingSettings;
        private int lastEscapeFrame = -1;
        private static bool gameMenuOpen;
        private static int externalModalCount;

        public event Action PresentationChanged;

        public static bool IsAnyMenuOpen =>
            gameMenuOpen || externalModalCount > 0;
        public static float UserInterfaceScale { get; private set; } = 1f;

        public bool IsOpen => menuState.Screen != GameMenuScreen.Closed;
        public bool IsTitleVisible => menuState.Screen == GameMenuScreen.Title;
        public bool IsSettingsVisible => menuState.IsSettings;
        public bool HasActiveSession => menuState.HasActiveSession;
        public bool CanLoadBusiness => persistence != null && persistence.HasSaveFile;
        public GameMenuScreen Screen => menuState.Screen;
        public SessionReplacementAction PendingReplacement =>
            menuState.PendingReplacement;
        public GameSettingsModel DraftSettings => draftSettings;
        public InputBindingSettings BindingSettings => bindingSettings;
        public MenuNotificationModel Notification => notification;
        public string StatusMessage => notification.Message;

        private void Awake()
        {
            preferences = PlayerPrefsGamePreferences.Instance;
            float defaultSensitivity = firstPersonController != null
                ? firstPersonController.MouseSensitivity
                : 0.1f;
            appliedSettings = GameSettingsModel.Load(
                preferences,
                defaultSensitivity,
                UnityEngine.Screen.fullScreen);
            draftSettings = appliedSettings.Clone();
            ApplyRuntimeSettings(appliedSettings);

            bindingSettings = new InputBindingSettings(inputActions, preferences);
            if (!bindingSettings.TryLoad(out string bindingError))
            {
                notification.ShowPersistent(
                    bindingError,
                    MenuNotificationKind.Error);
            }

            PublishPresentation();
        }

        private void Start()
        {
            if (!Application.isEditor && !Application.isBatchMode)
            {
                ShowTitleAtLaunch();
            }
        }

        private void OnDisable()
        {
            bindingSettings?.Dispose();
            if (IsOpen)
            {
                Time.timeScale = 1f;
            }
            gameMenuOpen = false;
        }

        private void Update()
        {
            if (notification.Tick(Time.unscaledTime))
            {
                PublishPresentation();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
            }
        }

        private void HandleEscapePressed()
        {
            if (lastEscapeFrame == Time.frameCount)
            {
                return;
            }
            lastEscapeFrame = Time.frameCount;

            if (externalModalCount > 0)
            {
                return;
            }

            if (bindingSettings?.IsRebinding == true)
            {
                bindingSettings.CancelCurrentRebind();
                return;
            }

            if (menuState.IsSettings)
            {
                CloseSettings();
                return;
            }
            if (menuState.Screen == GameMenuScreen.Title)
            {
                return;
            }
            if (menuState.Screen == GameMenuScreen.Pause)
            {
                Resume();
                return;
            }

            if (firstPersonController != null &&
                !firstPersonController.IsGameplayMode)
            {
                firstPersonController.SetGameplayMode(true);
                return;
            }

            OpenMenu();
        }

        public void OpenMenu()
        {
            if (externalModalCount > 0)
            {
                return;
            }
            menuState.OpenPause();
            notification.Clear();
            ApplyMenuEnvironment();
            PublishPresentation();
        }

        public void Resume()
        {
            menuState.EnterSession();
            notification.Clear();
            ApplyMenuEnvironment();
            PublishPresentation();
        }

        public void ShowTitleAtLaunch()
        {
            menuState.ShowTitleAtLaunch();
            notification.Clear();
            ApplyMenuEnvironment();
            PublishPresentation();
        }

        public void ReturnToTitle()
        {
            menuState.ReturnToTitle();
            notification.Clear();
            ApplyMenuEnvironment();
            PublishPresentation();
        }

        public void OpenSettings()
        {
            draftSettings = appliedSettings.Clone();
            menuState.OpenSettings();
            notification.Clear();
            PublishPresentation();
        }

        public void SelectSettingsTab(bool controls)
        {
            menuState.SelectSettingsTab(controls);
            PublishPresentation();
        }

        public void CloseSettings()
        {
            bindingSettings?.CancelCurrentRebind();
            draftSettings = appliedSettings.Clone();
            menuState.CloseSettings();
            notification.Clear();
            PublishPresentation();
        }

        public void SaveBusiness()
        {
            menuState.ClearPendingReplacement();
            if (persistence == null)
            {
                ShowError("Saving is unavailable in this build.");
                return;
            }

            bool success = persistence.TrySave();
            if (success)
            {
                ShowSuccess("Business saved.");
            }
            else
            {
                ShowError(FriendlyPersistenceFailure(
                    persistence.LastDiagnostic,
                    true));
            }
        }

        public void RequestNewBusiness()
        {
            if (menuState.Screen != GameMenuScreen.Title)
            {
                return;
            }
            if (persistence == null)
            {
                ShowError("Starting a new business is unavailable in this build.");
                return;
            }

            if (!menuState.ConfirmOrArmReplacement(
                    SessionReplacementAction.NewBusiness))
            {
                notification.ShowPersistent(
                    "Start a new business? Current unsaved progress will be replaced. " +
                    "Your disk save will remain. Select New Business again to confirm.",
                    MenuNotificationKind.Information);
                PublishPresentation();
                return;
            }

            if (!persistence.TryStartNewBusiness())
            {
                ShowError(FriendlyPersistenceFailure(
                    persistence.LastDiagnostic,
                    false));
                return;
            }

            Resume();
        }

        public void RequestLoadBusiness()
        {
            if (persistence == null)
            {
                ShowError("Loading is unavailable in this build.");
                return;
            }
            if (!persistence.HasSaveFile)
            {
                menuState.ClearPendingReplacement();
                ShowError("No saved business is available yet.");
                return;
            }

            if (!menuState.ConfirmOrArmReplacement(
                    SessionReplacementAction.LoadBusiness))
            {
                notification.ShowPersistent(
                    "Load the saved business? Current unsaved progress will be replaced. " +
                    "Select Load Business again to confirm.",
                    MenuNotificationKind.Information);
                PublishPresentation();
                return;
            }

            bool loadedFromTitle = menuState.Screen == GameMenuScreen.Title;
            if (!persistence.TryLoad())
            {
                ShowError(FriendlyPersistenceFailure(
                    persistence.LastDiagnostic,
                    false));
                return;
            }

            if (loadedFromTitle)
            {
                Resume();
            }
            else
            {
                ShowSuccess("Saved business loaded.");
            }
        }

        public void ApplySettings()
        {
            if (draftSettings == null)
            {
                return;
            }

            draftSettings.Clamp();
            appliedSettings = draftSettings.Clone();
            ApplyRuntimeSettings(appliedSettings);
            appliedSettings.Save(preferences);
            ShowSuccess("Settings saved.");
        }

        public void ResetBindingsToDefaults()
        {
            if (bindingSettings == null || !bindingSettings.IsAvailable)
            {
                ShowError("Control bindings are unavailable in this build.");
                return;
            }

            bindingSettings.ResetToDefaults();
            ShowSuccess("Control bindings reset to defaults.");
        }

        public void BeginRebind(PlayerBindingEntry entry)
        {
            if (bindingSettings == null || !bindingSettings.IsAvailable)
            {
                ShowError("Control bindings are unavailable in this build.");
                return;
            }

            if (!bindingSettings.BeginInteractiveRebind(
                    entry,
                    HandleRebindCompleted,
                    out string error))
            {
                ShowError(error);
                return;
            }

            notification.ShowPersistent(
                $"Rebinding {entry.Label}. Press a keyboard or mouse control; press Escape to cancel.",
                MenuNotificationKind.Information);
            PublishPresentation();
        }

        public void SetLookSensitivityLevel(int level)
        {
            if (draftSettings == null)
            {
                return;
            }
            draftSettings.LookSensitivityLevel = Mathf.Clamp(level, 1, 10);
            PublishPresentation();
        }

        public void SetInvertY(bool value)
        {
            if (draftSettings != null)
            {
                draftSettings.InvertY = value;
            }
        }

        public void SetCameraMotion(bool value)
        {
            if (draftSettings != null)
            {
                draftSettings.CameraMotion = value;
            }
        }

        public void SetMasterVolume(float value)
        {
            if (draftSettings != null)
            {
                draftSettings.MasterVolume = Mathf.Clamp01(value);
            }
        }

        public void SetFullscreen(bool value)
        {
            if (draftSettings != null)
            {
                draftSettings.Fullscreen = value;
            }
        }

        public void SetInterfaceScale(float value)
        {
            if (draftSettings != null)
            {
                draftSettings.InterfaceScale = Mathf.Clamp(value, 0.85f, 1.25f);
            }
        }

        public void DismissNotification()
        {
            notification.Clear();
            menuState.ClearPendingReplacement();
            PublishPresentation();
        }

        public void QuitToDesktop()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleRebindCompleted(RebindResult result)
        {
            switch (result.Outcome)
            {
                case RebindOutcome.Completed:
                    bindingSettings.Save();
                    ShowSuccess(result.Message);
                    break;
                case RebindOutcome.Cancelled:
                    notification.ShowTransient(
                        result.Message,
                        MenuNotificationKind.Information,
                        Time.unscaledTime,
                        notificationDuration);
                    PublishPresentation();
                    break;
                case RebindOutcome.Conflict:
                case RebindOutcome.Failed:
                    ShowError(result.Message);
                    break;
            }
        }

        private void ApplyRuntimeSettings(GameSettingsModel settings)
        {
            settings.Clamp();
            UserInterfaceScale = settings.InterfaceScale;
            float sensitivity = settings.UnderlyingLookSensitivity;
            firstPersonController?.ApplyPlayerSettings(
                sensitivity,
                settings.InvertY,
                settings.CameraMotion);
            AudioListener.volume = settings.MasterVolume;
            if (UnityEngine.Screen.fullScreen != settings.Fullscreen)
            {
                UnityEngine.Screen.fullScreen = settings.Fullscreen;
            }
        }

        private void ApplyMenuEnvironment()
        {
            gameMenuOpen = IsOpen;
            Time.timeScale = IsOpen ? 0f : 1f;
            firstPersonController?.SetGameplayMode(!IsOpen);
        }

        private void ShowSuccess(string message)
        {
            notification.ShowTransient(
                message,
                MenuNotificationKind.Success,
                Time.unscaledTime,
                notificationDuration);
            PublishPresentation();
        }

        private void ShowError(string message)
        {
            notification.ShowPersistent(message, MenuNotificationKind.Error);
            PublishPresentation();
        }

        private void PublishPresentation()
        {
            gameMenuOpen = IsOpen;
            PresentationChanged?.Invoke();
        }

        public static void RegisterExternalModal()
        {
            externalModalCount++;
        }

        public static void UnregisterExternalModal()
        {
            externalModalCount = Math.Max(0, externalModalCount - 1);
        }

        private static string FriendlyPersistenceFailure(
            string diagnostic,
            bool saving)
        {
            string value = diagnostic ?? string.Empty;
            if (value.Contains("held", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("holding", StringComparison.OrdinalIgnoreCase))
            {
                return "Put down what you are holding, then try again.";
            }
            if (value.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("transaction", StringComparison.OrdinalIgnoreCase))
            {
                return "Finish or cancel the current checkout, then try again.";
            }
            if (value.Contains("no accepted", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return "No saved business is available yet.";
            }
            if (!saving)
            {
                return "That session could not be opened. Your current business is unchanged.";
            }
            return "The business could not be saved. Finish the current action and try again.";
        }
    }
}
