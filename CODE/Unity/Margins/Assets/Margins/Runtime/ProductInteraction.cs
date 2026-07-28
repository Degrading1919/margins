using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    public sealed class ProductInteraction : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform holdPoint;
        [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
        [SerializeField] private LayerMask pickupLayers = ~0;
        [SerializeField] private ShelfFixture shelf;
        [SerializeField] private PlacementSaveController saveController;

        public ProductItem HeldProduct { get; private set; }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (HeldProduct == null)
                {
                    TryPickUpTargetedProduct();
                }
                else
                {
                    ReleaseHeldProduct();
                }
            }

            if (HeldProduct != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                HeldProduct.AdvanceQuarterTurn();
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                saveController?.TrySave();
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                HeldProduct = null;
                saveController?.TryLoad();
            }

            UpdatePlacementPreview();
        }

        public bool TryPickUp(ProductItem product)
        {
            if (HeldProduct != null || product == null || holdPoint == null)
            {
                return false;
            }

            HeldProduct = product;
            product.PickUp(holdPoint);
            Debug.Log($"Picked up product '{product.Definition?.StableProductId ?? "<missing>"}'.", product);
            return true;
        }

        public bool ReleaseHeldProduct()
        {
            if (HeldProduct == null)
            {
                return false;
            }

            ProductItem releasingProduct = HeldProduct;
            PlacementFailure failure = PlacementFailure.InvalidSnapPoint;
            if (shelf != null && shelf.TryPlaceNearest(
                    releasingProduct,
                    releasingProduct.transform.position,
                    releasingProduct.QuarterTurns,
                    out string snapPointId,
                    out failure))
            {
                HeldProduct = null;
                Debug.Log($"Snapped product to '{shelf.StableFixtureId}/{snapPointId}'.", releasingProduct);
                return true;
            }

            releasingProduct.ReleaseLoose();
            HeldProduct = null;
            Debug.LogWarning($"Product release did not occupy a shelf slot ({failure}).", releasingProduct);
            return false;
        }

        private void TryPickUpTargetedProduct()
        {
            if (viewCamera == null)
            {
                return;
            }

            Ray ray = new(viewCamera.transform.position, viewCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, pickupLayers, QueryTriggerInteraction.Ignore))
            {
                TryPickUp(hit.collider.GetComponentInParent<ProductItem>());
            }
        }

        private void UpdatePlacementPreview()
        {
            if (HeldProduct == null)
            {
                return;
            }

            bool isValid = shelf != null && shelf.TryFindNearestAvailable(
                HeldProduct,
                HeldProduct.transform.position,
                out _,
                out _);
            HeldProduct.SetPlacementPreview(isValid);
        }
    }
}
