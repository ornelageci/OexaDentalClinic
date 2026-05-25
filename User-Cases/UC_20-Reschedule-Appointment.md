| Field | Description |
|-------|-------------|
| **UC Name** | UC_20 – Reschedule Appointment |
| **Scenario** | US_20 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Manager reschedules appointment or individual treatment line when schedule changes |
| **Dependency** | Extends: UC_03 – Book Appointment |
| **Actors** | Manager |
| **Preconditions** | Appointment exists; manager has access |
| **Main Sequence** | Manager selects appointment or treatment line; picks new dentist and/or time slot; system checks availability; system updates schedule; notification email sent |
| **Alternative Sequence** | Slot conflict → error; patient window violated → rejected |
| **Non-functional requirements** | Scheduling reliability |
| **Postconditions** | Appointment or line rescheduled; stakeholders notified |
