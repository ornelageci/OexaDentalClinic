| Field | Description |
|-------|-------------|
| **UC Name** | UC_11 – Mark Appointment Completed |
| **Scenario** | US_11 |
| **State diagram** | `Diagrams/StateDiagrams/Treatment State Diagram-Page-2.drawio.png` |
| **Summary** | Dentist marks treatment work as completed for their assigned lines; visit completes when all dentists finish |
| **Dependency** | None |
| **Actors** | Dentist |
| **Preconditions** | Dentist is assigned to treatment line(s); appointment is Booked or InProgress |
| **Main Sequence** | Dentist selects appointment; marks their treatment line(s) complete; system sets DentistCompletedAt; if all assigned lines done, appointment becomes Completed and email sent |
| **Alternative Sequence** | Not assigned to line → denied; appointment Cancelled → not allowed |
| **Non-functional requirements** | Accurate status tracking |
| **Postconditions** | Dentist’s lines completed; appointment Completed when all dentists done |
