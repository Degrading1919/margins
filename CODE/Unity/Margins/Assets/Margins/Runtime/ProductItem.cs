using System.Collections;
using UnityEngine;

namespace Margins
{
    public sealed class ProductItem : MonoBehaviour
    {
        [SerializeField] private ProductDefinition definition;
        [SerializeField] private Rigidbody productRigidbody;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Coroutine feedbackReset;

        public ProductDefinition Definition => definition;
        public string PhysicalUnitId { get; private set; }
        public bool IsHeld { get; private set; }
        public bool IsSnapped => SnappedFixture != null;
        public bool IsReservedByCustomer { get; private set; }
        public ShelfFixture SnappedFixture { get; private set; }
        public string SnappedPointId { get; private set; }
        public int QuarterTurns { get; private set; }

        private void Awake()
        {
            EnsureReferences();
            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void PickUp(Transform holdPoint)
        {
            if (holdPoint == null)
            {
                return;
            }

            EnsureReferences();
            if (SnappedFixture != null)
            {
                SnappedFixture.ReleaseProduct(this);
            }

            SnappedFixture = null;
            SnappedPointId = null;
            IsReservedByCustomer = false;
            IsHeld = true;
            transform.SetParent(holdPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, QuarterTurns * 90f, 0f);
            SetPhysicsHeld(true);
            SetFeedbackMaterial(defaultMaterial);
        }

        internal void AssignPhysicalUnitId(string physicalUnitId)
        {
            if (!FirstStoreIdentifier.IsValid(physicalUnitId))
            {
                throw new System.ArgumentException(
                    "Physical product unit id is invalid.",
                    nameof(physicalUnitId));
            }

            if (PhysicalUnitId != null &&
                !string.Equals(
                    PhysicalUnitId,
                    physicalUnitId,
                    System.StringComparison.Ordinal))
            {
                throw new System.InvalidOperationException(
                    $"Physical product unit '{PhysicalUnitId}' cannot be reassigned.");
            }

            PhysicalUnitId = physicalUnitId;
        }

        internal void ApplyLoosePlacement(
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            EnsureReferences();
            if (SnappedFixture != null)
            {
                SnappedFixture.ReleaseProduct(this);
            }

            IsHeld = false;
            IsReservedByCustomer = false;
            SnappedFixture = null;
            SnappedPointId = null;
            QuarterTurns = 0;
            transform.SetParent(parent, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            SetPhysicsHeld(false);
            SetFeedbackMaterial(defaultMaterial);
        }

        public void AdvanceQuarterTurn()
        {
            AdjustQuarterTurns(1);
        }

        public bool AdjustQuarterTurns(int delta)
        {
            if (!IsHeld || delta == 0)
            {
                return false;
            }

            QuarterTurns = ((QuarterTurns + delta) % 4 + 4) % 4;
            transform.localRotation =
                Quaternion.Euler(0f, QuarterTurns * 90f, 0f);
            return true;
        }

        public void SetPlacementPreview(bool isValid)
        {
            if (IsHeld)
            {
                SetFeedbackMaterial(isValid ? validPlacementMaterial : invalidPlacementMaterial);
            }
        }

        public void ClearPlacementPreview()
        {
            SetFeedbackMaterial(defaultMaterial);
        }

        public void ReleaseLoose(bool showInvalidPlacementFeedback = true)
        {
            IsHeld = false;
            IsReservedByCustomer = false;
            SnappedFixture = null;
            SnappedPointId = null;
            transform.SetParent(null, true);
            SetPhysicsHeld(false);
            if (showInvalidPlacementFeedback)
            {
                ShowTemporaryInvalidFeedback();
            }
            else
            {
                SetFeedbackMaterial(defaultMaterial);
            }
        }

        public void ApplySnappedPlacement(
            ShelfFixture fixture,
            string snapPointId,
            int quarterTurns,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            EnsureReferences();
            IsHeld = false;
            IsReservedByCustomer = false;
            SnappedFixture = fixture;
            SnappedPointId = snapPointId;
            QuarterTurns = quarterTurns;
            transform.SetParent(fixture.transform, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            SetPhysicsHeld(true);
            SetFeedbackMaterial(defaultMaterial);
        }

        internal bool TryAttachToCustomer(
            Transform attachmentPoint,
            out string error)
        {
            EnsureReferences();
            if (attachmentPoint == null || IsHeld || IsReservedByCustomer ||
                SnappedFixture == null ||
                string.IsNullOrWhiteSpace(SnappedPointId) ||
                SnappedFixture.GetOccupant(SnappedPointId) != this)
            {
                error = "Only an unreserved physical shelf unit can be taken by a customer.";
                return false;
            }

            IsReservedByCustomer = true;
            transform.SetParent(attachmentPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, QuarterTurns * 90f, 0f);
            SetPhysicsHeld(true);
            SetFeedbackMaterial(defaultMaterial);
            error = null;
            return true;
        }

        internal bool TryReturnFromCustomer(out string error)
        {
            EnsureReferences();
            if (!IsReservedByCustomer || SnappedFixture == null ||
                !SnappedFixture.TryGetSnapPoint(
                    SnappedPointId,
                    out ShelfSnapPointDefinition snapPoint) ||
                SnappedFixture.GetOccupant(SnappedPointId) != this)
            {
                error = "The customer product no longer has its reserved shelf placement.";
                return false;
            }

            ShelfFixture fixture = SnappedFixture;
            IsReservedByCustomer = false;
            transform.SetParent(fixture.transform, true);
            transform.SetPositionAndRotation(
                fixture.GetWorldPosition(snapPoint),
                fixture.GetWorldRotation(snapPoint) *
                Quaternion.Euler(0f, QuarterTurns * 90f, 0f));
            SetPhysicsHeld(true);
            SetFeedbackMaterial(defaultMaterial);
            error = null;
            return true;
        }

        internal bool TryMoveCustomerReservation(
            Transform attachmentPoint,
            out string error)
        {
            if (!IsReservedByCustomer || attachmentPoint == null ||
                SnappedFixture == null)
            {
                error = "The physical unit is not reserved by a customer.";
                return false;
            }

            transform.SetParent(attachmentPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, QuarterTurns * 90f, 0f);
            error = null;
            return true;
        }

        public bool TryGetPlacementState(out PlacedProductState state)
        {
            if (!IsSnapped || definition == null)
            {
                state = null;
                return false;
            }

            state = new PlacedProductState(
                definition.StableProductId,
                SnappedFixture.StableFixtureId,
                SnappedPointId,
                QuarterTurns);
            return true;
        }

        public void ResetToInitialLooseState()
        {
            EnsureReferences();
            IsHeld = false;
            IsReservedByCustomer = false;
            SnappedFixture = null;
            SnappedPointId = null;
            QuarterTurns = 0;
            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            SetPhysicsHeld(false);
            if (productRigidbody != null)
            {
                productRigidbody.linearVelocity = Vector3.zero;
                productRigidbody.angularVelocity = Vector3.zero;
            }
            SetFeedbackMaterial(defaultMaterial);
        }

        private void EnsureReferences()
        {
            if (productRigidbody == null)
            {
                productRigidbody = GetComponent<Rigidbody>();
            }

            if (feedbackRenderer == null)
            {
                feedbackRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void SetPhysicsHeld(bool isKinematic)
        {
            if (productRigidbody == null)
            {
                return;
            }

            productRigidbody.isKinematic = isKinematic;
            productRigidbody.useGravity = !isKinematic;
        }

        private void ShowTemporaryInvalidFeedback()
        {
            SetFeedbackMaterial(invalidPlacementMaterial);
            if (isActiveAndEnabled)
            {
                if (feedbackReset != null)
                {
                    StopCoroutine(feedbackReset);
                }
                feedbackReset = StartCoroutine(RestoreDefaultMaterialAfterDelay());
            }
        }

        private IEnumerator RestoreDefaultMaterialAfterDelay()
        {
            yield return new WaitForSeconds(0.75f);
            SetFeedbackMaterial(defaultMaterial);
            feedbackReset = null;
        }

        private void SetFeedbackMaterial(Material material)
        {
            if (feedbackRenderer != null && material != null)
            {
                feedbackRenderer.sharedMaterial = material;
            }
        }
    }
}
