using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins.Tests
{
    public sealed class GameMenuModelsEditModeTests
    {
        [Test]
        public void SensitivityScaleMapsOneFiveAndTenDeterministically()
        {
            Assert.That(GameSettingsModel.SensitivityForLevel(1), Is.EqualTo(0.02f));
            Assert.That(GameSettingsModel.SensitivityForLevel(5), Is.EqualTo(0.1f));
            Assert.That(GameSettingsModel.SensitivityForLevel(10), Is.EqualTo(0.5f));
            Assert.That(GameSettingsModel.LevelForSensitivity(0.1f), Is.EqualTo(5));
            Assert.That(GameSettingsModel.SensitivityDescription(1), Is.EqualTo("Very low"));
            Assert.That(GameSettingsModel.SensitivityDescription(5), Is.EqualTo("Medium"));
            Assert.That(GameSettingsModel.SensitivityDescription(10), Is.EqualTo("Very high"));
        }

        [Test]
        public void SettingsPersistAndReloadOnPlayerFacingScale()
        {
            MemoryPreferences preferences = new();
            GameSettingsModel expected = new()
            {
                LookSensitivityLevel = 8,
                InvertY = true,
                CameraMotion = false,
                MasterVolume = 0.65f,
                Fullscreen = true,
                InterfaceScale = 1.15f
            };

            expected.Save(preferences);
            GameSettingsModel loaded = GameSettingsModel.Load(
                preferences,
                0.1f,
                false);

            Assert.That(loaded.LookSensitivityLevel, Is.EqualTo(8));
            Assert.That(loaded.UnderlyingLookSensitivity, Is.EqualTo(0.28f));
            Assert.That(loaded.InvertY, Is.True);
            Assert.That(loaded.CameraMotion, Is.False);
            Assert.That(loaded.MasterVolume, Is.EqualTo(0.65f));
            Assert.That(loaded.Fullscreen, Is.True);
            Assert.That(loaded.InterfaceScale, Is.EqualTo(1.15f));
            Assert.That(preferences.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MenuTabsReturnToTheirTitleOrPauseOrigin()
        {
            GameMenuStateModel state = new();
            state.ShowTitleAtLaunch();
            state.OpenSettings();
            Assert.That(state.Screen, Is.EqualTo(GameMenuScreen.SettingsGeneral));
            state.SelectSettingsTab(true);
            Assert.That(state.Screen, Is.EqualTo(GameMenuScreen.SettingsControls));
            state.CloseSettings();
            Assert.That(state.Screen, Is.EqualTo(GameMenuScreen.Title));

            state.EnterSession();
            state.OpenPause();
            state.OpenSettings();
            state.SelectSettingsTab(true);
            state.CloseSettings();
            Assert.That(state.Screen, Is.EqualTo(GameMenuScreen.Pause));
        }

        [Test]
        public void ActiveSessionRequiresExplicitReplacementConfirmation()
        {
            GameMenuStateModel state = new();
            state.EnterSession();
            state.ReturnToTitle();

            Assert.That(
                state.ConfirmOrArmReplacement(
                    SessionReplacementAction.NewBusiness),
                Is.False);
            Assert.That(
                state.PendingReplacement,
                Is.EqualTo(SessionReplacementAction.NewBusiness));
            Assert.That(
                state.ConfirmOrArmReplacement(
                    SessionReplacementAction.NewBusiness),
                Is.True);
            Assert.That(
                state.PendingReplacement,
                Is.EqualTo(SessionReplacementAction.None));
        }

        [Test]
        public void SettingsSavedNotificationExpiresWithoutHidingErrors()
        {
            MenuNotificationModel notification = new();
            notification.ShowTransient(
                "Settings saved.",
                MenuNotificationKind.Success,
                10f,
                3f);
            Assert.That(notification.Tick(12.99f), Is.False);
            Assert.That(notification.IsVisible, Is.True);
            Assert.That(notification.Tick(13.01f), Is.True);
            Assert.That(notification.IsVisible, Is.False);

            notification.ShowPersistent(
                "An important error.",
                MenuNotificationKind.Error);
            Assert.That(notification.Tick(1000f), Is.False);
            Assert.That(notification.IsVisible, Is.True);
            Assert.That(notification.IsPersistent, Is.True);
        }

        [Test]
        public void BindingOverridesPersistLoadResetAndRejectConflicts()
        {
            MemoryPreferences preferences = new();
            InputActionAsset firstAsset = CreateInputAsset();
            InputActionAsset secondAsset = InputActionAsset.FromJson(firstAsset.ToJson());
            InputBindingSettings first = new(firstAsset, preferences);
            Assert.That(first.TryLoad(out string error), Is.True, error);

            PlayerBindingEntry jump = first.GetPlayerBindings()
                .Single(entry => entry.Label == "Jump");
            Assert.That(
                first.TryApplyBindingOverride(
                    jump,
                    "<Keyboard>/j",
                    out error),
                Is.True,
                error);
            first.Save();
            Assert.That(
                jump.Action.bindings[jump.BindingIndex].effectivePath,
                Is.EqualTo("<Keyboard>/j"));

            InputBindingSettings second = new(secondAsset, preferences);
            Assert.That(second.TryLoad(out error), Is.True, error);
            PlayerBindingEntry restoredJump = second.GetPlayerBindings()
                .Single(entry => entry.Label == "Jump");
            Assert.That(
                restoredJump.Action.bindings[restoredJump.BindingIndex].effectivePath,
                Is.EqualTo("<Keyboard>/j"));

            Assert.That(
                second.TryApplyBindingOverride(
                    restoredJump,
                    "<Keyboard>/e",
                    out error),
                Is.False);
            Assert.That(error, Does.Contain("already assigned to Interact"));
            Assert.That(
                restoredJump.Action.bindings[restoredJump.BindingIndex].effectivePath,
                Is.EqualTo("<Keyboard>/j"));

            second.ResetToDefaults();
            Assert.That(
                restoredJump.Action.bindings[restoredJump.BindingIndex].effectivePath,
                Is.EqualTo("<Keyboard>/space"));
            Assert.That(
                preferences.HasKey(InputBindingSettings.BindingOverridesKey),
                Is.False);

            first.Dispose();
            second.Dispose();
            UnityEngine.Object.DestroyImmediate(firstAsset);
            UnityEngine.Object.DestroyImmediate(secondAsset);
        }

        internal static InputActionAsset CreateInputAsset()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap player = new("Player");
            asset.AddActionMap(player);

            InputAction move = player.AddAction(
                "Move",
                InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", "Keyboard&Mouse")
                .With("Down", "<Keyboard>/s", "Keyboard&Mouse")
                .With("Left", "<Keyboard>/a", "Keyboard&Mouse")
                .With("Right", "<Keyboard>/d", "Keyboard&Mouse");
            InputAction look = player.AddAction(
                "Look",
                InputActionType.Value);
            look.AddBinding("<Pointer>/delta", groups: "Keyboard&Mouse");
            InputAction jump = player.AddAction(
                "Jump",
                InputActionType.Button);
            jump.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
            InputAction interact = player.AddAction(
                "Interact",
                InputActionType.Button);
            interact.AddBinding("<Keyboard>/e", groups: "Keyboard&Mouse");
            return asset;
        }

        internal sealed class MemoryPreferences : IGamePreferences
        {
            private readonly Dictionary<string, object> values = new();

            public int SaveCount { get; private set; }

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
            public void Save() => SaveCount++;
        }
    }
}
