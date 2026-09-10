using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Margins.Editor
{
    public static class ProductionAssetValidationMenu
    {
        private const string LedgerRelativePath =
            "04_CONTENT_PRODUCTION/Assets/Margins_Asset_Provenance_Ledger.csv";

        [MenuItem("Margins/Production Assets/Validate Selected Prefab")]
        public static void ValidateSelectedPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Select a production prefab asset or prefab instance to validate.");
                return;
            }

            if (!TryLoadLedger(out ProductionAssetLedger ledger))
            {
                return;
            }

            string assetPath = ResolvePrefabAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError(
                    "Select a production prefab asset or an instance of one; scene-only objects are not intake sources.",
                    selected);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                ProductionAssetMetadata metadata =
                    root.GetComponent<ProductionAssetMetadata>();
                ProductionAssetValidationReport report =
                    ProductionAssetValidator.Validate(metadata, ledger);
                LogReport(metadata == null ? root.name : metadata.AssetId, report, selected);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static string ResolvePrefabAssetPath(GameObject selected)
        {
            if (selected == null)
            {
                return string.Empty;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (!string.IsNullOrEmpty(path) &&
                string.Equals(
                    Path.GetExtension(path),
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected) ??
                   string.Empty;
        }

        [MenuItem("Margins/Production Assets/Validate Selected Prefab", true)]
        private static bool CanValidateSelectedPrefab()
        {
            return Selection.activeGameObject != null;
        }

        private static bool TryLoadLedger(out ProductionAssetLedger ledger)
        {
            ledger = null;
            string repositoryRoot = Directory.GetParent(Application.dataPath)?
                .Parent?.Parent?.Parent?.FullName;
            if (string.IsNullOrEmpty(repositoryRoot))
            {
                Debug.LogError("Could not resolve the Margins repository root from the Unity Assets path.");
                return false;
            }

            string ledgerPath = Path.Combine(
                repositoryRoot,
                LedgerRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(ledgerPath))
            {
                Debug.LogError($"Production asset ledger is missing at '{ledgerPath}'.");
                return false;
            }

            if (!ProductionAssetLedger.TryParse(
                    File.ReadAllText(ledgerPath),
                    out ledger,
                    out IReadOnlyList<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError(error);
                }

                return false;
            }

            return true;
        }

        private static void LogReport(
            string assetId,
            ProductionAssetValidationReport report,
            UnityEngine.Object context)
        {
            foreach (ProductionAssetValidationIssue issue in report.Issues)
            {
                if (issue.Severity == ProductionAssetValidationSeverity.Error)
                {
                    Debug.LogError($"[{assetId}] {issue.Message}", context);
                }
                else
                {
                    Debug.LogWarning($"[{assetId}] {issue.Message}", context);
                }
            }

            if (report.ProductionReady)
            {
                Debug.Log($"[{assetId}] Production asset validation passed and the ledger records owner acceptance.", context);
            }
            else if (report.TechnicalRequirementsPassed)
            {
                Debug.LogWarning($"[{assetId}] Automated technical validation passed, but owner-controlled review or production disposition remains open.", context);
            }
        }
    }
}
