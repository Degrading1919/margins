---
name: tripo-3d-prompting
description: Explicitly invoked workflow for writing fact-checked Tripo 3D prompts and reference-image briefs for Margins assets. Use when preparing or revising Tripo text-to-3D, image-to-3D, multiview, or rig-ready character inputs. Do not use to accept final assets, guarantee topology, polygon count, rig quality, or cleanup time, perform Blender cleanup, or replace art, technical, licensing, and owner review.
---

# Tripo 3D Prompting

Produce one ready-to-paste Tripo prompt package that is specific enough to improve the initial mesh while remaining faithful to current Margins art direction and technical ceilings.

## Operating boundary

- Require explicit invocation as `$tripo-3d-prompting`.
- Treat the repository as the source of truth for Margins style, scope, and budgets.
- Treat raw Tripo output as intake material, not an accepted production asset.
- Use only current official Tripo documentation for Tripo-specific capability claims.
- Distinguish:
  - **official Tripo fact**: documented by Tripo;
  - **approved Margins constraint**: recorded in the repository;
  - **working heuristic**: a practical recommendation that is not guaranteed by Tripo.
- Never claim that wording alone guarantees topology, exact polygon count, successful rigging, symmetry, production readiness, or a cleanup time such as fifteen minutes.
- Do not copy identifiable characters, brands, or protected designs. Apply the approved Stylized Contemporary Americana direction rather than requesting an exact replica of a reference work.

## 1. Run repository preflight

Before writing a prompt, read the current relevant sections of:

- `01_PRE-PRODUCTION/1.7 Art Audio & Presentation/Margins_Art_Audio_and_Presentation_Direction_v0.1.md`;
- `01_PRE-PRODUCTION/1.7 Art Audio & Presentation/Margins_3D_Asset_Technical_Budgets_v1.0.md`; and
- any current asset brief, model list, character direction, or owner-approved reference supplied for the requested asset.

Resolve:

- asset name and category;
- gameplay use and closest expected viewing distance;
- maximum LOD0 ceiling and any character, collider, material, or vegetation constraints;
- required movable, separable, animated, or interactive parts;
- whether the request is for a concept image, single-image reconstruction, multiview reconstruction, or text-to-model generation;
- owner-approved visual traits and exclusions.

Discover repository facts before asking questions. Ask only when missing information would materially change silhouette, pose, modularity, or reconstruction strategy.

## 2. Reverify Tripo before making platform claims

Tripo features and model versions can change. At the time of use, verify relevant claims through current official Tripo documentation.

Current official guidance verified on 2026-08-06 includes:

- image-to-model inputs should show a clearly visible subject with a clean background and minimal occlusion;
- multiview generation improves geometry accuracy and texture coverage, requires a front view, accepts at least two views, and expects the same object under consistent lighting;
- Tripo advanced image generation exposes a `t_pose` template for converting characters to a standard T-pose for rigging and animation;
- the documented advanced-image prompt limit is 1,024 characters, approximately 100 words;
- Tripo API model generation may expose `face_limit`, `smart_low_poly`, quad output, and related controls, but do not assume the Studio interface exposes the same controls;
- Tripo describes raw generated game assets as starting points that commonly require optimization, retopology, scale, pivot, UV, and engine review.

When official documentation cannot be verified, omit the uncertain feature or mark it `unavailable`. Never convert a remembered feature into current fact.

## 3. Choose the input strategy

Use the smallest strategy that provides enough control:

### Text-to-3D

Use for fast ideation, broad shape exploration, or batches where no approved visual reference exists.

Base the prompt on Tripo's documented structure:

`Object + Material + Style + Structural Details`

Add pose, orientation, and exclusions before decorative detail when reconstruction or rigging depends on them.

### Image-to-3D

Prefer when matching an approved concept, established Margins shape language, or a specific silhouette. The reference should contain one centered, fully visible subject on a plain background with even lighting and minimal occlusion.

Do not present a full environment scene when the requested output is one asset.

### Multiview-to-3D

Prefer when back, side, thickness, interior volume, limb placement, or mechanical geometry matters enough that a single image would force excessive guessing.

Use two to four consistent views. The front view is mandatory under Tripo's documented multiview contract. Keep scale, design, materials, pose, and lighting consistent across views.

### T-pose character preparation

Use Tripo's current native T-pose preparation option when available. Prompt wording should still state the pose precisely because a vague phrase such as “relaxed pose” does not define rigging geometry.

## 4. Build the prompt in this order

Write one coherent prompt using this order unless the asset requires a justified exception:

1. **Subject and use** — exactly what the model is and whether it is a reusable base, prop, machine, vehicle, module, or vegetation asset.
2. **Pose or orientation** — front-facing, front three-quarter, orthographic, T-pose, upright, open, closed, or other reconstruction-critical state.
3. **Silhouette and proportions** — body type, dimensions, major masses, stance, shape language, and scale cues.
4. **Structural details** — only the parts that must exist in geometry or remain separable.
5. **Margins style** — Stylized Contemporary Americana, near-human or believable proportions, simplified readable forms, controlled detail, restrained materials, and non-photoreal presentation.
6. **Reference presentation** — fully visible, centered, uncropped, plain light background, even studio lighting, soft neutral shadow, no environment.
7. **Exclusions** — no branding, no props, no extra people, no loose accessories, no dramatic pose, no hidden parts, or other asset-specific negatives.

Keep the prompt within the current Tripo input limit when one is documented for the selected surface. Prefer precise nouns and measurable pose language over mood words.

## 5. Category rules

### Rig-ready base characters

Default to a complete, symmetrical T-pose unless the owner or downstream rig explicitly requires an A-pose.

Specify:

- full body visible from head to feet;
- front-facing upright torso and head facing directly forward;
- arms extended horizontally;
- elbows straight;
- palms facing downward;
- fingers straight and slightly separated;
- legs straight and hip-width apart;
- feet parallel and fully visible;
- neutral facial expression;
- clear separation between arms and torso and between the legs;
- bald head when hair will be modular;
- minimal smooth fitted clothing that does not obscure anatomy;
- no jewelry, props, loose fabric, layered garments, pockets, belts, or unnecessary seams unless specifically required.

Describe approved physical traits directly. Avoid vague labels such as “heroic,” “beautiful,” or “relaxed” when body shape, pose, or facial structure can be stated concretely.

The approved 18,000-triangle LOD0 character ceiling includes the complete visible body, current clothing, hair, shoes, and accessories. Module limits are not additive. Prompt wording cannot enforce this ceiling; the exported mesh must be measured and normalized.

### Hard-surface props and machines

- Generate one dominant object per reconstruction.
- Use a front three-quarter view unless a front orthographic view better exposes the design.
- Name essential parts, hinges, openings, handles, controls, and separable pieces.
- Replace modeled microdetail, printed text, labels, and wear with texture intent whenever geometry is unnecessary.
- Avoid a dramatic scene, clutter, supporting props, or multiple variants in one image.

### Modular architecture and kits

- Generate one module at a time when pieces must remain separate.
- Do not ask single-image reconstruction to interpret a pile of disconnected kit pieces as one production mesh.
- Use consistent dimensions, thickness, pivot assumptions, trim language, and connection edges across prompts.
- Use multiview for doors, awnings, deep frames, machines, or modules whose thickness and rear construction matter.

### Vehicles

- Show the full vehicle in a clean front three-quarter view or consistent multiview set.
- Keep all wheels, mirrors, lights, roofline, bumpers, and wheel arches visible.
- Use generic contemporary American silhouettes with no real-world logos or identifiable model copying.
- State whether the asset is close-view LOD0 source material or a distinct low-cost background silhouette.

### Vegetation

- Use one isolated tree or landscaping cluster.
- Describe trunk, major branches, canopy masses, and silhouette rather than individual leaves.
- Keep foliage broad and readable so the result can be normalized to shared materials, controlled alpha-tested surfaces, and final-distance billboard or impostor requirements.

### Repeated products and packaging

- Prompt a reusable package shape, not dozens of unique branded meshes.
- Use labels, logos, prices, fine seams, and flavor variation as texture or material work.
- Keep geometry limited to silhouette-changing features.

## 6. Use budgets correctly

The Margins triangle figure is a maximum accepted LOD0 ceiling, not a target and not a promise Tripo will satisfy.

In the prompt:

- translate the ceiling into appropriate visual complexity, such as simple silhouette, restrained mechanical detail, or close-view readable construction;
- do not state that the prompt will produce an exact triangle count;
- do not encourage the model to fill the available budget.

When the current Tripo surface exposes a documented polygon or low-poly control, record the chosen setting separately. The output still requires measurement because topology quality, hidden geometry, material slots, and deformation readiness are not proven by face count alone.

## 7. Return this exact prompt package

### Mode

State one of:

- `text-to-3D`
- `image-to-3D`
- `multiview-to-3D`
- `image preparation for T-pose`

Give one sentence explaining the choice.

### Ready-to-paste prompt

Provide one compact prompt with no commentary inside it.

### Negative prompt

Provide only when the current Tripo surface supports it or when the user is generating the reference image in a tool that supports negative prompts. Keep it limited to the most likely failure modes.

### Reference-image requirements

State the required view, framing, background, lighting, and whether additional views are needed.

### Budget and cleanup note

State the authoritative LOD0 ceiling, what it includes, and the specific checks still required after export.

### Fact status

List any current Tripo feature relied upon, its official source, verification date, and any unverified assumption.

## 8. Quality gate

Before delivery, confirm:

- one clear asset is requested per generation unless a documented extraction workflow is being used;
- the complete subject is visible and uncropped;
- no required geometry is hidden, touching, or described ambiguously;
- character joints and limb separation are explicit when rigging matters;
- the prompt uses approved Margins direction without requesting exact imitation;
- the technical ceiling is treated as downstream acceptance authority;
- no guarantee is made about rigging, topology, polycount, cleanup duration, or production readiness;
- the output package contains the mode, prompt, reference requirements, budget note, and fact status.

## 9. Iteration rule

Tripo generation is not deterministic. Generate several candidates when practical, select the strongest silhouette and proportions, and revise the prompt only around observed failure modes.

Do not spend extensive cleanup time rescuing an obviously weak generation when another controlled variation is likely to provide a better starting mesh.

## Compact templates

### General prop template

> Generate a complete isolated [asset] for a reusable game asset. [Orientation and state]. Use [major proportions and silhouette]. Include [essential geometric parts]. Apply the Margins Stylized Contemporary Americana direction: believable scale, simplified readable forms, controlled low-to-mid-poly detail, restrained materials, and non-photoreal presentation. Fully visible and centered on a plain light-gray studio background with even soft lighting and a subtle shadow. No environment, branding, people, extra props, or unnecessary microdetail.

### Rig-ready base-character template

> Generate a complete full-body [male/female] base character in a perfect symmetrical T-pose for humanoid rigging. Front-facing upright torso, head directly forward, arms horizontal, elbows straight, palms down, fingers straight and slightly separated, legs straight and hip-width apart, feet parallel and fully visible. [Approved facial and body traits]. Bald, neutral expression, smooth fitted minimal clothing, clear anatomy and separated limbs. Apply the Margins near-human Stylized Contemporary Americana direction with grounded proportions and restrained stylization. Plain light background, even lighting, no props, accessories, loose fabric, layered clothing, or cropped parts.

## Official Tripo evidence

Capability claims must be rechecked against current official sources. The following were verified on 2026-08-06:

- Image to 3D Model documentation: `https://developers.tripo3d.ai/en/docs/generation-image-to-model`
- Multiview to 3D Model documentation: `https://developers.tripo3d.ai/en/docs/generation-multiview-to-model/standard`
- Advanced image generation and T-pose template documentation: `https://docs.tripo3d.ai/image-generation/advanced-image-generation.html`
- Official Tripo game-prop workflow and prompt structure: `https://www.tripo3d.ai/blog/ai-3d-props-for-games`
- Official Tripo character base-mesh prompting and retopology guidance: `https://www.tripo3d.ai/education/text-to-3d-base-mesh-character-sculpting`

API documentation is authoritative for exposed API parameters. Tripo blog and education pages provide official workflow guidance but are not substitutes for current API or Studio capability documentation.

## Maintenance and invalidation

Review this skill after material changes to:

- Tripo Studio or API prompt limits, models, image templates, pose preparation, multiview rules, low-poly controls, rigging, or export behavior;
- Margins art direction, character direction, technical budgets, modularity rules, or asset pipeline;
- the approved Tripo-to-Blender-to-Unity workflow.

Disable or revise any instruction that cannot be supported by current official Tripo evidence or current repository authority.