| Field | Description |
|-------|-------------|
| **UC Name** | UC_15 – Assign Patient to Dentist |
| **Scenario** | US_15 |
| **State diagram** | `Diagrams/StateDiagrams/Treatment State Diagram-Page-2.drawio.png` |
| **Summary** | Manager assigns dentists to appointment treatment lines (reception workflow) |
| **Dependency** | Includes: UC_03 – Book Appointment |
| **Actors** | Manager |
| **Preconditions** | Appointment is Booked with unassigned or reassignable lines |
| **Main Sequence** | Manager selects appointment; for each treatment line picks dentist matching category; system checks availability; system saves assignments; patient/dentist notified if schedule changes |
| **Alternative Sequence** | Dentist unavailable → choose another slot or reschedule; category mismatch → error |
| **Non-functional requirements** | Scheduling accuracy; no double booking |
| **Postconditions** | Treatment lines have assigned dentists |
