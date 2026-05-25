| Field | Description |
|-------|-------------|
| **UC Name** | UC_04 – Book Special Appointment |
| **Scenario** | US_04 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Patient books a special appointment for children or patients with specific needs |
| **Dependency** | Extends: UC_03 – Book Appointment |
| **Actors** | Patient |
| **Preconditions** | Patient is logged in |
| **Main Sequence** | Patient selects special/paediatric booking option; chooses appropriate problems and time; confirms; system creates Booked appointment flagged for special care; confirmation email sent |
| **Alternative Sequence** | No suitable slot → error; same as UC_03 validation failures |
| **Non-functional requirements** | Usability; accessibility of booking flow |
| **Postconditions** | Special appointment created; manager may assign paediatric dentist |
