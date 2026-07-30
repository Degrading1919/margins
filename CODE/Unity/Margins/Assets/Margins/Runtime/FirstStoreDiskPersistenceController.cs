using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    [Serializable]
    public sealed class FirstStoreDiskSaveData
    {
        public int version = FirstStoreDiskPersistenceController.CurrentFileVersion;
        public FirstStoreSnapshot firstStore;
        public FirstStorePlayerTransformSnapshot playerTransform;
        public PortfolioProgressionSnapshot portfolio;
    }

    public static class FirstStoreDiskSaveCodec
    {
        public static string ToJson(FirstStoreDiskSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            return JsonUtility.ToJson(saveData, true);
        }

        public static bool TryFromJson(
            string json,
            out FirstStoreDiskSaveData saveData,
            out string error)
        {
            saveData = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "First-store save content is empty.";
                return false;
            }

            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
                !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                error = "First-store save content is not a JSON object.";
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<FirstStoreDiskSaveData>(json);
            }
            catch (Exception exception)
            {
                error = $"First-store save JSON is malformed: {exception.Message}";
                return false;
            }

            if (saveData == null)
            {
                error = "First-store save JSON did not contain an object.";
                return false;
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Temporary, isolated first-store vertical-slice disk persistence.
    /// This is intentionally not a production save-slot or migration architecture.
    /// </summary>
    public sealed class FirstStoreDiskPersistenceController : MonoBehaviour
    {
        public const int LegacyFileVersion = 1;
        public const int CurrentFileVersion = 2;

        [SerializeField] private FirstStorePersistenceMapperComponent persistenceMapper;
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private FirstStoreInteractionController interactionController;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private StagedCheckoutWorldInteractionTarget stagedCheckoutWorldTarget;
        [SerializeField] private PortfolioProgressionController portfolioProgression;
        [SerializeField] private string saveFileName = "first-store-vertical-slice.json";

        public string LastDiagnostic { get; private set; } =
            "No first-store disk save or load has been attempted.";
        public bool LastOperationSucceeded { get; private set; }
        public string SavePath => Path.Combine(
            Application.persistentDataPath,
            "Margins",
            saveFileName);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                TrySave();
            }
            else if (keyboard.f9Key.wasPressedThisFrame)
            {
                TryLoad();
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (persistenceMapper == null ||
                firstPersonController == null ||
                interactionController == null ||
                stagedCheckout == null ||
                stagedCheckoutWorldTarget == null)
            {
                error =
                    "First-store disk persistence requires explicit mapper, player, interaction, and staged-checkout references.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(saveFileName) ||
                Path.GetFileName(saveFileName) != saveFileName)
            {
                error = "First-store disk persistence requires one plain save filename.";
                return false;
            }

            if (!persistenceMapper.TryValidateConfiguration(out error) ||
                !firstPersonController.TryPreflightApplyTransformSnapshot(
                    firstPersonController.CaptureTransformSnapshot(),
                    out error))
            {
                return false;
            }

            if (portfolioProgression != null &&
                (!portfolioProgression.TryValidateConfiguration(out error) ||
                 !portfolioProgression.TryCaptureSnapshot(out _, out error)))
            {
                return false;
            }

            error = null;
            return true;
        }

        public bool TrySave()
        {
            return TrySaveToPath(SavePath);
        }

        public bool TryLoad()
        {
            return TryLoadFromPath(SavePath);
        }

        public bool TrySaveToPath(string path)
        {
            if (!TryValidateConfiguration(out string error) ||
                !TryResolvePath(path, out string acceptedPath, out error))
            {
                return Reject($"Save rejected: {error}");
            }

            if (portfolioProgression != null &&
                !portfolioProgression.TrySynchronizeDetailedShift(out error))
            {
                return Reject($"Save rejected: company synchronization failed: {error}");
            }

            if (persistenceMapper.TryGetDiskSaveBlocker(out string blocker))
            {
                return Reject($"Save rejected: {blocker}");
            }

            if (!persistenceMapper.TryCapture(
                    out FirstStoreSnapshot firstStore,
                    out error))
            {
                return Reject($"Save rejected: {error}");
            }

            FirstStorePlayerTransformSnapshot playerTransform =
                firstPersonController.CaptureTransformSnapshot();
            if (!firstPersonController.TryPreflightApplyTransformSnapshot(
                    playerTransform,
                    out error))
            {
                return Reject($"Save rejected: {error}");
            }

            PortfolioProgressionSnapshot portfolio = null;
            if (portfolioProgression != null &&
                !portfolioProgression.TryCaptureSnapshot(
                    out portfolio,
                    out error))
            {
                return Reject($"Save rejected: {error}");
            }

            FirstStoreDiskSaveData saveData = new()
            {
                firstStore = firstStore,
                playerTransform = playerTransform,
                portfolio = portfolio
            };

            string json;
            try
            {
                json = FirstStoreDiskSaveCodec.ToJson(saveData);
            }
            catch (Exception exception)
            {
                return Reject($"Save serialization failed: {exception.Message}");
            }

            if (!TryWriteAcceptedFile(acceptedPath, json, out error))
            {
                return Reject($"Save write failed: {error}");
            }

            return Accept("Saved first-store state to disk.");
        }

        public bool TryLoadFromPath(string path)
        {
            if (!TryValidateConfiguration(out string error) ||
                !TryResolvePath(path, out string acceptedPath, out error))
            {
                return Reject($"Load rejected: {error}");
            }

            string json;
            try
            {
                if (!File.Exists(acceptedPath))
                {
                    return Reject("Load rejected: no accepted first-store save exists.");
                }

                json = File.ReadAllText(acceptedPath, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                return Reject($"Load read failed: {exception.Message}");
            }

            if (!FirstStoreDiskSaveCodec.TryFromJson(
                    json,
                    out FirstStoreDiskSaveData saveData,
                    out error))
            {
                return Reject($"Load rejected: {error}");
            }

            if (saveData.version != CurrentFileVersion &&
                saveData.version != LegacyFileVersion)
            {
                return Reject(
                    $"Load rejected: unsupported first-store file version {saveData.version}; expected {LegacyFileVersion} or {CurrentFileVersion}.");
            }

            if (saveData.firstStore == null ||
                !persistenceMapper.TryValidateSnapshot(saveData.firstStore, out error) ||
                !firstPersonController.TryPreflightApplyTransformSnapshot(
                    saveData.playerTransform,
                    out error))
            {
                return Reject($"Load rejected: {error ?? "first-store state is missing."}");
            }

            PortfolioProgressionSnapshot acceptedPortfolio = null;
            bool migratedLegacyPortfolio = false;
            if (portfolioProgression != null)
            {
                if (saveData.version == LegacyFileVersion)
                {
                    if (!portfolioProgression.TryCreateLegacyMigrationSnapshot(
                            saveData.firstStore,
                            out acceptedPortfolio,
                            out error))
                    {
                        return Reject($"Load rejected: legacy company migration failed: {error}");
                    }
                    migratedLegacyPortfolio = true;
                }
                else if (saveData.portfolio == null ||
                         !portfolioProgression.TryValidateSnapshot(
                             saveData.portfolio,
                             out error))
                {
                    return Reject(
                        $"Load rejected: {error ?? "portfolio state is missing."}");
                }
                else
                {
                    acceptedPortfolio = saveData.portfolio;
                }

                StoreOperatingSnapshot savedOperating =
                    saveData.firstStore.storeOperating;
                if (savedOperating?.hasResult == true &&
                    (!acceptedPortfolio.firstShiftCompleted ||
                     !string.Equals(
                         acceptedPortfolio.processedDetailedSessionId,
                         savedOperating.sessionId,
                         StringComparison.Ordinal)))
                {
                    return Reject(
                        "Load rejected: detailed first-shift result and portfolio posting disagree.");
                }
            }
            else if (saveData.portfolio != null)
            {
                return Reject(
                    "Load rejected: this scene has no portfolio controller for the saved company state.");
            }

            if (!persistenceMapper.TryCapture(
                    out FirstStoreSnapshot previousFirstStore,
                    out error))
            {
                return Reject($"Load rejected: current state could not be protected: {error}");
            }

            FirstStorePlayerTransformSnapshot previousPlayerTransform =
                firstPersonController.CaptureTransformSnapshot();
            PortfolioProgressionSnapshot previousPortfolio = null;
            if (portfolioProgression != null &&
                !portfolioProgression.TryCaptureSnapshot(
                    out previousPortfolio,
                    out error))
            {
                return Reject(
                    $"Load rejected: current company state could not be protected: {error}");
            }
            if (!persistenceMapper.TryRestore(saveData.firstStore, out error))
            {
                return Reject($"Load rejected: {error}");
            }

            if (portfolioProgression != null &&
                !portfolioProgression.TryRestoreSnapshot(
                    acceptedPortfolio,
                    out error))
            {
                string portfolioError = error;
                bool stateRolledBack = persistenceMapper.TryRestore(
                    previousFirstStore,
                    out string rollbackError);
                bool portfolioRolledBack = portfolioProgression.TryRestoreSnapshot(
                    previousPortfolio,
                    out string portfolioRollbackError);
                if (!stateRolledBack || !portfolioRolledBack)
                {
                    throw new InvalidOperationException(
                        $"Portfolio restore failed ('{portfolioError}') and live-state rollback failed " +
                        $"(store: '{rollbackError ?? "ok"}', portfolio: '{portfolioRollbackError ?? "ok"}').");
                }

                return Reject($"Load rejected: {portfolioError}");
            }

            if (!firstPersonController.TryApplyTransformSnapshot(
                    saveData.playerTransform,
                    out error))
            {
                string playerError = error;
                bool stateRolledBack = persistenceMapper.TryRestore(
                    previousFirstStore,
                    out string rollbackError);
                string portfolioRollbackError = null;
                bool portfolioRolledBack = portfolioProgression == null ||
                    portfolioProgression.TryRestoreSnapshot(
                        previousPortfolio,
                        out portfolioRollbackError);
                bool playerRolledBack = firstPersonController.TryApplyTransformSnapshot(
                    previousPlayerTransform,
                    out string playerRollbackError);
                if (!stateRolledBack || !portfolioRolledBack || !playerRolledBack)
                {
                    throw new InvalidOperationException(
                        $"Player restore failed ('{playerError}') and live-state rollback failed " +
                        $"(state: '{rollbackError ?? "ok"}', portfolio: " +
                        $"'{(portfolioProgression == null ? "ok" : portfolioRollbackError ?? "ok")}', " +
                        $"player: '{playerRollbackError ?? "ok"}').");
                }

                return Reject($"Load rejected: {playerError}");
            }

            stagedCheckout.ResetTransientStateAfterRestore();
            stagedCheckoutWorldTarget.ResetTransientStateAfterRestore();
            interactionController.ResetTransientStateAfterRestore();

            return Accept(
                migratedLegacyPortfolio
                    ? "Loaded first-store state and migrated legacy company progression."
                    : "Loaded first-store and portfolio state from disk.");
        }

        private static bool TryResolvePath(
            string path,
            out string resolvedPath,
            out string error)
        {
            resolvedPath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Save path is empty.";
                return false;
            }

            try
            {
                resolvedPath = Path.GetFullPath(path);
                if (string.IsNullOrWhiteSpace(Path.GetFileName(resolvedPath)))
                {
                    error = "Save path does not identify a file.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = $"Save path is invalid: {exception.Message}";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryWriteAcceptedFile(
            string acceptedPath,
            string json,
            out string error)
        {
            string directory = Path.GetDirectoryName(acceptedPath);
            string temporaryPath = acceptedPath + ".tmp";
            string backupPath = acceptedPath + ".previous";
            bool committed = false;
            try
            {
                Directory.CreateDirectory(directory);
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                using (FileStream stream = new(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(acceptedPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Replace(temporaryPath, acceptedPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, acceptedPath);
                }

                committed = true;
                error = null;
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
            finally
            {
                if (!committed && File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch
                    {
                        // Preserve the original failure as the bounded diagnostic.
                    }
                }
            }

            if (committed && File.Exists(backupPath))
            {
                try
                {
                    File.Delete(backupPath);
                }
                catch
                {
                    // The accepted file is already committed; a stale backup is recoverable.
                }
            }

            return committed;
        }

        private bool Accept(string diagnostic)
        {
            LastOperationSucceeded = true;
            LastDiagnostic = diagnostic;
            Debug.Log(diagnostic, this);
            return true;
        }

        private bool Reject(string diagnostic)
        {
            LastOperationSucceeded = false;
            LastDiagnostic = diagnostic;
            Debug.LogWarning(diagnostic, this);
            return false;
        }
    }
}
