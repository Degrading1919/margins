using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Defines the authored property volume and structural collision boundary used
    /// by fixture placement. FixturePlacementController remains the accepted-layout
    /// authority; this component only validates the physical property context.
    /// </summary>
    public sealed class OwnedPropertyPlacementArea : MonoBehaviour
    {
        [SerializeField] private BoxCollider ownedPropertyBounds;
        [SerializeField] private Collider placementSurface;
        [SerializeField] private Collider[] structuralObstacles;
        [SerializeField] private Transform player;

        public Collider PlacementSurface => placementSurface;
        public Transform Player => player;

        public bool TryValidateConfiguration(out string error)
        {
            if (ownedPropertyBounds == null || placementSurface == null || player == null)
            {
                error =
                    "Owned-property placement requires explicit bounds, placement-surface, and player references.";
                return false;
            }

            if (ownedPropertyBounds.size.x <= 0f ||
                ownedPropertyBounds.size.y <= 0f ||
                ownedPropertyBounds.size.z <= 0f)
            {
                error = "Owned-property bounds must have positive dimensions.";
                return false;
            }

            HashSet<Collider> uniqueObstacles = new();
            if (structuralObstacles != null)
            {
                foreach (Collider obstacle in structuralObstacles)
                {
                    if (obstacle == null ||
                        obstacle == ownedPropertyBounds ||
                        obstacle == placementSurface ||
                        !uniqueObstacles.Add(obstacle))
                    {
                        error =
                            "Owned-property structural obstacles contain a missing, duplicate, bounds, or placement-surface collider.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool ContainsPlayer()
        {
            return player != null && ContainsWorldPoint(player.position);
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            if (ownedPropertyBounds == null)
            {
                return false;
            }

            Vector3 localPoint = ownedPropertyBounds.transform.InverseTransformPoint(worldPoint) -
                                 ownedPropertyBounds.center;
            Vector3 halfSize = ownedPropertyBounds.size * 0.5f;
            const float tolerance = 0.001f;
            return Mathf.Abs(localPoint.x) <= halfSize.x + tolerance &&
                   Mathf.Abs(localPoint.y) <= halfSize.y + tolerance &&
                   Mathf.Abs(localPoint.z) <= halfSize.z + tolerance;
        }

        public bool TryValidateFixturePlacement(
            PlaceableFixtureComponent fixture,
            Transform gridOrigin,
            float cellSize,
            int quarterTurns,
            out FixturePlacementFailure failure,
            out string error)
        {
            failure = FixturePlacementFailure.None;
            if (!TryValidateConfiguration(out error) || fixture == null ||
                gridOrigin == null || cellSize <= 0f)
            {
                failure = FixturePlacementFailure.InvalidSupport;
                error ??= "Fixture placement physical validation is unavailable.";
                return false;
            }

            GridFootprint footprint = fixture.Footprint.Rotate(quarterTurns);
            Vector3 halfRight = fixture.transform.right *
                                Mathf.Max(0f, footprint.width * cellSize * 0.5f - 0.02f);
            Vector3 halfForward = fixture.transform.forward *
                                  Mathf.Max(0f, footprint.depth * cellSize * 0.5f - 0.02f);
            Vector3 center = fixture.transform.position;
            Vector3[] supportSamples =
            {
                center + halfRight + halfForward,
                center + halfRight - halfForward,
                center - halfRight + halfForward,
                center - halfRight - halfForward
            };

            for (int index = 0; index < supportSamples.Length; index++)
            {
                Vector3 sample = supportSamples[index];
                if (!ContainsWorldPoint(sample) ||
                    !placementSurface.Raycast(
                        new Ray(sample + Vector3.up * 2f, Vector3.down),
                        out _,
                        4f))
                {
                    failure = FixturePlacementFailure.InvalidSupport;
                    error = "The whole fixture must remain on supported owned property.";
                    return false;
                }
            }

            Physics.SyncTransforms();
            Collider[] fixtureColliders = fixture.GetComponentsInChildren<Collider>(true);
            if (structuralObstacles != null)
            {
                foreach (Collider fixtureCollider in fixtureColliders)
                {
                    if (fixtureCollider == null || !fixtureCollider.enabled ||
                        fixtureCollider.isTrigger)
                    {
                        continue;
                    }

                    foreach (Collider obstacle in structuralObstacles)
                    {
                        if (obstacle == null || !obstacle.enabled || obstacle.isTrigger ||
                            obstacle.transform.IsChildOf(fixture.transform) ||
                            fixtureCollider.transform.IsChildOf(obstacle.transform))
                        {
                            continue;
                        }

                        if (Physics.ComputePenetration(
                                fixtureCollider,
                                fixtureCollider.transform.position,
                                fixtureCollider.transform.rotation,
                                obstacle,
                                obstacle.transform.position,
                                obstacle.transform.rotation,
                                out _,
                                out float penetrationDistance) &&
                            penetrationDistance > 0.002f)
                        {
                            failure = FixturePlacementFailure.StructuralCollision;
                            error = "That fixture collides with the building or another structural obstacle.";
                            return false;
                        }
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
