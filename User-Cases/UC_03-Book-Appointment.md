| Field | Description |
|-------|-------------|
| **UC Name** | UC_03 – Book Appointment |
| **Scenario** | US_03 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Patient books an appointment by selecting services/problems, date, and time |
| **Dependency** | Includes: check availability |
| **Actors** | Patient |
| **Preconditions** | Patient is logged in |
| **Main Sequence** | Patient opens book appointment; selects dental problem(s) and preferred date/time; confirms booking; system creates appointment (Booked) and treatment lines; system sends confirmation email |
| **Alternative Sequence** | Slot unavailable → error; invalid input → validation error |
| **Non-functional requirements** | Performance; reliability; email delivery |
| **Postconditions** | Appointment created; visible in patient history and manager/dentist queues |
