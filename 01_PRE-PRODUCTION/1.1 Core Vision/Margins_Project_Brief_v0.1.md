# Margins Project Brief

## Status and authority

- **Status:** Current pre-production brief synchronized to the approved foundational and engine-selection decisions
- **Authority:** This brief summarizes approved direction but does not outrank records in `00_ADMIN/Decisions`.
- **Maturity:** Foundational direction and Unity as the production engine are approved; detailed game design, Unity baseline, technical architecture, tuning, schedules, and milestone acceptance criteria remain in development.

## Production mandate

Margins is planned as a deliberately bounded solo-developed PC game supported by extensive agentic AI workflows.

Current constraints:

- less than **$1,000** in pre-revenue development spending;
- approximately **20–30 direct human development hours per week**;
- a playable vertical slice as the first objective; and
- a commercial premium single-player release as the end goal.

Later expansion may occur through updates, revenue, collaborators, or a sequel only when justified. None of those expansion paths is presently committed.

## Technical baseline status

**Unity is the approved production engine.**

The exact Unity version, rendering pipeline, packages, coding conventions, project structure, persistence approach, target hardware, and performance budgets remain unresolved. Unreal Engine and Godot are retained only as historical evaluated alternatives unless the project owner later reopens engine selection after a concrete Unity blocker.

## High concept

A first-person business-management and property-development simulator in which the player begins with one small leased storefront, learns the business through tactile work, develops employees and managers, expands across a fictional contemporary American city, and ultimately controls a diversified business and commercial-property portfolio.

## Core fantasy

Begin behind the counter. Learn the operation by doing the work. Build reliable procedures. Hire and develop employees. Delegate to managers. Open new locations under different market conditions. Acquire and improve commercial property. Eventually direct brands, properties, financing, and portfolio strategy from a headquarters the player has earned and shaped.

## Core progression

**Operate → Systemize → Delegate → Expand → Develop → Control**

- **Early game:** tactile first-person operation and survival of the first location.
- **Mid-game:** staffing, procedures, management, multi-location expansion, and remote oversight.
- **Late game:** holding-company strategy, property development, corporate finance, competition, acquisitions, and legacy projects.

Delegation must replace repetitive labor with larger decisions rather than remove gameplay. Later strategic systems must remain visibly connected to the physical businesses that taught the player how the company works.

## Product identity and simulation depth

Margins is a deliberate progression hybrid rather than exclusively a shop simulator, spreadsheet simulator, or construction game.

The default experience should be approachable but meaningfully deep. Advanced optimization, reporting, automation controls, and harder difficulty may provide additional depth. Players should be able to understand why a business succeeds or fails without relying on outside guides.

Complexity must create choices rather than clerical burden.

## Setting and presentation

Margins takes place in one fictional, contemporary American city composed of expanding handcrafted districts built from reusable modules.

The approved visual direction is **Stylized Contemporary Americana**:

- **Road 96** is the primary visual reference for stylized contemporary and roadside Americana;
- **Schedule I** is the practical model and animation complexity reference; and
- **Firewatch** is the reference for lighting, color, atmosphere, silhouettes, and environmental composition.

The approved tone is grounded with light humor:

> The business is real; the people and brands have personality.

Named references identify selected qualities only and are not replication targets.

## Shared-system principle

Business categories should feel distinct while sharing a reusable simulation foundation rather than becoming unrelated mini-games.

The current planning guideline remains:

- roughly 70% shared systems;
- roughly 20% business-specific content and rules; and
- no more than roughly 10% unique engineering per business category without explicit justification.

The shared foundation includes property and location, customer demand, staffing and management, pricing and inventory, service capacity, maintenance, reputation, finance, delegation, off-site aggregate simulation, reporting, construction, and repeatable layouts.

## Vertical-slice objective

The first playable proof is a **standalone convenience store** operating across at least two commercial locations with meaningfully different market conditions.

The vertical slice must prove that it is enjoyable and understandable to:

1. lease and prepare the first store;
2. perform essential receiving, stocking, checkout or service, cleaning, and maintenance work;
3. manage products, pricing, customers, and satisfaction;
4. hire and schedule at least two worker roles;
5. appoint a manager and delegate operations;
6. open a second location;
7. simulate the first location while absent;
8. review both locations through location and portfolio reporting; and
9. save and restore layouts and business state.

Vertical-slice work should survive into the commercial product rather than be treated as disposable implementation.

## Long-term company structure

The long-term company fantasy is a holding-company hybrid that may include:

- multiple locations under one brand;
- multiple distinct business brands;
- standardized or franchised formats;
- owned commercial properties containing player-operated businesses or outside tenants; and
- a headquarters coordinating the portfolio.

The 1.0 minimum targets at least two complete business categories, property ownership and development, and the core holding-company progression. The second category will be selected only after the convenience-store vertical slice is validated.

## Commercial path

The approved release path is:

**Internal vertical slice → public demo or controlled playtest → evidence-driven stabilization → possible quality-gated Early Access → complete premium release**

Early Access is optional rather than guaranteed. Release date, price, storefront, and marketing plan remain unresolved.

## Explicitly unresolved

This brief does not select or define:

- Unity version, rendering pipeline, packages, scripting boundaries, or runtime architecture;
- exact economy, progression, difficulty, or balance values;
- the second business category;
- city name, map, district roster, or full content plan;
- exact property-construction depth;
- whether full driving is ultimately adopted;
- detailed rival-company or acquisition implementation;
- public markets, IPOs, or stock trading;
- public mod support or compatibility guarantees;
- detailed art bible, audio direction, animation standards, or technical budgets;
- accessibility feature scope;
- roadmap stages, schedules, or acceptance criteria; or
- any post-1.0 expansion promise.

Use `00_ADMIN/Decisions/Margins_Foundational_Decisions_v1.0.md` and `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md` whenever more precise status or boundary language is required.
