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
