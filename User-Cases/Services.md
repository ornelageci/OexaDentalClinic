| Field | Description |
|---|---|
| UC Name | UC_6 – Manage Services |
| Summary | Admin manages clinic services |
| Dependency | None |
| Actors | Admin |
| Preconditions | Admin logged in |
| Main Sequence | Open services panel; add/update/delete service; save changes |
| Alternative Sequence | Invalid action → error |
| Non-functional requirements | Security, usability |
| Postconditions | Service list updated |



| Field | Description |
|---|---|
| UC Name | UC_7 – Add Service |
| Summary | Admin adds a new service |
| Dependency | Includes: Manage Services |
| Actors | Admin |
| Preconditions | Admin logged in |
| Main Sequence | Open add service form; enter details; save service |
| Alternative Sequence | Invalid data → error |
| Non-functional requirements | Security, usability |
| Postconditions | New service added |

| Field | Description |
|---|---|
| UC Name | UC_8 – Update Service |
| Summary | Admin edits an existing service |
| Dependency | Includes: Manage Services |
| Actors | Admin |
| Preconditions | Admin logged in |
| Main Sequence | Select service; edit information; save changes |
| Alternative Sequence | Service not found → error |
| Non-functional requirements | Security, usability |
| Postconditions | Service updated |

| Field | Description |
|---|---|
| UC Name | UC_9 – Delete Service |
| Summary | Admin removes a clinic service |
| Dependency | Includes: Manage Services |
| Actors | Admin |
| Preconditions | Admin logged in |
| Main Sequence | Select service; confirm delete; remove service |
| Alternative Sequence | Service already deleted → error |
| Non-functional requirements | Security, reliability |
| Postconditions | Service deleted |
