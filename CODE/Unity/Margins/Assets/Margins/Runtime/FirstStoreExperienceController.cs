using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Presentation-only orchestration for the first store: world-state feedback,
    /// objective beacon, procedural audio, and small prop animations.
    /// </summary>
    public sealed class FirstStoreExperienceController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private FirstStoreInteractionController interaction;
        [SerializeField] private FirstStorePromptPresenter promptPresenter;
        [SerializeField] private DeliveryBoxComponent delivery;
        [SerializeField] private StockingController stocking;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private CleaningTaskComponent cleaning;
        [SerializeField] private StoreOperatingController store;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private PlaceableFixtureComponent checkoutFixture;
        [SerializeField] private ProductDefinition colaProduct;
        [SerializeField] private ProductDefinition chipsProduct;

        [Header("Dynamic presentation")]
        [SerializeField] private Transform deliveryLidPivot;
        [SerializeField] private Collider colaDeliveryCollider;
        [SerializeField] private Collider chipsDeliveryCollider;
        [SerializeField] private Renderer colaDeliveryRenderer;
        [SerializeField] private Renderer chipsDeliveryRenderer;
        [SerializeField] private Collider checkoutInteractionCollider;
        [SerializeField] private TextMesh storefrontStateText;
        [SerializeField] private Renderer storefrontStateRenderer;
        [SerializeField] private TextMesh checkoutDisplayText;
        [SerializeField] private GameObject checkoutColaProp;
        [SerializeField] private GameObject checkoutChipsProp;
        [SerializeField] private Transform cleaningSpillVisual;
        [SerializeField] private Renderer cleaningSpillRenderer;
        [SerializeField] private Transform objectiveBeacon;
        [SerializeField] private Transform focusIndicator;
        [SerializeField] private Renderer[] focusIndicatorRenderers;
        [SerializeField] private Light[] interiorLights;
        [SerializeField] private Renderer[] practicalLightRenderers;

        private readonly List<AudioClip> generatedClips = new();
        private AudioSource effectsSource;
        private AudioSource ambienceSource;
        private AudioClip ambienceClip;
        private AudioClip footstepClip;
        private AudioClip briskFootstepClip;
        private AudioClip successClip;
        private AudioClip invalidClip;
        private AudioClip cardboardClip;
        private AudioClip pickupClip;
        private AudioClip placementClip;
        private AudioClip scannerClip;
        private AudioClip saleClip;
        private AudioClip cleaningClip;
        private AudioClip chimeClip;
        private float[] baseLightIntensities;
        private Vector3 spillInitialScale;
        private Vector3 beaconTargetPosition;
        private float deliveryLidOpenAmount;
        private bool wasInside;
        private StoreOperatingState priorStoreState;
        private int priorCleaningProgress;
        private int priorCompletedTransactions;
        private Material storefrontStateMaterial;
        private MaterialPropertyBlock lightPropertyBlock;
        private Renderer[] checkoutFixtureRenderers;
        private Collider[] checkoutFixtureColliders;
        private MaterialPropertyBlock focusPropertyBlock;
        private float focusFeedbackUntil;
        private bool focusFeedbackSucceeded = true;

        public bool TryValidateConfiguration(out string error)
        {
            if (player == null || interaction == null || promptPresenter == null ||
                delivery == null || stocking == null || checkout == null ||
                stagedCheckout == null || cleaning == null || store == null ||
                fixturePlacement == null || checkoutFixture == null ||
                colaProduct == null || chipsProduct == null || objectiveBeacon == null)
            {
                error = "First-store experience presentation is missing a required reference.";
                return false;
            }

            error = null;
            return true;
        }

        private void Awake()
        {
            CreateAudio();
            spillInitialScale = cleaningSpillVisual != null
                ? cleaningSpillVisual.localScale
                : Vector3.one;
            baseLightIntensities = new float[interiorLights?.Length ?? 0];
            for (int index = 0; index < baseLightIntensities.Length; index++)
            {
                baseLightIntensities[index] = interiorLights[index] != null
                    ? interiorLights[index].intensity
                    : 0f;
            }

            if (storefrontStateRenderer != null)
            {
                storefrontStateMaterial = storefrontStateRenderer.material;
            }
            lightPropertyBlock = new MaterialPropertyBlock();
            focusPropertyBlock = new MaterialPropertyBlock();
            checkoutFixtureRenderers = checkoutFixture != null
                ? checkoutFixture.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            checkoutFixtureColliders = checkoutFixture != null
                ? checkoutFixture.GetComponentsInChildren<Collider>(true)
                : Array.Empty<Collider>();
        }

        private void OnEnable()
        {
            if (interaction != null)
            {
                interaction.InteractionResolved += HandleInteractionResolved;
            }
            if (player != null)
            {
                player.Footstep += HandleFootstep;
                player.Landed += HandleLanding;
            }
        }

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            wasInside = player.transform.position.z > -5.05f;
            priorStoreState = store.State;
            priorCleaningProgress = cleaning.CompletedProgressUnits;
            priorCompletedTransactions = checkout.CompletedTransactionCount;
            if (ambienceSource != null && ambienceClip != null)
            {
                ambienceSource.clip = ambienceClip;
                ambienceSource.loop = true;
                ambienceSource.volume = 0.055f;
                ambienceSource.Play();
            }
            RefreshImmediateState();
        }

        private void OnDisable()
        {
            if (interaction != null)
            {
                interaction.InteractionResolved -= HandleInteractionResolved;
            }
            if (player != null)
            {
                player.Footstep -= HandleFootstep;
                player.Landed -= HandleLanding;
            }
        }

        private void OnDestroy()
        {
            for (int index = 0; index < generatedClips.Count; index++)
            {
                if (generatedClips[index] != null)
                {
                    Destroy(generatedClips[index]);
                }
            }
            generatedClips.Clear();
        }

        private void Update()
        {
            UpdateDeliveryPresentation();
            UpdateFixtureAndCheckoutPresentation();
            UpdateCleaningPresentation();
            UpdateStorePresentation();
            UpdateObjectiveBeacon();
            UpdateFocusIndicator();
            UpdateThresholdAudio();
        }

        private void RefreshImmediateState()
        {
            deliveryLidOpenAmount = delivery.IsOpen ? 1f : 0f;
            ApplyDeliveryLid();
            UpdateDeliveryPresentation();
            UpdateFixtureAndCheckoutPresentation();
            UpdateCleaningPresentation();
            UpdateStorePresentation();
            UpdateObjectiveBeacon(true);
        }

        private void UpdateDeliveryPresentation()
        {
            bool isOpen = delivery.IsOpen;
            deliveryLidOpenAmount = Mathf.MoveTowards(
                deliveryLidOpenAmount,
                isOpen ? 1f : 0f,
                Time.deltaTime * 2.5f);
            ApplyDeliveryLid();

            int colaRemaining = RemainingUnits(colaProduct);
            int chipsRemaining = RemainingUnits(chipsProduct);
            SetDeliveryChoice(
                colaDeliveryCollider,
                colaDeliveryRenderer,
                isOpen && colaRemaining > 0);
            SetDeliveryChoice(
                chipsDeliveryCollider,
                chipsDeliveryRenderer,
                isOpen && chipsRemaining > 0);
        }

        private void ApplyDeliveryLid()
        {
            if (deliveryLidPivot != null)
            {
                deliveryLidPivot.localRotation = Quaternion.Euler(
                    Mathf.SmoothStep(0f, -108f, deliveryLidOpenAmount),
                    0f,
                    0f);
            }
        }

        private static void SetDeliveryChoice(
            Collider collider,
            Renderer renderer,
            bool visible)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        private int RemainingUnits(ProductDefinition product)
        {
            return delivery.TryGetConfiguredProductRemaining(
                product,
                out _,
                out int remaining,
                out _)
                    ? remaining
                    : 0;
        }

        private void UpdateFixtureAndCheckoutPresentation()
        {
            bool fixturePlaced = fixturePlacement.IsPlaced(
                checkoutFixture.StableFixtureInstanceId);
            bool fixturePreview =
                checkoutFixture.PreviewState != FixturePlacementPreviewState.None;
            bool fixtureVisible = fixturePlaced || fixturePreview;
            for (int index = 0; index < checkoutFixtureRenderers.Length; index++)
            {
                if (checkoutFixtureRenderers[index] != null &&
                    checkoutFixtureRenderers[index].transform != checkoutFixture.transform)
                {
                    checkoutFixtureRenderers[index].enabled = fixtureVisible;
                }
            }
            for (int index = 0; index < checkoutFixtureColliders.Length; index++)
            {
                Collider fixtureCollider = checkoutFixtureColliders[index];
                if (fixtureCollider != null &&
                    fixtureCollider != checkoutInteractionCollider &&
                    fixtureCollider.transform != checkoutFixture.transform)
                {
                    fixtureCollider.enabled = fixtureVisible;
                }
            }
            if (checkoutInteractionCollider != null)
            {
                checkoutInteractionCollider.enabled = fixturePlaced;
            }

            if (checkoutDisplayText == null)
            {
                return;
            }

            if (!fixturePlaced)
            {
                checkoutDisplayText.text = "REGISTER\nOFFLINE";
                SetCheckoutProps(false, false);
                return;
            }

            if (store.State != StoreOperatingState.Open &&
                store.State != StoreOperatingState.Closing)
            {
                checkoutDisplayText.text = "REGISTER\nCLOSED";
                SetCheckoutProps(false, false);
                return;
            }

            if (stagedCheckout.AllBasketsComplete)
            {
                checkoutDisplayText.text =
                    $"SALE COMPLETE\n{FormatCents(checkout.GrossSalesCents)}";
                SetCheckoutProps(false, false);
                return;
            }

            switch (stagedCheckout.NextAction)
            {
                case StagedCheckoutPrimaryAction.Begin:
                    checkoutDisplayText.text = "NEXT\nCUSTOMER";
                    SetCheckoutProps(true, true);
                    break;

                case StagedCheckoutPrimaryAction.Scan:
                    checkoutDisplayText.text =
                        $"SCAN {ShortProductName(stagedCheckout.ActiveProduct)}\n" +
                        $"{FormatCents(stagedCheckout.SubtotalCents)}";
                    SetCheckoutProps(
                        stagedCheckout.ActiveProduct == colaProduct,
                        stagedCheckout.ActiveProduct == chipsProduct);
                    break;

                case StagedCheckoutPrimaryAction.Complete:
                    checkoutDisplayText.text =
                        $"TOTAL\n{FormatCents(stagedCheckout.SubtotalCents)}";
                    SetCheckoutProps(false, false);
                    break;

                default:
                    checkoutDisplayText.text = "LANE\nREADY";
                    SetCheckoutProps(false, false);
                    break;
            }
        }

        private void SetCheckoutProps(bool showCola, bool showChips)
        {
            if (checkoutColaProp != null)
            {
                checkoutColaProp.SetActive(showCola);
            }
            if (checkoutChipsProp != null)
            {
                checkoutChipsProp.SetActive(showChips);
            }
        }

        private void UpdateCleaningPresentation()
        {
            if (cleaningSpillVisual != null)
            {
                float remaining = cleaning.RequiredProgressUnits <= 0
                    ? 0f
                    : 1f - Mathf.Clamp01(
                        cleaning.CompletedProgressUnits /
                        (float)cleaning.RequiredProgressUnits);
                Vector3 targetScale = spillInitialScale * Mathf.Lerp(0.05f, 1f, remaining);
                cleaningSpillVisual.localScale = Vector3.Lerp(
                    cleaningSpillVisual.localScale,
                    targetScale,
                    1f - Mathf.Exp(-9f * Time.deltaTime));
            }

            if (cleaningSpillRenderer != null)
            {
                cleaningSpillRenderer.enabled = !cleaning.IsComplete ||
                    (cleaningSpillVisual != null &&
                     cleaningSpillVisual.localScale.sqrMagnitude > 0.01f);
            }

            if (cleaning.CompletedProgressUnits != priorCleaningProgress)
            {
                priorCleaningProgress = cleaning.CompletedProgressUnits;
                Play(cleaningClip, cleaning.IsComplete ? 1.1f : 0.75f);
            }
        }

        private void UpdateStorePresentation()
        {
            if (storefrontStateText != null)
            {
                storefrontStateText.text = store.State switch
                {
                    StoreOperatingState.Closed => "CLOSED",
                    StoreOperatingState.Preparing => "SETUP",
                    StoreOperatingState.Open => "OPEN",
                    StoreOperatingState.Closing => "CLOSING",
                    StoreOperatingState.ClosedWithResultPending => "RESULT READY",
                    _ => "CLOSED"
                };
            }

            bool open = store.State == StoreOperatingState.Open ||
                        store.State == StoreOperatingState.Closing;
            Color signColor = open
                ? new Color(0.1f, 0.95f, 0.7f, 1f)
                : store.State == StoreOperatingState.ClosedWithResultPending
                    ? new Color(1f, 0.6f, 0.22f, 1f)
                    : new Color(0.72f, 0.18f, 0.14f, 1f);
            if (storefrontStateMaterial != null)
            {
                storefrontStateMaterial.color = signColor * 0.4f;
                storefrontStateMaterial.SetColor("_EmissionColor", signColor * 3.2f);
            }

            float lightMultiplier = open ? 1.05f :
                store.State == StoreOperatingState.Preparing ? 0.88f : 0.52f;
            for (int index = 0; index < baseLightIntensities.Length; index++)
            {
                if (interiorLights[index] != null)
                {
                    interiorLights[index].intensity = Mathf.Lerp(
                        interiorLights[index].intensity,
                        baseLightIntensities[index] * lightMultiplier,
                        1f - Mathf.Exp(-3.5f * Time.deltaTime));
                }
            }
            for (int index = 0; practicalLightRenderers != null &&
                                index < practicalLightRenderers.Length; index++)
            {
                Renderer renderer = practicalLightRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(lightPropertyBlock);
                Color emission = new Color(1f, 0.68f, 0.36f, 1f) *
                                 Mathf.Lerp(0.9f, 3.2f, lightMultiplier / 1.05f);
                lightPropertyBlock.SetColor("_EmissionColor", emission);
                renderer.SetPropertyBlock(lightPropertyBlock);
            }

            if (store.State != priorStoreState)
            {
                StoreOperatingState previous = priorStoreState;
                priorStoreState = store.State;
                if (store.State == StoreOperatingState.Open)
                {
                    Play(chimeClip, 1f);
                }
                else if (store.State == StoreOperatingState.ClosedWithResultPending)
                {
                    Play(saleClip, 0.9f);
                }
                else if (previous == StoreOperatingState.Open)
                {
                    Play(successClip, 0.75f);
                }
            }

            if (checkout.CompletedTransactionCount != priorCompletedTransactions)
            {
                priorCompletedTransactions = checkout.CompletedTransactionCount;
                cleaning.TryCreateMess();
                Play(saleClip, 1f);
            }
        }

        private void UpdateObjectiveBeacon(bool immediate = false)
        {
            if (objectiveBeacon == null)
            {
                return;
            }

            Transform target = promptPresenter.CurrentObjectiveTarget;
            if (target == null || promptPresenter.CurrentObjectiveKind == FirstStoreObjectiveKind.Complete)
            {
                objectiveBeacon.gameObject.SetActive(false);
                return;
            }

            objectiveBeacon.gameObject.SetActive(true);
            beaconTargetPosition = target.position + Vector3.up * 1.85f;
            float pulse = Mathf.Sin(Time.unscaledTime * 3.2f) * 0.1f;
            Vector3 desired = beaconTargetPosition + Vector3.up * pulse;
            objectiveBeacon.position = immediate
                ? desired
                : Vector3.Lerp(
                    objectiveBeacon.position,
                    desired,
                    1f - Mathf.Exp(-8f * Time.deltaTime));
            objectiveBeacon.Rotate(0f, 58f * Time.deltaTime, 0f, Space.World);
            float scale = 0.92f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
            objectiveBeacon.localScale = Vector3.one * scale;
        }

        private void UpdateFocusIndicator()
        {
            if (focusIndicator == null)
            {
                return;
            }

            bool visible = interaction != null &&
                           interaction.IsWorldInteractionEnabled &&
                           interaction.CurrentPrompt != null &&
                           interaction.HasFocusedWorldPoint;
            if (focusIndicator.gameObject.activeSelf != visible)
            {
                focusIndicator.gameObject.SetActive(visible);
            }
            if (!visible)
            {
                return;
            }

            Camera viewCamera = Camera.main;
            Vector3 point = interaction.FocusedWorldPoint +
                            interaction.FocusedWorldNormal * 0.025f;
            focusIndicator.position = point;
            if (viewCamera != null)
            {
                Vector3 fromCamera = point - viewCamera.transform.position;
                if (fromCamera.sqrMagnitude > 0.001f)
                {
                    focusIndicator.rotation = Quaternion.LookRotation(
                        fromCamera.normalized,
                        Vector3.up);
                }

                float distance = fromCamera.magnitude;
                float baseScale = Mathf.Clamp(distance * 0.065f, 0.16f, 0.34f);
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.045f;
                if (Time.unscaledTime < focusFeedbackUntil &&
                    !focusFeedbackSucceeded)
                {
                    point += focusIndicator.right *
                             Mathf.Sin(Time.unscaledTime * 46f) * 0.025f;
                    focusIndicator.position = point;
                }
                focusIndicator.localScale = Vector3.one * baseScale * pulse;
            }

            Color color = Time.unscaledTime < focusFeedbackUntil
                ? focusFeedbackSucceeded
                    ? new Color(0.12f, 0.9f, 0.72f, 1f)
                    : new Color(1f, 0.22f, 0.14f, 1f)
                : new Color(0.2f, 0.88f, 0.74f, 1f);
            for (int index = 0;
                 focusIndicatorRenderers != null &&
                 index < focusIndicatorRenderers.Length;
                 index++)
            {
                Renderer renderer = focusIndicatorRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(focusPropertyBlock);
                focusPropertyBlock.SetColor("_BaseColor", color);
                focusPropertyBlock.SetColor("_Color", color);
                focusPropertyBlock.SetColor("_EmissionColor", color * 3.2f);
                renderer.SetPropertyBlock(focusPropertyBlock);
            }
        }

        private void UpdateThresholdAudio()
        {
            bool inside = player.transform.position.z > -5.05f;
            if (inside != wasInside)
            {
                wasInside = inside;
                Play(chimeClip, inside ? 0.72f : 0.45f);
            }

            if (ambienceSource != null)
            {
                float target = inside ? 0.065f : 0.028f;
                ambienceSource.volume = Mathf.Lerp(
                    ambienceSource.volume,
                    target,
                    1f - Mathf.Exp(-2f * Time.deltaTime));
            }
        }

        private void HandleInteractionResolved(FirstStoreInteractionFeedback feedback)
        {
            focusFeedbackSucceeded = feedback.Succeeded;
            focusFeedbackUntil = Time.unscaledTime +
                                 (feedback.Succeeded ? 0.22f : 0.48f);
            if (!feedback.Succeeded)
            {
                Play(invalidClip, 0.72f);
                return;
            }

            string id = feedback.TargetId ?? string.Empty;
            if (id.Contains("delivery-open", StringComparison.Ordinal))
            {
                Play(cardboardClip, 0.9f);
            }
            else if (id.Contains("delivery-", StringComparison.Ordinal))
            {
                Play(pickupClip, 0.82f);
            }
            else if (id.Contains("stock-", StringComparison.Ordinal))
            {
                Play(placementClip, 0.82f);
            }
            else if (id.Contains("checkout", StringComparison.Ordinal))
            {
                Play(
                    feedback.Action.Contains("Finish", StringComparison.OrdinalIgnoreCase)
                        ? saleClip
                        : scannerClip,
                    0.85f);
            }
            else if (id.Contains("cleaning", StringComparison.Ordinal))
            {
                Play(cleaningClip, 0.68f);
            }
            else if (id.Contains("fixture", StringComparison.Ordinal))
            {
                Play(placementClip, 0.8f);
            }
            else if (id.Contains("store-control", StringComparison.Ordinal))
            {
                Play(chimeClip, 0.75f);
            }
            else
            {
                Play(successClip, 0.58f);
            }
        }

        private void HandleFootstep(bool brisk)
        {
            if (effectsSource == null)
            {
                return;
            }

            effectsSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            effectsSource.PlayOneShot(brisk ? briskFootstepClip : footstepClip, brisk ? 0.24f : 0.18f);
            effectsSource.pitch = 1f;
        }

        private void HandleLanding()
        {
            Play(footstepClip, 0.28f);
        }

        private void CreateAudio()
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.volume = 0.78f;

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;

            ambienceClip = Register(CreateAmbientClip());
            footstepClip = Register(CreateNoiseClip("Floor step", 0.09f, 95f, 0.38f, 4101));
            briskFootstepClip = Register(CreateNoiseClip("Brisk floor step", 0.08f, 125f, 0.42f, 4102));
            successClip = Register(CreateToneClip("Action accepted", 520f, 720f, 0.12f, 0.2f));
            invalidClip = Register(CreateToneClip("Action blocked", 150f, 112f, 0.18f, 0.22f));
            cardboardClip = Register(CreateNoiseClip("Delivery open", 0.28f, 58f, 0.7f, 4103));
            pickupClip = Register(CreateToneClip("Product pickup", 310f, 460f, 0.1f, 0.18f));
            placementClip = Register(CreateNoiseClip("Product placed", 0.12f, 180f, 0.5f, 4104));
            scannerClip = Register(CreateToneClip("Scanner beep", 1060f, 1320f, 0.105f, 0.24f));
            saleClip = Register(CreateToneClip("Sale complete", 620f, 980f, 0.34f, 0.24f));
            cleaningClip = Register(CreateNoiseClip("Cleaning swish", 0.24f, 320f, 0.28f, 4105));
            chimeClip = Register(CreateChimeClip());
        }

        private AudioClip Register(AudioClip clip)
        {
            generatedClips.Add(clip);
            return clip;
        }

        private static AudioClip CreateAmbientClip()
        {
            const int sampleRate = 44100;
            const float duration = 4f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new(9091);
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float hum = Mathf.Sin(time * Mathf.PI * 2f * 60f) * 0.016f;
                hum += Mathf.Sin(time * Mathf.PI * 2f * 120f) * 0.006f;
                float airflow = ((float)random.NextDouble() * 2f - 1f) * 0.009f;
                float fade = Mathf.Min(1f, index / 1000f) *
                             Mathf.Min(1f, (sampleCount - index) / 1000f);
                samples[index] = (hum + airflow) * fade;
            }
            AudioClip clip = AudioClip.Create(
                "Store HVAC ambience",
                sampleCount,
                1,
                sampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateToneClip(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float t = index / (float)Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += Mathf.PI * 2f * frequency / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * t);
                envelope *= envelope;
                samples[index] = Mathf.Sin(phase) * envelope * amplitude;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseClip(
            string name,
            float duration,
            float toneFrequency,
            float amplitude,
            int seed)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random random = new(seed);
            float filtered = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float t = index / (float)Mathf.Max(1, sampleCount - 1);
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.08f);
                float tone = Mathf.Sin(index / (float)sampleRate * Mathf.PI * 2f * toneFrequency);
                float envelope = Mathf.Pow(1f - t, 2.4f) * Mathf.Min(1f, t * 30f);
                samples[index] = (filtered * 0.72f + tone * 0.28f) * envelope * amplitude;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateChimeClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.72f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float first = Mathf.Sin(time * Mathf.PI * 2f * 784f) *
                              Mathf.Exp(-time * 7f);
                float secondTime = Mathf.Max(0f, time - 0.18f);
                float second = time < 0.18f
                    ? 0f
                    : Mathf.Sin(secondTime * Mathf.PI * 2f * 988f) *
                      Mathf.Exp(-secondTime * 6f);
                samples[index] = (first + second) * 0.18f;
            }
            AudioClip clip = AudioClip.Create("Entry chime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null)
            {
                effectsSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        private static string ShortProductName(ProductDefinition product)
        {
            if (product == null)
            {
                return "ITEM";
            }
            if (product.StableProductId.Contains("cola", StringComparison.Ordinal))
            {
                return "COLA";
            }
            if (product.StableProductId.Contains("chips", StringComparison.Ordinal))
            {
                return "CHIPS";
            }
            return string.IsNullOrWhiteSpace(product.DisplayName)
                ? "ITEM"
                : product.DisplayName.ToUpperInvariant();
        }

        private static string FormatCents(long cents)
        {
            bool negative = cents < 0;
            ulong absolute = negative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            string dollars = (absolute / 100).ToString(
                "N0",
                System.Globalization.CultureInfo.InvariantCulture);
            return negative
                ? $"-${dollars}.{absolute % 100:00}"
                : $"${dollars}.{absolute % 100:00}";
        }
    }
}
