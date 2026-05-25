| Field | Description |
|-------|-------------|
| **UC Name** | UC_02 – User Login |
| **Scenario** | US_02 |
| **Summary** | User logs into the system with email and password |
| **Dependency** | None |
| **Actors** | Patient, Dentist, Manager, Admin, Marketer |
| **Preconditions** | User has a registered account |
| **Main Sequence** | User opens login (public site or staff portal); enters email and password; submits; system validates credentials; system returns role; user is redirected to the correct dashboard |
| **Alternative Sequence** | Invalid credentials → error; wrong portal for role → access denied or redirect |
| **Non-functional requirements** | Fast response; secure authentication |
| **Postconditions** | User is authenticated with role-based access |
