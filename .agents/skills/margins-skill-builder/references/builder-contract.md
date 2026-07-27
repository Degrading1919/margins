# Margins Candidate Builder Contract

Read this reference only after the artifact gate authorizes `build` or `revise`, or while reviewing an existing candidate. Current approved Margins decisions and specifications remain authoritative.

## 1. Artifact-decision record

Record these fields for every invocation:

| Field | Required content |
|---|---|
| Identity | Candidate ID, title, date, owner, requested outcome, representative tasks |
| Repository | Root, remote, base/head, branch, dirty paths, authority files and revisions |
| Recurrence | Why the behavior is expected to recur and who consumes it |
| Classification | Selected artifact; alternatives considered and rejected |
| Scope | One job, positive trigger, nearest exclusions, non-goals, allowed and protected paths |
| Inputs | Required, optional, discoverable, user-decided, prohibited, freshness-sensitive |
| Capabilities | Tools, versions, permissions, authentication, network/data flow, fallbacks |
| Ownership | Owned, Recommended, Prohibited, Approved; human gates and approval state |
| Overlap | Existing local artifacts and any external prior art |
| Contract | Outputs, evidence, completion, blocked/failed behavior, stops, rollback |
| Evaluation | Positive, negative, conflict, permission, rollback, and invariant cases |
| Lifecycle | Provenance, maintainer, invalidation triggers, review date, disable/replace path |
| Decision | `build`, `revise`, `use-other-artifact`, `blocked`, `reject`, or `defer` |

Do not create a package when the decision is not `build` or `revise`.

## 2. Artifact classification

| Need | Prefer | Reject as a skill when |
|---|---|---|
| One completed answer or transient transformation | Prompt or ordinary output | No recurring procedure remains |
| Always-on project rule | Governance or repository instruction | Correctness disappears unless the skill is invoked |
| Assumable lens, focus, or delegated ownership | Role definition or agent configuration | The request is a title, tone, or perspective rather than a procedure |
| Knowledge without ordered action | Reference | No repeatable decision sequence exists |
| Deterministic transformation | Script or tool | Prose is less reliable than reviewed automation |
| Data shape or enforceable invariant | Schema, validator, test, runtime control | Skill prose is proposed as enforcement |
| Conditional repeatable procedure | Workflow skill | The job is nonrecurring, incoherent, or duplicates another artifact |

A role may use skills, and a skill may define role handoffs, but they are separate artifacts.

## 3. Candidate package requirements

### Frontmatter

- Directory and `name` must match in lowercase hyphen case.
- `description` must identify the positive task and nearest exclusions.
- High-impact or persistent-write workflows must require explicit invocation.

### SKILL.md core

Keep only material needed on nearly every valid run:

1. purpose and bounded outcome;
2. authority and required inputs;
3. discover-before-ask rule;
4. workflow and decision gates;
5. tool, permission, write, and approval boundaries;
6. exact output and status vocabulary;
7. failure, stopping, rollback, and maintenance rules;
8. direct links to conditional references;
9. critical never-do rules.

### Resources

- Use references only for conditional detail that would otherwise bloat common context.
- Add scripts only when deterministic reliability outweighs executable risk.
- Add assets only when an approved consumer, provenance, and rights are known.
- Do not add package-level README files, changelogs, speculative examples, or empty directories.
- Every resource must be reachable directly from `SKILL.md`.

## 4. Candidate contract

Define before authoring:

- **Inputs:** source, precedence, freshness, required status, discover-or-ask behavior.
- **Questions:** only decision-changing questions after repository discovery.
- **Tools:** exact capability, permission, authentication, data flow, fallback, and truthful unavailable state.
- **Writes:** repository root, base/head, dirty-state rule, allowed paths, protected paths, temporary artifacts, staging, recovery, and publication owner.
- **Output:** exact artifact, required sections, evidence, non-goals, and consumer.
- **Completion:** observable outputs and gates; never substitute `pass` for `blocked`, `not_run`, or `unavailable`.
- **Failure:** recoverable retry, missing prerequisite, refusal, failed attempt, escalation owner, cleanup, and rollback.
- **Lifecycle:** maintainer, supported surfaces, compatibility limits, re-audit triggers, disable and replacement paths.

## 5. Role ownership model

For role-like behavior, record:

| Dimension | Meaning |
|---|---|
| Owned | Bounded decisions the role may make and record |
| Recommended | Options the role may analyze but not decide |
| Prohibited | Decisions, writes, or effects the role must never perform |
| Approved | Human owner of acceptance, exception, publication, or release |

Use a role definition when the core value is a persistent lens, interpretation method, attention priority, or delegated responsibility. Use a skill only for the repeatable procedure the role may execute.

## 6. Margins applicability matrix

| Invariant | Applicability question | Required handling | Critical stop |
|---|---|---|---|
| Progression fantasy | Could the candidate keep the player trapped in repetitive labor or automate before mastery? | Trace manual action through employee, manager, and portfolio control | Progression no longer moves from doing to systemizing and delegation |
| Shared architecture | Could the candidate create business-specific systems? | Map shared reuse and justify unique engineering against current approved guidance | A business becomes an unrelated mini-game or silently forks core systems |
| Scope | Could the candidate add vertical-slice work or dependencies? | Compare against current scope commitments and deferred list | Deferred work is treated as committed without owner approval |
| Simulation modes | Could it affect detailed and off-site simulation? | Define both models and transitions, including state reconciliation | Outcomes diverge, duplicate, disappear, or become exploitable during mode changes |
| Economy explainability | Could it alter revenue, cost, demand, staffing, or reports? | Identify tunable inputs, derived outputs, and player-visible causal explanation | Financial outcomes cannot be traced to understandable causes |
| Data and persistence | Could it create structured content, IDs, saves, or migrations? | Use approved schemas and validation ownership | Fields or contracts are invented before approval |
| Technical authority | Could it select an engine, language, service, or architecture? | Remain neutral or reference an approved decision | A tentative tool choice becomes project authority |
| Rights and provenance | Could it copy, adapt, install, or distribute external material? | Record source, revision, license, notices, and intended use | Rights are assumed or incompatible material is reused |
| Human direction | Does acceptance depend on creative direction, scope, accessibility, security, licensing, or release risk? | Name the accountable human reviewer | An LLM score substitutes for owner approval |
| Defense in depth | Is the skill proposed as the only safeguard? | Put durable rules in governance, schemas, tests, runtime, and human review | Protection disappears when the skill is not invoked |

## 7. Evaluation specification

For each candidate define:

- exact candidate revision and context manifest;
- baseline current workflow and treatment with the candidate;
- positive, paraphrased, near-miss, irrelevant, stale/conflicting-authority, missing-input/tool, permission-denied, prohibited-expansion, and rollback cases;
- expected and prohibited behavior for every case;
- required files, outputs, diffs, tool effects, and human approvals;
- critical vetoes that cannot be averaged away;
- unsupported or unavailable layers and their truthful status.

Structural validity proves package shape only. Adoption requires evidence that the skill improves recurring work without violating authority, scope, permissions, or project invariants.

## 8. Acceptance and maintenance

Before handoff:

- inspect the complete candidate tree and diff;
- confirm all links and paths resolve;
- confirm the skill catalog is updated in the same change set;
- run the available structural and clean-context checks;
- record unavailable validators truthfully;
- preserve initial failures and retries;
- confirm no commit, push, merge, publication, registration, or installation occurred without explicit permission;
- define maintainer, invalidation triggers, and rollback through Git history.