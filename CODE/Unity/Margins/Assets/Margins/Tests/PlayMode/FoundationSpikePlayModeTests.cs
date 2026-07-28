using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    public sealed class FoundationSpikePlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsWithEveryFoundationComponent()
        {
            yield return SceneManager.LoadSceneAsync("FoundationSpike", LoadSceneMode.Single);
            yield return null;

            Assert.That(Object.FindAnyObjectByType<FirstPersonController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ProductInteraction>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ShelfFixture>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<PlacementSaveController>(), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<ProductItem>(), Has.Length.EqualTo(2));
            Assert.That(Object.FindAnyObjectByType<WaypointNavAgent>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator NavigationPlaceholderMovesOnACompletePath()
        {
            yield return SceneManager.LoadSceneAsync("FoundationSpike", LoadSceneMode.Single);
            yield return null;

            WaypointNavAgent waypointAgent = Object.FindAnyObjectByType<WaypointNavAgent>();
            Assert.That(waypointAgent, Is.Not.Null);
            Assert.That(waypointAgent.Agent.isOnNavMesh, Is.True);

            NavMeshPath path = new();
            Assert.That(waypointAgent.Agent.CalculatePath(waypointAgent.PointB.position, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));

            Vector3 startingPosition = waypointAgent.transform.position;
            float timeout = Time.time + 3f;
            while (Time.time < timeout && Vector3.Distance(startingPosition, waypointAgent.transform.position) < 0.5f)
            {
                yield return null;
            }

            Assert.That(Vector3.Distance(startingPosition, waypointAgent.transform.position), Is.GreaterThan(0.5f));
        }
    }
}
