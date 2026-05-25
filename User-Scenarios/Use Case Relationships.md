# Use Case Relationships

| Use Case | Relationship | Target Use Case |
|---|---|---|
| UC_04 – Book Special Appointment | extends | UC_03 – Book Appointment |
| UC_05 – View Reminders | extends | UC_03 – Book Appointment |
| UC_21 – Cancel Appointment (Manager) | extends | UC_08 – Cancel Appointment |
| UC_20 – Reschedule Appointment | extends | UC_03 – Book Appointment |
| UC_18 – Finalize Receipt | includes | UC_17 – Prescribe Medication |
| UC_18 – Finalize Receipt | includes | UC_13 – Update Treatment |
| UC_19 – View Receipt | includes | UC_18 – Finalize Receipt |
| UC_24 – Apply Promotions | includes | UC_22 – Update Service Prices |
| UC_24 – Apply Promotions | includes | UC_23 – Edit Customer Categories |
| UC_15 – Assign Patient to Dentist | includes | UC_03 – Book Appointment |
