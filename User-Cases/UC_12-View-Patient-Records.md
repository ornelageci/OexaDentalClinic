| Field | Description |
|-------|-------------|
| **UC Name** | UC_12 – View Patient Records |
| **Scenario** | US_12 |
| **Summary** | Dentist views patient information and appointment history for assigned visits |
| **Dependency** | None |
| **Actors** | Dentist |
| **Preconditions** | Dentist is logged in; patient has appointment with this dentist |
| **Main Sequence** | Dentist selects appointment or patient; system loads patient profile and visit details; system displays problems, notes, and history relevant to the visit |
| **Alternative Sequence** | Patient not found → error; no access to unrelated patients |
| **Non-functional requirements** | Privacy; role-based access |
| **Postconditions** | Dentist has context for treatment |
