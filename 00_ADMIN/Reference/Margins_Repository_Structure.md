# Margins Repository Structure

## Purpose

This repository separates project governance, pre-production decisions, prototypes, canonical systems, production content, implementation, data, media, and reusable agent workflows. It adapts the organizational discipline used in Caelmor while replacing RPG-specific domains with the needs of a first-person business and property simulator.

## Source-of-truth hierarchy

When files disagree, prefer material in this order unless an approved decision record states otherwise:

1. Approved decisions in `00_ADMIN/Decisions`
2. Current pre-production scope and technical-foundation documents
3. Canonical specifications in `DESIGNS` and `03_CORE_SYSTEMS`
4. Current data definitions in `DATA`
5. Vertical-slice documents and prototypes
6. Research and reference notes
7. Archived or experimental material

Skills and role configurations apply project authority but do not outrank or replace it.

## Folder roles

- `.agents`: repository-local agent workflows and, only after a consuming format is selected, platform-specific agent configuration
- `00_ADMIN`: project governance, milestones, roadmaps, schedules, research, references, continuity, and living agent-artifact indexes
- `01_PRE-PRODUCTION`: the current definition of the game before broad implementation
- `02_VERTICAL_SLICE`: the smallest complete version proving hands-on operation, delegation, expansion, off-site simulation, and portfolio reporting
- `03_CORE_SYSTEMS`: shared systems from which individual business types are assembled
- `04_CONTENT_PRODUCTION`: production content built on stable systems
- `05_ALPHA_BETA`: testing, balancing, performance, accessibility, onboarding, polish, and launch work
- `CODE`: engine-neutral implementation staging until the engine structure is locked
- `DATA`: data-driven definitions and tuning values
- `DESIGNS`: behavioral specifications, contracts, templates, and schemas
- `MEDIA`: non-runtime source media and references
- `TOOLS`: internal content, editor, build, and validation tooling
- `ARCHIVE`: material that is no longer authoritative

## Agent artifact rules

- Workflow skills live under `.agents/skills/<skill-name>/`.
- The living skill index is `00_ADMIN/Reference/Margins_Skill_Catalog.md`.
- Roles are assumable decision lenses and should remain separate from workflow skills.
- Canonical role definitions belong in an approved governance document until a native agent-role format is selected.
- Adding, renaming, repurposing, deprecating, or removing a skill requires a matching skill-catalog update in the same change set.
- Neither a role nor a skill may become the sole carrier of permanent project truth.

## Naming rules

- Use descriptive file names rather than generic names such as `notes.md`.
- Introduce stage numbers only after the roadmap defines them.
- Use semantic versions for approved schemas and externally consumed formats.
- Do not maintain active files named `final`, `final2`, or `latest`.
- Move replaced work to `ARCHIVE/Deprecated`.
- Remove a folder's `.gitkeep` once real content is added.