# Eloomen

> **README.md**

**Eloomen** is a secure, relationship-based digital vault platform that allows users to store, organize, and share sensitive data with specific people or groups — **immediately, conditionally, or at a future time** (including preset “expected death” dates).

Eloomen is not just a password manager or document storage app.  
It is a **policy-driven sharing system** designed for families, partners, roommates, caregivers, doctors, and lawyers to safely access the right information at the right time.

---

## ✨ What Problem Eloomen Solves

People store critical information across emails, apps, cloud drives, and notebooks:

- Parents want children to access documents later in life
- Spouses share subscriptions, estate info, and personal data
- Roommates share leases and utility contracts
- Patients want doctors to access records in emergencies
- Individuals want information released after a preset future date (e.g., expected death year)

**Eloomen centralizes all of this while enforcing strict access rules, time-based release policies, and full auditability.**

---

## 🧠 Core Concept

Eloomen is built around **four core ideas**:

1. **Vaults (Sections)** – User-defined containers like *Family*, *Spouse*, *Roommates*, *Medical*, *Legal*, etc.
2. **Dynamic Groups** – Fully configurable relationship groups (not hard-coded roles).
3. **Items (Any Data Type)** – Documents, passwords, crypto wallets, notes, links — all encrypted.
4. **Policies (When & How Sharing Happens)** – Time-based, inactivity-based, expiry-based, or manual release rules.

---

## 🔐 Supported Data Types

### 📁 Documents
- PNG, JPG, PDF, DOCX, XLSX, etc.
- Custom titles & metadata
- Stored securely using Supabase Storage
- Version-ready design

### 🔑 Password Records
- Banks, subscriptions, websites
- Username + encrypted password
- Custom fields supported

### ₿ Crypto Wallet Info
- Encrypted seed phrases / private keys
- Wallet type & blockchain
- Public address + notes

### 🔗 Important Links
- URLs with titles
- Optional notes

### 📝 Secure Notes
- Encrypted text
- Markdown or rich-text

---

## ⏳ Time-Based & Conditional Sharing

Eloomen fully supports delayed and conditional access, including **expected death time**.

Examples:
- Release a vault in 2035
- Release if inactive for 12 months
- Emergency access for 72 hours
- Auto-revoke access after expiry

Policy types:
- Fixed date release
- Inactivity (dead-man switch)
- Expiry
- Manual trigger

---

## 🏗️ Architecture Overview

**Frontend (Next.js)**  
→ **ASP.NET Core API (.NET 8 + JWT)**  
→ **PostgreSQL (Supabase)**  
→ **Supabase Storage (documents & files)**  
→ **RabbitMQ (background workflows & policy execution)**

---

## 🧰 Tech Stack

### Frontend
- Next.js (TypeScript)
- TailwindCSS
- JWT authentication
- WebCrypto API (client-side encryption)

### Backend
- ASP.NET Core (.NET 8)
- Custom JWT Auth (Access + Refresh tokens)
- Policy-based authorization
- Background workers for policy evaluation

### Database
- PostgreSQL (Supabase)
- Fully relational (foreign keys, transactions)

### Storage
- Supabase Storage
- Private buckets
- Signed URLs for upload/download

### Messaging
- **RabbitMQ**
- Asynchronous policy execution
- Inactivity checks
- Delayed releases
- Audit event processing

### CI/CD
- GitHub Actions

---

## 🔑 Authentication & Authorization

- JWT Access Tokens (short-lived)
- Rotating Refresh Tokens
- MFA for sensitive actions
- Role + policy-based access control

---

## 🗄️ Database Overview (Relational)

Core tables include:
- Users, UserProfiles
- Vaults, Groups, GroupMembers
- Items
- Documents, PasswordEntries, CryptoWallets, Notes, Links
- ItemGrants
- Policies, PolicySchedules
- AuditEvents

---

## 🔍 Security Principles

- No plaintext secrets
- Encrypted storage
- Time-boxed access
- Full audit logging
- Explicit user consent for all delayed releases

---

## 🚀 CI/CD (GitHub Actions)

Pipeline stages:
1. Build & test
2. Docker image creation
3. Database migrations
4. Deployment
5. Smoke tests

---

## 🎯 Why Eloomen Is Different

- Relationship-based, not app-based
- Time-aware data sharing
- Policy engine at the core
- Supports but not limited to estate planning

---

## 📌 Future Enhancements

- Mobile apps (iOS / Android)
- Hardware key support
- Encrypted search
- Enterprise & family plans

---

**Eloomen — Secure your digital life. Share it on your terms.**
