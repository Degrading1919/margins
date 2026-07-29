# Margins First-Store Economy and Progression v0.1

## Status and authority

- **Status:** Proposed for project-owner review
- **Scope:** Tuning hypotheses for the first hands-on store proof
- **Balance status:** Unbalanced until exercised in deterministic scenarios and Unity playtests

## Approved economic philosophy

The following direction is already approved and is not reopened here:

- Default pressure should be challenging but recoverable.
- Failure should have visible causes and corrective options before restart or bankruptcy.
- Progression should be capability-based rather than driven by arbitrary levels.
- Cash flow, rent, inventory cost, pricing, maintenance, and later staffing should matter.
- Complexity should create choices rather than bookkeeping chores.
- Reports should connect outcomes to causes.

## Working assumptions

These assumptions exist only to make the first proof testable:

- One small leased convenience store.
- No starting loan, interest, tax, payroll, or customer-demand simulation in this proof.
- One preconfigured delivery and a scripted set of checkout baskets.
- Money uses integer cents.
- Daily rent is represented as a session allocation, not a legal or accounting treatment.
- Wholesale cost is attached to the test product fixture so cost of sold units can be explained.
- Unsold inventory retains its cost basis; no spoilage or shrinkage.
- A negative session result reduces cash but does not trigger eviction or bankruptcy.

## Proposed tuning ranges

| Variable | Low test | Planning test | High-pressure test | Player decision created |
|---|---:|---:|---:|---|
| Startup capital | $8,000 | $10,000 | $14,000 | How much liquidity remains after setup |
| Deposit plus initial lease/setup charge | $2,000 | $3,000 | $4,500 | Store quality versus reserve |
| Essential fixtures and equipment | $1,500 | $2,250 | $3,250 | Minimum setup versus extra capacity |
| Opening inventory purchase | $700 | $1,100 | $1,800 | Assortment and stock depth |
| Daily rent allocation | $70 | $90 | $120 | Fixed-pressure visibility |
| Delivery fee | $45 | $65 | $90 | Order efficiency |
| Utility proxy per session | $20 | $30 | $45 | Basic operating overhead |
| Cleaning/basic-maintenance expense | $10 | $20 | $35 | Cost of keeping the store ready |
| Wholesale unit cost | $0.45 | $1.25 | $4.50 | Product cash tied up |
| Retail unit price | $0.99 | $2.49 | $7.99 | Margin versus later demand |
| Gross margin percentage | 30% | 42% | 55% | Product-mix tradeoff |
| Opening-session gross revenue target | $350 | $500 | $700 | A readable first success band |
| Minimum cash reserve after opening setup | $2,000 | $2,750 | $4,000 | Resilience versus faster expansion |

The low, planning, and high-pressure columns are scenarios, not difficulty modes and not a promise that all values move in the same direction.

## Relationships

```text
gross_sales = sum(completed_line_quantity × unit_price_cents)
cost_of_goods_sold = sum(completed_line_quantity × wholesale_unit_cost_cents)
gross_profit = gross_sales - cost_of_goods_sold
included_operating_expenses = rent_allocation + delivery_fee + utility_proxy + cleaning_or_maintenance
session_contribution = gross_profit - included_operating_expenses
ending_cash = starting_cash - setup_costs - inventory_purchases + gross_sales - included_operating_expenses
```

All calculations use integer cents. The first proof should display labels such as “session contribution,” not imply audited net income.

## Opening-day target interpretation

- Below $350: pressure scenario; the result should identify whether insufficient transactions, low shelf availability, or excessive setup cost caused the miss.
- $350–$700: intended proof band for comparing cost, pricing, and stock.
- Above $700: inspect the scripted basket and price assumptions for an overly easy scenario or exploit before treating it as success.

Because customers and demand are deferred, revenue is scenario input rather than evidence of market balance.

## Recoverable failure pressure

Proposed recovery envelope:

- A weak first session should consume no more than roughly 5–10% of starting capital after setup.
- The planning case should retain enough cash for at least three equivalent fixed-expense sessions plus one minimum reorder.
- No first-proof action may create negative inventory, hidden debt, or irreversible closure.
- A negative contribution result should recommend one or more legible responses: reduce next delivery size, change product mix, increase price within the scenario, restock higher-margin units, or replay the opening session.
- Save/load must not reset a completed loss or duplicate revenue.

## Capability-based progression proposals

### Expand the initial assortment

Proposed for project-owner review. Allow the next small product batch only when:

- the essential loop has been completed and restored successfully;
- ending cash remains at or above the approved minimum reserve after the proposed order;
- an appropriate fixture location has unused capacity;
- at least two completed sessions have no inventory-integrity defect; and
- the added category creates a visible stocking or pricing choice rather than content volume alone.

### Consider the first worker later

Not implemented in this proof. A later design may consider hiring when:

- the store has repeated positive session contribution;
- reserve covers a proposed 7–14 days of wages and current fixed expenses;
- hands-on task demand competes with pricing, ordering, or growth decisions;
- a worker role has a bounded task contract and measurable value; and
- scheduling, wages, skill, failure, and persistence have passed their own integration review.

### Consider a second location later

Not implemented in this proof. A later design may consider a second location when:

- the first store can run as a coherent simulated business with customers and employees;
- a manager can execute bounded policies;
- detailed-to-aggregate reconciliation is proven;
- the first store maintains an owner-approved reserve after projected second-location setup;
- the second market creates a meaningful location decision; and
- combined location reporting explains performance.

These are capability categories and test hypotheses, not approved unlock thresholds.

## Test scenarios

### Scenario E1 — Viable planning case

- Startup cash: $10,000
- Deposit/setup charge: $3,000
- Fixtures/equipment: $2,250
- Inventory purchase: $1,100
- Fixed session expenses: $205
- Scripted gross sales: $500
- Scripted COGS: $285

Expected:

- gross profit $215;
- session contribution $10;
- ending cash $3,945;
- reserve above $2,750;
- result explains that the store barely covered included session expenses.

### Scenario E2 — Recoverable weak opening

- Same setup as E1
- Scripted gross sales: $300
- Scripted COGS: $180

Expected:

- session contribution negative $85;
- ending cash $3,745, which remains positive and above a three-session fixed-expense floor;
- no automatic closure or restart;
- result identifies low sales relative to fixed expenses.

### Scenario E3 — Inventory-overbuy pressure

- Startup cash: $8,000
- Deposit/setup charge: $3,000
- Fixtures/equipment: $2,250
- Inventory purchase: $1,800
- Fixed session expenses: $205
- Scripted gross sales: $500
- Scripted COGS: $285

Expected:

- contribution remains $10, but ending liquidity is only $1,245;
- reserve warning is shown;
- additional assortment is blocked by the proposed reserve condition;
- unsold units remain inventory rather than disappearing.

### Scenario E4 — Idempotent checkout and reload

Complete one $500 basket set, save, reload, and request completion again.

Expected:

- gross sales remain $500;
- sold units are not consumed twice;
- session totals and ending cash remain unchanged;
- existing transaction summary is returned.

## Exploit and integrity checks

- Repeated checkout completion cannot duplicate revenue or consume stock twice.
- Cancelled or invalid scans cannot create revenue.
- Save/load cannot restore sold units while retaining the sale.
- Moving units among box, loose, held, and shelf locations cannot change total units.
- A rejected fixture or shelf placement cannot create additional capacity.
- Removing a delivery container cannot discard or duplicate contents.
- Negative prices, costs, quantities, or expenses are rejected.

## Unresolved owner choices

- Exact starting cash and whether startup debt exists
- Exact lease deposit and daily rent representation
- Initial fixture budget and whether fixtures can be resold
- Product-specific wholesale and retail values
- Desired opening-session duration and transaction count
- Target default gross margin band
- Minimum reserve formula or fixed threshold
- Whether delivery and cleaning costs are charged on the first guided session
- Whether the first result uses contribution, cash change, or both as its primary headline
- Exact future hiring and second-location gates

No number becomes canonical or enters runtime assets until approved and validated.
