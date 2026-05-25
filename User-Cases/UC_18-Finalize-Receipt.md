| Field | Description |
|-------|-------------|
| **UC Name** | UC_18 – Finalize Receipt |
| **Scenario** | US_18 |
| **State diagram** | `Diagrams/StateDiagrams/Receipt State Diagram-Page-6.drawio.png` |
| **Summary** | Manager finalizes receipt from treatments and prescribed medications including TVSH (20% VAT) |
| **Dependency** | Includes: UC_13 – Update Treatment; UC_17 – Prescribe Medication |
| **Actors** | Manager |
| **Preconditions** | Appointment Completed; receipt in Draft with priced lines |
| **Main Sequence** | Manager opens receipt; reviews treatments and medications by dentist; confirms unit prices; system calculates subtotal, 20% VAT, and total; manager finalizes; status becomes Finalized |
| **Alternative Sequence** | Missing prices → cannot finalize; sync errors on old data → manual fix |
| **Non-functional requirements** | Correct VAT calculation; financial accuracy |
| **Postconditions** | Receipt Finalized; totals stored for reporting |
