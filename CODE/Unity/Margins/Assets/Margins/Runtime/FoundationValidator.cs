using System;
using System.Collections.Generic;

namespace Margins
{
    public static class FoundationValidator
    {
        public static bool TryValidateAuthoredData(
            IReadOnlyList<ProductDefinition> productDefinitions,
            IReadOnlyList<ShelfFixture> fixtures,
            IReadOnlyList<ProductItem> sceneProducts,
            out string error)
        {
            if (!TryValidateProductDefinitions(productDefinitions, out error))
            {
                return false;
            }

            if (!TryValidateFixtures(fixtures, out error))
            {
                return false;
            }

            if (sceneProducts == null || sceneProducts.Count == 0)
            {
                error = "At least one scene product instance is required.";
                return false;
            }

            string approvedProductId = productDefinitions[0].StableProductId;
            foreach (ProductItem product in sceneProducts)
            {
                if (product == null || product.Definition == null)
                {
                    error = "Every scene product instance must reference the product definition.";
                    return false;
                }

                if (!string.Equals(product.Definition.StableProductId, approvedProductId, StringComparison.Ordinal))
                {
                    error = $"Scene product '{product.name}' references unapproved product id '{product.Definition.StableProductId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static bool TryValidateProductDefinitions(
            IReadOnlyList<ProductDefinition> productDefinitions,
            out string error)
        {
            if (productDefinitions == null || productDefinitions.Count == 0)
            {
                error = "Exactly one product definition is required; none were provided.";
                return false;
            }

            HashSet<string> identifiers = new(StringComparer.Ordinal);
            foreach (ProductDefinition definition in productDefinitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.StableProductId))
                {
                    error = "Every product definition requires a stable identifier.";
                    return false;
                }

                if (!identifiers.Add(definition.StableProductId))
                {
                    error = $"Duplicate product identifier '{definition.StableProductId}'.";
                    return false;
                }
            }

            if (productDefinitions.Count != 1)
            {
                error = $"The foundation spike requires exactly one product definition; found {productDefinitions.Count}.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryValidateFixtures(IReadOnlyList<ShelfFixture> fixtures, out string error)
        {
            if (fixtures == null || fixtures.Count != 1 || fixtures[0] == null)
            {
                error = "The foundation spike requires exactly one shelf fixture.";
                return false;
            }

            HashSet<string> fixtureIdentifiers = new(StringComparer.Ordinal);
            foreach (ShelfFixture fixture in fixtures)
            {
                if (string.IsNullOrWhiteSpace(fixture.StableFixtureId))
                {
                    error = "Every shelf fixture requires a stable identifier.";
                    return false;
                }

                if (!fixtureIdentifiers.Add(fixture.StableFixtureId))
                {
                    error = $"Duplicate fixture identifier '{fixture.StableFixtureId}'.";
                    return false;
                }

                HashSet<string> snapIdentifiers = new(StringComparer.Ordinal);
                foreach (ShelfSnapPointDefinition snapPoint in fixture.SnapPoints)
                {
                    if (snapPoint == null || string.IsNullOrWhiteSpace(snapPoint.StableSnapPointId))
                    {
                        error = $"Fixture '{fixture.StableFixtureId}' has a snap point without a stable identifier.";
                        return false;
                    }

                    if (!snapIdentifiers.Add(snapPoint.StableSnapPointId))
                    {
                        error = $"Fixture '{fixture.StableFixtureId}' has duplicate snap point identifier '{snapPoint.StableSnapPointId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
