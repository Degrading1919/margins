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
        private const string CatalogRelativePath =
            "01_PRE-PRODUCTION/1.7 Art Audio & Presentation/Margins_3D_Asset_Budget_Catalog_v0.1.csv";

        [MenuItem("Margins/Production Assets/Measure Selected Prefab For Intake")]
        public static void MeasureSelectedPrefab()
        {
            ValidateSelectedPrefab(ProductionAssetValidationMode.IntakeMeasurement);
        }

        [MenuItem("Margins/Production Assets/Validate Selected Prefab")]
        public static void ValidateSelectedPrefab()
        {
            ValidateSelectedPrefab(ProductionAssetValidationMode.ProductionReadiness);
        }

        private static void ValidateSelectedPrefab(ProductionAssetValidationMode mode)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Select a production prefab asset or prefab instance to validate.");
                return;
            }

            if (!TryLoadData(
                    mode,
                    out ProductionAssetLedger ledger,
                    out ProductionAssetBudgetCatalog catalog))
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
                    ProductionAssetValidator.Validate(metadata, ledger, catalog, mode);
                LogReport(
                    metadata == null ? root.name : metadata.AssetId,
                    report,
                    mode,
                    selected);
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

        public static bool CanContinueWithoutCatalog(
            ProductionAssetValidationMode mode)
        {
            return mode == ProductionAssetValidationMode.IntakeMeasurement;
        }

        private static bool TryLoadData(
            ProductionAssetValidationMode mode,
            out ProductionAssetLedger ledger,
            out ProductionAssetBudgetCatalog catalog)
        {
            ledger = null;
            catalog = null;
            string repositoryRoot = Directory.GetParent(Application.dataPath)?
                .Parent?.Parent?.Parent?.FullName;
            if (string.IsNullOrEmpty(repositoryRoot))
            {
                Debug.LogError("Could not resolve the Margins repository root from the Unity Assets path.");
                return false;
            }

            string ledgerPath = ResolveRepositoryPath(repositoryRoot, LedgerRelativePath);
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

            string catalogPath = ResolveRepositoryPath(repositoryRoot, CatalogRelativePath);
            if (!File.Exists(catalogPath))
            {
                string message =
                    $"Production asset budget catalog is missing at '{catalogPath}'.";
                if (!CanContinueWithoutCatalog(mode))
                {
                    Debug.LogError(message);
                    return false;
                }

                Debug.LogWarning(
                    message + " Unity measurements will still be reported, but catalog budget checks cannot pass.");
                return true;
            }

            if (!ProductionAssetBudgetCatalog.TryParse(
                    File.ReadAllText(catalogPath),
                    out catalog,
                    out errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError(error);
                }

                return false;
            }

            return true;
        }

        private static string ResolveRepositoryPath(
            string repositoryRoot,
            string relativePath)
        {
            return Path.Combine(
                repositoryRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void LogReport(
            string assetId,
            ProductionAssetValidationReport report,
            ProductionAssetValidationMode mode,
            UnityEngine.Object context)
        {
            ProductionAssetMeasurements measured = report.Measurements;
            Debug.Log(
                $"[{assetId}] Unity measurements: " +
                $"LOD0={measured.Lod0Triangles}, LOD1={measured.Lod1Triangles}, " +
                $"LOD2={measured.Lod2Triangles}, collision={measured.CollisionTriangles}, " +
                $"materialSlots={measured.Lod0MaterialSlots}, " +
                $"maxTexturePx={measured.MaximumTextureDimension}, bones={measured.Lod0BoneCount}, " +
                $"maxInfluences={measured.MaximumInfluencesPerVertex}, " +
                $"skinnedRenderers={measured.Lod0SkinnedRendererCount}, " +
                $"alphaTestedMaterials={measured.AlphaTestedMaterialCount}.",
                context);

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
            else if (report.TechnicalRequirementsPassed &&
                     mode == ProductionAssetValidationMode.IntakeMeasurement)
            {
                Debug.Log(
                    $"[{assetId}] Intake measurement passed. Record the Unity measurements in the provenance ledger before strict production validation.",
                    context);
            }
            else if (report.TechnicalRequirementsPassed)
            {
                Debug.LogWarning($"[{assetId}] Automated technical validation passed, but owner-controlled review or production disposition remains open.", context);
            }
        }
    }
}
