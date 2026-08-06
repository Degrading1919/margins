# Margins Skill Catalog

## Purpose

This is the living index of repository-local Margins skills. It provides a short explanation of what each skill does and when it should be invoked without duplicating the complete workflow contained in each package.

The skill package and current approved project documents remain authoritative. This catalog is a navigation and maintenance record.

## Maintenance rule

Update this document in the same change set whenever a skill is:

- added;
- renamed;
- materially repurposed;
- replaced;
- deprecated;
- quarantined;
- removed.

Keep each description to one or two concise lines. Record the exact invocation name and nearest non-use case.

## Active skills

### `$margins-skill-builder`

Decides whether a recurring Margins behavior belongs as a skill, role, repository rule, reference, script, schema, validator, test, or ordinary output, and authors one bounded skill only when justified. Use deliberately when evaluating or creating reusable LLM workflows; do not use for routine project work.

Path: `.agents/skills/margins-skill-builder/`

### `$tripo-3d-prompting`

Writes fact-checked Tripo prompts and reference-image briefs for Margins props, architecture, vehicles, vegetation, products, and rig-ready base characters while applying current art direction and technical ceilings. Use before Tripo generation; not for final asset acceptance, cleanup, rig repair, or guarantees about topology, polygon count, or correction time.

Path: `.agents/skills/tripo-3d-prompting/`

### `$margins-business-type-designer`

Designs or substantially revises one business category while mapping shared systems, bounding unique engineering, defining detailed and off-site simulation, and tracing delegation. Use when proposing businesses such as convenience stores, gas stations, coffee shops, laundromats, arcades, or hobby shops; not for minor product or balance edits.

Path: `.agents/skills/margins-business-type-designer/`

### `$margins-vertical-slice-scope-gate`

Classifies proposed work against the current vertical-slice commitments and deferred list, exposes hidden scope multipliers, and identifies the smallest acceptable proof. Use before adding features or dependencies to the first playable milestone; it cannot approve exceptions or rewrite scope.

Path: `.agents/skills/margins-vertical-slice-scope-gate/`

### `$margins-simulation-feature-integration-reviewer`

Reviews a feature across detailed first-person simulation, aggregate off-site simulation, delegation, economy, persistence, reporting, and mode transitions. Use on specifications, implementation plans, or pull requests that change durable simulation state; not for isolated art, copy, or minor tuning.

Path: `.agents/skills/margins-simulation-feature-integration-reviewer/`

## Skill versus role

A **skill** is a conditionally invoked, repeatable procedure with defined inputs, steps, outputs, stops, permissions, and validation.

A **role** is an assumable operating lens that changes what an agent prioritizes, how it interprets evidence, which decisions it owns, what it may recommend, and what it must defer. Roles should be defined as role or agent configuration artifacts, not disguised as skills merely because they influence tone, focus, or expertise.

A role may invoke one or more skills. A skill may route work to a role owner. They remain separate:

- the role determines **how the agent thinks and what it is accountable for**;
- the skill determines **which repeatable procedure it performs**.

See `00_ADMIN/Reference/Margins_Role_and_Skill_Model.md` for the project convention.

## Status vocabulary

- **active:** available for intended use;
- **candidate:** authored but not accepted for normal use;
- **blocked:** missing required authority, capability, or validation;
- **quarantined:** retained for inspection but excluded from activation;
- **deprecated:** superseded and awaiting removal or archival;
- **retired:** no longer part of the active catalog.

## Review triggers

Review this catalog and affected skills after material changes to:

- project vision or scope;
- engine, language, schemas, save architecture, or simulation model;
- repository instructions or authority hierarchy;
- skill runtime, client metadata, official validators, or supported tools;
- permissions, network access, external dependencies, or publication workflow;
- the responsibilities of a role that consumes a skill.