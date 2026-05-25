| Field | Description |
|-------|-------------|
| **UC Name** | UC_10 – View Schedule |
| **Scenario** | US_10 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Dentist views scheduled appointments and assigned treatment lines |
| **Dependency** | None |
| **Actors** | Dentist |
| **Preconditions** | Dentist is logged in |
| **Main Sequence** | Dentist opens dashboard; system loads appointments where dentist is assigned; system displays date, patient, problems, and status; dentist filters by day/status if needed |
| **Alternative Sequence** | No assignments → empty schedule |
| **Non-functional requirements** | Performance; clear schedule view |
| **Postconditions** | Dentist knows upcoming and active patients |
