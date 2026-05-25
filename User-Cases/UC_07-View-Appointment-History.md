| Field | Description |
|-------|-------------|
| **UC Name** | UC_07 – View Appointment History |
| **Scenario** | US_07 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Patient views past and current appointments |
| **Dependency** | None |
| **Actors** | Patient |
| **Preconditions** | Patient is logged in |
| **Main Sequence** | Patient opens My Appointments; system retrieves appointments; system lists visits with status (Booked, InProgress, Completed, Cancelled); patient opens details |
| **Alternative Sequence** | No appointments → empty list; load error → error message |
| **Non-functional requirements** | Performance; clear status labels |
| **Postconditions** | Patient has reviewed appointment history |
