using System.Linq;
using UnityEngine;

namespace Margins
{
    public sealed class ProceduralGenerationDebugView : MonoBehaviour
    {
        [SerializeField] private bool showRuntimeOverlays = true;
        [SerializeField] private bool showFootprint = true;
        [SerializeField] private bool showZones = true;
        [SerializeField] private bool showCirculation = true;
        [SerializeField] private bool showPlacementBounds = true;
        [SerializeField] private bool showClearances = true;
        [SerializeField] private bool showSockets = true;

        public bool ShowRuntimeOverlays => showRuntimeOverlays;

        private void OnDrawGizmos()
        {
            ProceduralCommercialBuilding building =
                GetComponent<ProceduralCommercialBuilding>();
            ProceduralGenerationResult result = building?.LastResult;
            if (result == null)
            {
                return;
            }

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (showFootprint)
            {
                Gizmos.color = Color.white;
                foreach (PlanRect rectangle in result.Footprint.Rectangles)
                {
                    DrawRect(rectangle, 0.05f);
                }
            }

            if (showZones)
            {
                foreach (GeneratedFunctionalZone zone in result.Zones)
                {
                    Gizmos.color = new Color(0.15f, 0.55f, 0.95f, 0.7f);
                    DrawRect(zone.BoundsMeters, 0.08f);
                }
            }

            if (showCirculation)
            {
                Gizmos.color = Color.green;
                foreach (GeneratedCirculationPath path in result.Circulation)
                {
                    DrawRect(path.BoundsMeters, 0.12f);
                }
            }

            if (showPlacementBounds)
            {
                Gizmos.color = Color.yellow;
                foreach (GeneratedAssetPlacement placement in result.Placements)
                {
                    DrawRect(placement.PhysicalBoundsMeters, 0.18f);
                }
            }

            if (showClearances)
            {
                Gizmos.color = Color.magenta;
                foreach (GeneratedAssetClearance clearance in result.Placements
                             .SelectMany(item => item.Clearances))
                {
                    DrawRect(clearance.BoundsMeters, 0.22f);
                }
            }

            if (showSockets)
            {
                Gizmos.color = Color.cyan;
                foreach (ProceduralSocketComponent socket in
                         GetComponentsInChildren<ProceduralSocketComponent>(true))
                {
                    Vector3 local = transform.InverseTransformPoint(socket.transform.position);
                    Gizmos.DrawWireSphere(local, 0.12f);
                }
            }

            Gizmos.matrix = previous;
        }

        private static void DrawRect(PlanRect rectangle, float y)
        {
            Gizmos.DrawWireCube(
                new Vector3(rectangle.Center.x, y, rectangle.Center.y),
                new Vector3(rectangle.Width, 0.05f, rectangle.Depth));
        }
    }
}
