using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    [Category("FirstStorePersistence")]
    public sealed class FirstPersonControllerSnapshotEditModeTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void TransformSnapshotRoundTripRestoresPositionYawPitchAndCharacterControllerState()
        {
            PlayerRig rig = CreateRig();
            FirstStorePlayerTransformSnapshot accepted = new(
                new Vector3(8.25f, 1.5f, -3.75f),
                271.5f,
                -42.25f);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(accepted, out string error), Is.True, error);
            FirstStorePlayerTransformSnapshot captured = rig.Controller.CaptureTransformSnapshot();

            Assert.That(
                rig.Controller.TryApplyTransformSnapshot(
                    new FirstStorePlayerTransformSnapshot(Vector3.one, 15f, 10f),
                    out error),
                Is.True,
                error);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(captured, out error), Is.True, error);

            Assert.That(rig.Controller.transform.position, Is.EqualTo(accepted.worldPosition));
            Assert.That(rig.Controller.transform.eulerAngles.y, Is.EqualTo(accepted.bodyYawDegrees).Within(0.001f));
            Assert.That(rig.CameraPivot.localEulerAngles.x, Is.EqualTo(360f + accepted.cameraPitchDegrees).Within(0.001f));
            Assert.That(rig.CharacterController.enabled, Is.True);
        }

        [Test]
        public void InvalidTransformSnapshotLeavesLiveStateUnchanged()
        {
            PlayerRig rig = CreateRig();
            FirstStorePlayerTransformSnapshot accepted = new(
                new Vector3(-2f, 0.5f, 6f),
                125f,
                18f);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(accepted, out string error), Is.True, error);
            FirstStorePlayerTransformSnapshot before = rig.Controller.CaptureTransformSnapshot();
            bool characterControllerWasEnabled = rig.CharacterController.enabled;

            FirstStorePlayerTransformSnapshot invalidPitch = new(
                new Vector3(0f, 0f, 0f),
                45f,
                86f);
            Assert.That(rig.Controller.TryPreflightApplyTransformSnapshot(invalidPitch, out error), Is.False);
            StringAssert.Contains("pitch", error);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(invalidPitch, out error), Is.False);

            FirstStorePlayerTransformSnapshot nonFinite = new(
                new Vector3(float.NaN, 0f, 0f),
                45f,
                0f);
            Assert.That(rig.Controller.TryPreflightApplyTransformSnapshot(nonFinite, out error), Is.False);
            StringAssert.Contains("non-finite", error);

            FirstStorePlayerTransformSnapshot after = rig.Controller.CaptureTransformSnapshot();
            Assert.That(after.worldPosition, Is.EqualTo(before.worldPosition));
            Assert.That(after.bodyYawDegrees, Is.EqualTo(before.bodyYawDegrees).Within(0.001f));
            Assert.That(after.cameraPitchDegrees, Is.EqualTo(before.cameraPitchDegrees).Within(0.001f));
            Assert.That(rig.CharacterController.enabled, Is.EqualTo(characterControllerWasEnabled));
        }

        [Test]
        public void ApplyingTransformSnapshotDoesNotChangeGameplayMode()
        {
            PlayerRig rig = CreateRig();
            FirstStorePlayerTransformSnapshot snapshot = new(Vector3.zero, 90f, 0f);

            rig.Controller.SetGameplayMode(false);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(snapshot, out string error), Is.True, error);
            Assert.That(rig.Controller.IsGameplayMode, Is.False);

            rig.Controller.SetGameplayMode(true);
            Assert.That(rig.Controller.TryApplyTransformSnapshot(snapshot, out error), Is.True, error);
            Assert.That(rig.Controller.IsGameplayMode, Is.True);
        }

        private PlayerRig CreateRig()
        {
            GameObject player = new("First Person Controller Test");
            createdObjects.Add(player);
            CharacterController characterController = player.AddComponent<CharacterController>();
            FirstPersonController controller = player.AddComponent<FirstPersonController>();
            GameObject pivot = new("Camera Pivot");
            createdObjects.Add(pivot);
            pivot.transform.SetParent(player.transform, false);

            SerializedObject serialized = new(controller);
            serialized.FindProperty("characterController").objectReferenceValue = characterController;
            serialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            controller.SetGameplayMode(false);
            return new PlayerRig(controller, characterController, pivot.transform);
        }

        private readonly struct PlayerRig
        {
            public PlayerRig(
                FirstPersonController controller,
                CharacterController characterController,
                Transform cameraPivot)
            {
                Controller = controller;
                CharacterController = characterController;
                CameraPivot = cameraPivot;
            }

            public FirstPersonController Controller { get; }
            public CharacterController CharacterController { get; }
            public Transform CameraPivot { get; }
        }
    }
}
