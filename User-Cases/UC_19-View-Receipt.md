| Field | Description |
|-------|-------------|
| **UC Name** | UC_19 – View Receipt |
| **Scenario** | US_19 |
| **State diagram** | `Diagrams/StateDiagrams/Receipt State Diagram-Page-6.drawio.png` |
| **Summary** | Manager views generated receipt and final appointment cost |
| **Dependency** | Includes: UC_18 – Finalize Receipt (for finalized view) |
| **Actors** | Manager |
| **Preconditions** | Manager is logged in; receipt exists for appointment |
| **Main Sequence** | Manager selects appointment; system loads receipt with treatments, medications, subtotal, VAT, and total; manager reviews line items by dentist |
| **Alternative Sequence** | No receipt yet → draft or empty state; appointment not found → error |
| **Non-functional requirements** | Clear financial breakdown |
| **Postconditions** | Manager has reviewed billing details |
