| Field | Description |
|-------|-------------|
| **UC Name** | UC_22 – Update Service Prices |
| **Scenario** | US_22 |
| **State diagram** | `Diagrams/StateDiagrams/Promotion State Diagram-Page-7.drawio.png` |
| **Summary** | Marketer updates promotional service pricing without full admin access |
| **Dependency** | None |
| **Actors** | Marketer |
| **Preconditions** | Marketer is logged in |
| **Main Sequence** | Marketer opens services/promotions pricing; selects service or offer; sets promotional price and validity; system saves temporary promotional pricing |
| **Alternative Sequence** | Invalid price → validation error; unauthorized service → denied |
| **Non-functional requirements** | Limited scope vs admin; audit of price changes |
| **Postconditions** | Promotional prices active for campaign period |
