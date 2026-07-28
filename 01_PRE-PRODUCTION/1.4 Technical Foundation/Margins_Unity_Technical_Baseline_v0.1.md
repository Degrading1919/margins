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
| Unity release lane | **Unity 6.5 Supported (`6000.5.x`)** | Supported Update release suited to a new production baseline while retaining Unity's current production-ready Update support model. |
| Project creation patch | Use the latest project-owner-approved `6000.5.x` patch available at execution time. Record the exact editor patch in `CODE/Unity/Margins/ProjectSettings/ProjectVersion.txt` and in the implementation PR body. | Keeps the lane current while making the exact editor version auditable. |
| Patch policy | Allow `6000.5.x` patch updates only after release-note review and a clean open/run/test/build check. | Keeps fixes available without silent editor churn. |
| Update migration | Unity 6.5 is a Supported Update release, not LTS, and is supported until the next Unity Update release. Do not update Unity automatically; any patch or Update migration requires release-note review and clean open, Play Mode, test, and build checks. | Prevents hidden baseline changes during agent work. |
| Render pipeline | **Universal Render Pipeline** | Best fit for stylized contemporary PC visuals, solo production, broad asset compatibility, lighting control, and agent-debuggable settings. |
| Implementation language | **C# by default; scenes, prefabs, components, and ScriptableObjects authored in the Unity Editor; no Visual Scripting for spike logic.** | Keeps behavior inspectable, diffable, testable, and easier for small agent patches. |
| Development build target | **Windows desktop x64 development build first.** | Matches the PC-only vertical-slice constraint and the owner's likely first local build environment. |
| Mandatory license cost | **$0 only if Unity Personal eligibility is confirmed by the project owner.** | Do not declare eligibility in this document; confirm the applicable Unity-defined case before project creation. |
| Save proof | Human-readable JSON in local persistent storage. | Enough to prove placement persistence without a database, cloud save, or migration framework. |

## Official Package Baseline

| Package | Spike status | Why now | Must not depend on it yet |
|---|---|---|---|
| Universal Render Pipeline | Mandatory through project template/baseline; use the editor-matched Unity 6.5 package lane | Establishes the render pipeline before assets, lighting, and materials accumulate. | No custom shader architecture or final art pipeline. |
| Input System `1.20.0` | Mandatory | First-person movement, mouse look, pickup, release, and future remapping path. | No full control rebinding UI or gamepad support requirement. |
| AI Navigation `2.0.14` | Mandatory | One placeholder navigation agent moving between two points on a NavMesh. | No customer simulation, employee task AI, crowd behavior, or dynamic store routing. |
| Unity Test Framework | Mandatory; use the editor-matched Unity 6.5 package lane | Focused EditMode or PlayMode checks for identifiers, snap references, occupied slots, and save/reload equality. | No CI, coverage targets, performance test package, or broad test architecture. |

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

- one product definition with stable identifier, display name, visual prefab reference, physical shelf footprint, and snap compatibility;
- one authored shelf or fixture definition with stable fixture identifier, explicit snap-point identifiers, local position/orientation, and accepted compatibility tags;
- runtime occupancy on the shelf instance during play or derived from current placed-product records;
- one placed-product runtime state with product definition identifier, fixture identifier, snap-point identifier, and quarter-turn orientation;
- one save file with version field and placed-product records.

On load, begin with every runtime snap point unoccupied, validate saved placement records, place valid products, and rebuild occupancy only from accepted placements.

Do not mutate shared authored definitions or ScriptableObjects to record slot use.

Do not define full inventory, economy, supplier, price, spoilage, theft, customer, employee, property, or multi-location schemas.

## Verified Facts

| Fact type | Verified fact | Official source |
|---|---|---|
| Unity 6 lanes | Unity 6 has LTS releases and Supported Update releases; Unity 6.5 is a Supported Update release, not LTS, and is supported until the next Unity Update release. | https://unity.com/releases/unity-6/support |
| Update lane | Unity describes Update releases as production-ready and recommended for new or mid-cycle productions, but supported only until the next release. | https://unity.com/releases/unity-6/support |
| URP | URP is a Unity package documented for Unity 6.5; choose the render pipeline before development because URP projects are not compatible with HDRP or Built-In. | https://docs.unity3d.com/6000.5/Documentation/Manual/urp/urp-introduction.html |
| Built-In status | Built-In Render Pipeline is deprecated and supported through the Unity 6.7 LTS lifecycle. | https://docs.unity3d.com/6000.5/Documentation/Manual/built-in-render-pipeline.html |
| Input System | Input System 1.20.0 is retained for the Unity 6.5 baseline; it is Unity's newer extensible input package and supports keyboard and mouse devices. | https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html |
| AI Navigation | AI Navigation 2.0.14 is retained for the Unity 6.5 baseline and supports NavMesh construction and NavMeshAgent pathfinding. | https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html |
| Test Framework | Unity Test Framework supports EditMode and PlayMode tests. Record the resolved editor-matched Unity 6.5 package version after project creation. | https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/index.html |
| Runtime UI | UI Toolkit and uGUI are both supported runtime UI systems, but neither is needed for this spike's product proof. | https://docs.unity3d.com/6000.5/Documentation/Manual/UIToolkits.html |
| Smart Merge | UnityYAMLMerge can semantically merge scene and prefab files and Unity documents Git configuration through a local mergetool workflow. | https://docs.unity3d.com/6000.5/Documentation/Manual/SmartMerge.html |
| Licensing and desktop | Unity Personal eligibility depends on the applicable Unity-defined case. For an individual developing their own first-party project and not providing Unity-related services to a third party, Unity's terms measure the amount generated in connection with that individual's Unity Software use rather than unrelated personal employment income. Legal entities, organizations, and individuals or entities providing Unity-related development services to third parties are measured under their applicable Unity terms. Unity 6.5 desktop player supports Windows 10 21H1 or newer with DX10/DX11/DX12/Vulkan-capable GPU. This is not legal advice. | https://unity.com/legal/editor-terms-of-service/software; https://docs.unity3d.com/6000.5/Documentation/Manual/system-requirements.html |

## Unresolved Owner Choices

- Confirm the project is being developed in the applicable individual first-party capacity or identify the applicable legal-entity/service-provider case before project creation.
- Confirm the relevant Unity-defined finances remain below the Unity Personal threshold before relying on Unity Personal.
- Approve Unity 6.5 Supported and URP as the project baseline.
- Approve `CODE/Unity/Margins` as the Unity project location.
- Confirm Windows x64 as the first build target.
- Decide later whether any UI framework is needed after the tactile spike.
