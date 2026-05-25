| Field | Description |
|-------|-------------|
| **UC Name** | UC_16 – Manage Promotions |
| **Scenario** | US_16 |
| **State diagram** | `Diagrams/StateDiagrams/Promotion State Diagram-Page-7.drawio.png` |
| **Summary** | Marketer creates and manages promotional content and campaigns |
| **Dependency** | None |
| **Actors** | Marketer |
| **Preconditions** | Marketer is logged in |
| **Main Sequence** | Marketer opens promotions panel; creates or edits promotion (title, discount, dates, description); saves; system stores and publishes active campaigns |
| **Alternative Sequence** | Invalid dates → error; save failure → retry |
| **Non-functional requirements** | Usability; campaign validity rules |
| **Postconditions** | Promotion saved; visible on offers when active |
