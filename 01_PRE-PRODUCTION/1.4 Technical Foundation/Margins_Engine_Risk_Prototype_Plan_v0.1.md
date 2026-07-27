# Margins Engine Risk Prototype Plan v0.1

**Status:** Proposed comparative experiment plan; no prototype work authorized by this file<br>
**Authority:** Derived from approved foundational decisions and `Margins_Technical_Foundation_Direction_v0.1.md`; subordinate to project-owner approval<br>
**Candidates:** Unreal Engine, Unity, Godot<br>
**Selection state:** Unresolved

## Purpose and boundary

Produce the smallest comparable executable evidence that can distinguish the three shortlisted engines. This task created no code, project, scaffold, or implementation ticket.

Future prototypes must not become a convenience-store vertical slice. They use representative content only and must not implement final art, animation, UI, economy, AI, persistence, production architecture, or deferred features.

## Shared controls

Prepare one engine-neutral fixture before candidate work:

- one small blockout store with a counter, stockroom, three furnished aisle runs, two repositionable fixtures, and legal/illegal placement cells;
- twelve product records, two box types, four fixture records, three employee-role records (two worker roles and one manager role), two location records with different test demand inputs, and deliberate invalid data cases;
- one modular environment sample, representative product meshes/materials, one UI sample, and one modest-complexity character/animation sample;
- fixed input sequences and random seeds where supported, the same scenario durations, and a semantic expected-state ledger;
- a common observation sheet for setup time, active human time, agent time, iteration/build time, defects, recoveries, dependencies, costs, artifact sizes, and evidence links.

Use the same development hardware, project-owner-designated desktop PC target, source-control host, asset inputs, and release-date cutoff. Pin every engine, package, plugin, and export-template version. Candidate-native implementations may differ; forcing a shared architecture would contaminate the comparison.

Performance must be captured, but final frame-time and minimum-hardware gates remain unset until the project owner defines target hardware.

Before implementation, the owner must approve one primary prototype lane per candidate and it must remain fixed unless a remediation records the change. Proposed controls—not production-language decisions—are Unreal Blueprint for editor/interaction wiring with C++ only for a test that cannot be expressed comparably, Unity C#, and Godot GDScript. If the owner instead chooses Unreal C++ or Godot C#, revise the effort range and profiler/test dependencies before work. Also pin the agent provider/model, retention/training setting, maximum change size, prompt/acceptance template, and licensed material that may be exposed. Record the evaluator's prior experience per lane, separate onboarding from task time, and rotate candidate order between packages so one engine is not always first or last.

### Gate definitions

| Term | Fixed meaning |
|---|---|
| Package timebox | The upper direct-human estimate stated for that package, including candidate-specific onboarding and dependency troubleshooting. Reaching it stops work; it does not silently relax a pass condition. |
| One bounded remediation | One root-cause-specific correction after the first complete run, capped at 2 hours for P4a/P4b, 4 hours for P1/P2, and 6 hours for P3. The cap is included in each range. Multiple unrelated failures do not receive repeated resets. |
| Clean environment | A fresh OS user or ephemeral machine/VM matching the owner-designated PC target, with no project checkout or candidate cache. Install only documented prerequisites, restore the pinned checkout/dependencies, then run checks/build. Credentials may be supplied but not committed. |
| Candidate gate failure | The package remains below its fixed pass condition after the bounded remediation. It triggers hard-disqualifier review; it is not automatically an elimination. |

The planning estimate is not the D8 effort ceiling. The owner must approve that ceiling separately.

## Package P1 — Tactile store cell and representative content

| Field | Specification |
|---|---|
| Question | Can one solo developer create stable, readable first-person handling and placement with the required stylized content workflow? |
| Requirements traced | Criteria C1 and C6; Foundational Decisions FD-004, FD-011, FD-016; Project Brief; Art/Audio/Presentation Direction |
| Minimum implementation | Import the shared environment, product, UI, and character samples. In one blockout cell: pick up/rotate/place a product; open/carry/place a box containing six product units; snap products to explicit shelf slots; move one fixture on a grid; scan six products exactly once. Apply one shared secondary-interaction pattern to both a dirty surface and a faulty fixture so cleaning and maintenance are covered without separate systems. Use placeholder hands and animation. |
| Evidence to capture | Uncut task recording; logical-state assertions; collision/physics defects; a fixed matrix of twelve valid and twelve invalid shelf placements; three complete box unloads; three checkout sequences; same-evaluator tactile/readability rubric; three clean reimports per asset sample; import/setup and iteration time; conversion/material/rig/animation defects; dependency and cost list. |
| Pass condition | The log and visible state agree; all valid placements are accepted and all invalid targets rejected with no lost/duplicated item; each checkout records each item once; no unrecoverable physics state occurs. On a 0–2 rubric for target readability, grasp/rotation control, snap feedback, and scan feedback, no dimension scores 0 and the total is at least 6/8 from the same evaluator in every candidate. Each environment/product/UI/character sample reimports three times at the specified scale/orientation with required material/texture references intact; the placeholder character animation plays without rig corruption. |
| Failure or disqualification condition | Candidate gate failure if the pass condition remains unmet after one documented remediation within the package timebox. Escalate for D4/D8 review if the failure requires engine modification, an unapproved paid dependency, or effort beyond the owner-approved ceiling; do not auto-disqualify from one defect. |
| Must not be built | Full receiving, inventory, pricing, payment, economy, customer service, final hands/animation, final art/UI, generalized interaction framework, or production placement system. |
| Reusable / disposable | Reuse only source assets, input records, recordings, and observations. Treat engine projects and all interaction code/graphs as disposable unless later accepted explicitly. |
| Direct human effort | **18–30 hours per candidate**, excluding the shared fixture and including the four-hour remediation cap. |
| Agent-delegable work | Import checklists, bounded interaction drafts, data fixtures, focused checks, log parsing, evidence indexing. Human retains feel/readability judgment, dependency approval, defect triage, and time verification. |
| Dependencies | Shared fixture and approved candidate release; no P2/P3 system required. |
| Cross-candidate comparison | Pass/fail first; then human hours, iteration latency, recoveries, required custom logic, dependencies/cost, diff review burden, and asset defects. Lines of code/nodes are not a success metric. |

## Package P2 — Furnished navigation and rearrangement

| Field | Specification |
|---|---|
| Question | Can customers and persistent staff roles navigate a furnished, player-rearrangeable interior without brittle or disproportionate custom work? |
| Requirements traced | Criteria C2 and C4; Foundational Decisions FD-004 and FD-015; Initial Scope Boundaries; Technical Foundation Direction |
| Minimum implementation | Reuse the P1 store blockout. Instantiate twelve placeholder customers plus one representative for each of two worker roles and one manager role; all fifteen remain active. Mark legal goals by a solo-agent precheck against static collision. Issue fifty seeded requests per layout at a fixed two-second cadence across entrance, aisles, shelves, counter, stockroom, and staff point. Run a baseline layout, move two fixtures once, apply the candidate's supported navigation-update method, and repeat. Roles need only goal markers—not production behavior. |
| Evidence to capture | Goal issue/arrival/failure log; solo-agent baseline travel time per route; paths through collision; no-progress and recovery events; navigation update latency; crowding recording; setup and tuning time; custom avoidance/rebake work; behavior after save/load of the rearranged layout if P3 is already available. Comprehensive profiling remains P4b work. |
| Pass condition | At least 49 of 50 legal goals arrive in each layout before `4 × solo baseline travel time + 5 seconds`; no path crosses a solid fixture; all legal zones remain reachable after rearrangement. “Stuck” means less than 0.25 m progress for 10 seconds while a legal path exists; “permanent” means no supported recovery within the next 10 seconds. No permanent stuck event is allowed. Failures and navigation-update latency must be observable and reproducible. |
| Failure or disqualification condition | Candidate gate failure for repeatable collision crossing, unreachable legal zones, or permanent stuck states after one bounded remediation. Escalate for D4/D6/D8 review when supported workflows cannot expose or correct the failure within the timebox. |
| Must not be built | Final customer AI, schedules, queues, demand, needs, dialogue, task delegation, detailed animation, crowd simulation, or generalized behavior-tree architecture. |
| Reusable / disposable | Reuse the common store geometry, goal set, seeds, and logs. Navigation code/graphs, placeholder agents, and tuning are disposable comparison work. |
| Direct human effort | **14–24 hours per candidate**, including the four-hour remediation cap. |
| Agent-delegable work | Seeded goal generation, instrumentation, log summaries, focused reachability checks, and documentation. Human verifies visible navigation quality, diagnoses engine/tool behavior, and accepts no hidden test relaxations. |
| Dependencies | Shared fixture; P1 blockout/import result. It does not depend on P1 interaction code. |
| Cross-candidate comparison | Same layouts, fifteen-agent concurrency, request cadence, goals, seeds, hardware, and run count. Compare pass rate, permanent/recoverable failures, update latency, human tuning time, and custom/dependency burden. |

## Package P3 — Data, save, simulation handoff, and two-location report

| Field | Specification |
|---|---|
| Question | Can the engine support inspectable structured content, actionable validation, versioned state, and reliable detailed/aggregate ownership without premature production architecture? |
| Requirements traced | Criteria C2, C3, and C5; Foundational Decisions FD-004, FD-015, FD-028; Economy/Progression Direction; Technical Foundation Direction |
| Minimum implementation | Load the shared product, box, fixture, role, and two-location records. Reject deliberate missing, duplicate, type, range, and reference errors. Save and restore: modified layout, inventory, persistent employee identity/role/status, both locations, and portfolio totals. Migrate one synthetic prior save revision after one field rename/addition. Run a small deterministic demand ledger while Location A is detailed and Location B aggregate. At the presence boundary, convert four scheduled visit tokens into nearby placeholder customer records and return unresolved tokens exactly once when leaving; no customer behavior is required. Switch presence twice. Render one fixed report table reconciling per-location inventory, sales/cost totals, staffing, and exceptions, with one threshold-driven action cue such as “reorder” or “staffing gap.” |
| Evidence to capture | Source data and invalid cases; validator output and exit behavior; semantic before/after state comparison; save format/version and migration result; twenty seeded transition sequences; event/time/demand ownership log; visit-token/instantiated-customer reconciliation; duplicate/lost entity and negative-state checks; report totals, exception, and action-cue reconciliation; implementation and diagnosis time. |
| Pass condition | All valid records load and all deliberate invalid cases fail with record/field/action guidance; save/restore is semantically equal; the synthetic old revision migrates or fails safely without overwrite. Across twenty sequences, each interval, demand event, and visit token is processed once; exactly the expected nearby customer records exist while detailed and reconcile on return to aggregate; no product/employee/location/customer token is lost or duplicated; invariants hold; report values and its action cue reconcile exactly to the stored test ledger. |
| Failure or disqualification condition | Candidate gate failure if invalid content can silently enter, a failed migration corrupts the prior save, state cannot round-trip, transition ownership double-counts/skips work, or reports do not reconcile after one bounded remediation. Escalate for D3/D6/D8 review; ordinary project-owned save code is not itself a disqualifier. |
| Must not be built | Production economy, demand model, customer simulation, final reports, database/service layer, generalized event bus, complete save framework, cloud saves, properties, holding company, finance, acquisitions, competition, public markets, or public modding. |
| Reusable / disposable | Reuse engine-neutral fixture data, expected ledger, invalid cases, and semantic assertions. Candidate adapters, UI, saves, and simulation code are disposable unless later reviewed independently. |
| Direct human effort | **24–40 hours per candidate**, including the six-hour remediation cap. |
| Agent-delegable work | Fixture generation, invalid-case expansion, bounded loader/validator/save drafts, deterministic checks, state diffs, and report reconciliation. Human owns state semantics, migration safety, architecture restraint, and interpretation of failures. |
| Dependencies | Shared records and expected ledger; P1 provides the representative layout state. P2 navigation is not required. |
| Cross-candidate comparison | Use identical semantic records, invalid cases, saves, seeds, transitions, and expected ledger. Compare correctness first, then human hours, error quality, diffability, migration burden, automation, custom code/dependencies, and diagnosis time. |

## Package P4 — Reproducible development, agent, profiling, and PC delivery

| Field | Specification |
|---|---|
| Question | Can a solo human and bounded agents reproduce, review, diagnose, and deliver the evidence without fragile hidden state? |
| Requirements traced | Criteria C4, C7, and C8; Foundational Decisions FD-001, FD-002, FD-003, FD-009, FD-010, FD-028; Assistant Roles; Technical Foundation Direction |
| Minimum implementation | **P4a smoke—run before P1:** resolve owner/entity tier eligibility, binding terms, mandatory cost, owner-designated PC target, implementation lane, and agent-provider training/retention compatibility; install the pinned release; from a clean environment restore a minimal tracked project, run one failing and one passing automated check, capture CPU/GPU/memory evidence, make/merge/revert one script-or-graph/data/scene change, and produce a runnable target-PC build. **P4b completion—run after P1–P3:** repeat from a clean environment with the fixed five-minute prototype workload; exercise two branches changing one script/graph, one data record, and one representative scene/resource; then give the pinned agent one bounded defect/data change with acceptance checks and record human review/correction. |
| Evidence to capture | Current binding terms and eligibility inputs; permitted agent data path; clean-environment instructions; dependency lock/inventory; check and target-build outputs; build/restore time and size; CPU/GPU/memory profiler/trace files; source-control diffs/conflicts/revert; failed-test visibility; agent prompt boundary, patch, result, review time, defects, and human corrections; mandatory/optional costs. |
| Pass condition | **P4a:** tier/license eligibility and compliant agent use are resolved; mandatory cost fits the approved cap; the owner-designated PC target is supported; a clean environment reproduces checks, actual CPU/GPU/memory evidence, source-control recovery, and a runnable build. **P4b:** a second clean environment reproduces all focused checks and the workload build; failures identify a bounded cause; actual CPU/GPU/memory evidence covers the workload; representative changes are reviewed, merged/recovered, reverted, and rebuilt; the agent change is accepted only through the same checks and human review. Recording a missing diagnostic capability is evidence of failure, not a pass. |
| Failure or disqualification condition | Candidate gate failure if eligibility/compliant agent use, clean reproduction, target build, actual diagnostic coverage, or change recovery remains unresolved after the applicable bounded remediation. Escalate for D1, D2, D5, D6, D7, D8, or D9 review using the captured evidence. |
| Must not be built | Production CI/CD, release automation, custom framework/wrapper, broad editor extension, code-generation system, asset pipeline service, telemetry, installer, or optimization pass. |
| Reusable / disposable | Reuse instructions, check specifications, dependency/cost inventory, captures, and observations. Candidate build scripts, agent changes, and merge-conflict fixtures are disposable. |
| Direct human effort | **12–20 hours per candidate:** 6–10 for P4a and 6–10 for P4b, each including its two-hour remediation cap. |
| Agent-delegable work | Focused checks, build/run documentation drafts, log/trace indexing, dependency inventory, and the bounded trial change. Human validates clean environments, licensing/cost, profiler interpretation, merge recovery, and every accepted agent change. |
| Dependencies | P4a requires the unresolved owner choices and runs before candidate feature work. P4b runs only for candidates surviving P1–P3 and uses exactly that work rather than adding features. |
| Cross-candidate comparison | Same hardware, clean-checkout condition, workload duration, branch exercise, defect request, and observation sheet. Compare reproducibility, diagnosis quality, active/wait time, review burden, conflicts, mandatory dependencies/cost, and recovery effort. |

## Staging and direct human effort

1. Resolve the owner choices required by P4a; no target-build or licensing gate can pass without them.
2. Run **P4a** in all candidates before feature work; apply only evidence-backed hard-disqualifier gates.
3. Prepare the **shared fixture:** 10–18 hours.
4. Run P1 and P2 in every survivor, rotating candidate order by package.
5. Run P3 in every survivor; do not fill eliminated candidates with assumed scores.
6. Run **P4b** in every survivor.
7. Recheck unstable terms and sources; normalize non-overlapping evidence and score: 16–30 hours.

| Scope | Direct human estimate |
|---|---:|
| One candidate, P1–P4 | 68–114 hours |
| Full three-candidate comparison, shared work, and normalization | **230–390 hours** |
| Calendar implication at 20–30 direct human hours/week | Approximately **8–20 weeks**, depending on eliminations, learning, and remediation |

These are planning ranges, not commitments or the D8 ceiling. They include installation, first-use onboarding, dependency troubleshooting, the stated remediation caps, and comparison normalization; they exclude owner-response delay and hardware procurement. An elimination after P4a can avoid that candidate's later 62–104 hours; one after P1/P2 can avoid 30–50 hours. Agent time is recorded separately and never used to conceal direct human review or waiting.

## Future code-minimization standard

Any future prototype code or visual scripting must:

- implement only what answers the stated question; avoid speculative abstractions, duplicate systems, unnecessary dependencies, and premature optimization;
- use clear names and bounded modules; add only logging needed for simple diagnosis and expose failures clearly;
- prefer data/fixtures over repeated hardcoding only when that improves comparison;
- add focused automated checks where they replace repeated manual verification;
- omit comments that only restate obvious code;
- add no framework, wrapper, service, or manager without demonstrated test need;
- avoid reusable production architecture unless the prototype explicitly tests it;
- label disposable work as disposable; measure success by decision evidence, not generated volume or lines of code.

## Completion rule

The comparison is ready for an owner decision only when every surviving candidate has comparable artifacts, unresolved questions are visible, hard-disqualifier evidence has been reviewed, criteria total 100%, and no score exceeds its evidence confidence. This plan does not authorize or imply engine selection.
