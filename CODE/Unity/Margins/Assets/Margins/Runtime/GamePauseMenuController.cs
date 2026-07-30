using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    /// <summary>
    /// Normal game-menu input ownership for the playable build. Save/load use
    /// the same persistence controller as the world and company state.
    /// </summary>
    public sealed class GamePauseMenuController : MonoBehaviour
    {
        private const string SensitivityKey = "margins.look_sensitivity";
        private const string InvertYKey = "margins.invert_y";
        private const string CameraMotionKey = "margins.camera_motion";
        private const string VolumeKey = "margins.master_volume";
        private const string FullscreenKey = "margins.fullscreen";

        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private FirstStoreDiskPersistenceController persistence;

        private bool isOpen;
        private bool showSettings;
        private bool showTitle;
        private bool gameplayModeBeforeMenu;
        private float sensitivity;
        private bool invertY;
        private bool cameraMotion;
        private float masterVolume;
        private bool fullscreen;
        private string status;
        private bool statusSucceeded = true;
        private int lastEscapeFrame = -1;

        public static bool IsAnyMenuOpen { get; private set; }
        public bool IsOpen => isOpen || showTitle;

        private void Awake()
        {
            sensitivity = PlayerPrefs.GetFloat(
                SensitivityKey,
                firstPersonController != null
                    ? firstPersonController.MouseSensitivity
                    : 0.1f);
            invertY = PlayerPrefs.GetInt(InvertYKey, 0) != 0;
            cameraMotion = PlayerPrefs.GetInt(CameraMotionKey, 1) != 0;
            masterVolume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            fullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0) != 0;
            ApplySettings(false);
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
            if (showTitle)
            {
                return;
            }
            if (showSettings)
            {
                showSettings = false;
                return;
            }
            if (isOpen)
            {
                Resume();
            }
            else
            {
                OpenMenu();
            }
        }

        public void OpenMenu()
        {
            if (firstPersonController == null || persistence == null)
            {
                status = "Menu references are unavailable.";
                statusSucceeded = false;
                return;
            }

            gameplayModeBeforeMenu = firstPersonController.IsGameplayMode;
            isOpen = true;
            showSettings = false;
            showTitle = false;
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            firstPersonController.SetGameplayMode(false);
        }

        public void Resume()
        {
            isOpen = false;
            showSettings = false;
            showTitle = false;
            IsAnyMenuOpen = false;
            Time.timeScale = 1f;
            firstPersonController?.SetGameplayMode(gameplayModeBeforeMenu);
        }

        private void ReturnToTitle()
        {
            isOpen = false;
            showSettings = false;
            showTitle = true;
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            firstPersonController?.SetGameplayMode(false);
            status = "Game paused at title. Your running company remains in memory.";
            statusSucceeded = true;
        }

        private void Save()
        {
            statusSucceeded = persistence.TrySave();
            status = persistence.LastDiagnostic;
        }

        private void Load()
        {
            statusSucceeded = persistence.TryLoad();
            status = persistence.LastDiagnostic;
        }

        private void ApplySettings(bool persist)
        {
            firstPersonController?.ApplyPlayerSettings(
                sensitivity,
                invertY,
                cameraMotion);
            AudioListener.volume = Mathf.Clamp01(masterVolume);
            if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }

            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
            PlayerPrefs.SetInt(InvertYKey, invertY ? 1 : 0);
            PlayerPrefs.SetInt(CameraMotionKey, cameraMotion ? 1 : 0);
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            status = "Settings applied and saved.";
            statusSucceeded = true;
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent != null &&
                currentEvent.type == EventType.KeyDown &&
                currentEvent.keyCode == KeyCode.Escape)
            {
                HandleEscapePressed();
                currentEvent.Use();
            }

            if (!IsOpen)
            {
                return;
            }

            float width = Mathf.Min(620f, Screen.width - 40f);
            float height = showSettings ? 610f : showTitle ? 520f : 600f;
            Rect panel = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(
                panel.x + 34f,
                panel.y + 28f,
                panel.width - 68f,
                panel.height - 56f));
            GUILayout.Label(
                showTitle ? "MARGINS" : showSettings ? "SETTINGS" : "GAME MENU",
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 32,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                },
                GUILayout.Height(52f));
            GUILayout.Space(14f);

            if (showSettings)
            {
                DrawSettings();
            }
            else if (showTitle)
            {
                DrawTitle();
            }
            else
            {
                DrawPauseMenu();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                Color previous = GUI.color;
                GUI.color = statusSucceeded
                    ? new Color(0.65f, 1f, 0.78f)
                    : new Color(1f, 0.62f, 0.55f);
                GUILayout.Space(16f);
                GUILayout.Box(status, GUILayout.MinHeight(52f));
                GUI.color = previous;
            }
            GUILayout.EndArea();
        }

        private void DrawPauseMenu()
        {
            if (MenuButton("RESUME")) Resume();
            if (MenuButton("SAVE GAME")) Save();
            if (MenuButton("LOAD GAME")) Load();
            if (MenuButton("SETTINGS")) showSettings = true;
            if (MenuButton("RETURN TO TITLE")) ReturnToTitle();
            if (MenuButton("QUIT")) QuitGame();
            GUILayout.Space(8f);
            GUILayout.Label("Escape resumes  /  Tab opens company management while playing");
        }

        private void DrawTitle()
        {
            GUILayout.Label(
                "OPERATE  •  SYSTEMIZE  •  DELEGATE  •  EXPAND  •  DEVELOP  •  CONTROL",
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 15
                },
                GUILayout.Height(68f));
            if (MenuButton("CONTINUE COMPANY")) Resume();
            if (MenuButton("LOAD GAME")) Load();
            if (MenuButton("SETTINGS")) showSettings = true;
            if (MenuButton("QUIT")) QuitGame();
        }

        private void DrawSettings()
        {
            GUILayout.Label($"LOOK SENSITIVITY  {sensitivity:0.00}");
            sensitivity = GUILayout.HorizontalSlider(sensitivity, 0.01f, 0.5f);
            GUILayout.Space(14f);
            invertY = GUILayout.Toggle(invertY, "Invert vertical look");
            cameraMotion = GUILayout.Toggle(cameraMotion, "Camera motion / walk bob");
            fullscreen = GUILayout.Toggle(fullscreen, "Fullscreen");
            GUILayout.Space(14f);
            GUILayout.Label($"MASTER VOLUME  {masterVolume * 100f:0}%");
            masterVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);
            GUILayout.Space(24f);
            if (MenuButton("APPLY SETTINGS")) ApplySettings(true);
            if (MenuButton("BACK")) showSettings = false;
        }

        private static bool MenuButton(string label)
        {
            GUILayout.Space(6f);
            return GUILayout.Button(label, GUILayout.Height(48f));
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
