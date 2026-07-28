using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class PlacedProductState : IEquatable<PlacedProductState>
    {
        public string productDefinitionId;
        public string fixtureId;
        public string snapPointId;
        public int quarterTurns;

        public PlacedProductState(string productDefinitionId, string fixtureId, string snapPointId, int quarterTurns)
        {
            this.productDefinitionId = productDefinitionId;
            this.fixtureId = fixtureId;
            this.snapPointId = snapPointId;
            this.quarterTurns = quarterTurns;
        }

        public bool Equals(PlacedProductState other)
        {
            return other != null &&
                   string.Equals(productDefinitionId, other.productDefinitionId, StringComparison.Ordinal) &&
                   string.Equals(fixtureId, other.fixtureId, StringComparison.Ordinal) &&
                   string.Equals(snapPointId, other.snapPointId, StringComparison.Ordinal) &&
                   quarterTurns == other.quarterTurns;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PlacedProductState);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(productDefinitionId, fixtureId, snapPointId, quarterTurns);
        }
    }

    [Serializable]
    public sealed class FoundationSaveData
    {
        public int version = PlacementSaveController.CurrentSaveVersion;
        public List<PlacedProductState> placedProducts = new();
    }

    public static class FoundationSaveCodec
    {
        public static string ToJson(FoundationSaveData saveData)
        {
            return JsonUtility.ToJson(saveData, true);
        }

        public static bool TryFromJson(string json, out FoundationSaveData saveData, out string error)
        {
            saveData = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Save content is empty.";
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<FoundationSaveData>(json);
            }
            catch (Exception exception)
            {
                error = $"Malformed JSON: {exception.Message}";
                return false;
            }

            if (saveData == null)
            {
                error = "Save JSON did not contain an object.";
                return false;
            }

            saveData.placedProducts ??= new List<PlacedProductState>();
            error = null;
            return true;
        }
    }

    public sealed class PlacementSaveController : MonoBehaviour
    {
        public const int CurrentSaveVersion = 1;

        [SerializeField] private ProductDefinition[] productDefinitions;
        [SerializeField] private ShelfFixture[] fixtures;
        [SerializeField] private ProductItem[] sceneProducts;
        [SerializeField] private string saveFileName = "foundation-spike-save.json";

        public IReadOnlyList<ProductDefinition> ProductDefinitions => productDefinitions;
        public IReadOnlyList<ShelfFixture> Fixtures => fixtures;
        public IReadOnlyList<ProductItem> SceneProducts => sceneProducts;
        public string SavePath => Path.Combine(Application.persistentDataPath, "Margins", saveFileName);

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
            if (!FoundationValidator.TryValidateAuthoredData(productDefinitions, fixtures, sceneProducts, out string validationError))
            {
                Debug.LogError($"Foundation spike save blocked: {validationError}", this);
                return false;
            }

            FoundationSaveData saveData = new();
            foreach (ProductItem product in sceneProducts)
            {
                if (product.TryGetPlacementState(out PlacedProductState state))
                {
                    saveData.placedProducts.Add(state);
                }
            }

            saveData.placedProducts.Sort(ComparePlacements);

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, FoundationSaveCodec.ToJson(saveData));
                Debug.Log($"Saved {saveData.placedProducts.Count} placement(s) to '{path}'.", this);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not write foundation spike save: {exception.Message}", this);
                return false;
            }
        }

        public bool TryLoadFromPath(string path)
        {
            ResetRuntimePlacementState();

            if (!FoundationValidator.TryValidateAuthoredData(productDefinitions, fixtures, sceneProducts, out string validationError))
            {
                Debug.LogError($"Foundation spike load blocked: {validationError}", this);
                return false;
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"No foundation spike save exists at '{path}'. Placement state remains empty.", this);
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not read foundation spike save: {exception.Message}", this);
                return false;
            }

            if (!FoundationSaveCodec.TryFromJson(json, out FoundationSaveData saveData, out string parseError))
            {
                Debug.LogError($"Foundation spike save was not loaded: {parseError}", this);
                return false;
            }

            if (saveData.version != CurrentSaveVersion)
            {
                Debug.LogError($"Unsupported foundation spike save version {saveData.version}; expected {CurrentSaveVersion}.", this);
                return false;
            }

            HashSet<ProductItem> usedProducts = new();
            int acceptedCount = 0;
            bool allRecordsAccepted = true;

            foreach (PlacedProductState state in saveData.placedProducts)
            {
                if (!TryLoadPlacement(state, usedProducts, out string rejection))
                {
                    allRecordsAccepted = false;
                    Debug.LogError($"Rejected saved placement: {rejection}", this);
                    continue;
                }

                acceptedCount++;
            }

            Debug.Log($"Loaded {acceptedCount} placement(s) from '{path}'.", this);
            return allRecordsAccepted;
        }

        private bool TryLoadPlacement(
            PlacedProductState state,
            HashSet<ProductItem> usedProducts,
            out string rejection)
        {
            if (state == null)
            {
                rejection = "placement record is null.";
                return false;
            }

            if (state.quarterTurns < 0 || state.quarterTurns > 3)
            {
                rejection = $"placement orientation {state.quarterTurns} is outside 0-3.";
                return false;
            }

            ProductDefinition definition = FindProductDefinition(state.productDefinitionId);
            if (definition == null)
            {
                rejection = $"product id '{state.productDefinitionId}' is missing.";
                return false;
            }

            ShelfFixture fixture = FindFixture(state.fixtureId);
            if (fixture == null)
            {
                rejection = $"fixture id '{state.fixtureId}' is missing.";
                return false;
            }

            if (!fixture.TryGetSnapPoint(state.snapPointId, out _))
            {
                rejection = $"snap point id '{state.snapPointId}' is missing from fixture '{state.fixtureId}'.";
                return false;
            }

            ProductItem product = FindAvailableProduct(definition, usedProducts);
            if (product == null)
            {
                rejection = $"no unused scene instance exists for product id '{state.productDefinitionId}'.";
                return false;
            }

            if (!fixture.TryPlaceAt(product, state.snapPointId, state.quarterTurns, out PlacementFailure failure))
            {
                rejection = $"target '{state.fixtureId}/{state.snapPointId}' rejected placement ({failure}).";
                return false;
            }

            usedProducts.Add(product);
            rejection = null;
            return true;
        }

        private ProductDefinition FindProductDefinition(string productId)
        {
            foreach (ProductDefinition definition in productDefinitions)
            {
                if (definition != null && string.Equals(definition.StableProductId, productId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }
            return null;
        }

        private ShelfFixture FindFixture(string fixtureId)
        {
            foreach (ShelfFixture fixture in fixtures)
            {
                if (fixture != null && string.Equals(fixture.StableFixtureId, fixtureId, StringComparison.Ordinal))
                {
                    return fixture;
                }
            }
            return null;
        }

        private ProductItem FindAvailableProduct(ProductDefinition definition, HashSet<ProductItem> usedProducts)
        {
            foreach (ProductItem product in sceneProducts)
            {
                if (product != null && product.Definition == definition && !usedProducts.Contains(product))
                {
                    return product;
                }
            }
            return null;
        }

        private void ResetRuntimePlacementState()
        {
            if (fixtures != null)
            {
                foreach (ShelfFixture fixture in fixtures)
                {
                    fixture?.ClearRuntimeOccupancy();
                }
            }

            if (sceneProducts != null)
            {
                foreach (ProductItem product in sceneProducts)
                {
                    product?.ResetToInitialLooseState();
                }
            }
        }

        private static int ComparePlacements(PlacedProductState left, PlacedProductState right)
        {
            int fixtureComparison = string.CompareOrdinal(left.fixtureId, right.fixtureId);
            if (fixtureComparison != 0)
            {
                return fixtureComparison;
            }

            int snapComparison = string.CompareOrdinal(left.snapPointId, right.snapPointId);
            return snapComparison != 0
                ? snapComparison
                : string.CompareOrdinal(left.productDefinitionId, right.productDefinitionId);
        }
    }
}
