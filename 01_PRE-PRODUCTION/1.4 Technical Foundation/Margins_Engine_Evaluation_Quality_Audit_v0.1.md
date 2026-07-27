# Margins Engine Evaluation Quality Audit v0.1

**Status:** Completed quality record for a proposed engine-neutral package<br>
**Package reviewed:**

- `Margins_Engine_Evaluation_Criteria_v0.1.md`
- `Margins_Engine_Candidate_Shortlist_v0.1.md`
- `Margins_Engine_Risk_Prototype_Plan_v0.1.md`

This audit checks the evaluation package; it does not select an engine, amend approved decisions, or create project-owner approval.

## Gate A — Repository fidelity

**Disposition:** PASS

| Check | Result |
|---|---|
| Current repository authority | Reviewed current `main`, the repository authority hierarchy, approved Foundational Decisions v1.0, their quality/synchronization records, assistant role boundaries, and current pre-production direction before drafting. |
| Roadmap status | The first roadmap PR is merged on `main`. `Margins_Master_Roadmap_v0.1.md` identifies itself as a proposed planning baseline; the package keeps it subordinate to approved decisions and does not modify it. |
| Criterion traceability | Every weighted criterion maps to named repository files and, where available, Foundational Decision IDs. Weights reflect the tactile convenience-store slice, simulation boundary, data/persistence needs, solo constraint, and budget—not a generic engine template. |
| Hard requirement versus preference | PC, budget, human hours, solo/agent workflow, slice interactions, two locations, detailed/aggregate state, roles, data control, saves, and reports remain requirements. Stylized modular production is evaluated without turning a specific asset pack or rendering feature into a requirement. |
| Employee wording | The package preserves “at least two worker roles and one manager role”; it does not convert that wording into an approved minimum employee headcount. Prototype representatives are test fixtures only. |
| Deferred scope | Multiplayer, driving, public modding, public markets, and deep rival-company AI remain excluded. Property, holding-company, finance, competition, acquisitions, and additional businesses appear only as later extensibility concerns and are explicitly excluded from prototype implementation. |
| Unresolved decisions | Target PC operating system/hardware/performance gates, per-candidate effort ceiling, implementation/release lanes, Unity entity/Total Finances, source-access need, agent-provider data controls, dependency tolerance, binary-asset workflow, and budget allocation remain project-owner choices. |
| Authority boundary | The Technical Architect proposes evidence and criteria. No assistant, score, roadmap line, source, or prototype plan is presented as engine approval; only the project owner may select an engine or change constraints. |
| Artifact scope | Exactly four documentation files are proposed in Technical Foundation. No foundational decision, role, skill, scope, roadmap, code, project, scaffold, or ticket is modified or generated. |

### Gate A notes

- Current pre-production files are used as subordinate direction; they are not promoted above approved decisions.
- Later extensibility affects support/dependency durability evaluation only. It does not add vertical-slice systems.
- Criteria weights total exactly **100%**.

## Gate B — Source and factual accuracy

**Disposition:** PASS after factual correction; executable suitability remains unproven

Official primary sources were accessed on 2026-07-27. The shortlist retains no more than five load-bearing sources per candidate and labels project-specific implications separately.

| Fact class | Verified result | Residual limitation |
|---|---|---|
| Licensing, pricing, royalty | Unreal EULA: intended Royalty Product development has no seat fee; 5% is the standard rate after exclusions including the first $1 million lifetime gross revenue, while a conditional Launch Everywhere path is 3.5%. Unity terms: Personal/Pro/Enterprise depend on defined trailing-12-month Total Finances; current minimum Pro price is $2,310/year. Godot: MIT permits commercial use with notice obligations and no engine royalty. | Unity eligibility is unresolved because approved facts do not establish the developing entity/individual or Total Finances; Pro would trigger potential D2. Terms can change and need owner/legal review. Optional assets/plugins/support remain unpriced. |
| Version and support status | Unreal 5.8 release status/date and Epic's stated UE5 maintenance direction; Unity current 6.5 update family and 6.3 LTS support date; Godot 4.6.3 stable listing/date. | A prototype release lane is not yet chosen. Future releases may change behavior or support. |
| PC delivery | Repository authority says PC, not Windows. The target desktop operating system remains an owner choice, so the earlier Windows assumption was removed. P4a requires current official target support and a runnable target build before feature work. | No engine was installed and no build/export was executed; no candidate receives platform credit yet. |
| Source access | Unreal's current licensing material provides source-code access under its license; Unity's current plan information associates source-code access with Enterprise; Godot publishes engine source under MIT terms. | Access, buildability, and practical value were not tested. The owner has not decided whether engine source is required. |
| Navigation | Official Unreal and Unity navigation documentation establishes testable systems. Godot's 4.6 class documentation marks `NavigationObstacle3D` experimental and documents repeated rebuild behavior for moving vertex obstacles. | Furnished-store reliability, crowding, movable-fixture cost, and recovery are prototype questions. |
| Testing and profiling | Official Unreal automation/Insights, current Unity Test Framework 1.6 and Unity 6.5 Profiler, and Godot profiler documentation were checked. Godot documents a current C# profiling limitation. | Project test-stack completeness, command-line reproducibility, actual CPU/GPU/memory coverage, trace usefulness, and language choice remain unexecuted. |
| Tooling and AI | Unreal 5.8's MCP integration is officially experimental. The binding EULA also restricts Licensed Technology as generative-AI training input, including prompted services that train on inputs. Other agent-workflow statements remain inference or hypothesis. | Approved agent providers, retention/training terms, and permissible licensed inputs are unresolved. No integration was exercised. |
| Asset pipeline | The package makes no quantitative ecosystem claim and gives no score for marketplace size. | Representative environment, product, UI, and character import is intentionally deferred to P1. |
| Fact versus inference | Candidate profiles label verified facts, Margins-specific inference, hypotheses/prototype questions, and unresolved issues. Every composite preliminary score is U and no weighted total is calculated. | Numeric scores wait for complete, non-overlapping evidence. |

### Gate B factual limitations

- Research used publicly accessible official pages; no account-gated engine source repository, paid plan, asset, or support channel was inspected.
- No editor, package, plugin, export template, profiler, test runner, asset import, save migration, or source-control merge was executed.
- Current terms were summarized for comparison and require owner/legal review if they become decision-critical.
- Performance targets cannot be validated before target hardware and budgets are approved.

## Gate C — Adversarial practicality

**Disposition:** PASS AFTER REQUIRED CORRECTIONS

| Item | Record |
|---|---|
| Independent auditor | GPT-5.6 Sol, fresh independent context |
| Reasoning setting | Max |
| Blocker findings | **2 on draft:** Unity zero-cost assumption lacked Total Finances authority; Unreal summary omitted the conditional 3.5% rate and binding generative-AI input restriction. |
| Major findings | **9 on draft:** unsupported composite scores; Windows authority leak; missing demand/customer-instantiation handoff; diagnostic-gap pass contradiction; undefined remediation/navigation/clean-environment terms; weak tactile/asset acceptance; C4/C7/C8 evidence overlap; optimistic effort; unpinned implementation lanes. |
| Minor findings | **5 on draft:** Epic release wording, Unity effective-date wording, stale Unity test source, repeated candidate questions, and non-actionable report acceptance. |
| Required corrections | Binding EULA/terms rechecked; Unity cost made unresolved/potential D2; all preliminary scores changed to U; owner-designated PC target restored; scoring evidence partitioned; lanes/agent data controls added; prototype gates and effort ranges revised. All blocker, major, and minor findings were corrected. |
| Prototype reductions | P4a smoke moved before feature work; cleaning/maintenance share one secondary pattern; P1 repetitions became a fixed placement matrix plus assertions; comprehensive profiling removed from P2; P3 uses one fixed report table and one action cue. |
| Unsupported claims removed | Removed free-Unity implication, Windows-as-authority claim, unsupported numeric/confidence scores, and “possible 5.9.” Corrected Unity dates and current Test Framework source; standard versus conditional Unreal royalty rates are explicit. |
| Unresolved risks | Unity tier eligibility; owner-designated PC target; agent-provider/license compatibility; Godot C# profiling cost; implementation lanes/prior experience/order bias; paid assets, DCC tools, plugins, storage/LFS, and specialist support. |
| Final disposition | The independent auditor returned **FAIL on the draft**. After targeted primary-source rechecks and correction of every blocker, major, and minor finding, the orchestrator disposition is **PASS for publication as an unresolved evaluation package**. |

No second broad audit was run. The new binding-term statements were targeted-rechecked against the current official Unreal EULA, Unity Editor Software Terms, and Unity pricing page on 2026-07-27.
