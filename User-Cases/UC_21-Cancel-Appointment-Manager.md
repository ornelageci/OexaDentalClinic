| Field | Description |
|-------|-------------|
| **UC Name** | UC_21 – Cancel Appointment (Manager) |
| **Scenario** | US_21 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Manager cancels appointment for clinic-related reasons |
| **Dependency** | Extends: UC_08 – Cancel Appointment |
| **Actors** | Manager |
| **Preconditions** | Manager is logged in; appointment is active |
| **Main Sequence** | Manager selects appointment; confirms cancellation; system sets status Cancelled; system notifies patient (and dentist) |
| **Alternative Sequence** | Already Completed → not allowed; system error → failure |
| **Non-functional requirements** | Notification delivery |
| **Postconditions** | Appointment Cancelled by clinic |
