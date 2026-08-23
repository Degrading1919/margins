using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    [Serializable]
    public sealed class PersistentGeneratedLocationDiskSnapshot
    {
        public string locationId;
        public FirstStoreSnapshot detailedStore;
        public FirstStorePlayerTransformSnapshot firstStoreReturnTransform;
        public bool customerFlowEnabled = true;
        public bool employeeWorkEnabled = true;
    }

    [Serializable]
    public sealed class FirstStoreDiskSaveData
    {
        public int version = FirstStoreDiskPersistenceController.CurrentFileVersion;
        public FirstStoreSnapshot firstStore;
        public FirstStorePlayerTransformSnapshot playerTransform;
        public PortfolioProgressionSnapshot portfolio;
        public bool hasGeneratedLocation;
        public PersistentGeneratedLocationDiskSnapshot generatedLocation;
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

            if (!saveData.hasGeneratedLocation)
            {
                // JsonUtility may materialize a null nested class as an empty
                // instance. The explicit presence bit is authoritative and
                // also leaves file-envelope versions 1-3 unambiguous.
                saveData.generatedLocation = null;
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
        public const int VersionTwo = 2;
        public const int PriorFileVersion = 3;
        public const int CurrentFileVersion = 4;

        [SerializeField] private FirstStorePersistenceMapperComponent persistenceMapper;
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private FirstStoreInteractionController interactionController;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private StagedCheckoutWorldInteractionTarget stagedCheckoutWorldTarget;
        [SerializeField] private PortfolioProgressionController portfolioProgression;
        [SerializeField] private string saveFileName = "first-store-vertical-slice.json";

        private float quickLoadConfirmationUntil;
        private FirstStoreDiskSaveData newBusinessTemplate;

        public event Action<bool, string> OperationCompleted;

        public string LastDiagnostic { get; private set; } =
            "No first-store disk save or load has been attempted.";
        public bool LastOperationSucceeded { get; private set; }
        public string SavePath => Path.Combine(
            Application.persistentDataPath,
            "Margins",
            saveFileName);
        public bool HasSaveFile => File.Exists(SavePath);
        public bool HasNewBusinessTemplate => newBusinessTemplate != null;

        private IEnumerator Start()
        {
            yield return null;
            if (!TryCaptureNewBusinessTemplate(out string error))
            {
                Debug.LogError(
                    $"New-business initialization template could not be captured: {error}",
                    this);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (GamePauseMenuController.IsAnyMenuOpen)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                quickLoadConfirmationUntil = 0f;
                TrySave();
            }
            else if (keyboard.f9Key.wasPressedThisFrame)
            {
                if (Time.unscaledTime <= quickLoadConfirmationUntil)
                {
                    quickLoadConfirmationUntil = 0f;
                    TryLoad();
                }
                else
                {
                    quickLoadConfirmationUntil = Time.unscaledTime + 3.5f;
                    LastOperationSucceeded = true;
                    LastDiagnostic = HasSaveFile
                        ? "Press F9 again to reload your last save."
                        : "No saved company is available yet.";
                    OperationCompleted?.Invoke(
                        LastOperationSucceeded,
                        LastDiagnostic);
                }
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

        public bool TryStartNewBusiness()
        {
            if (TryGetGeneratedLocationPersistenceBlocker(out string blocker))
            {
                return Reject($"New business rejected: {blocker}");
            }

            if (newBusinessTemplate == null &&
                !TryCaptureNewBusinessTemplate(out string captureError))
            {
                return Reject(
                    $"New business rejected: clean initialization state is unavailable: {captureError}");
            }

            string json;
            try
            {
                json = FirstStoreDiskSaveCodec.ToJson(newBusinessTemplate);
            }
            catch (Exception exception)
            {
                return Reject(
                    $"New business rejected: initialization state could not be copied: {exception.Message}");
            }

            if (!FirstStoreDiskSaveCodec.TryFromJson(
                    json,
                    out FirstStoreDiskSaveData cleanState,
                    out string error))
            {
                return Reject(
                    $"New business rejected: initialization state could not be copied: {error}");
            }

            return TryRestoreSaveData(cleanState, true);
        }

        public bool TrySaveToPath(string path)
        {
            if (!TryValidateConfiguration(out string error) ||
                !TryResolvePath(path, out string acceptedPath, out error))
            {
                return Reject($"Save rejected: {error}");
            }

            if (!TryCaptureCurrentSaveData(out FirstStoreDiskSaveData saveData,
                    out error))
            {
                return Reject($"Save rejected: {error}");
            }

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

            return TryRestoreSaveData(saveData, false);
        }

        private bool TryCaptureNewBusinessTemplate(out string error)
        {
            if (newBusinessTemplate != null)
            {
                error = null;
                return true;
            }

            if (!TryValidateConfiguration(out error) ||
                !persistenceMapper.TryCapture(
                    out FirstStoreSnapshot firstStore,
                    out error))
            {
                return false;
            }

            FirstStorePlayerTransformSnapshot playerTransform =
                firstPersonController.CaptureTransformSnapshot();
            if (!firstPersonController.TryPreflightApplyTransformSnapshot(
                    playerTransform,
                    out error))
            {
                return false;
            }

            PortfolioProgressionSnapshot portfolio = null;
            if (portfolioProgression != null &&
                !portfolioProgression.TryCaptureSnapshot(
                    out portfolio,
                    out error))
            {
                return false;
            }

            newBusinessTemplate = new FirstStoreDiskSaveData
            {
                version = CurrentFileVersion,
                firstStore = firstStore,
                playerTransform = playerTransform,
                portfolio = portfolio
            };
            error = null;
            return true;
        }

        private bool TryCaptureCurrentSaveData(
            out FirstStoreDiskSaveData saveData,
            out string error)
        {
            saveData = null;
            FirstStoreSnapshot firstStore;
            PersistentGeneratedLocationDiskSnapshot generatedLocation = null;
            PersistentPortfolioLocationSceneAdapter sceneAdapter =
                portfolioProgression?.LocationSceneAdapter;
            if (sceneAdapter?.HasActiveGeneratedLocation == true)
            {
                if (!sceneAdapter.TryCapturePersistenceState(
                        out firstStore,
                        out generatedLocation,
                        out error))
                {
                    return false;
                }
            }
            else
            {
                if (portfolioProgression != null &&
                    !portfolioProgression.TrySynchronizeDetailedShift(out error))
                {
                    error = $"Company synchronization failed: {error}";
                    return false;
                }
                if (persistenceMapper.TryGetDiskSaveBlocker(out error) ||
                    !persistenceMapper.TryCapture(out firstStore, out error))
                {
                    return false;
                }
            }

            FirstStorePlayerTransformSnapshot playerTransform =
                firstPersonController.CaptureTransformSnapshot();
            if (!firstPersonController.TryPreflightApplyTransformSnapshot(
                    playerTransform,
                    out error))
            {
                return false;
            }

            PortfolioProgressionSnapshot portfolio = null;
            if (portfolioProgression != null &&
                !portfolioProgression.TryCaptureSnapshot(
                    out portfolio,
                    out error))
            {
                return false;
            }

            saveData = new FirstStoreDiskSaveData
            {
                version = CurrentFileVersion,
                firstStore = firstStore,
                playerTransform = playerTransform,
                portfolio = portfolio,
                hasGeneratedLocation = generatedLocation != null,
                generatedLocation = generatedLocation
            };
            if (portfolioProgression != null &&
                (!portfolioProgression.TryValidateDetailedProcurementReconciliation(
                     firstStore,
                     portfolio,
                     out error) ||
                 !portfolioProgression.TryValidateDetailedMerchandisingReconciliation(
                     firstStore,
                     portfolio,
                     out error)))
            {
                saveData = null;
                return false;
            }
            if (generatedLocation != null &&
                (!portfolioProgression.TryValidateDetailedProcurementReconciliation(
                     generatedLocation.locationId,
                     generatedLocation.detailedStore,
                     portfolio,
                     out error) ||
                 !portfolioProgression.TryValidateDetailedMerchandisingReconciliation(
                     generatedLocation.locationId,
                     generatedLocation.detailedStore,
                     portfolio,
                     out error)))
            {
                saveData = null;
                return false;
            }

            error = null;
            return true;
        }

        private bool TryCaptureRollbackState(
            out FirstStoreDiskSaveData saveData,
            out PortfolioProgressionSnapshot portfolio,
            out string error)
        {
            saveData = null;
            portfolio = null;
            PersistentPortfolioLocationSceneAdapter sceneAdapter =
                portfolioProgression?.LocationSceneAdapter;
            if (sceneAdapter?.HasActiveGeneratedLocation == true)
            {
                if (!sceneAdapter.TryCapturePersistenceRollbackState(
                        out FirstStoreSnapshot parkedFirstStore,
                        out PersistentGeneratedLocationDiskSnapshot generated,
                        out error))
                {
                    return false;
                }
                FirstStorePlayerTransformSnapshot activePlayerTransform =
                    firstPersonController.CaptureTransformSnapshot();
                if (!firstPersonController.TryPreflightApplyTransformSnapshot(
                        activePlayerTransform,
                        out error) ||
                    (portfolioProgression != null &&
                     !portfolioProgression.TryCaptureSnapshot(
                         out portfolio,
                         out error)))
                {
                    return false;
                }
                saveData = new FirstStoreDiskSaveData
                {
                    version = CurrentFileVersion,
                    firstStore = parkedFirstStore,
                    playerTransform = activePlayerTransform,
                    portfolio = portfolio,
                    hasGeneratedLocation = true,
                    generatedLocation = generated
                };
                return true;
            }

            if (persistenceMapper.TryGetLoadRollbackBlocker(out error) ||
                !persistenceMapper.TryCapture(
                    out FirstStoreSnapshot firstStore,
                    out error))
            {
                return false;
            }
            FirstStorePlayerTransformSnapshot playerTransform =
                firstPersonController.CaptureTransformSnapshot();
            if (!firstPersonController.TryPreflightApplyTransformSnapshot(
                    playerTransform,
                    out error) ||
                (portfolioProgression != null &&
                 !portfolioProgression.TryCaptureSnapshot(
                     out portfolio,
                     out error)))
            {
                return false;
            }

            saveData = new FirstStoreDiskSaveData
            {
                version = CurrentFileVersion,
                firstStore = firstStore,
                playerTransform = playerTransform,
                portfolio = portfolio,
                hasGeneratedLocation = false,
                generatedLocation = null
            };
            error = null;
            return true;
        }

        private bool TryRestoreSaveData(
            FirstStoreDiskSaveData saveData,
            bool startingNewBusiness)
        {
            string rejectionPrefix = startingNewBusiness
                ? "New business rejected"
                : "Load rejected";
            if (!TryValidateConfiguration(out string error))
            {
                return Reject($"{rejectionPrefix}: {error}");
            }
            if (!TryPrepareRestore(
                    saveData,
                    out PortfolioProgressionSnapshot acceptedPortfolio,
                    out bool migratedLegacyPortfolio,
                    out error))
            {
                return Reject($"{rejectionPrefix}: {error}");
            }

            bool priorGameplayMode = firstPersonController.IsGameplayMode;
            if (!TryCaptureRollbackState(
                    out FirstStoreDiskSaveData previousState,
                    out PortfolioProgressionSnapshot previousPortfolio,
                    out error))
            {
                return Reject(
                    $"{rejectionPrefix}: current state could not be protected: {error}");
            }

            if (!TryApplyAcceptedRestore(
                    saveData,
                    acceptedPortfolio,
                    out error))
            {
                string restoreError = error;
                if (!TryApplyAcceptedRestore(
                        previousState,
                        previousPortfolio,
                        out string rollbackError))
                {
                    throw new InvalidOperationException(
                        $"Persistence restore failed ('{restoreError}') and live-state rollback failed ('{rollbackError}').");
                }

                firstPersonController.SetGameplayMode(priorGameplayMode);
                return Reject($"{rejectionPrefix}: {restoreError}");
            }

            if (!GamePauseMenuController.IsAnyMenuOpen)
            {
                firstPersonController.SetGameplayMode(priorGameplayMode);
            }

            return Accept(
                startingNewBusiness
                    ? "Started a clean first-store business. The existing disk save was kept."
                    : migratedLegacyPortfolio
                    ? "Loaded first-store state and migrated legacy company progression."
                    : "Loaded first-store and portfolio state from disk.");
        }

        private bool TryPrepareRestore(
            FirstStoreDiskSaveData saveData,
            out PortfolioProgressionSnapshot acceptedPortfolio,
            out bool migratedLegacyPortfolio,
            out string error)
        {
            acceptedPortfolio = null;
            migratedLegacyPortfolio = false;
            if (saveData == null ||
                (saveData.version != CurrentFileVersion &&
                 saveData.version != PriorFileVersion &&
                 saveData.version != VersionTwo &&
                 saveData.version != LegacyFileVersion))
            {
                error = saveData == null
                    ? "First-store save state is missing."
                    : $"Unsupported first-store file version {saveData.version}; expected {LegacyFileVersion}, {VersionTwo}, {PriorFileVersion}, or {CurrentFileVersion}.";
                return false;
            }

            error = null;
            if (saveData.firstStore == null ||
                !persistenceMapper.TryValidateSnapshot(
                    saveData.firstStore,
                    out error) ||
                !firstPersonController.TryPreflightApplyTransformSnapshot(
                    saveData.playerTransform,
                    out error))
            {
                error ??= "First-store state is missing.";
                return false;
            }

            if (portfolioProgression != null)
            {
                if (saveData.version == LegacyFileVersion)
                {
                    if (!portfolioProgression.TryCreateLegacyMigrationSnapshot(
                            saveData.firstStore,
                            out acceptedPortfolio,
                            out error))
                    {
                        error = $"Legacy company migration failed: {error}";
                        return false;
                    }
                    migratedLegacyPortfolio = true;
                }
                else if (saveData.portfolio == null ||
                         !PortfolioProgression.TryRestore(
                             saveData.portfolio,
                             out PortfolioProgression restoredPortfolio,
                             out error))
                {
                    error ??= "Portfolio state is missing.";
                    return false;
                }
                else
                {
                    acceptedPortfolio = restoredPortfolio.CreateSnapshot();
                }
            }
            else if (saveData.portfolio != null)
            {
                error =
                    "This scene has no portfolio controller for the saved company state.";
                return false;
            }

            PersistentGeneratedLocationDiskSnapshot generated =
                saveData.generatedLocation;
            if (generated != null)
            {
                if (saveData.version != CurrentFileVersion ||
                    portfolioProgression == null ||
                    !FirstStoreIdentifier.IsValid(generated.locationId) ||
                    string.Equals(
                        generated.locationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal) ||
                    generated.detailedStore == null ||
                    !persistenceMapper.TryValidateSnapshot(
                        generated.detailedStore,
                        out error) ||
                    !firstPersonController.TryPreflightApplyTransformSnapshot(
                        generated.firstStoreReturnTransform,
                        out error))
                {
                    error ??=
                        "Generated detailed-location persistence state is invalid.";
                    return false;
                }
                if (!string.Equals(
                        acceptedPortfolio.company.activeDetailedLocationId,
                        generated.locationId,
                        StringComparison.Ordinal) ||
                    acceptedPortfolio.locations.All(location => !string.Equals(
                        location.locationId,
                        generated.locationId,
                        StringComparison.Ordinal)))
                {
                    error =
                        "Generated detailed-location state contradicts the portfolio's active location.";
                    return false;
                }
                if (saveData.firstStore.customerFlow?.customers?.Count > 0)
                {
                    error =
                        "A generated-location save cannot park active customers in a different first-store location.";
                    return false;
                }
            }
            else if (acceptedPortfolio != null &&
                     !string.IsNullOrWhiteSpace(
                         acceptedPortfolio.company.activeDetailedLocationId) &&
                     !string.Equals(
                         acceptedPortfolio.company.activeDetailedLocationId,
                         PortfolioProgressionRules.FirstLocationId,
                         StringComparison.Ordinal))
            {
                error =
                    "Portfolio state identifies an active generated location without its detailed scene snapshot.";
                return false;
            }

            if (acceptedPortfolio != null)
            {
                StoreOperatingSnapshot savedOperating =
                    saveData.firstStore.storeOperating;
                PortfolioDetailedReconciliationSnapshot reconciliation =
                    acceptedPortfolio.locations?.Find(location => string.Equals(
                        location.locationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal))?.detailedReconciliation;
                if (savedOperating?.hasResult == true &&
                    (!acceptedPortfolio.firstShiftCompleted ||
                     !string.Equals(
                         reconciliation?.sessionId,
                         savedOperating.sessionId,
                         StringComparison.Ordinal)))
                {
                    error =
                        "Detailed first-shift result and portfolio posting disagree.";
                    return false;
                }
                if (!portfolioProgression.TryValidateDetailedProcurementReconciliation(
                        saveData.firstStore,
                        acceptedPortfolio,
                        out error) ||
                    !portfolioProgression.TryValidateDetailedMerchandisingReconciliation(
                        saveData.firstStore,
                        acceptedPortfolio,
                        out error))
                {
                    return false;
                }
                if (generated != null &&
                    (!portfolioProgression.TryValidateDetailedProcurementReconciliation(
                         generated.locationId,
                         generated.detailedStore,
                         acceptedPortfolio,
                         out error) ||
                     !portfolioProgression.TryValidateDetailedMerchandisingReconciliation(
                         generated.locationId,
                         generated.detailedStore,
                         acceptedPortfolio,
                         out error)))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool TryApplyAcceptedRestore(
            FirstStoreDiskSaveData saveData,
            PortfolioProgressionSnapshot acceptedPortfolio,
            out string error)
        {
            PersistentPortfolioLocationSceneAdapter sceneAdapter =
                portfolioProgression?.LocationSceneAdapter;
            if (sceneAdapter != null &&
                !sceneAdapter.TryResetForPersistenceRestore(out error))
            {
                return false;
            }
            if (!persistenceMapper.TryRestore(saveData.firstStore, out error))
            {
                return false;
            }
            if (portfolioProgression != null &&
                !portfolioProgression.TryRestoreSnapshot(
                    acceptedPortfolio,
                    out error))
            {
                return false;
            }
            if (saveData.generatedLocation != null &&
                (sceneAdapter == null ||
                 !sceneAdapter.TryRestorePersistenceState(
                     saveData.generatedLocation,
                     out error)))
            {
                error ??=
                    "The scene has no generated-location adapter for the saved detailed location.";
                return false;
            }
            if (!firstPersonController.TryApplyTransformSnapshot(
                    saveData.playerTransform,
                    out error))
            {
                return false;
            }

            stagedCheckout.ResetTransientStateAfterRestore();
            stagedCheckoutWorldTarget.ResetTransientStateAfterRestore();
            interactionController.ResetTransientStateAfterRestore();
            error = null;
            return true;
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

        private bool TryGetGeneratedLocationPersistenceBlocker(
            out string blocker)
        {
            PersistentPortfolioLocationSceneAdapter sceneAdapter =
                portfolioProgression?.LocationSceneAdapter;
            if (sceneAdapter?.HasActiveGeneratedLocation == true)
            {
                blocker =
                    "Leave the generated location before starting a new business.";
                return true;
            }

            blocker = null;
            return false;
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
            OperationCompleted?.Invoke(true, diagnostic);
            return true;
        }

        private bool Reject(string diagnostic)
        {
            LastOperationSucceeded = false;
            LastDiagnostic = diagnostic;
            Debug.LogWarning(diagnostic, this);
            OperationCompleted?.Invoke(false, diagnostic);
            return false;
        }
    }
}
