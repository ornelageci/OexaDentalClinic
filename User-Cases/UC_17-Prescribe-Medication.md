| Field | Description |
|-------|-------------|
| **UC Name** | UC_17 – Prescribe Medication |
| **Scenario** | US_17 |
| **State diagram** | `Diagrams/StateDiagrams/Prescription State Diagram-Page-5.drawio.png` |
| **Summary** | Dentist adds medications to the appointment receipt after treatment |
| **Dependency** | None |
| **Actors** | Dentist |
| **Preconditions** | Dentist is assigned to visit; receipt exists or is created in Draft |
| **Main Sequence** | Dentist opens receipt for appointment; adds medication name, quantity, notes; submits; system saves ReceiptMedication linked to submitting dentist |
| **Alternative Sequence** | Not assigned dentist → denied; invalid data → validation error |
| **Non-functional requirements** | Per-dentist attribution on multi-dentist visits |
| **Postconditions** | Medications on draft receipt ready for manager pricing |
