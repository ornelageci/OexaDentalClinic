| Field | Description |
|-------|-------------|
| **UC Name** | UC_13 – Update Treatment |
| **Scenario** | US_13 |
| **State diagram** | `Diagrams/StateDiagrams/Treatment State Diagram-Page-2.drawio.png` |
| **Summary** | Dentist updates or records treatment information after examination |
| **Dependency** | None |
| **Actors** | Dentist |
| **Preconditions** | Dentist is assigned to the appointment treatment line |
| **Main Sequence** | Dentist opens visit details; updates treatment notes, diagnosis, or recommendations; system saves treatment record linked to appointment |
| **Alternative Sequence** | Save fails → error; unauthorized dentist → denied |
| **Non-functional requirements** | Data integrity; audit trail |
| **Postconditions** | Treatment information stored on patient visit |
