| Field | Description |
|-------|-------------|
| **UC Name** | UC_25 – View Financial Summary |
| **Scenario** | US_25 |
| **State diagram** | `Diagrams/StateDiagrams/Receipt State Diagram-Page-6.drawio.png` |
| **Summary** | Manager views financial summaries: receipts, revenue, VAT, and per-dentist breakdown |
| **Dependency** | Includes: UC_18 – Finalize Receipt |
| **Actors** | Manager |
| **Preconditions** | Manager is logged in; finalized receipts exist for selected period |
| **Main Sequence** | Manager opens financial/revenue view; selects period (month or year); system loads finalized receipts for Completed appointments; system displays totals, TVSH, and breakdown by dentist; manager reviews summary |
| **Alternative Sequence** | No data for period → zero totals; invalid period → error |
| **Non-functional requirements** | Accurate aggregation; performance on large datasets |
| **Postconditions** | Manager has financial overview for the period |
