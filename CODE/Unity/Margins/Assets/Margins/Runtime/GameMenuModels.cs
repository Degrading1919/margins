using System;
using UnityEngine;

namespace Margins
{
    public interface IGamePreferences
    {
        bool HasKey(string key);
        int GetInt(string key, int defaultValue);
        float GetFloat(string key, float defaultValue);
        string GetString(string key, string defaultValue);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }

    public sealed class PlayerPrefsGamePreferences : IGamePreferences
    {
        public static PlayerPrefsGamePreferences Instance { get; } = new();

        private PlayerPrefsGamePreferences()
        {
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public int GetInt(string key, int defaultValue) =>
            PlayerPrefs.GetInt(key, defaultValue);
        public float GetFloat(string key, float defaultValue) =>
            PlayerPrefs.GetFloat(key, defaultValue);
        public string GetString(string key, string defaultValue) =>
            PlayerPrefs.GetString(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }

    [Serializable]
    public sealed class GameSettingsModel
    {
        private const string LookLevelKey = "margins.look_sensitivity_level";
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

        private static readonly float[] SensitivityByLevel =
        {
            0.02f,
            0.035f,
            0.05f,
            0.075f,
            0.1f,
            0.14f,
            0.2f,
            0.28f,
            0.38f,
            0.5f
        };

        public int LookSensitivityLevel { get; set; } = 5;
        public bool InvertY { get; set; }
        public bool CameraMotion { get; set; } = true;
        public float MasterVolume { get; set; } = 0.8f;
        public bool Fullscreen { get; set; }
        public float InterfaceScale { get; set; } = 1f;

        public float UnderlyingLookSensitivity =>
            SensitivityForLevel(LookSensitivityLevel);

        public static GameSettingsModel Load(
            IGamePreferences preferences,
            float defaultSensitivity,
            bool defaultFullscreen)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            int lookLevel;
            if (preferences.HasKey(LookLevelKey))
            {
                lookLevel = preferences.GetInt(LookLevelKey, 5);
            }
            else
            {
                float legacy = preferences.GetFloat(
                    LegacySensitivityKey,
                    defaultSensitivity);
                float horizontal = preferences.GetFloat(
                    HorizontalSensitivityKey,
                    legacy);
                float vertical = preferences.GetFloat(
                    VerticalSensitivityKey,
                    legacy);
                lookLevel = LevelForSensitivity((horizontal + vertical) * 0.5f);
            }

            GameSettingsModel result = new()
            {
                LookSensitivityLevel = lookLevel,
                InvertY = preferences.GetInt(InvertYKey, 0) != 0,
                CameraMotion = preferences.GetInt(CameraMotionKey, 1) != 0,
                MasterVolume = preferences.GetFloat(VolumeKey, 0.8f),
                Fullscreen = preferences.GetInt(
                    FullscreenKey,
                    defaultFullscreen ? 1 : 0) != 0,
                InterfaceScale = preferences.GetFloat(InterfaceScaleKey, 1f)
            };
            result.Clamp();
            return result;
        }

        public void Save(IGamePreferences preferences)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            Clamp();
            float sensitivity = UnderlyingLookSensitivity;
            preferences.SetInt(LookLevelKey, LookSensitivityLevel);
            preferences.SetFloat(LegacySensitivityKey, sensitivity);
            preferences.SetFloat(HorizontalSensitivityKey, sensitivity);
            preferences.SetFloat(VerticalSensitivityKey, sensitivity);
            preferences.SetInt(InvertYKey, InvertY ? 1 : 0);
            preferences.SetInt(CameraMotionKey, CameraMotion ? 1 : 0);
            preferences.SetFloat(VolumeKey, MasterVolume);
            preferences.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            preferences.SetFloat(InterfaceScaleKey, InterfaceScale);
            preferences.Save();
        }

        public GameSettingsModel Clone()
        {
            return new GameSettingsModel
            {
                LookSensitivityLevel = LookSensitivityLevel,
                InvertY = InvertY,
                CameraMotion = CameraMotion,
                MasterVolume = MasterVolume,
                Fullscreen = Fullscreen,
                InterfaceScale = InterfaceScale
            };
        }

        public void Clamp()
        {
            LookSensitivityLevel = Mathf.Clamp(LookSensitivityLevel, 1, 10);
            MasterVolume = Mathf.Clamp01(MasterVolume);
            InterfaceScale = Mathf.Clamp(InterfaceScale, 0.85f, 1.25f);
        }

        public static float SensitivityForLevel(int level)
        {
            int accepted = Mathf.Clamp(level, 1, 10);
            return SensitivityByLevel[accepted - 1];
        }

        public static int LevelForSensitivity(float sensitivity)
        {
            int closestLevel = 1;
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < SensitivityByLevel.Length; index++)
            {
                float distance = Mathf.Abs(SensitivityByLevel[index] - sensitivity);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLevel = index + 1;
                }
            }

            return closestLevel;
        }

        public static string SensitivityDescription(int level)
        {
            return Mathf.Clamp(level, 1, 10) switch
            {
                1 => "Very low",
                2 or 3 => "Low",
                4 => "Medium low",
                5 => "Medium",
                6 or 7 => "High",
                8 or 9 => "Very high",
                _ => "Very high"
            };
        }
    }

    public enum GameMenuScreen
    {
        Closed = 0,
        Title = 1,
        Pause = 2,
        SettingsGeneral = 3,
        SettingsControls = 4
    }

    public enum SessionReplacementAction
    {
        None = 0,
        NewBusiness = 1,
        LoadBusiness = 2
    }

    public sealed class GameMenuStateModel
    {
        private GameMenuScreen settingsOrigin = GameMenuScreen.Pause;

        public GameMenuScreen Screen { get; private set; } = GameMenuScreen.Closed;
        public bool HasActiveSession { get; private set; }
        public SessionReplacementAction PendingReplacement { get; private set; }
        public bool IsSettings =>
            Screen == GameMenuScreen.SettingsGeneral ||
            Screen == GameMenuScreen.SettingsControls;

        public void ShowTitleAtLaunch()
        {
            Screen = GameMenuScreen.Title;
            HasActiveSession = false;
            PendingReplacement = SessionReplacementAction.None;
        }

        public void ReturnToTitle()
        {
            Screen = GameMenuScreen.Title;
            PendingReplacement = SessionReplacementAction.None;
        }

        public void OpenPause()
        {
            Screen = GameMenuScreen.Pause;
            HasActiveSession = true;
            PendingReplacement = SessionReplacementAction.None;
        }

        public void EnterSession()
        {
            Screen = GameMenuScreen.Closed;
            HasActiveSession = true;
            PendingReplacement = SessionReplacementAction.None;
        }

        public void OpenSettings()
        {
            settingsOrigin = Screen == GameMenuScreen.Title
                ? GameMenuScreen.Title
                : GameMenuScreen.Pause;
            Screen = GameMenuScreen.SettingsGeneral;
            PendingReplacement = SessionReplacementAction.None;
        }

        public void SelectSettingsTab(bool controls)
        {
            if (!IsSettings)
            {
                return;
            }

            Screen = controls
                ? GameMenuScreen.SettingsControls
                : GameMenuScreen.SettingsGeneral;
        }

        public void CloseSettings()
        {
            if (IsSettings)
            {
                Screen = settingsOrigin;
            }
            PendingReplacement = SessionReplacementAction.None;
        }

        public bool ConfirmOrArmReplacement(SessionReplacementAction action)
        {
            if (!HasActiveSession)
            {
                PendingReplacement = SessionReplacementAction.None;
                return true;
            }

            if (PendingReplacement == action)
            {
                PendingReplacement = SessionReplacementAction.None;
                return true;
            }

            PendingReplacement = action;
            return false;
        }

        public void ClearPendingReplacement()
        {
            PendingReplacement = SessionReplacementAction.None;
        }
    }

    public enum MenuNotificationKind
    {
        Information = 0,
        Success = 1,
        Error = 2
    }

    public sealed class MenuNotificationModel
    {
        private float hideAt = float.PositiveInfinity;

        public string Message { get; private set; }
        public MenuNotificationKind Kind { get; private set; }
        public bool IsVisible => !string.IsNullOrWhiteSpace(Message);
        public bool IsPersistent => IsVisible && float.IsPositiveInfinity(hideAt);

        public void ShowTransient(
            string message,
            MenuNotificationKind kind,
            float now,
            float duration)
        {
            Message = message;
            Kind = kind;
            hideAt = now + Mathf.Max(0.1f, duration);
        }

        public void ShowPersistent(string message, MenuNotificationKind kind)
        {
            Message = message;
            Kind = kind;
            hideAt = float.PositiveInfinity;
        }

        public bool Tick(float now)
        {
            if (!IsVisible || IsPersistent || now < hideAt)
            {
                return false;
            }

            Clear();
            return true;
        }

        public void Clear()
        {
            Message = null;
            Kind = MenuNotificationKind.Information;
            hideAt = float.PositiveInfinity;
        }
    }
}
