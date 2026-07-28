# Margins Unity Bootstrap Standard v0.1

## Status

- **Status:** Proposed bootstrap standard for the first Unity project.
- **Authority boundary:** Implements the approved Unity engine decision without recording lower-level choices as approved.
- **Purpose:** Make the first Unity coding session reproducible, reviewable, and small.

## Repository Location

Create the Unity project at:

```text
CODE/Unity/Margins/
```

Reason: `CODE` is the repository's implementation staging area, and a nested Unity project keeps generated Unity folders scoped away from governance, design, data, and media documentation.

## Minimal Unity Folder Tree

Create only folders that have immediate use:

```text
CODE/Unity/Margins/
  Assets/
    Margins/
      Runtime/
      Content/
      Scenes/
      Tests/
        EditMode/
  Packages/
  ProjectSettings/
```

| Path | Current reason |
|---|---|
| `Assets/Margins/Runtime` | C# runtime scripts for first-person movement, product definitions, snapping, placement, save/reload, and navigation placeholder behavior. |
| `Assets/Margins/Content` | Project-owned prefabs, ScriptableObjects, primitive materials, and placeholder product/shelf content. |
| `Assets/Margins/Scenes` | One graybox first-store spike scene. |
| `Assets/Margins/Tests/EditMode` | Data and validation tests that do not require Play Mode. |
| `Packages` | Unity package manifest and lock file. Commit both. |
| `ProjectSettings` | Unity project settings. Commit all required settings. |

Do not create feature folders, service layers, manager directories, or tool directories until real code needs them.

Conditional folders:

- `Assets/Margins/Tests/PlayMode` only if a required check cannot be expressed as an EditMode test.
- `Assets/Margins/Editor` only when editor-only code exists.
- `Assets/ThirdParty` only when an approved licensed third-party asset is actually added.

## Namespace and Assemblies

- Root namespace: `Margins`.
- Runtime assembly: `Margins.Runtime`.
- EditMode test assembly: `Margins.Tests.EditMode`, references runtime.
- PlayMode test assembly: `Margins.Tests.PlayMode`, only if PlayMode tests are needed.
- Editor assembly: `Margins.Editor`, only when editor-only code exists.

Do not create per-system assemblies, package-style modules, dependency-injection containers, or generic infrastructure assemblies for the spike.

## Source Control Rules

- Commit Unity `Assets`, `Packages`, `ProjectSettings`, and all `.meta` files.
- Do not commit generated `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, build folders, IDE solution files, or local environment files.
- Keep text, C#, YAML, JSON, Markdown, UXML, USS, shader text, and `.meta` files out of Git LFS.
- Use Git LFS only for large or non-diffable binary source assets such as textures, models, audio, video, and `.unitypackage` files.
- Treat Unity scenes, prefabs, materials, and asset files as YAML text with LF normalization.
- Do not expect Smart Merge to run from `.gitattributes`; this repository does not define a low-level UnityYAMLMerge Git driver.
- When scene, prefab, or other Unity YAML conflicts appear, resolve them with a locally configured UnityYAMLMerge mergetool through `git mergetool`, or resolve manually when the conflict is simple.
- Do not commit machine-specific Unity installation paths, merge-driver configuration, scripts, or custom automation for this baseline.

## Package and Dependency Rules

Selected for the first spike:

- Universal Render Pipeline: editor-matched Unity 6.5 package lane;
- Input System: `1.20.0`;
- AI Navigation: `2.0.14`;
- Unity Test Framework: editor-matched Unity 6.5 package lane.

Rules:

- Create the project with the latest stable project-owner-approved Unity 6.5 Supported (`6000.5.x`) patch available at execution time. Record the exact patch from `CODE/Unity/Margins/ProjectSettings/ProjectVersion.txt` in the implementation PR body.
- Add no other package during this spike. If a first-spike acceptance criterion cannot be met with the selected baseline, stop and request owner approval before changing packages.
- Record any owner-approved package change in the PR body with exact reason and approval status.
- Record resolved package versions from `CODE/Unity/Margins/Packages/manifest.json` and `CODE/Unity/Margins/Packages/packages-lock.json` in the implementation PR body.
- Do not update the Unity editor or packages automatically after project creation. Any editor patch or Update migration requires release-note review and clean project-open, Play Mode, test, and build checks.
- If a named package version is unavailable or incompatible, stop and report the blocker instead of selecting an unapproved substitute.
- No paid packages, Asset Store packages, cloud services, analytics, ads, multiplayer, DOTS/ECS, or third-party gameplay frameworks.

## Code Minimization Standard

The coding agent must optimize for correctness, clarity, inspectability, and easy removal.

Required:

- fewest scripts that cleanly express the behavior;
- one responsibility per script when it improves debugging;
- explicit inspector fields for stable identifiers, snap targets, product references, and feedback materials;
- deterministic snap-point selection;
- fail-fast validation for duplicate or missing stable identifiers;
- explicit validation errors for invalid snap references and occupied slots;
- bounded debug logging for pickup, snap success/failure, save, and load.

Prohibited:

- empty abstraction layers;
- speculative interfaces;
- global service locator;
- dependency-injection framework;
- generalized event bus;
- singleton unless the implementation prompt proves no simpler option works;
- reflection-heavy framework;
- third-party gameplay framework;
- comments that restate obvious code;
- performance optimization without measured evidence.

## Debugging and Testing Standard

- Prefer focused EditMode tests for pure data validation and save equality.
- Use PlayMode tests or deterministic scene checks only where scene/runtime behavior is required.
- Tests must cover duplicate product identifiers, invalid snap-point references, occupied-slot rejection, and save/reload placement equality.
- Manual validation must include editor Play Mode and one local Windows desktop development build.
- Any editor, test, or build step the agent cannot run must be reported exactly, with the unverified acceptance criteria listed.
