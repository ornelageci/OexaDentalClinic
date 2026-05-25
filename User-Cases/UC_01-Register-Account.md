| Field | Description |
|-------|-------------|
| **UC Name** | UC_01 – Register Account |
| **Scenario** | US_01 |
| **Summary** | Patient creates a new account on the public site |
| **Dependency** | None |
| **Actors** | Patient |
| **Preconditions** | User is not logged in; email is not already registered |
| **Main Sequence** | User opens registration page; enters name, email, password, phone; submits form; system validates input; system creates Patient account; user is redirected to login or home |
| **Alternative Sequence** | Email already exists → error message; invalid data → validation errors |
| **Non-functional requirements** | Data validation; usability; secure password handling |
| **Postconditions** | Patient account exists and can log in |
