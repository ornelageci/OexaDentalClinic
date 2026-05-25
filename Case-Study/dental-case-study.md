# Case Study – Oexa Dental Clinic Management System

## Overview

Oexa Dental Clinic is a **web application** for managing day-to-day clinic operations: patient self-service booking, multi-dentist treatment scheduling, clinical completion, itemised receipts with **20% TVSH (VAT)**, promotional campaigns, and admin revenue reporting. The solution uses an **ASP.NET Core** REST API, **MySQL** database, and a **multi-page HTML/JavaScript** frontend with role-based portals.

## Actors

| Actor | Description |
|-------|-------------|
| **Patient** | Registers, books appointments (including paediatric/special), views history and offers, cancels, receives emails, rates dentists |
| **Dentist** | Views schedule, completes assigned treatment lines, submits medications on receipts |
| **Manager (receptionist)** | Assigns/reschedules dentists per treatment, sets receipt prices, finalizes receipts, manages day-to-day appointments |
| **Admin** | Manages users and treatment catalog, views all appointments, runs **Revenue** reports by month or year |
| **Marketer** | Creates and maintains promotions and customer loyalty categories |

## State-driven domains

Behaviour of key entities is documented in `Diagrams/StateDiagrams/`:

| Domain | Diagram | Typical states |
|--------|---------|----------------|
| Appointment | `Appointment State Diagram-Page-1.drawio.png` | Booked → InProgress → Completed / Cancelled |
| Treatment line | `Treatment State Diagram-Page-2.drawio.png` | Unassigned → Assigned → Completed (per dentist) |
| Medications | `Prescription State Diagram-Page-5.drawio.png` | Submitted by dentist on draft receipt |
| Receipt | `Receipt State Diagram-Page-6.drawio.png` | Draft → priced → Finalized (with VAT breakdown) |
| Promotion | `Promotion State Diagram-Page-7.drawio.png` | Draft / active / expired campaigns |

---

## Scenario 1 – Patient registers and books an appointment

**Actors:** Patient  
**Related UC:** UC_01, UC_02, UC_03  
**Diagram:** Appointment  

**Assumption:** Patient has no account yet (or is logged in).

**Normal flow**
1. Patient registers on the public site with personal details.
2. Patient logs in and opens **Book Appointment**.
3. Patient selects dental problem(s), preferred date/time, and optional notes.
4. System creates appointment in **Booked** status with treatment lines (dentists not yet assigned).
5. Confirmation email is sent to the patient (and staff when configured).

**What can go wrong**
- Invalid or duplicate email on registration.
- Preferred slot conflicts with clinic rules.
- Email delivery fails (appointment still saved).

**Outcome:** Appointment visible in patient **My Appointments** and in manager queue for dentist assignment.

---

## Scenario 2 – Manager assigns dentists (reception workflow)

**Actors:** Manager  
**Related UC:** UC_15, UC_20  
**Diagram:** Treatment, Appointment  

**Assumption:** Appointment is **Booked** with unassigned treatment lines.

**Normal flow**
1. Manager opens the manager dashboard and selects the appointment.
2. For each treatment line, manager picks a dentist matching the problem category (general, orthodontics, paediatric, etc.).
3. System checks dentist availability and sequential scheduling within the patient window.
4. Assignments are saved; patient may receive updated timing by email.

**What can go wrong**
- No dentist available in the selected slot → manager reschedules line or whole visit.
- Category mismatch (e.g. paediatric problem assigned to non-paediatric dentist).

**Outcome:** All lines assigned; visit can proceed to **InProgress** when patient arrives.

---

## Scenario 3 – Dentist completes treatment and submits medications

**Actors:** Dentist  
**Related UC:** UC_10, UC_11, UC_13  
**Diagram:** Treatment, Prescription  

**Assumption:** Dentist is assigned to at least one line on an active appointment.

**Normal flow**
1. Dentist views today’s schedule on the dentist dashboard.
2. Dentist marks their treatment line(s) as completed.
3. When all assigned dentists finish, appointment moves to **Completed**.
4. Dentist opens **Receipt** for the visit and adds medications (name, quantity, notes) for their part only.

**What can go wrong**
- Dentist tries to complete another dentist’s line → denied.
- Medications submitted before visit is in progress → validation error.

**Outcome:** Clinical work recorded; draft receipt contains treatment lines and per-dentist medications.

---

## Scenario 4 – Manager finalizes receipt with TVSH

**Actors:** Manager  
**Related UC:** UC_18, UC_19  
**Diagram:** Receipt  

**Assumption:** Appointment **Completed**; receipt in **Draft**.

**Normal flow**
1. Manager opens receipt: treatments and medications grouped by dentist.
2. Manager enters or confirms unit prices for each line.
3. System calculates **subtotal**, **20% VAT (TVSH)**, and **total**.
4. Manager finalizes receipt → status **Finalized**.

**What can go wrong**
- Missing prices on lines → cannot finalize.
- Old appointments without treatment lines → sync service backfills from `ServiceNeeded` where possible.

**Outcome:** Patient and admin can view finalized totals; receipt included in revenue reports.

---

## Scenario 5 – Admin views revenue for a period

**Actors:** Admin  
**Related UC:** UC_25  
**Diagram:** Receipt (financial view)  

**Assumption:** At least one finalized receipt exists for the selected period.

**Normal flow**
1. Admin opens **Revenue** (`admin/reports.html`).
2. Admin enters period as `YYYY-MM` (e.g. `2026-06`) or `YYYY-00` for full year.
3. System lists finalized receipts with breakdown per dentist (treatments + medications, VAT).
4. Summary shows total revenue, subtotal, and VAT for the period.

**What can go wrong**
- No finalized receipts in period → empty list with zero totals.
- Invalid period format → validation message.

**Outcome:** Financial overview for management decisions.

---

## Scenario 6 – Marketer publishes a promotion

**Actors:** Marketer, Patient  
**Related UC:** UC_16, UC_06, UC_24  
**Diagram:** Promotion  

**Assumption:** Marketer is logged into the marketer dashboard.

**Normal flow**
1. Marketer creates a promotion (title, discount, validity dates, target categories).
2. System stores and activates the campaign.
3. Patient opens **Offers** on the public site and sees active promotions.

**What can go wrong**
- End date before start date → rejected.
- Expired promotion hidden from patients automatically.

**Outcome:** Promotion visible to patients within validity window.

---

## Scenario 7 – Patient cancels and receives notification

**Actors:** Patient  
**Related UC:** UC_08  
**Diagram:** Appointment  

**Normal flow**
1. Patient opens **My Appointments** and selects a future **Booked** visit.
2. Patient confirms cancellation.
3. Status becomes **Cancelled**; email sent to patient (and dentist/manager as configured).

**Outcome:** Slot freed; appointment no longer appears as upcoming.

---

## Scenario 8 – Staff authentication (all roles)

**Actors:** Patient, Dentist, Manager, Admin, Marketer  
**Related UC:** UC_02  

**Normal flow**
1. User opens patient login or **staff portal** login.
2. User enters email and password.
3. API validates credentials and returns role.
4. Frontend redirects to the correct dashboard (or public home for patient).

**Outcome:** Session established; UI shows only actions allowed for that role.

---

## Conclusion

The system:

- **Organises** multi-dentist visits and reception assignment in one place  
- **Reduces errors** in scheduling and billing through structured states and VAT rules  
- **Improves patient experience** with booking, reminders, offers, and transparent receipts  
- **Supports decisions** via admin dashboard and revenue reporting  

Formal requirements are captured in **25 use cases** (`User-Cases/`, UC_01–UC_25) and **state diagrams** under `Diagrams/StateDiagrams/`.
