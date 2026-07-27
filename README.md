# Margins

**Margins** is a first-person business, property-development, and portfolio-management simulator.

The player begins as a hands-on owner-operator, learns the business by doing the work, then hires employees, develops managers, acquires or constructs properties, creates repeatable business models, and builds a citywide portfolio managed from a headquarters office.

## Core progression

**Operate → Systemize → Delegate → Expand → Develop → Control**

## Current status

Pre-production and repository foundation.

## Repository map

- `.agents` — repository-local agent workflows
- `00_ADMIN` — governance, decisions, roadmaps, references, continuity, and agent-artifact indexes
- `01_PRE-PRODUCTION` — vision, pillars, scope, technical foundation, and content strategy
- `02_VERTICAL_SLICE` — focused prototypes proving the complete owner-to-portfolio loop
- `03_CORE_SYSTEMS` — reusable simulation systems
- `04_CONTENT_PRODUCTION` — business types, districts, buildings, NPCs, products, events, and presentation content
- `05_ALPHA_BETA` — balancing, QA, onboarding, accessibility, performance, and launch preparation
- `CODE` — runtime, editor, prototype, test, and utility implementation
- `DATA` — data-driven definitions and economy tuning
- `DESIGNS` — detailed system specifications, templates, schemas, and UX documentation
- `MEDIA` — concept art, references, maps, branding, audio reference, and UI mockups
- `TOOLS` — internal build, content, editor, and validation utilities
- `ARCHIVE` — deprecated and experimental material outside the active source of truth

## Agent workflows

Repository-local workflow skills live under `.agents/skills`. Their short descriptions and invocation guidance are maintained in `00_ADMIN/Reference/Margins_Skill_Catalog.md`.

Roles and skills are separate artifacts: roles provide bounded professional lenses and decision ownership, while skills provide repeatable procedures. See `00_ADMIN/Reference/Margins_Role_and_Skill_Model.md`.

See `00_ADMIN/Reference/Margins_Repository_Structure.md` for organizational rules.