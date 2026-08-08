using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;

namespace Margins
{
    public readonly struct PlayerBindingEntry
    {
        public PlayerBindingEntry(
            InputAction action,
            int bindingIndex,
            string label,
            string displayValue,
            bool canRebind)
        {
            Action = action;
            BindingIndex = bindingIndex;
            Label = label;
            DisplayValue = displayValue;
            CanRebind = canRebind;
        }

        public InputAction Action { get; }
        public int BindingIndex { get; }
        public string Label { get; }
        public string DisplayValue { get; }
        public bool CanRebind { get; }
        public string StableKey => $"{Action?.id}:{BindingIndex}";
    }

    public enum RebindOutcome
    {
        Completed = 0,
        Cancelled = 1,
        Conflict = 2,
        Failed = 3
    }

    public readonly struct RebindResult
    {
        public RebindResult(
            RebindOutcome outcome,
            PlayerBindingEntry binding,
            string message)
        {
            Outcome = outcome;
            Binding = binding;
            Message = message;
        }

        public RebindOutcome Outcome { get; }
        public PlayerBindingEntry Binding { get; }
        public string Message { get; }
    }

    /// <summary>
    /// Applies and persists overrides on the project's existing Input System asset.
    /// It does not own a second binding definition or gameplay action state.
    /// </summary>
    public sealed class InputBindingSettings : IDisposable
    {
        public const string BindingOverridesKey = "margins.input.binding_overrides";

        private const string PlayerMapName = "Player";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";

        private readonly InputActionAsset inputActions;
        private readonly IGamePreferences preferences;
        private InputActionRebindingExtensions.RebindingOperation rebindOperation;
        private InputAction[] actionsEnabledBeforeRebind = Array.Empty<InputAction>();
        private PlayerBindingEntry activeBinding;

        public InputBindingSettings(
            InputActionAsset inputActions,
            IGamePreferences preferences)
        {
            this.inputActions = inputActions;
            this.preferences = preferences ??
                throw new ArgumentNullException(nameof(preferences));
        }

        public bool IsAvailable => inputActions != null;
        public bool IsRebinding => rebindOperation != null;
        public string ActiveBindingKey => IsRebinding ? activeBinding.StableKey : null;
        public int Revision { get; private set; }

        public bool TryLoad(out string error)
        {
            if (inputActions == null)
            {
                error = "The Input System action asset is not configured.";
                return false;
            }

            inputActions.RemoveAllBindingOverrides();
            if (!preferences.HasKey(BindingOverridesKey))
            {
                Revision++;
                error = null;
                return true;
            }

            string json = preferences.GetString(BindingOverridesKey, string.Empty);
            try
            {
                inputActions.LoadBindingOverridesFromJson(json, true);
            }
            catch (Exception exception)
            {
                inputActions.RemoveAllBindingOverrides();
                preferences.DeleteKey(BindingOverridesKey);
                preferences.Save();
                error = $"Saved control bindings were invalid and defaults were restored: {exception.Message}";
                Revision++;
                return false;
            }

            if (!TryValidateNoConflicts(out error))
            {
                inputActions.RemoveAllBindingOverrides();
                preferences.DeleteKey(BindingOverridesKey);
                preferences.Save();
                error = $"Saved control bindings conflicted and defaults were restored: {error}";
                Revision++;
                return false;
            }

            Revision++;
            error = null;
            return true;
        }

        public void Save()
        {
            if (inputActions == null)
            {
                return;
            }

            preferences.SetString(
                BindingOverridesKey,
                inputActions.SaveBindingOverridesAsJson());
            preferences.Save();
        }

        public void ResetToDefaults()
        {
            CancelCurrentRebind();
            inputActions?.RemoveAllBindingOverrides();
            preferences.DeleteKey(BindingOverridesKey);
            preferences.Save();
            Revision++;
        }

        public IReadOnlyList<PlayerBindingEntry> GetPlayerBindings()
        {
            if (inputActions == null)
            {
                return Array.Empty<PlayerBindingEntry>();
            }

            InputActionMap player = inputActions.FindActionMap(PlayerMapName, false);
            if (player == null)
            {
                return Array.Empty<PlayerBindingEntry>();
            }

            List<PlayerBindingEntry> result = new();
            Dictionary<string, int> labelCounts = new(StringComparer.Ordinal);
            foreach (InputAction action in player.actions)
            {
                for (int index = 0; index < action.bindings.Count; index++)
                {
                    InputBinding binding = action.bindings[index];
                    if (binding.isComposite || !IsKeyboardMouseBinding(binding))
                    {
                        continue;
                    }

                    string baseLabel = BindingLabel(action, binding);
                    labelCounts.TryGetValue(baseLabel, out int priorCount);
                    labelCounts[baseLabel] = priorCount + 1;
                    string label = priorCount == 0
                        ? baseLabel
                        : $"{baseLabel} — Alternate {priorCount + 1}";
                    string display = action.GetBindingDisplayString(
                        index,
                        InputBinding.DisplayStringOptions.DontIncludeInteractions);
                    bool canRebind = !string.Equals(
                        action.name,
                        "Look",
                        StringComparison.Ordinal);
                    result.Add(new PlayerBindingEntry(
                        action,
                        index,
                        label,
                        display,
                        canRebind));
                }
            }

            return result;
        }

        public bool TryApplyBindingOverride(
            PlayerBindingEntry entry,
            string path,
            out string error)
        {
            if (!TryValidateEntry(entry, out error))
            {
                return false;
            }

            string canonicalPath = CanonicalPath(path);
            if (!IsKeyboardMousePath(canonicalPath))
            {
                error = "Choose a keyboard or mouse control for this binding.";
                return false;
            }

            InputActionMap player = inputActions.FindActionMap(PlayerMapName, false);
            foreach (InputAction action in player.actions)
            {
                for (int index = 0; index < action.bindings.Count; index++)
                {
                    InputBinding binding = action.bindings[index];
                    if (binding.isComposite || !IsKeyboardMouseBinding(binding) ||
                        (action == entry.Action && index == entry.BindingIndex))
                    {
                        continue;
                    }

                    if (string.Equals(
                            canonicalPath,
                            CanonicalPath(binding.effectivePath),
                            StringComparison.Ordinal))
                    {
                        error =
                            $"That control is already assigned to {BindingLabel(action, binding)}. " +
                            "Choose another control or reset bindings first.";
                        return false;
                    }
                }
            }

            entry.Action.ApplyBindingOverride(entry.BindingIndex, path);
            Revision++;
            error = null;
            return true;
        }

        public bool BeginInteractiveRebind(
            PlayerBindingEntry entry,
            Action<RebindResult> completed,
            out string error)
        {
            if (IsRebinding)
            {
                error = "Another control is already waiting for input.";
                return false;
            }
            if (!entry.CanRebind)
            {
                error = "Pointer look is configured through look sensitivity.";
                return false;
            }
            if (!TryValidateEntry(entry, out error))
            {
                return false;
            }

            activeBinding = entry;
            InputActionMap map = entry.Action.actionMap;
            actionsEnabledBeforeRebind = map.actions
                .Where(action => action.enabled)
                .ToArray();
            map.Disable();

            string selectedPath = null;
            try
            {
                rebindOperation = entry.Action
                    .PerformInteractiveRebinding(entry.BindingIndex)
                    .WithCancelingThrough("<Keyboard>/escape")
                    .WithActionEventNotificationsBeingSuppressed()
                    .OnApplyBinding((_, path) => selectedPath = path)
                    .OnCancel(_ => FinishCancelled(completed))
                    .OnComplete(_ => FinishCompleted(selectedPath, completed));
                rebindOperation.Start();
            }
            catch (Exception exception)
            {
                DisposeOperationAndRestoreActions();
                error = $"Rebinding could not start: {exception.Message}";
                return false;
            }

            error = null;
            return true;
        }

        public void CancelCurrentRebind()
        {
            rebindOperation?.Cancel();
        }

        public void Dispose()
        {
            if (rebindOperation != null)
            {
                rebindOperation.Cancel();
                if (rebindOperation != null)
                {
                    DisposeOperationAndRestoreActions();
                }
            }
        }

        private void FinishCancelled(Action<RebindResult> completed)
        {
            PlayerBindingEntry binding = activeBinding;
            DisposeOperationAndRestoreActions();
            completed?.Invoke(new RebindResult(
                RebindOutcome.Cancelled,
                binding,
                "Rebind canceled."));
        }

        private void FinishCompleted(
            string selectedPath,
            Action<RebindResult> completed)
        {
            PlayerBindingEntry binding = activeBinding;
            bool applied = TryApplyBindingOverride(
                binding,
                selectedPath,
                out string error);
            string display = applied
                ? binding.Action.GetBindingDisplayString(binding.BindingIndex)
                : null;
            DisposeOperationAndRestoreActions();

            if (!applied)
            {
                completed?.Invoke(new RebindResult(
                    RebindOutcome.Conflict,
                    binding,
                    error));
                return;
            }

            completed?.Invoke(new RebindResult(
                RebindOutcome.Completed,
                binding,
                $"{binding.Label} is now {display}."));
        }

        private void DisposeOperationAndRestoreActions()
        {
            InputActionRebindingExtensions.RebindingOperation operation =
                rebindOperation;
            rebindOperation = null;
            operation?.Dispose();
            foreach (InputAction action in actionsEnabledBeforeRebind)
            {
                action.Enable();
            }
            actionsEnabledBeforeRebind = Array.Empty<InputAction>();
            activeBinding = default;
        }

        private bool TryValidateEntry(
            PlayerBindingEntry entry,
            out string error)
        {
            if (inputActions == null || entry.Action == null ||
                entry.Action.actionMap?.asset != inputActions)
            {
                error = "The selected control does not belong to the configured action asset.";
                return false;
            }
            if (entry.BindingIndex < 0 ||
                entry.BindingIndex >= entry.Action.bindings.Count ||
                entry.Action.bindings[entry.BindingIndex].isComposite)
            {
                error = "The selected control binding is not rebindable.";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryValidateNoConflicts(out string error)
        {
            InputActionMap player = inputActions.FindActionMap(PlayerMapName, false);
            if (player == null)
            {
                error = $"Input action map '{PlayerMapName}' is missing.";
                return false;
            }

            Dictionary<string, string> owners = new(StringComparer.Ordinal);
            foreach (InputAction action in player.actions)
            {
                for (int index = 0; index < action.bindings.Count; index++)
                {
                    InputBinding binding = action.bindings[index];
                    if (binding.isComposite || !IsKeyboardMouseBinding(binding))
                    {
                        continue;
                    }

                    string path = CanonicalPath(binding.effectivePath);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    string label = BindingLabel(action, binding);
                    if (owners.TryGetValue(path, out string existing))
                    {
                        error = $"{existing} and {label} both use the same control.";
                        return false;
                    }
                    owners[path] = label;
                }
            }

            error = null;
            return true;
        }

        private static bool IsKeyboardMouseBinding(InputBinding binding)
        {
            return !string.IsNullOrEmpty(binding.groups) &&
                   binding.groups.IndexOf(
                       KeyboardMouseGroup,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsKeyboardMousePath(string canonicalPath)
        {
            return canonicalPath.StartsWith("keyboard/", StringComparison.Ordinal) ||
                   canonicalPath.StartsWith("mouse/", StringComparison.Ordinal) ||
                   canonicalPath.StartsWith("pointer/", StringComparison.Ordinal);
        }

        private static string CanonicalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Trim()
                .TrimStart('/')
                .Replace("<", string.Empty)
                .Replace(">", string.Empty)
                .ToLowerInvariant();
        }

        private static string BindingLabel(InputAction action, InputBinding binding)
        {
            if (binding.isPartOfComposite)
            {
                return binding.name.ToLowerInvariant() switch
                {
                    "up" => "Move Forward",
                    "down" => "Move Backward",
                    "left" => "Move Left",
                    "right" => "Move Right",
                    _ => $"{PlayerFacingActionLabel(action.name)} — {Humanize(binding.name)}"
                };
            }

            return PlayerFacingActionLabel(action.name);
        }

        private static string PlayerFacingActionLabel(string actionName)
        {
            return actionName switch
            {
                "Interact" => "Interact / Select",
                "Cancel" => "Place / Cancel / Put Down",
                _ => Humanize(actionName)
            };
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder result = new();
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (index > 0 && char.IsUpper(current) &&
                    !char.IsWhiteSpace(value[index - 1]))
                {
                    result.Append(' ');
                }
                result.Append(current);
            }
            return result.ToString();
        }
    }
}
