| Field | Description |
|-------|-------------|
| **UC Name** | UC_08 – Cancel Appointment |
| **Scenario** | US_08 |
| **State diagram** | `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png` |
| **Summary** | Patient cancels an existing appointment |
| **Dependency** | None |
| **Actors** | Patient |
| **Preconditions** | Patient is logged in; appointment exists and is cancellable (e.g. Booked) |
| **Main Sequence** | Patient selects appointment; confirms cancel; system sets status to Cancelled; system sends notification email |
| **Alternative Sequence** | Appointment already Completed/Cancelled → not allowed; system error → failure message |
| **Non-functional requirements** | Reliability; notification delivery |
| **Postconditions** | Appointment is Cancelled; no longer active for scheduling |
