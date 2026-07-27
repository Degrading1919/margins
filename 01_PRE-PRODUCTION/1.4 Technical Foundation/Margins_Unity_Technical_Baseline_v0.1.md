# Margins Unity Technical Baseline v0.1

## Status and Authority

- **Status:** Proposed technical baseline for project-owner review.
- **Primary authority:** `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md`.
- **Role lens:** Technical Architect Assistant, with Producer and Roadmap Assistant as a secondary scope lens.
- **Approved:** Unity is the production engine for Margins.
- **Not approved by this file:** exact editor patch, render pipeline, packages, folder structure, save approach, testing conventions, target hardware, paid dependencies, or production architecture beyond the first spike.

## Recommended Baseline

| Area | Recommendation | Reason |
|---|---|---|
| Unity release lane | **Unity 6.3 LTS (`6000.3.x`)** | Stable production lane with two-year LTS support, better for locking the first durable project than chasing Update releases. |
| Patch policy | Allow `6000.3.x` patch updates only after release-note review and a clean open/run/test/build check. | Keeps security and bug fixes available without silent editor churn. |
| Major/update migration | No Unity 6.4+ or later LTS migration without owner approval and a short migration note. | Prevents hidden baseline changes during agent work. |
| Render pipeline | **Universal Render Pipeline** | Best fit for stylized contemporary PC visuals, solo production, broad asset compatibility, lighting control, and agent-debuggable settings. |
| Implementation language | **C# by default; scenes, prefabs, components, and ScriptableObjects authored in the Unity Editor; no Visual Scripting for spike logic.** | Keeps behavior inspectable, diffable, testable, and easier for small agent patches. |
| Development build target | **Windows desktop x64 development build first.** | Matches the PC-only vertical-slice constraint and the owner's likely first local build environment. |
| Mandatory license cost | **$0 only if Unity Personal eligibility is confirmed.** | If the owner/entity does not satisfy Unity's under-$200K revenue-and-funding threshold, implementation is blocked until the owner approves a paid plan. |
| Save proof | Human-readable JSON in local persistent storage. | Enough to prove placement persistence without a database, cloud save, or migration framework. |

## Official Package Baseline

| Package | Spike status | Why now | Must not depend on it yet |
|---|---|---|---|
| Universal Render Pipeline | Mandatory through project template/baseline | Establishes the render pipeline before assets, lighting, and materials accumulate. | No custom shader architecture or final art pipeline. |
| Input System | Mandatory | First-person movement, mouse look, pickup, release, and future remapping path. | No full control rebinding UI or gamepad support requirement. |
| AI Navigation | Mandatory | One placeholder navigation agent moving between two points on a NavMesh. | No customer simulation, employee task AI, crowd behavior, or dynamic store routing. |
| Unity Test Framework | Mandatory | Focused EditMode or PlayMode checks for identifiers, snap references, occupied slots, and save/reload equality. | No CI, coverage targets, performance test package, or broad test architecture. |

## Explicit Rejections for the First Spike

| Technology or package | Disposition |
|---|---|
| Built-In Render Pipeline | Reject for new project; deprecated and a poor long-term starting point. |
| High Definition Render Pipeline | Reject; unnecessary production weight for Stylized Contemporary Americana and solo PC development. |
| Visual Scripting | Reject for spike behavior; raises hidden-state and review risk. |
| Cinemachine | Reject for this spike; a simple first-person camera is sufficient until proven otherwise in a separate owner-approved change. |
| ProBuilder | Reject; use Unity primitives for the graybox room. |
| UI Toolkit or uGUI | Reject for the spike; prefer in-world color/material feedback and inspector fields. |
| Addressables, DOTS/ECS, Netcode, Analytics, Ads, Cloud Save, Unity Services | Reject; no first-spike requirement depends on them. |
| Third-party gameplay, save, input, or DI frameworks | Reject; official Unity baseline is enough for the spike. |

## Data and Save Boundary

The spike should define only:

- one product definition with stable identifier, display name, visual prefab reference, physical shelf footprint, snap compatibility, and a scan-demo value;
- one shelf or fixture definition with stable fixture identifier, explicit snap-point identifiers, local position/orientation, accepted compatibility tags, and occupied state;
- one placed-product runtime state with product definition identifier, fixture identifier, snap-point identifier, and quarter-turn orientation;
- one save file with version field and placed-product records.

Do not define full inventory, economy, supplier, price, spoilage, theft, customer, employee, property, or multi-location schemas.

## Verified Facts

| Fact type | Verified fact | Official source |
|---|---|---|
| Unity 6 lanes | Unity 6 has LTS releases and Update releases; Unity 6.3 LTS is the latest LTS and is supported until December 2027. | https://unity.com/releases/unity-6/support |
| Update lane | Unity describes Update releases as production-ready and recommended for new or mid-cycle productions, but supported only until the next release. | https://unity.com/releases/unity-6/support |
| URP | URP is a core Unity package; Unity 6.3 docs require choosing the render pipeline before development because URP projects are not compatible with HDRP or Built-In. | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/requirements.html |
| Built-In status | Built-In Render Pipeline is deprecated and supported through the Unity 6.7 LTS lifecycle. | https://docs.unity3d.com/6000.5/Documentation/Manual/built-in-render-pipeline.html |
| Input System | Input System 1.20.0 is released for Unity 6.3; it is Unity's newer extensible input package and supports keyboard and mouse devices. | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html |
| AI Navigation | AI Navigation 2.0.14 is released for Unity 6.3 and supports NavMesh construction and NavMeshAgent pathfinding. | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.ai.navigation.html |
| Test Framework | Unity Test Framework is a Unity 6.3 core package and supports EditMode, PlayMode, and command-line tests. | https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.test-framework.html |
| Runtime UI | UI Toolkit and uGUI are both supported runtime UI systems, but neither is needed for this spike's product proof. | https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html |
| Smart Merge | UnityYAMLMerge can semantically merge scene and prefab files and can be configured for Git. | https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html |
| Licensing and desktop | Unity Personal is for individuals/small organizations below the stated revenue/funding threshold; Unity 6.3 desktop player supports Windows 10 21H1 or newer with DX10/DX11/DX12/Vulkan-capable GPU. | https://unity.com/products/unity-personal; https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html |

## Unresolved Owner Choices

- Confirm Unity Personal eligibility before project creation.
- Approve Unity 6.3 LTS and URP as the project baseline.
- Approve `CODE/Unity/Margins` as the Unity project location.
- Confirm Windows x64 as the first build target.
- Decide later whether any UI framework is needed after the tactile spike.
