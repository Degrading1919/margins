# Margins Unity Foundation Baseline Decision v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** July 27, 2026
- **Scope:** The first Unity foundation spike only
- **Supersedes:** The proposed approval state in `Margins_Unity_Technical_Baseline_v0.1.md` and `Margins_Unity_Bootstrap_Standard_v0.1.md`

## Decision

The following baseline is approved for the first Margins Unity implementation:

- Unity 6.5 Supported (`6000.5.x`), not LTS;
- the latest stable project-owner-approved `6000.5.x` patch available when the project is created;
- Universal Render Pipeline using the editor-matched Unity 6.5 package lane;
- C# for spike behavior, with scenes, prefabs, components, and ScriptableObjects authored in the Unity Editor where appropriate;
- no Visual Scripting for spike behavior;
- Input System `1.20.0`;
- AI Navigation `2.0.14`;
- Unity Test Framework using the editor-matched Unity 6.5 package lane;
- project path `CODE/Unity/Margins`;
- root namespace `Margins`;
- human-readable local JSON for the save proof; and
- Windows desktop x64 as the first development-build target.

The source-control, folder, assembly, dependency, data-boundary, validation, and code-minimization rules in the approved technical baseline and bootstrap standard apply to the spike.

## Version evidence

The exact editor patch is intentionally not fixed before installation. The implementation pull request must record:

- the exact patch from `CODE/Unity/Margins/ProjectSettings/ProjectVersion.txt`;
- resolved package versions from `Packages/manifest.json`; and
- resolved package versions from `Packages/packages-lock.json`.

Do not update the editor or packages automatically after project creation. Any patch or Update migration requires release-note review and clean project-open, Play Mode, test, and build checks.

## Licensing boundary

This decision does not declare Unity Personal eligibility. Before relying on Unity Personal, the project owner must confirm the applicable Unity-defined individual, legal-entity, or service-provider case and confirm the relevant Unity-defined finances are within the applicable threshold.

No paid package, Asset Store dependency, cloud service, analytics, ads, multiplayer package, DOTS/ECS package, or third-party gameplay framework is approved for the spike.

## Scope boundary

This decision approves only the foundation spike defined in:

- `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_v0.1.md`; and
- `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_Agent_Prompt.md`.

It does not approve production architecture, full inventory, economy, customers, employees, multiple locations, aggregate simulation, production UI, final art, or any other excluded system.

## Immediate direction

After the required local Unity editor installation and licensing confirmation are complete, execute the existing foundation-spike agent prompt from current `main`, measure the workflow, open a draft implementation pull request, and do not merge it automatically.

## Reopening rule

Reopen this baseline only when installation, package resolution, licensing, implementation, testing, or build evidence demonstrates a concrete blocker or materially better project-specific path. Preference or hypothetical future flexibility alone is insufficient.