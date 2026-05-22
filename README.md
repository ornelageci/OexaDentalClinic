# Oexa Dental Clinic Management System

> A modern, web-based platform that digitalises and optimises the full operational workflow of a dental clinic — connecting patients, dentists, managers, and marketers through a single, role-driven environment.

![Version](https://img.shields.io/badge/version-1.0.0-0D7377?style=flat-square)
![Status](https://img.shields.io/badge/status-In%20Development-14A085?style=flat-square)
![License](https://img.shields.io/badge/license-Academic-0B1F3A?style=flat-square)
![Roles](https://img.shields.io/badge/user%20roles-4-C9922A?style=flat-square)
![Scenarios](https://img.shields.io/badge/user%20scenarios-25-0D7377?style=flat-square)

---

## Table of Contents

- [Project Overview](#-project-overview)
- [Project Objectives](#-project-objectives)
- [System Actors](#-system-actors)
- [Core Features](#-core-features)
- [System Architecture](#-system-architecture)
- [Technologies Used](#-technologies-used)
- [Security & Reliability](#-security--reliability)
- [Future Enhancements](#-future-enhancements)

---

## Project Overview

**Oexa Dental Clinic Management System** is a comprehensive web-based application developed to replace fragmented, paper-based clinic workflows with an intelligent, centralised digital platform.

The system provides real-time interaction capabilities for every stakeholder in the clinic ecosystem — from patients booking appointments online, to dentists managing their schedules, to managers overseeing operations and marketers running promotional campaigns.

By eliminating manual scheduling and physical records, the system:

- Significantly reduces administrative overhead
- Minimises human error in scheduling and billing
- Delivers a measurably better experience for patients and clinical staff
- Centralises all clinic data in a secure, auditable platform

> **Core design philosophy:** *Usability, efficiency, automation, and patient experience* are the four pillars that guide every design and implementation decision in this system.

---

## Project Objectives

| # | Objective | Description |
|---|-----------|-------------|
| G-01 | **Digitalise Operations** | Replace all paper-based processes with a fully digital, auditable workflow |
| G-02 | **Automate Scheduling** | Reduce manual appointment management effort through intelligent automation |
| G-03 | **Improve Communication** | Ensure all stakeholders receive timely, accurate information via automated notifications |
| G-04 | **Increase Efficiency** | Centralise clinic operations, schedules, billing, and records in one platform |
| G-05 | **Enhance Patient Experience** | Provide patients with self-service tools for appointments, history, and reminders |
| G-06 | **Support Marketing** | Enable targeted promotional campaigns, discounts, and patient loyalty management |

---

## System Actors

The system defines **four distinct user roles**, each with a dedicated permission set and a tailored dashboard experience. Role-Based Access Control (RBAC) is enforced at both the API and UI level.

| Actor | Description | Key Capabilities |
|-------|-------------|-----------------|
|  **Patient** | Primary end-user of the clinic booking system | Book / cancel / view appointments, view treatment history, rate dentists, view promotions |
|  **Dentist** | Clinical professional managing their schedule and patient records | View schedule, update treatment notes, mark appointments completed, issue prescriptions |
|  **Manager (Admin)** | Administrative authority with system-wide oversight | Manage all users, reschedule/cancel appointments, finalise receipts, view financial summaries |
|  **Marketer** | Marketing specialist managing promotions and pricing | Create promotional campaigns, update service prices, manage customer loyalty categories |

---

##  Core Features

###  Appointment Management
- Online appointment booking with **real-time availability checking**
- Appointment cancellation and rescheduling
- **Slot-lock concurrency control** — prevents double-booking race conditions
- Automated 24-hour email and SMS reminders
- Dedicated **paediatric / special appointment** booking flow

###  Dentist Management
- Daily and weekly schedule views
- Real-time appointment status tracking
- Full patient record access (assigned patients only)
- Treatment notes and appointment completion
- Prescription issuance linked to appointments

###  Patient Dashboard
- Personal profile management
- Full appointment history (past, upcoming, cancelled)
- Treatment history and prescription visibility
- Notification inbox with read/unread tracking
- Dental record access

###  Ratings & Feedback System
- Post-visit dentist rating (1–5 stars)
- Written review submission (max 500 characters)
- Aggregate rating displayed on dentist profile
- One rating per appointment rule enforced

###  Email Notification Service

The system automatically sends email notifications for:

| Trigger | Recipients |
|---------|------------|
| Appointment booking confirmation | Patient + Dentist |
| Appointment cancellation | Patient + Dentist |
| Appointment completion summary | Patient |
| Appointment rescheduling | Patient + Dentist |
| 24-hour reminder | Patient |
| Password reset | User |

###  Marketing Module
- Promotional campaign management (create, edit, publish, deactivate)
- Service price promotions with validity date ranges
- Customer loyalty tiers — New Client, Regular, VIP
- Discount application at booking checkout

###  Financial Management
- Itemised receipt generation after appointment completion
- Manager receipt review and finalisation
- Payment status tracking (pending / paid)
- Financial summary dashboard with revenue reporting
- Exportable reports (PDF / CSV)

---

## System Architecture

The project follows a **three-tier client-server architecture** with clear separation of concerns between the presentation, business logic, and data persistence layers.

```
┌─────────────────────────────────────────┐
│           PRESENTATION LAYER            │
│      Frontend (HTML / CSS / JS)         │
│  User interface · Responsive design     │
│  Form validation · Dynamic rendering   │
└──────────────────┬──────────────────────┘
                   │  HTTPS / REST API
┌──────────────────▼──────────────────────┐
│           APPLICATION LAYER             │
│         Backend (Node.js / Java)        │
│  Business logic · JWT authentication   │
│  RBAC · Notifications · Scheduling     │
└──────────────────┬──────────────────────┘
                   │  SQL / ORM
┌──────────────────▼──────────────────────┐
│              DATA LAYER                 │
│       Database (PostgreSQL)             │
│  Patients · Appointments · Treatments  │
│  Receipts · Audit logs · Promotions    │
└─────────────────────────────────────────┘
```

All inter-layer communication occurs over **HTTPS** using a **RESTful API** contract. The backend is stateless by design, enabling horizontal scaling.

---

## Technologies Used

| Layer | Technologies |
|-------|-------------|
| **Frontend** | HTML5, CSS3, JavaScript |
| **Backend** | Node.js / Express.js (PHP / Java as alternative) |
| **Database** | PostgreSQL · MySQL · Redis (cache) |
| **Authentication** | JWT · OAuth 2.0 · bcrypt |
| **Notifications** | SMTP / SendGrid · Twilio SMS |
| **Version Control** | Git · GitHub |
| **Deployment** | Docker · AWS / Azure |
| **Security** | HTTPS / TLS 1.3 · AES-256 at rest |
| **Testing** | Jest / JUnit · Selenium (E2E) |

---

## Security & Reliability

Given the sensitive nature of patient health data, security is a **first-class concern** across all system layers.

| Measure | Implementation |
|---------|---------------|
| **Secure Authentication** | JWT tokens (8h expiry), bcrypt hashing, account lockout after 5 failed attempts |
| **Role-Based Access Control** | RBAC middleware enforced on every API endpoint |
| **Data Encryption** | AES-256 at rest · TLS 1.3 in transit · HTTPS globally enforced |
| **GDPR Compliance** | Patient consent recorded at registration · Right of access and erasure supported |
| **Audit Logging** | Every data-modifying action recorded with user ID, timestamp, table, and record ID |
| **High Availability** | 99.9% uptime SLA · Daily automated encrypted backups · Fault-tolerant recovery |
| **Input Validation** | All user inputs sanitised and validated — prevents SQL injection and XSS |

---


