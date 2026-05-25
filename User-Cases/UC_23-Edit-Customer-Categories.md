| Field | Description |
|-------|-------------|
| **UC Name** | UC_23 – Edit Customer Categories |
| **Scenario** | US_23 |
| **State diagram** | `Diagrams/StateDiagrams/Promotion State Diagram-Page-7.drawio.png` |
| **Summary** | Marketer creates and edits customer loyalty categories (New, Regular, VIP) |
| **Dependency** | None |
| **Actors** | Marketer |
| **Preconditions** | Marketer is logged in |
| **Main Sequence** | Marketer opens customer categories; adds or edits category name and rules; saves; system persists category definitions |
| **Alternative Sequence** | Duplicate category → error; category in use → cannot delete |
| **Non-functional requirements** | Usability; data consistency |
| **Postconditions** | Customer categories available for promotions |
