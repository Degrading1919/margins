# Margins Engine Candidate Shortlist v0.1

**Status:** Proposed research shortlist; preliminary and unproven<br>
**Authority:** Applies the proposed `Margins_Engine_Evaluation_Criteria_v0.1.md`; subordinate to approved decisions<br>
**Evidence current through:** 2026-07-27<br>
**Decision:** Unresolved; this file does not recommend an engine

## Candidate boundary

| Candidate pool considered | Preliminary elimination | Detailed shortlist |
|---|---|---|
| Unreal Engine, Unity, Godot | None. No verified hard disqualifier was found in documentary research. | Unreal Engine, Unity, Godot |

No fourth candidate was added because research found no strong Margins-specific reason to increase comparison cost. Engines outside this bounded pool were not evaluated and are **not** disqualified.

## Evidence labels

- **Fact:** supported by a current official source.
- **Inference:** a Margins-specific implication of facts; not executable evidence.
- **Prototype question:** must be answered by the same representative test in each engine.
- **Unresolved:** primary evidence or an owner decision is missing.

Feature existence is not proof of suitability. Asset-store size, open-source status, commercial support, and AI code generation are not scored as proxies for production fit.

## Candidate profiles

### Unreal Engine

| Area | Current evidence and implication |
|---|---|
| Version and maintenance | **Fact:** Unreal Engine 5.8 was released 2026-06-23. Epic describes it as the last planned major UE5 release while UE6 work ramps up, with continued UE5 bug/regression support; another official UE5 release may occur [U2]. **Risk:** the prototype must record whether the 5.8-to-later transition changes the maintenance case. |
| Verified strengths | **Fact:** official documentation covers indoor 3D navigation [U3], automated functional/feature testing [U4], and CPU/GPU/memory/UI/load tracing [U5]. **Inference:** the documented coverage is broad enough to justify—not pass—the Margins prototypes. |
| Verified weaknesses | **Fact:** the official automation framework notes that it is not ideal for pure unit testing [U4]. The 5.8 Model Context Protocol integration is explicitly experimental and not production-ready [U2]. Neither is a disqualifier; both limit assumptions about automated and agentic workflow. |
| Margins-specific risks | **Prototype questions:** tactile handling and snapping; furnished navigation after occasional fixture moves; human iteration burden; schema/validator and save-migration ergonomics; detailed/aggregate handoff; two-location reports; scene/content merge recovery; target-PC build size/time; representative stylized asset adaptation. High-fidelity defaults and marketplace breadth do not prove fit or budget safety. |
| AI-agent workflow | **Fact:** 5.8 includes an experimental MCP integration [U2]. The binding EULA prohibits using Licensed Technology as training input to a generative-AI program, including prompt input when that program trains on inputs [U1]. **Implication:** the owner must approve provider retention/training terms and the material exposed; experimental tool access does not prove reviewability or compliance. |
| License and cost | **Fact:** intended public game development as a Royalty Product requires no seat subscription. The standard rate is 5% of Royalty Revenue after applicable exclusions, including the first $1 million in lifetime gross revenue; a conditional “Launch Everywhere with Epic Release” path reduces the rate to 3.5% while its storefront/parity conditions are met [U1]. **Implication:** no mandatory pre-revenue engine fee was identified for that use, but optional dependencies and future distribution obligations remain unpriced. |
| Unknowns that can change ranking | Implementation lane; prior-experience/onboarding cost; agent-provider compliance; navigation and simulation evidence; minimum test/data stack; source-control friction; owner-designated PC target; asset conversion; total custom save/simulation burden. |

### Unity

| Area | Current evidence and implication |
|---|---|
| Version and maintenance | **Fact:** Unity 6.5 is the current supported update family; Unity 6.3 is the current LTS line and is listed as supported through December 2027 [Y3]. **Unresolved:** use 6.3 LTS or the current update for the prototype. |
| Verified strengths | **Fact:** AI Navigation supports edit-time and runtime NavMesh use, dynamic obstacles, and links [Y4]. Current Unity Test Framework documentation covers project testing [Y5]. These facts justify—not pass—the comparative tests. |
| Verified weaknesses | **Fact:** eligibility is recalculated from the prior twelve months. Personal permits at most $200,000 in **Total Finances**; Pro covers $200,001–$24,999,999; Enterprise is required at $25 million or more [Y1]. Current Pro pricing starts at $2,310/year prepaid or $210/month [Y2], which alone exceeds the approved pre-revenue cap. |
| Margins-specific risks | **Prototype questions:** tactile stability and snapping; dynamic furnished navigation; editor/package iteration; schema validation, save migration, and detailed/aggregate reconciliation; UI report authoring; scene/prefab merge recovery; target-PC delivery; render-pipeline/package and asset adaptation burden. |
| AI-agent and tooling implications | **Inference:** C#, project tests, and editor serialization can expose inspectable changes, but package/version state, scene/prefab semantics, provider data-use terms, and generated-change maintainability require the same branch/merge/revert exercise. Source access must not be assumed: current plan information associates it with Enterprise [Y2]. |
| License and cost | **Unresolved:** approved project facts do not identify the developing individual/entity or its Unity-defined Total Finances, so Personal eligibility and mandatory editor cost cannot be concluded. If Total Finances exceed $200,000, current minimum Pro pricing creates a potential D2 hard disqualifier unless the owner changes the total budget [Y1][Y2]. Optional tools, assets, and services are also unpriced. |
| Unknowns that can change ranking | Tier eligibility; LTS/update lane; onboarding and domain-reload time; package churn; data/save/test stack; profiling usefulness; YAML/binary merge burden; owner-designated PC target; assets; paid tooling. |

### Godot

| Area | Current evidence and implication |
|---|---|
| Version and maintenance | **Fact:** Godot 4.6.3 was listed as stable on 2026-05-20 [G2]. The exact supported patch used by the prototypes must be pinned. |
| Verified strengths | **Fact:** Godot is MIT-licensed and permits commercial use, modification, and distribution subject to the license notice [G1]. Official documentation provides desktop export workflows [G5], runtime navigation APIs [G3], and a built-in profiler [G4]. |
| Verified weaknesses | **Fact:** `NavigationObstacle3D` is marked experimental, and moving its vertex-defined obstacle every frame requires repeated avoidance-map rebuilds [G3]. The built-in profiler currently does not support profiling C# scripts [G4]. These are concrete prototype risks, not documentary elimination. |
| Margins-specific risks | **Prototype questions:** tactile stability and snapping; occasional furnished-layout updates and congestion; enforceable schemas/test harness; save migration and detailed/aggregate state; report productivity; editor/resource merge recovery; target-PC delivery; representative environment/product/UI/character conversion. Open source does not imply lower total effort. |
| AI-agent and tooling implications | **Hypothesis:** script/resource changes may be amenable to bounded agent patches. The prototype must pin GDScript or C#, account for the documented C# profiling gap, verify provider data-use terms, and measure semantic scene/resource review and human correction. |
| License and cost | **Fact:** the MIT license has no engine royalty or mandatory license fee [G1]. **Current Margins implication:** engine license cost fits the pre-revenue cap, but third-party tools, assets, specialist support, and extra human implementation time remain real costs. |
| Unknowns that can change ranking | Navigation mitigation; test stack; GDScript/C# lane; external profiling cost if needed; onboarding; assets; save/schema tooling; editor extensibility; support/dependency burden. |

## Preliminary score state — unproven

The documentary review supports a shortlist and risk questions, not complete composite scores. Every preliminary score is therefore `U` (unscored). `U` is not zero, a midpoint, or a tie; no total or ranking can be calculated.

| Criterion | Unreal | Unity | Godot |
|---|---:|---:|---:|
| C1. Tactile first-person and placement | U | U | U |
| C2. Furnished navigation and simulation boundary | U | U | U |
| C3. Data, validation, persistence, migration | U | U | U |
| C4. Solo iteration, tests, diagnostics, PC delivery | U | U | U |
| C5. Management UI and reporting | U | U | U |
| C6. Modular stylized art and assets | U | U | U |
| C7. Agent-assisted and source-control workflow | U | U | U |
| C8. Licensing, mandatory cost, support/dependency durability | U | U | U |
| **Weighted total** | **Not calculated** | **Not calculated** | **Not calculated** |

Unity's unresolved tier eligibility is evaluated as a potential D2 before scoring; it is not converted into a numeric penalty. The same rule applies to any candidate-specific fatal risk.

## Load-bearing primary sources

Accessed 2026-07-27. Pricing, terms, versions, and support status must be rechecked at prototype start and before selection.

### Unreal Engine

- **U1:** [Unreal Engine EULA and Royalty Addendum](https://www.unrealengine.com/eula/unreal)
- **U2:** [Unreal Engine 5.8 is now available](https://www.unrealengine.com/news/unreal-engine-5-8-is-now-available) (2026-06-23)
- **U3:** [Navigation System in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/navigation-system-in-unreal-engine)
- **U4:** [Automation Test Framework in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/automation-test-framework-in-unreal-engine)
- **U5:** [Unreal Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-insights-in-unreal-engine)

### Unity

- **Y1:** [Unity Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software) (updated 2026-06-30)
- **Y2:** [Unity pricing updates](https://unity.com/products/pricing-updates) (Pro price change effective 2026-01-12; DevOps changes effective 2026-03-01)
- **Y3:** [Unity 6 release support](https://unity.com/releases/unity-6/support)
- **Y4:** [AI Navigation package 2.0 documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation%402.0/manual/index.html)
- **Y5:** [Unity Test Framework 1.6 documentation](https://docs.unity3d.com/Packages/com.unity.test-framework%401.6/manual/index.html)

### Godot

- **G1:** [Godot license](https://godotengine.org/license/)
- **G2:** [Godot download archive](https://godotengine.org/download/archive/)
- **G3:** [`NavigationObstacle3D` reference](https://docs.godotengine.org/en/4.6/classes/class_navigationobstacle3d.html)
- **G4:** [Godot profiler](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/the_profiler.html)
- **G5:** [Exporting projects](https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html)

## Selection state

All three candidates proceed to the same staged prototype plan. None is preferred, eliminated, or approved. Only the project owner may select an engine after comparable evidence and quality review.
