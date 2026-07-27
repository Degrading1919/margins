# Margins Role and Skill Model

## Purpose

Margins treats roles and skills as separate reusable artifacts.

## Roles

A role is an assumable operating lens. It changes what an agent prioritizes, how it interprets evidence, which risks it emphasizes, what decisions it owns, and what it must defer.

Every role should define:

- mission;
- lens and priorities;
- Owned decisions;
- Recommended decisions;
- Prohibited decisions or effects;
- Approved human owner;
- authoritative inputs;
- expected outputs and handoffs;
- activation and exclusion conditions;
- conflict and escalation rules.

Examples may later include Game Director, Systems Designer, Economy Designer, Technical Architect, UX Designer, Producer, or Art Director.

## Skills

A skill is a conditionally invoked, repeatable procedure. It defines concrete triggers, inputs, ordered steps, decision gates, permissions, outputs, evidence, stopping conditions, rollback, and maintenance.

Roles answer: **From which bounded professional perspective should this work be interpreted and owned?**

Skills answer: **Which repeatable procedure should be performed?**

## Composition

A role may use one or more skills, but the two are not interchangeable.

For example, an Economy Designer role may evaluate incentives, risk, pacing, and business viability. The `$margins-business-type-designer` skill supplies the repeatable procedure for producing a business-type design record. The `$margins-simulation-feature-integration-reviewer` skill then reviews the design across simulation, persistence, delegation, and reporting.

The role supplies the lens. The skill supplies the workflow. Approved project documents supply authority. Schemas, validators, tests, and runtime code enforce durable invariants.

## Storage convention

Until a native role format is deliberately selected:

- canonical role definitions should live in `00_ADMIN/Reference/Margins_Assistant_Roles.md` or another approved governance path;
- platform-specific role configurations may later use `.agents/roles/` after the consuming tool and format are selected;
- workflow skills live under `.agents/skills/<skill-name>/`;
- the living skill index is `00_ADMIN/Reference/Margins_Skill_Catalog.md`.

Do not create a platform-specific role directory merely to anticipate an unselected tool.

## When a role needs a companion skill

Create a companion skill only when the role repeatedly performs a coherent procedure with stable inputs, ordered gates, an exact output, and meaningful negative cases.

- “Think like a producer and prioritize schedule risk” is role behavior.
- “Run the approved vertical-slice scope review and return a formal disposition” is a skill.
- “Interpret features through financial incentives and player comprehension” is role behavior.
- “Generate a schema-compliant economy tuning record” may become a skill once the schema and workflow are approved.

Neither a role nor a skill may become the sole owner of project vision, scope, technical authority, data contracts, licensing requirements, or release decisions.