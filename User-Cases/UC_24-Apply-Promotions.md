| Field | Description |
|-------|-------------|
| **UC Name** | UC_24 – Apply Promotions |
| **Scenario** | US_24 |
| **State diagram** | `Diagrams/StateDiagrams/Promotion State Diagram-Page-7.drawio.png` |
| **Summary** | Marketer applies promotions to selected services or customer categories |
| **Dependency** | Includes: UC_22 – Update Service Prices; UC_23 – Edit Customer Categories |
| **Actors** | Marketer |
| **Preconditions** | Promotion and targets exist |
| **Main Sequence** | Marketer selects promotion; links services and/or customer categories; confirms apply; system updates promotion rules and affected offers |
| **Alternative Sequence** | No targets selected → validation error; expired promotion → rejected |
| **Non-functional requirements** | Rule consistency |
| **Postconditions** | Promotion rules active for eligible patients/services |
