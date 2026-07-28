using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    public sealed class FoundationSpikeInputPlayModeTests : InputTestFixture
    {
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTest]
        public IEnumerator FirstPersonMovementRespondsToInputSystemKeyboard()
        {
            yield return SceneManager.LoadSceneAsync("FoundationSpike", LoadSceneMode.Single);
            yield return null;

            FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(controller, Is.Not.Null);
            Transform player = controller.transform;
            Vector3 startingPosition = player.position;

            Press(keyboard.wKey, queueEventOnly: true);
            yield return new WaitForSeconds(0.3f);
            Release(keyboard.wKey, queueEventOnly: true);
            yield return null;

            Assert.That(Vector3.Distance(startingPosition, player.position), Is.GreaterThan(0.05f));
        }

        [UnityTest]
        public IEnumerator PickupFeedbackPlacementRejectionAndPersistenceCompleteTheRuntimeLoop()
        {
            yield return SceneManager.LoadSceneAsync("FoundationSpike", LoadSceneMode.Single);
            yield return null;

            FirstPersonController movement = Object.FindAnyObjectByType<FirstPersonController>();
            movement.enabled = false;
            ProductInteraction interaction = Object.FindAnyObjectByType<ProductInteraction>();
            ShelfFixture shelf = Object.FindAnyObjectByType<ShelfFixture>();
            PlacementSaveController saveController = Object.FindAnyObjectByType<PlacementSaveController>();
            ProductItem[] products = Object.FindObjectsByType<ProductItem>();
            ProductItem primary = products.Single(product => product.name == "Product Primary");
            ProductItem second = products.Single(product => product.name == "Product Occupied-Slot Fixture");
            Camera camera = Camera.main;
            camera.transform.LookAt(primary.transform.position);

            Press(keyboard.eKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.eKey, queueEventOnly: true);
            yield return null;

            Assert.That(interaction.HeldProduct, Is.SameAs(primary));
            primary.SetPlacementPreview(false);
            Assert.That(primary.GetComponent<Renderer>().sharedMaterial.name, Is.EqualTo("InvalidPlacement"));
            primary.SetPlacementPreview(true);
            Assert.That(primary.GetComponent<Renderer>().sharedMaterial.name, Is.EqualTo("ValidPlacement"));

            Assert.That(shelf.TryGetSnapPoint("slot-01", out ShelfSnapPointDefinition snapPoint), Is.True);
            primary.transform.position = shelf.GetWorldPosition(snapPoint);
            primary.AdvanceQuarterTurn();
            Assert.That(interaction.ReleaseHeldProduct(), Is.True);
            Assert.That(primary.IsSnapped, Is.True);
            Assert.That(primary.QuarterTurns, Is.EqualTo(1));

            Assert.That(interaction.TryPickUp(second), Is.True);
            second.transform.position = shelf.GetWorldPosition(snapPoint);
            Assert.That(interaction.ReleaseHeldProduct(), Is.False);
            Assert.That(shelf.GetOccupant("slot-01"), Is.SameAs(primary));
            Assert.That(second.IsSnapped, Is.False);

            Assert.That(primary.TryGetPlacementState(out PlacedProductState expected), Is.True);
            string savePath = Path.Combine(Application.temporaryCachePath, "foundation-spike-playmode-save.json");
            try
            {
                Assert.That(saveController.TrySaveToPath(savePath), Is.True);
                shelf.ReleaseProduct(primary);
                primary.ReleaseLoose();
                Assert.That(primary.IsSnapped, Is.False);

                Assert.That(saveController.TryLoadFromPath(savePath), Is.True);
                ProductItem restored = products.Single(product => product.IsSnapped);
                Assert.That(restored.TryGetPlacementState(out PlacedProductState actual), Is.True);
                Assert.That(actual, Is.EqualTo(expected));
            }
            finally
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
        }
    }
}
