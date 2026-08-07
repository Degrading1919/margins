using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    [Category("FirstStoreNavigation")]
    public sealed class FirstStorePhysicalNavigationPlayModeTests
    {
        [UnityTest]
        public IEnumerator BakedNavigationRoutesExteriorActorsThroughActualDoorway()
        {
            yield return LoadValidationScene();

            Transform arrival = Require("Customer Exterior Arrival Boundary").transform;
            Transform browse = Require("Customer Browse Cola").transform;
            Assert.That(
                NavMesh.SamplePosition(
                    arrival.position,
                    out NavMeshHit arrivalHit,
                    1.5f,
                    NavMesh.AllAreas),
                Is.True);
            Assert.That(
                NavMesh.SamplePosition(
                    browse.position,
                    out NavMeshHit browseHit,
                    1.5f,
                    NavMesh.AllAreas),
                Is.True);

            NavMeshPath path = new();
            Assert.That(
                NavMesh.CalculatePath(
                    arrivalHit.position,
                    browseHit.position,
                    NavMesh.AllAreas,
                    path),
                Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(
                TryFindPlaneCrossingX(path.corners, -7f, out float crossingX),
                Is.True,
                "The exterior-to-interior path must cross the storefront plane.");
            Assert.That(
                Mathf.Abs(crossingX),
                Is.LessThan(0.72f),
                "The path must cross through the doorway gap, not storefront glass.");
        }

        [UnityTest]
        public IEnumerator PhysicalBlockersAndDetailedEmployeesUsePrimitivePresence()
        {
            yield return LoadValidationScene();

            AssertSolidPrimitiveCollider("Experience Car Body");
            AssertSolidPrimitiveCollider("Left Storefront Glass");
            AssertSolidPrimitiveCollider("Right Storefront Glass");
            AssertSolidPrimitiveCollider("Left Wall");
            AssertSolidPrimitiveCollider("Right Wall");
            Assert.That(
                Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include),
                Is.Empty,
                "Placeholder geometry should stay on primitive/compound colliders.");

            PlaceableFixtureComponent[] fixtures =
                Object.FindObjectsByType<PlaceableFixtureComponent>(
                    FindObjectsInactive.Include);
            Assert.That(fixtures.Length, Is.GreaterThanOrEqualTo(4));
            foreach (PlaceableFixtureComponent fixture in fixtures)
            {
                Assert.That(
                    fixture.GetComponentsInChildren<Collider>(true).Any(collider =>
                        collider.enabled && !collider.isTrigger),
                    Is.True,
                    fixture.name);
                NavMeshObstacle obstacle = fixture.GetComponent<NavMeshObstacle>();
                Assert.That(obstacle, Is.Not.Null, fixture.name);
                Assert.That(obstacle.carving, Is.True, fixture.name);
            }

            LocalNavigationAgent[] employees = Object
                .FindObjectsByType<LocalNavigationAgent>(FindObjectsInactive.Include)
                .Where(agent => agent.name.StartsWith("Detailed "))
                .ToArray();
            Assert.That(employees.Length, Is.EqualTo(3));
            foreach (LocalNavigationAgent employee in employees)
            {
                Assert.That(
                    employee.TryValidateConfiguration(out string error),
                    Is.True,
                    error);
                Assert.That(employee.PhysicalCollider.isTrigger, Is.False);
                Assert.That(employee.GetComponent<Rigidbody>(), Is.Null);
                Assert.That(
                    employee.Agent.obstacleAvoidanceType,
                    Is.Not.EqualTo(ObstacleAvoidanceType.NoObstacleAvoidance));
            }

            TextMesh[] statusWords = Object
                .FindObjectsByType<TextMesh>(FindObjectsInactive.Include)
                .Where(label =>
                    label.name == "Customer Status Diagnostic" ||
                    (label.name.StartsWith("Detailed ") &&
                     label.name.EndsWith(" Label")))
                .ToArray();
            Assert.That(
                statusWords.All(label => !label.gameObject.activeInHierarchy),
                Is.True,
                "Floating actor status words must not appear in the normal build.");
        }

        [UnityTest]
        public IEnumerator CustomerRepathsWhenItsMovableFixtureTargetMoves()
        {
            yield return LoadValidationScene();
            PreparedStore prepared = PrepareOpenStore();

            Assert.That(
                prepared.Flow.TryAdmitCustomerNow(
                    out string customerId,
                    out string error),
                Is.True,
                error);
            LocalNavigationAgent navigation = null;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline)
            {
                prepared.Flow.TryGetCustomerNavigationAgent(
                    customerId,
                    out navigation);
                if (navigation != null && navigation.CurrentTarget != null &&
                    navigation.RepathCount > 0)
                {
                    break;
                }
                yield return null;
            }

            Assert.That(navigation, Is.Not.Null);
            Assert.That(navigation.CurrentTarget, Is.Not.Null);
            Assert.That(navigation.PhysicalCollider.isTrigger, Is.False);
            PlaceableFixtureComponent targetFixture = navigation.CurrentTarget
                .GetComponentInParent<PlaceableFixtureComponent>();
            Assert.That(targetFixture, Is.Not.Null);
            int repathsBeforeMove = navigation.RepathCount;
            Vector3 destinationBeforeMove = navigation.LastRequestedDestination;
            GridPosition destination = targetFixture.StableFixtureInstanceId.Contains("cola")
                ? new GridPosition(4, 21)
                : new GridPosition(18, 21);

            FixturePlacementResult moved = prepared.Placement.TryMove(
                targetFixture,
                destination,
                0);
            Assert.That(moved.IsSuccess, Is.True, moved.Failure.ToString());

            deadline = Time.realtimeSinceStartup + 2f;
            while (navigation.RepathCount <= repathsBeforeMove &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(navigation.CurrentTarget, Is.Not.Null);
            Assert.That(navigation.RepathCount, Is.GreaterThan(repathsBeforeMove));
            Assert.That(
                Vector3.Distance(
                    destinationBeforeMove,
                    navigation.LastRequestedDestination),
                Is.GreaterThan(1f));
            Assert.That(
                navigation.State,
                Is.Not.EqualTo(LocalNavigationState.PathUnavailable));
        }

        [UnityTest]
        public IEnumerator ActiveQueueRestrictsCheckoutButNotAnIdleFixture()
        {
            yield return LoadValidationScene();
            PreparedStore prepared = PrepareOpenStore();

            Assert.That(
                prepared.Flow.TryAdmitCustomerNow(out _, out string error),
                Is.True,
                error);
            float deadline = Time.realtimeSinceStartup + 18f;
            while (prepared.Flow.QueuedCustomerCount == 0 &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(prepared.Flow.QueuedCustomerCount, Is.EqualTo(1));

            PlaceableFixtureComponent checkout = Require("Essential Checkout Fixture")
                .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementResult blocked = prepared.Placement.TryMove(
                checkout,
                new GridPosition(18, 18),
                0);
            Assert.That(
                blocked.Failure,
                Is.EqualTo(FixturePlacementFailure.OperatingStateRestricted));

            PlaceableFixtureComponent deliveryDrop = Require("Stockroom Delivery Drop")
                .GetComponent<PlaceableFixtureComponent>();
            FixturePlacementResult idleMove = prepared.Placement.TryMove(
                deliveryDrop,
                new GridPosition(1, 12),
                0);
            Assert.That(idleMove.IsSuccess, Is.True, idleMove.Failure.ToString());
        }

        private static PreparedStore PrepareOpenStore()
        {
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();

            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);
            SetField(flow, "queuePatienceSeconds", 60f);
            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            foreach (string productId in checkout.ConfiguredProductIds)
            {
                Assert.That(
                    checkout.TryGetProductDefinition(
                        productId,
                        out ProductDefinition product),
                    Is.True);
                Assert.That(
                    delivery.TryRemoveOneUnit(
                        product,
                        out ProductItem loose,
                        out _,
                        out _,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    stocking.TryPickUpLooseUnit(loose, out _, out error),
                    Is.True,
                    error);
                Assert.That(
                    stocking.TryStockHeldUnit(0, out error),
                    Is.True,
                    error);
            }

            for (int step = 0; step < 8 && cleaning.NeedsCleaning; step++)
            {
                cleaning.TryApplyProgress(1);
            }
            Assert.That(cleaning.IsComplete, Is.True);
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            return new PreparedStore(flow, placement);
        }

        private static IEnumerator LoadValidationScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;
        }

        private static void AssertSolidPrimitiveCollider(string objectName)
        {
            GameObject target = Require(objectName);
            Collider collider = target.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null, objectName);
            Assert.That(collider.enabled, Is.True, objectName);
            Assert.That(collider.isTrigger, Is.False, objectName);
            Assert.That(collider, Is.Not.InstanceOf<MeshCollider>(), objectName);
        }

        private static bool TryFindPlaneCrossingX(
            Vector3[] corners,
            float z,
            out float crossingX)
        {
            for (int index = 1; index < corners.Length; index++)
            {
                Vector3 from = corners[index - 1];
                Vector3 to = corners[index];
                if ((from.z - z) * (to.z - z) > 0f ||
                    Mathf.Approximately(from.z, to.z))
                {
                    continue;
                }

                float interpolation = Mathf.InverseLerp(from.z, to.z, z);
                crossingX = Mathf.Lerp(from.x, to.x, interpolation);
                return true;
            }

            crossingX = 0f;
            return false;
        }

        private static GameObject Require(string objectName)
        {
            GameObject result = GameObject.Find(objectName);
            Assert.That(result, Is.Not.Null, objectName);
            return result;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private readonly struct PreparedStore
        {
            public PreparedStore(
                StoreCustomerFlowController flow,
                FixturePlacementController placement)
            {
                Flow = flow;
                Placement = placement;
            }

            public StoreCustomerFlowController Flow { get; }
            public FixturePlacementController Placement { get; }
        }
    }
}
