| Field | Description |
|-------|-------------|
| **UC Name** | UC_14 – Manage Users |
| **Scenario** | US_14 |
| **Summary** | Manager manages users and related system accounts |
| **Dependency** | None |
| **Actors** | Manager |
| **Preconditions** | Manager is logged in |
| **Main Sequence** | Manager opens user management; views list of users by role; adds, edits, or deactivates users; system validates and persists changes |
| **Alternative Sequence** | Duplicate email → error; invalid role → rejected; delete user in use → warning |
| **Non-functional requirements** | Security; RBAC |
| **Postconditions** | User records updated in database |
