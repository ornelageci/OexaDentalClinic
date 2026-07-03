# Oexa Dental Clinic Management System

> A web-based platform that digitalises clinic operations — connecting **patients**, **dentists**, **managers (reception)**, **admins**, and **marketers** through role-specific portals backed by a single ASP.NET Core API.

<img width="1882" height="1018" alt="image" src="https://github.com/user-attachments/assets/965f0e08-aa17-4ebd-80b0-630ae6af64de" />
<img width="1862" height="1021" alt="image" src="https://github.com/user-attachments/assets/b436314d-aa0d-4123-98c5-de9abb7ab88d" />
<img width="1871" height="871" alt="image" src="https://github.com/user-attachments/assets/3a17e163-b885-4d71-9116-34160e0cf5f2" />
<img width="1870" height="1002" alt="image" src="https://github.com/user-attachments/assets/aadb7164-616d-4730-8d2d-05d131ec1535" />


## Project Overview

**Oexa Dental Clinic** replaces fragmented, paper-based workflows with a centralised system for booking, multi-treatment scheduling, clinical completion, receipt billing (with **20% TVSH/VAT**), promotions, and revenue reporting.

| Pillar | How it shows up in the product |
|--------|--------------------------------|
| **Usability** | Separate public site and staff portals; responsive Bootstrap UI |
| **Efficiency** | Sequential treatment scheduling; manager assign/reschedule in one dashboard |
| **Automation** | Email on book/cancel/status change; 24h appointment reminders (hosted service) |
| **Patient experience** | Self-service booking, history, offers, ratings, notifications |

---

## System Actors (5 roles)

RBAC is enforced in the API and each role has its own dashboard (or public pages for patients).

| Actor | Role key | Portal / pages | Primary responsibilities |
|-------|----------|------------------|---------------------------|
| **Patient** | `Patient` | Public site: register, book, my appointments, offers | Book/cancel appointments, view history & promotions, rate dentists after completed visits |
| **Dentist** | `Dentist` | `dentist/dashboard.html` | View schedule, complete assigned treatment lines, submit receipt medications for their patients |
| **Manager** | `Manager` | `manager/dashboard.html` | Reception: assign/reschedule dentists per treatment line, set receipt prices, finalize receipts (VAT), cancel/reschedule appointments |
| **Admin** | `Admin` | `admin/*.html` | System oversight: users, treatment catalog, appointments list, **Revenue** reports (`YYYY-MM` / `YYYY-00`), dashboard stats |
| **Marketer** | `Marketer` | `marketer/dashboard.html` | Create/edit promotions, validity dates, loyalty tiers, promotional pricing |

Staff sign in via `portal/login.html`; patients use `user/login.html` and `user/register.html`.

---

## Core Features (aligned with implementation)

### Appointment lifecycle
States: **Booked** → **InProgress** → **Completed** (or **Cancelled**).  
See `Diagrams/StateDiagrams/Appointment State Diagram-Page-1.drawio.png`.

- Online booking with preferred date/time and selected dental problems (multi-treatment)
- Special / paediatric flow (`Book Special Appointment`)
- Manager assigns dentists per `AppointmentTreatment` line with category matching
- Manager reschedules individual treatment lines within patient availability
- Email notifications on book, cancel, and status changes

### Treatment lines
See `Diagrams/StateDiagrams/Treatment State Diagram-Page-2.drawio.png`.

- Each appointment can have multiple treatments with duration and assigned dentist
- Dentist marks their line complete (`DentistCompletedAt`); appointment completes when all assigned lines are done

### Receipt & medications (prescription flow)
See `Diagrams/StateDiagrams/Prescription State Diagram-Page-5.drawio.png` and `Receipt State Diagram-Page-6.drawio.png`.

- Receipt status: **Draft** → priced by manager → **Finalized**
- Each dentist submits medications on the receipt (attributed by `SubmittedByDentistUserId`)
- Treatment lines synced to `ReceiptTreatment` with per-dentist unit prices
- **TVSH 20%**: subtotal, VAT amount, and total stored on finalized receipts
- Admin **Revenue** page: finalized receipts only, filter by month (`2026-06`) or full year (`2026-00`)

### Promotions
See `Diagrams/StateDiagrams/Promotion State Diagram-Page-7.drawio.png`.

- Marketer manages campaigns; patients view active offers on `offers.html`
- Loyalty categories (e.g. New, Regular, VIP) used in promotion rules

### Admin dashboard
- Appointment counts by status; current-month revenue from finalized receipts

---

## Repository structure

```
OexaDentalClinic/
├── Backend/OexaDentalClinic.Api/   # ASP.NET Core 8 Web API, EF Core, MySQL
├── Frontend/                       # HTML, CSS, JavaScript (Bootstrap 5)
├── Diagrams/StateDiagrams/         # Appointment, Treatment, Prescription, Receipt, Promotion
├── User-Cases/                     # UC_01 … UC_25 (25 use cases)
├── User-Scenarios/                 # Scenario list, extended flows, relationships
├── User-Stories/                   # Agile user stories by actor
├── Case-Study/                     # Narrative case study
└── OexaDentalClinic.sln
```

---

## Technologies

| Layer | Stack |
|-------|--------|
| **Frontend** | HTML5, CSS3, vanilla JavaScript, Bootstrap 5 |
| **Backend** | ASP.NET Core 8, Entity Framework Core |
| **Database** | MySQL (Pomelo/Oracle MySQL provider) |
| **Email** | SMTP (`EmailService`, configurable in `appsettings`) |
| **Deployment** | Docker, Railway (`railway.toml`) |

---

## Getting started

### Backend

```bash
cd Backend/OexaDentalClinic.Api
# Configure connection string in appsettings.Development.json (see .example file)
dotnet run
```

API: `http://localhost:5xxx` (see `launchSettings.json`). Swagger enabled in Development.

### Frontend

Serve `Frontend/` with any static server or open HTML files. Set API base URL in `Frontend/js/shared/api.js`.

### Default seeded users (Development)

| Email | Role | Password (demo) |
|-------|------|-----------------|
| `admin@oexa.com` | Admin | `admin123` |
| `manager@oexa.com` | Manager | `manager123` |
| `marketer@oexa.com` | Marketer | `marketer123` |
| `patient@oexa.com` | Patient | `patient123` |
| `alkeo@oexa.com` | Dentist (general) | `dentist123` |

---

## Documentation map

| Document | Purpose |
|----------|---------|
| [User-Cases/](User-Cases/) | 25 formal use cases (UC_01–UC_25) |
| [User-Scenarios/User_Scenarios_List.md](User-Scenarios/User_Scenarios_List.md) | Scenario index |
| [User-Scenarios/User_Scenarios_Extended.md](User-Scenarios/User_Scenarios_Extended.md) | Step-by-step flows |
| [User-Scenarios/Use Case Relationships.md](User-Scenarios/Use%20Case%20Relationships.md) | extends / includes |
| [User-Stories/user-stories.md](User-Stories/user-stories.md) | User stories by actor |
| [Case-Study/dental-case-study.md](Case-Study/dental-case-study.md) | Case study narrative |
| [Diagrams/StateDiagrams/md](Diagrams/StateDiagrams/md) | State diagram index |

---

## Security notes

- Role checked per endpoint; staff vs patient login separated
- Passwords stored for demo seeding — use proper hashing and secrets in production
- HTTPS recommended in production; CORS configured for frontend origins
