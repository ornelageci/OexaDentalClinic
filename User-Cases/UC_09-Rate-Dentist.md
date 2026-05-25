| Field | Description |
|-------|-------------|
| **UC Name** | UC_09 – Rate Dentist |
| **Scenario** | US_09 |
| **Summary** | Patient rates the dentist after a completed appointment |
| **Dependency** | None |
| **Actors** | Patient |
| **Preconditions** | Appointment is Completed; patient has not already rated this visit |
| **Main Sequence** | Patient opens completed appointment; selects star rating (1–5); optionally enters review text; submits; system stores review linked to appointment and dentist |
| **Alternative Sequence** | Appointment not completed → rating disabled; duplicate rating → rejected |
| **Non-functional requirements** | One review per appointment; max review length |
| **Postconditions** | Review stored; aggregate dentist rating updated |
