using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    /// <summary>
    /// Owns title, pause, settings, save/load confirmation, and menu input.
    /// Gameplay and company-management views remain separate input contexts.
    /// </summary>
    public sealed class GamePauseMenuController : MonoBehaviour
    {
        private const string LegacySensitivityKey = "margins.look_sensitivity";
        private const string HorizontalSensitivityKey =
            "margins.look_sensitivity_horizontal";
        private const string VerticalSensitivityKey =
            "margins.look_sensitivity_vertical";
        private const string InvertYKey = "margins.invert_y";
        private const string CameraMotionKey = "margins.camera_motion";
        private const string VolumeKey = "margins.master_volume";
        private const string FullscreenKey = "margins.fullscreen";
        private const string InterfaceScaleKey = "margins.interface_scale";

        private static readonly Color Ink =
            new(0.94f, 0.96f, 0.95f, 1f);
        private static readonly Color MutedInk =
            new(0.64f, 0.7f, 0.7f, 1f);
        private static readonly Color Night =
            new(0.025f, 0.04f, 0.052f, 0.985f);
        private static readonly Color NightSoft =
            new(0.055f, 0.08f, 0.092f, 0.98f);
        private static readonly Color NightRaised =
            new(0.085f, 0.115f, 0.125f, 1f);
        private static readonly Color Teal =
            new(0.12f, 0.78f, 0.68f, 1f);
        private static readonly Color Amber =
            new(1f, 0.58f, 0.2f, 1f);
        private static readonly Color Error =
            new(0.95f, 0.3f, 0.23f, 1f);

        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private FirstStoreDiskPersistenceController persistence;

        private bool isOpen;
        private bool showSettings;
        private bool showTitle;
        private bool settingsOpenedFromTitle;
        private bool loadConfirmationPending;
        private float horizontalSensitivity;
        private float verticalSensitivity;
        private bool invertY;
        private bool cameraMotion;
        private float masterVolume;
        private bool fullscreen;
        private float interfaceScale;
        private string status;
        private bool statusSucceeded = true;
        private int selectedOption;
        private int lastEscapeFrame = -1;
        private GUIStyle brandStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle buttonSelectedStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle valueStyle;

        public static bool IsAnyMenuOpen { get; private set; }
        public static float UserInterfaceScale { get; private set; } = 1f;
        public bool IsOpen => isOpen || showTitle;
        public bool IsTitleVisible => showTitle && !showSettings;
        public bool IsSettingsVisible => showSettings;
        public string StatusMessage => status;

        private void Awake()
        {
            float legacySensitivity = PlayerPrefs.GetFloat(
                LegacySensitivityKey,
                firstPersonController != null
                    ? firstPersonController.MouseSensitivity
                    : 0.1f);
            horizontalSensitivity = PlayerPrefs.GetFloat(
                HorizontalSensitivityKey,
                firstPersonController != null
                    ? firstPersonController.HorizontalLookSensitivity
                    : legacySensitivity);
            verticalSensitivity = PlayerPrefs.GetFloat(
                VerticalSensitivityKey,
                firstPersonController != null
                    ? firstPersonController.VerticalLookSensitivity
                    : legacySensitivity);
            invertY = PlayerPrefs.GetInt(InvertYKey, 0) != 0;
            cameraMotion = PlayerPrefs.GetInt(CameraMotionKey, 1) != 0;
            masterVolume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            fullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0) != 0;
            interfaceScale = PlayerPrefs.GetFloat(InterfaceScaleKey, 1f);
            ApplySettings(false);
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
            if (IsOpen)
            {
                Time.timeScale = 1f;
            }
            IsAnyMenuOpen = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame)
            {
                MoveSelection(-1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame ||
                     keyboard.sKey.wasPressedThisFrame)
            {
                MoveSelection(1);
            }

            if (showSettings &&
                (keyboard.leftArrowKey.wasPressedThisFrame ||
                 keyboard.aKey.wasPressedThisFrame))
            {
                AdjustSelectedSetting(-1);
            }
            else if (showSettings &&
                     (keyboard.rightArrowKey.wasPressedThisFrame ||
                      keyboard.dKey.wasPressedThisFrame))
            {
                AdjustSelectedSetting(1);
            }

            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame)
            {
                ActivateSelectedOption();
            }
        }

        private void HandleEscapePressed()
        {
            if (lastEscapeFrame == Time.frameCount)
            {
                return;
            }
            lastEscapeFrame = Time.frameCount;

            if (showSettings)
            {
                CloseSettings();
                return;
            }

            if (showTitle)
            {
                return;
            }

            if (isOpen)
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
            isOpen = true;
            showSettings = false;
            showTitle = false;
            settingsOpenedFromTitle = false;
            loadConfirmationPending = false;
            selectedOption = 0;
            status = null;
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            firstPersonController?.SetGameplayMode(false);
        }

        public void Resume()
        {
            isOpen = false;
            showSettings = false;
            showTitle = false;
            settingsOpenedFromTitle = false;
            loadConfirmationPending = false;
            IsAnyMenuOpen = false;
            Time.timeScale = 1f;
            firstPersonController?.SetGameplayMode(true);
        }

        private void ShowTitleAtLaunch()
        {
            isOpen = false;
            showSettings = false;
            showTitle = true;
            settingsOpenedFromTitle = false;
            loadConfirmationPending = false;
            selectedOption = 0;
            status = null;
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            firstPersonController?.SetGameplayMode(false);
        }

        private void ReturnToTitle()
        {
            isOpen = false;
            showSettings = false;
            showTitle = true;
            settingsOpenedFromTitle = false;
            loadConfirmationPending = false;
            selectedOption = 0;
            status = null;
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            firstPersonController?.SetGameplayMode(false);
        }

        private void OpenSettings()
        {
            settingsOpenedFromTitle = showTitle;
            showSettings = true;
            selectedOption = 0;
            loadConfirmationPending = false;
            status = null;
        }

        private void CloseSettings()
        {
            showSettings = false;
            showTitle = settingsOpenedFromTitle;
            isOpen = !settingsOpenedFromTitle;
            selectedOption = settingsOpenedFromTitle ? 2 : 3;
            status = null;
        }

        private void Save()
        {
            loadConfirmationPending = false;
            if (persistence == null)
            {
                SetStatus(false, "Saving is unavailable in this build.");
                return;
            }

            bool success = persistence.TrySave();
            SetStatus(
                success,
                success
                    ? "Company saved."
                    : FriendlyPersistenceFailure(persistence.LastDiagnostic, true));
        }

        private void RequestLoad()
        {
            if (persistence == null)
            {
                SetStatus(false, "Loading is unavailable in this build.");
                return;
            }
            if (!persistence.HasSaveFile)
            {
                loadConfirmationPending = false;
                SetStatus(false, "No saved company is available yet.");
                return;
            }
            if (!loadConfirmationPending)
            {
                loadConfirmationPending = true;
                SetStatus(
                    true,
                    "Load the last save? Unsaved changes will be replaced.");
                return;
            }

            loadConfirmationPending = false;
            bool success = persistence.TryLoad();
            SetStatus(
                success,
                success
                    ? "Saved company loaded."
                    : FriendlyPersistenceFailure(persistence.LastDiagnostic, false));
            if (success && showTitle)
            {
                Resume();
            }
        }

        private void ApplySettings(bool persist)
        {
            horizontalSensitivity = Mathf.Clamp(
                horizontalSensitivity,
                0.01f,
                0.5f);
            verticalSensitivity = Mathf.Clamp(
                verticalSensitivity,
                0.01f,
                0.5f);
            masterVolume = Mathf.Clamp01(masterVolume);
            interfaceScale = Mathf.Clamp(interfaceScale, 0.85f, 1.25f);
            UserInterfaceScale = interfaceScale;
            firstPersonController?.ApplyPlayerSettings(
                horizontalSensitivity,
                verticalSensitivity,
                invertY,
                cameraMotion);
            AudioListener.volume = masterVolume;
            if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }

            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(
                HorizontalSensitivityKey,
                horizontalSensitivity);
            PlayerPrefs.SetFloat(
                VerticalSensitivityKey,
                verticalSensitivity);
            PlayerPrefs.SetInt(InvertYKey, invertY ? 1 : 0);
            PlayerPrefs.SetInt(CameraMotionKey, cameraMotion ? 1 : 0);
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.SetFloat(InterfaceScaleKey, interfaceScale);
            PlayerPrefs.Save();
            SetStatus(true, "Settings saved.");
        }

        private void MoveSelection(int direction)
        {
            int optionCount = showSettings
                ? 9
                : showTitle
                    ? 4
                    : 6;
            selectedOption =
                (selectedOption + direction + optionCount) % optionCount;
            if ((!showTitle && !showSettings && selectedOption != 2) ||
                (showTitle && !showSettings && selectedOption != 1))
            {
                loadConfirmationPending = false;
            }
        }

        private void AdjustSelectedSetting(int direction)
        {
            switch (selectedOption)
            {
                case 0:
                    horizontalSensitivity = Mathf.Clamp(
                        horizontalSensitivity + direction * 0.01f,
                        0.01f,
                        0.5f);
                    break;
                case 1:
                    verticalSensitivity = Mathf.Clamp(
                        verticalSensitivity + direction * 0.01f,
                        0.01f,
                        0.5f);
                    break;
                case 2:
                    invertY = direction > 0;
                    break;
                case 3:
                    cameraMotion = direction > 0;
                    break;
                case 4:
                    fullscreen = direction > 0;
                    break;
                case 5:
                    interfaceScale = Mathf.Clamp(
                        interfaceScale + direction * 0.05f,
                        0.85f,
                        1.25f);
                    break;
                case 6:
                    masterVolume = Mathf.Clamp01(
                        masterVolume + direction * 0.05f);
                    break;
            }
        }

        private void ActivateSelectedOption()
        {
            if (showSettings)
            {
                switch (selectedOption)
                {
                    case 2:
                        invertY = !invertY;
                        break;
                    case 3:
                        cameraMotion = !cameraMotion;
                        break;
                    case 4:
                        fullscreen = !fullscreen;
                        break;
                    case 7:
                        ApplySettings(true);
                        break;
                    case 8:
                        CloseSettings();
                        break;
                }
                return;
            }

            if (showTitle)
            {
                switch (selectedOption)
                {
                    case 0:
                        Resume();
                        break;
                    case 1:
                        RequestLoad();
                        break;
                    case 2:
                        OpenSettings();
                        break;
                    case 3:
                        QuitGame();
                        break;
                }
                return;
            }

            switch (selectedOption)
            {
                case 0:
                    Resume();
                    break;
                case 1:
                    Save();
                    break;
                case 2:
                    RequestLoad();
                    break;
                case 3:
                    OpenSettings();
                    break;
                case 4:
                    ReturnToTitle();
                    break;
                case 5:
                    QuitGame();
                    break;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();
            float scale = Mathf.Max(
                0.72f,
                Mathf.Min(Screen.width / 1920f, Screen.height / 1080f) *
                UserInterfaceScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawPanel(
                new Rect(0f, 0f, width, height),
                new Color(0.008f, 0.014f, 0.02f, showTitle ? 0.93f : 0.8f));
            Rect panel = new(
                (width - 650f) * 0.5f,
                (height - (showSettings ? 790f : 720f)) * 0.5f,
                650f,
                showSettings ? 790f : 720f);
            DrawPanel(panel, Night);
            DrawPanel(
                new Rect(panel.x, panel.y, 8f, panel.height),
                showSettings ? Amber : Teal);
            DrawHeader(panel);

            if (showSettings)
            {
                DrawSettings(panel);
            }
            else if (showTitle)
            {
                DrawTitle(panel);
            }
            else
            {
                DrawPauseMenu(panel);
            }

            DrawStatus(panel);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawHeader(Rect panel)
        {
            string heading = showSettings
                ? "Settings"
                : showTitle
                    ? "Margins"
                    : "Paused";
            string subheading = showSettings
                ? "Make the game comfortable, then get back to work."
                : showTitle
                    ? "Build the business. Then build the company."
                    : "The business is waiting.";
            GUI.Label(
                new Rect(panel.x + 46f, panel.y + 36f, panel.width - 92f, 22f),
                showTitle ? "OWNER / OPERATOR" : "MARGINS",
                brandStyle);
            GUI.Label(
                new Rect(panel.x + 46f, panel.y + 68f, panel.width - 92f, 52f),
                heading,
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 46f, panel.y + 124f, panel.width - 92f, 30f),
                subheading,
                subtitleStyle);
        }

        private void DrawPauseMenu(Rect panel)
        {
            float y = panel.y + 190f;
            DrawMenuButton(panel, ref y, 0, "Resume");
            DrawMenuButton(panel, ref y, 1, "Save Company");
            DrawMenuButton(
                panel,
                ref y,
                2,
                loadConfirmationPending ? "Confirm Load" : "Load Company");
            DrawMenuButton(panel, ref y, 3, "Settings");
            DrawMenuButton(panel, ref y, 4, "Return to Title");
            DrawMenuButton(panel, ref y, 5, "Quit to Desktop");
            GUI.Label(
                new Rect(panel.x + 46f, panel.y + panel.height - 48f, panel.width - 92f, 24f),
                "Arrow keys or W/S navigate  •  Enter selects  •  Esc resumes",
                smallStyle);
        }

        private void DrawTitle(Rect panel)
        {
            float y = panel.y + 212f;
            DrawMenuButton(panel, ref y, 0, "Enter Store");
            DrawMenuButton(
                panel,
                ref y,
                1,
                loadConfirmationPending ? "Confirm Load" : "Load Company",
                persistence != null && persistence.HasSaveFile);
            DrawMenuButton(panel, ref y, 2, "Settings");
            DrawMenuButton(panel, ref y, 3, "Quit to Desktop");

            Rect callout = new(
                panel.x + 46f,
                panel.y + panel.height - 156f,
                panel.width - 92f,
                78f);
            DrawPanel(callout, NightSoft);
            GUI.Label(
                new Rect(callout.x + 20f, callout.y + 13f, callout.width - 40f, 22f),
                "FIRST-PERSON BUSINESS SIMULATION",
                brandStyle);
            GUI.Label(
                new Rect(callout.x + 20f, callout.y + 40f, callout.width - 40f, 24f),
                "Operate on the floor. Step back to manage. Grow beyond yourself.",
                smallStyle);
        }

        private void DrawSettings(Rect panel)
        {
            float y = panel.y + 178f;
            DrawSliderSetting(
                panel,
                ref y,
                0,
                "Horizontal look",
                ref horizontalSensitivity,
                0.01f,
                0.5f,
                horizontalSensitivity.ToString("0.00"));
            DrawSliderSetting(
                panel,
                ref y,
                1,
                "Vertical look",
                ref verticalSensitivity,
                0.01f,
                0.5f,
                verticalSensitivity.ToString("0.00"));
            DrawToggleSetting(panel, ref y, 2, "Invert vertical look", ref invertY);
            DrawToggleSetting(panel, ref y, 3, "Camera motion", ref cameraMotion);
            DrawToggleSetting(panel, ref y, 4, "Fullscreen", ref fullscreen);
            DrawSliderSetting(
                panel,
                ref y,
                5,
                "Interface scale",
                ref interfaceScale,
                0.85f,
                1.25f,
                $"{interfaceScale * 100f:0}%");
            DrawSliderSetting(
                panel,
                ref y,
                6,
                "Master volume",
                ref masterVolume,
                0f,
                1f,
                $"{masterVolume * 100f:0}%");
            y += 8f;
            DrawMenuButton(panel, ref y, 7, "Apply Settings", true, 54f);
            DrawMenuButton(panel, ref y, 8, "Back", true, 54f);
        }

        private void DrawSliderSetting(
            Rect panel,
            ref float y,
            int option,
            string label,
            ref float value,
            float minimum,
            float maximum,
            string formattedValue)
        {
            Rect row = new(panel.x + 46f, y, panel.width - 92f, 67f);
            DrawPanel(
                row,
                selectedOption == option ? NightRaised : NightSoft);
            GUI.Label(
                new Rect(row.x + 18f, row.y + 9f, row.width - 128f, 24f),
                label,
                bodyStyle);
            GUI.Label(
                new Rect(row.x + row.width - 98f, row.y + 9f, 80f, 24f),
                formattedValue,
                valueStyle);
            value = GUI.HorizontalSlider(
                new Rect(row.x + 18f, row.y + 43f, row.width - 36f, 18f),
                value,
                minimum,
                maximum);
            if (Event.current.type == EventType.MouseDown &&
                row.Contains(Event.current.mousePosition))
            {
                selectedOption = option;
            }
            y += 76f;
        }

        private void DrawToggleSetting(
            Rect panel,
            ref float y,
            int option,
            string label,
            ref bool value)
        {
            Rect row = new(panel.x + 46f, y, panel.width - 92f, 54f);
            DrawPanel(
                row,
                selectedOption == option ? NightRaised : NightSoft);
            GUI.Label(
                new Rect(row.x + 18f, row.y + 15f, row.width - 118f, 24f),
                label,
                bodyStyle);
            Rect toggle = new(row.x + row.width - 90f, row.y + 11f, 72f, 32f);
            DrawPanel(toggle, value ? Teal : new Color(0.22f, 0.27f, 0.28f, 1f));
            GUI.Label(toggle, value ? "ON" : "OFF", valueStyle);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                selectedOption = option;
                value = !value;
            }
            y += 63f;
        }

        private void DrawMenuButton(
            Rect panel,
            ref float y,
            int option,
            string label,
            bool enabled = true,
            float height = 60f)
        {
            Rect button = new(panel.x + 46f, y, panel.width - 92f, height);
            bool selected = selectedOption == option;
            Color color = !enabled
                ? new Color(0.055f, 0.07f, 0.075f, 0.7f)
                : selected
                    ? new Color(0.09f, 0.28f, 0.26f, 1f)
                    : NightSoft;
            DrawPanel(button, color);
            if (selected)
            {
                DrawPanel(new Rect(button.x, button.y, 6f, button.height), Teal);
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (GUI.Button(
                    button,
                    enabled ? label : $"{label}  —  No save found",
                    selected ? buttonSelectedStyle : buttonStyle))
            {
                selectedOption = option;
                ActivateSelectedOption();
            }
            GUI.enabled = previousEnabled;
            y += height + 10f;
        }

        private void DrawStatus(Rect panel)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            Rect statusRect = new(
                panel.x + 46f,
                panel.y + panel.height - 92f,
                panel.width - 92f,
                50f);
            DrawPanel(statusRect, NightRaised);
            DrawPanel(
                new Rect(statusRect.x, statusRect.y, 6f, statusRect.height),
                statusSucceeded ? Teal : Error);
            GUI.Label(
                new Rect(
                    statusRect.x + 18f,
                    statusRect.y + 9f,
                    statusRect.width - 36f,
                    32f),
                status,
                smallStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            brandStyle = CreateStyle(13, FontStyle.Bold, Teal);
            titleStyle = CreateStyle(42, FontStyle.Bold, Ink);
            subtitleStyle = CreateStyle(17, FontStyle.Normal, MutedInk);
            bodyStyle = CreateStyle(18, FontStyle.Normal, Ink);
            smallStyle = CreateStyle(14, FontStyle.Normal, MutedInk);
            valueStyle = CreateStyle(
                15,
                FontStyle.Bold,
                Ink,
                TextAnchor.MiddleCenter);
            buttonStyle = CreateStyle(
                19,
                FontStyle.Normal,
                Ink,
                TextAnchor.MiddleLeft);
            buttonStyle.padding = new RectOffset(24, 20, 0, 0);
            buttonSelectedStyle = new GUIStyle(buttonStyle)
            {
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle CreateStyle(
            int size,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = color },
                hover = { textColor = color },
                active = { textColor = color },
                focused = { textColor = color }
            };
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void SetStatus(bool success, string message)
        {
            statusSucceeded = success;
            status = message;
        }

        private static string FriendlyPersistenceFailure(
            string diagnostic,
            bool saving)
        {
            string value = diagnostic ?? string.Empty;
            if (value.Contains("held", System.StringComparison.OrdinalIgnoreCase) ||
                value.Contains("holding", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Put down what you are holding, then try again.";
            }
            if (value.Contains("checkout", System.StringComparison.OrdinalIgnoreCase) ||
                value.Contains("transaction", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Finish or cancel the current checkout, then try again.";
            }
            if (value.Contains("no accepted", System.StringComparison.OrdinalIgnoreCase) ||
                value.Contains("does not exist", System.StringComparison.OrdinalIgnoreCase))
            {
                return "No saved company is available yet.";
            }
            if (!saving)
            {
                return "That save could not be opened. Your current company is unchanged.";
            }
            return "The company could not be saved. Finish the current action and try again.";
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
