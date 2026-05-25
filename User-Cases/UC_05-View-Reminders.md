| Field | Description |
|-------|-------------|
| **UC Name** | UC_05 – View Reminders |
| **Scenario** | US_05 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Patient views appointment reminder notifications from the system |
| **Dependency** | Extends: UC_03 – Book Appointment |
| **Actors** | Patient, System |
| **Preconditions** | Patient has upcoming Booked appointments; reminder emails may have been sent |
| **Main Sequence** | Patient logs in; opens notifications or my appointments; system shows upcoming visits and reminder status; patient reads reminder details |
| **Alternative Sequence** | No upcoming appointments → empty state; notification load error → retry message |
| **Non-functional requirements** | Timely reminders (e.g. 24h before); availability |
| **Postconditions** | Patient is informed of upcoming visit |
