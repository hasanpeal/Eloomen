# Eloomen

<div align="center">
<img src="./client/public/icon.png" alt="Eloomen Logo" width="120" height="120" />

[![Next.js](https://img.shields.io/badge/Next.js-16.1-black?logo=next.js)](https://nextjs.org/)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-336791?logo=postgresql)](https://www.postgresql.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?logo=typescript)](https://www.typescriptlang.org/)

**A Secure, Policy-Driven Digital Vault Platform**
**Secure your digital life. Share it on your terms.**

</div>

---

## 🎯 Overview

**Eloomen** is a production-ready, enterprise-grade digital vault platform that enables secure, relationship-based data sharing with sophisticated time-based and conditional access policies. Built from the ground up with security-first principles, Eloomen solves critical real-world problems around digital estate planning, family data sharing, and conditional information access.

### Key Differentiators

- **Policy-Driven Architecture**: Sophisticated time-based release policies (immediate, scheduled, expiry-based, manual)
- **Relationship-Based Access Control**: Dynamic, configurable groups with granular permissions
- **Multi-Type Data Support**: Documents, passwords, crypto wallets, notes, and links — all encrypted
- **Enterprise Security**: End-to-end encryption, audit logging, JWT with refresh tokens, device verification
- **Real-Time Notifications**: Comprehensive notification system with email and in-app notifications
- **Production-Ready**: Automated migrations, CI/CD pipelines, comprehensive error handling

---

## 🏗️ System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Next.js 16 Frontend                      │
│  React 19, TypeScript, TailwindCSS, WebCrypto API           │
│  - Client-side encryption                                   │
│  - JWT token management                                     │
│  - Real-time notifications                                  │
└───────────────────────┬─────────────────────────────────────┘
                        │ REST API (JWT Auth)
                        │ HTTPS/TLS
┌───────────────────────▼─────────────────────────────────────┐
│              ASP.NET Core 9 Backend API                     │
│  - Controllers (API Endpoints)                              │
│  - Service Layer (Business Logic)                           │
│  - Entity Framework Core (ORM)                              │
│  - Policy-based Authorization                               │
└───────┬───────────────┬───────────────┬─────────────────────┘
        │               │               │
┌───────▼────┐  ┌───────▼────┐  ┌──────▼──────────┐
│PostgreSQL  │  │  S3 Bucket  │  │  SendGrid Email │
│(Supabase)  │  │  (Storage)  │  │  (Notifications)│
│            │  │             │  │                 │
│ - 24 Tables│  │ - Documents │  │ - Transactional│
│ - Triggers │  │ - Signed URLs│ │ - Templates    │
│ - Functions│  │             │  │                 │
└────────────┘  └─────────────┘  └─────────────────┘
```

### Detailed Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                   │
│  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Pages      │  │ Components  │  │  Contexts    │  │  API Client  │      │
│  │  (Next.js)   │  │  (React)    │  │  (Auth)      │  │  (HTTP)      │      │
│  └──────┬───────┘  └──────┬──────┘  └──────┬──────-┘  └──────┬───────┘      │
│         │                 │                │                 │              │
│         └─────────────────┴──────────────-─┴───────────────--┘              │
│                              │ JWT Auth                                     │
└──────────────────────────────┼──────────────────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────────────────┐
│                           CONTROLLER LAYER                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │   Account    │  │    Vault     │  │  VaultItem   │  │ Notification │   │
│  │  Controller  │  │  Controller  │  │  Controller  │  │  Controller  │   │
│  └──────┬───────┘  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘   │
│         │                  │                 │                 │            │
└─────────┼──────────────────┼─────────────────┼─────────────────┼────────────┘
          │                  │                 │                 │
┌─────────▼──────────────────▼─────────────────▼─────────────────▼────────────┐
│                            SERVICE LAYER                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │   Token      │  │    Vault      │  │  VaultItem    │  │ Notification │ │
│  │   Service    │  │   Service     │  │   Service     │  │   Service     │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                  │                 │                 │          │
│  ┌──────▼───────┐  ┌───────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐ │
│  │   Device     │  │   Email       │  │ Encryption   │  │   S3         │ │
│  │   Service    │  │   Service     │  │   Service    │  │   Service    │ │
│  └──────────────┘  └───────────────┘  └──────────────┘  └──────────────┘ │
└─────────┬────────────────────────────────────────────────────────────────────┘
          │
┌─────────▼────────────────────────────────────────────────────────────────────┐
│                         ENTITY FRAMEWORK CORE (ORM)                           │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │                    ApplicationDBContext                                 │ │
│  │  - DbSet<User>                                                          │ │
│  │  - DbSet<Vault>                                                         │ │
│  │  - DbSet<VaultItem>                                                     │ │
│  │  - DbSet<Notification>                                                  │ │
│  │  - ... (24 tables)                                                      │ │
│  └──────────────────────────────┬───────────────────────────────────────────┘ │
└─────────────────────────────────┼─────────────────────────────────────────────┘
                                  │
┌─────────────────────────────────▼─────────────────────────────────────────────┐
│                           POSTGRESQL DATABASE                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │                        24 Database Tables                                │ │
│  │  • Users, Roles, UserRoles, UserClaims, RoleClaims                      │ │
│  │  • UserDevices, RefreshTokens, VerificationCodes                         │ │
│  │  • Vaults, VaultMembers, VaultInvites, VaultPolicies                   │ │
│  │  • VaultItems, VaultItemVisibilities                                   │ │
│  │  • VaultDocuments, VaultPasswords, VaultNotes                           │ │
│  │  • VaultLinks, VaultCryptoWallets                                      │ │
│  │  • VaultLogs, AccountLogs, Notifications                               │ │
│  │  • UserLogins, UserTokens                                              │ │
│  └──────────────────────────────┬───────────────────────────────────────────┘ │
│                                 │                                             │
│  ┌──────────────────────────────▼──────────────────────────────────────────┐ │
│  │                    PostgreSQL Triggers & Functions                      │ │
│  │  • notify_vault_released() - Auto-notify on vault release              │ │
│  │  • vault_release_notification_trigger - Monitors ReleaseStatus changes │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         NOTIFICATION FLOW                                    │
│                                                                              │
│  Event Triggered (Vault Release, Item Edit, Invite, etc.)                  │
│         │                                                                    │
│         ├──────────────────────────────────────────────────────┐            │
│         │                                                        │            │
│  ┌──────▼──────────┐                                  ┌────────▼──────┐   │
│  │ Service Layer    │                                  │ PostgreSQL    │   │
│  │ (Business Logic) │                                  │ Trigger       │   │
│  └──────┬───────────┘                                  └────────┬───────┘   │
│         │                                                        │            │
│  ┌──────▼──────────┐                                  ┌────────▼──────┐   │
│  │ Email Service    │                                  │ Notifications  │   │
│  │ (SendGrid)       │                                  │ Table         │   │
│  │                  │                                  └────────┬───────┘   │
│  │ • Send Email     │                                           │            │
│  │ • HTML Templates│                                           │            │
│  └──────────────────┘                                  ┌────────▼──────┐   │
│                                                        │ Notification  │   │
│                                                        │ Service       │   │
│                                                        │ (Create/Read) │   │
│                                                        └────────┬───────┘   │
│                                                                 │            │
│                                                        ┌────────▼──────┐   │
│                                                        │ Frontend      │   │
│                                                        │ (Real-time UI)│   │
│                                                        └───────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Service Layer Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            SERVICE INTERFACES                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ ITokenService│  │ IVaultService│  │IVaultItem     │  │INotification │   │
│  │              │  │              │  │Service        │  │Service       │   │
│  └──────┬───────┘  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘   │
│         │                  │                 │                 │            │
│  ┌──────▼───────┐  ┌───────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐ │
│  │ TokenService  │  │  VaultService  │  │VaultItem     │  │Notification │ │
│  │              │  │                │  │Service       │  │Service      │ │
│  │ • JWT Gen    │  │ • CRUD Ops     │  │ • CRUD Ops   │  │ • Create     │ │
│  │ • Refresh    │  │ • Policy Mgmt  │  │ • Encryption │  │ • Mark Read  │ │
│  │ • Validation │  │ • Invites      │  │ • Permissions│  │ • Delete     │ │
│  │              │  │ • Members       │  │ • S3 Upload  │  │ • Query      │ │
│  └──────────────┘  └───────┬────────┘  └──────┬───────┘  └──────────────┘ │
│                            │                  │                           │
│  ┌─────────────────────────▼──────────────────▼───────────────────────────┐ │
│  │                    SUPPORTING SERVICES                                 │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐│ │
│  │  │ IEmailService│  │IEncryption  │  │  IS3Service  │  │IDeviceService││ │
│  │  │              │  │Service      │  │              │  │              ││ │
│  │  │ • SendGrid   │  │ • AES-256   │  │ • Upload     │  │ • Fingerprint││ │
│  │  │ • Templates  │  │ • Encrypt   │  │ • Download   │  │ • Verify     ││ │
│  │  │ • Notify     │  │ • Decrypt   │  │ • Delete     │  │ • Manage     ││ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘│ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Technical Stack Deep Dive

#### **Frontend Stack**

- **Framework**: Next.js 16.1.1 (App Router, Server Components)
- **Language**: TypeScript 5.0+ (strict mode)
- **UI**: React 19.2.3, TailwindCSS 4.0
- **State Management**: React Context API, Custom hooks
- **Authentication**: JWT with automatic token refresh
- **Encryption**: WebCrypto API for client-side encryption
- **HTTP Client**: Custom API client with retry logic and error handling
- **Notifications**: Real-time notification system with polling and badge counts

#### **Backend Stack**

- **Framework**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0 (Code-First migrations)
- **Database**: PostgreSQL 15+ (via Supabase)
- **Authentication**: ASP.NET Core Identity + Custom JWT implementation
- **Authorization**: Policy-based with role hierarchy (Owner → Admin → Member)
- **File Storage**: S3 bucket for document storage
- **Email**: SendGrid integration for transactional emails
- **API Documentation**: Swagger/OpenAPI
- **Notifications**: In-app notification system with PostgreSQL triggers

#### **Infrastructure & DevOps**

- **Database**: Supabase (PostgreSQL + Storage)
- **Object Storage**: S3 bucket
- **CI/CD**: GitHub Actions (build, test, deploy)
- **Migrations**: Automatic EF Core migrations on startup
- **Logging**: Structured logging with ILogger
- **Error Handling**: Global exception handling, custom error responses
- **Database Triggers**: PostgreSQL functions for automated notifications

---

## 🔐 Security Architecture

### Authentication & Authorization

**Multi-Layer Security Model:**

1. **JWT Authentication**

   - Short-lived access tokens (15 minutes)
   - Long-lived refresh tokens (stored in HTTP-only cookies)
   - Automatic token rotation on refresh
   - Security stamp validation for token revocation

2. **Device Verification**

   - Device fingerprinting for new device detection
   - Email-based device verification codes
   - Device management dashboard

3. **Role-Based Access Control (RBAC)**

   - **Owner**: Full control (create, edit, delete, manage members, transfer ownership)
   - **Admin**: Manage items and members (cannot delete vault or transfer ownership)
   - **Member**: View and edit items (permission-based)

4. **Policy-Based Access Control**
   - Vault-level policies override member access
   - Time-based release policies
   - Expiry-based access revocation
   - Manual release triggers

### Data Encryption

- **At Rest**: All sensitive data encrypted before database storage
- **In Transit**: HTTPS/TLS for all API communications
- **Client-Side**: WebCrypto API for encryption before transmission
- **Secrets**: Passwords, crypto keys, and sensitive notes encrypted with AES-256

### Audit & Compliance

- **Comprehensive Audit Logging**: All vault operations logged (create, update, delete, invite, member changes)
- **Account Activity Logs**: User authentication, device changes, profile updates
- **Immutable Logs**: Timestamped, user-attributed audit trail
- **Data Retention**: Configurable retention policies
- **Change Tracking**: Detailed field-level change tracking for vault items

---

## 📊 Database Schema

### Complete PostgreSQL Tables (24 Tables)

The application uses **24 PostgreSQL tables** organized into the following categories:

#### **Identity & Authentication Tables (7 tables)**

1. **Users** - Core user accounts with email, username, security stamps
2. **Roles** - System roles (Admin, User)
3. **UserRoles** - Many-to-many relationship between users and roles
4. **UserClaims** - Custom claims for users
5. **RoleClaims** - Custom claims for roles
6. **UserLogins** - External login providers
7. **UserTokens** - External authentication tokens

#### **Device & Session Management (3 tables)**

8. **UserDevices** - Registered devices with fingerprinting
9. **RefreshTokens** - JWT refresh tokens linked to devices
10. **VerificationCodes** - Email/device verification codes

#### **Vault Core Tables (4 tables)**

11. **Vaults** - Main vault entities with owner relationships
12. **VaultMembers** - Vault membership with privileges (Owner/Admin/Member)
13. **VaultInvites** - Invitation system with tokens and expiration
14. **VaultPolicies** - Policy configuration (Immediate/TimeBased/ExpiryBased/ManualRelease)

#### **Vault Items & Content (7 tables)**

15. **VaultItems** - Base vault item entity (polymorphic)
16. **VaultItemVisibilities** - Granular permissions per item per member
17. **VaultDocuments** - Document items with S3 object keys
18. **VaultPasswords** - Password items (encrypted)
19. **VaultNotes** - Note items (encrypted)
20. **VaultLinks** - Link/bookmark items
21. **VaultCryptoWallets** - Cryptocurrency wallet items (encrypted)

#### **Audit & Notifications (3 tables)**

22. **VaultLogs** - Comprehensive vault operation audit logs
23. **AccountLogs** - User account activity logs
24. **Notifications** - In-app notification system

### Entity Relationship Diagram

```
Users (1) ──────< (N) Vaults (Owner)
  │                    │
  │                    ├──< (N) VaultMembers >── (N) Users
  │                    │
  │                    ├──< (N) VaultInvites
  │                    │
  │                    ├──< (1) VaultPolicies
  │                    │
  │                    └──< (N) VaultItems
  │                           │
  │                           ├──< (1) VaultDocuments
  │                           ├──< (1) VaultPasswords
  │                           ├──< (1) VaultNotes
  │                           ├──< (1) VaultLinks
  │                           ├──< (1) VaultCryptoWallets
  │                           │
  │                           └──< (N) VaultItemVisibilities >── (N) VaultMembers
  │
  ├──< (N) UserDevices
  │      └──< (N) RefreshTokens
  │
  ├──< (N) VerificationCodes
  ├──< (N) AccountLogs
  └──< (N) Notifications

VaultPolicies ──> PostgreSQL Trigger ──> Notifications (Auto-create on release)
```

### Key Design Decisions

- **Soft Deletes**: Vaults and items support 30-day recovery window
- **Cascade Deletes**: Proper foreign key constraints with cascade rules
- **Indexing**: Optimized indexes on frequently queried fields (userId, vaultId, status, timestamps)
- **Transactions**: Critical operations wrapped in database transactions
- **Migration Strategy**: Code-first migrations with automatic application
- **Database Triggers**: PostgreSQL functions for automated notification creation on vault release
- **Polymorphic Items**: Single VaultItems table with one-to-one relationships to specific item types

---

## 🚀 Key Features & Implementation Highlights

### 1. **Policy Engine**

Sophisticated policy system supporting multiple release strategies:

- **Immediate**: Instant access upon vault creation
- **TimeBased**: Scheduled release at a future date/time
- **ExpiryBased**: Access expires after a set date
- **ManualRelease**: Requires explicit owner action

**Implementation**: Policy evaluation runs on every vault access, automatically updating release status based on current time and policy rules. PostgreSQL triggers automatically create notifications when vaults are released.

### 2. **Granular Item Permissions**

Each vault item can have different visibility rules per member:

- **View**: Read-only access
- **Edit**: Full edit capabilities
- **Inherit**: Default vault-level permissions

**Implementation**: `VaultItemVisibility` junction table enables fine-grained access control without performance overhead. Owners always have Edit permission and are excluded from visibility checks.

### 3. **Invite System**

Robust invitation workflow with:

- Email-based invitations with secure tokens
- Expiration handling (default 7 days, configurable)
- Status tracking (Pending → Sent → Accepted/Cancelled/Expired)
- Automatic member creation on acceptance
- Resend and cancel capabilities
- Email notifications to vault owner when invites are sent
- Notifications to both inviter and invitee when invites expire

**Security**: Tokens hashed with SHA-256 before storage, never stored in plaintext.

### 4. **File Upload & Storage**

- **S3 Bucket Integration**: Secure document storage with signed URLs
- **Signed URLs**: Time-limited download URLs for secure file access
- **Metadata Tracking**: File size, MIME type, original filename
- **Cleanup**: Automatic file deletion on item/vault deletion

### 5. **Notification System**

Comprehensive notification system with multiple channels:

- **In-App Notifications**: Real-time notification center with unread badges
- **Email Notifications**: SendGrid integration for critical events
- **PostgreSQL Triggers**: Automated notification creation on vault release
- **Notification Types**:
  - Vault released
  - Vault policy changed
  - Vault deleted
  - Vault item edited/deleted (to owner)
  - Invite sent/accepted/expired
  - Password changed
  - Account deleted

**Implementation**: Notifications are created both programmatically in services and automatically via database triggers for vault release events.

### 6. **Email Notification System**

Comprehensive email notification system covering:

- **Account Events**: Email verification, password changes, account deletion
- **Vault Events**: Vault released, policy changed, vault deleted
- **Item Events**: Item edited/deleted by other members (notifies owner)
- **Invite Events**: Invite sent, accepted, expired
- **Security Events**: Device verification, password reset

**Implementation**: SendGrid service with HTML email templates, dark mode styling, and professional branding.

### 7. **Audit Logging**

Comprehensive audit trail:

- **VaultLogs**: All vault operations (create, update, delete, invite, member changes)
- **AccountLogs**: User authentication, device changes, profile updates
- **Change Tracking**: Field-level change tracking for vault items (title, description, permissions, etc.)
- **Immutable Logs**: Timestamped, user-attributed audit trail

### 8. **Error Handling & Resilience**

- **Custom Error Classes**: `SessionExpiredError` for graceful auth failures
- **Retry Logic**: Automatic token refresh on 401 responses
- **User-Friendly Messages**: Transformed technical errors into actionable user feedback
- **Logging**: Comprehensive error logging with context for debugging

---

## 🧪 Development & Testing

### Code Quality

- **TypeScript**: Strict mode enabled, full type safety
- **ESLint**: Next.js recommended rules
- **Code Organization**: Feature-based folder structure
- **Separation of Concerns**: Clear boundaries between UI, business logic, and data access

### API Design

- **RESTful Principles**: Standard HTTP methods and status codes
- **DTO Pattern**: Separate request/response DTOs for type safety
- **Validation**: Model validation with ASP.NET Core Data Annotations
- **Error Responses**: Consistent error response format

### Performance Optimizations

- **Database Queries**: Eager loading with `.Include()` to prevent N+1 queries
- **Indexing**: Comprehensive indexes on foreign keys, status fields, and timestamps
- **Pagination Ready**: Architecture supports pagination (future enhancement)
- **Caching Strategy**: Ready for Redis integration (future)
- **Frontend**: Code splitting, lazy loading, optimized bundle size
- **Notification Polling**: Efficient 30-second polling for new notifications

---

## 🧪 Testing

### Test Setup

The project includes comprehensive backend testing using **xUnit** and **Moq** for .NET 9.0.

#### Prerequisites

- **.NET 9 SDK** (required for running tests)
- **PostgreSQL** (for integration tests, can use in-memory database for unit tests)

#### Test Project Structure

```
server/
├── Tests/
│   ├── Controllers/          # Controller integration tests
│   │   ├── AccountControllerTests.cs
│   │   ├── VaultControllerTests.cs
│   │   ├── VaultItemControllerTests.cs
│   │   ├── NotificationControllerTests.cs
│   │   ├── ContactControllerTests.cs
│   │   └── HealthControllerTests.cs
│   ├── Services/             # Service layer unit tests
│   │   ├── TokenServiceTests.cs
│   │   ├── VaultServiceTests.cs
│   │   ├── VaultItemServiceTests.cs
│   │   ├── NotificationServiceTests.cs
│   │   ├── EncryptionServiceTests.cs
│   │   ├── DeviceServiceTests.cs
│   │   └── VaultServiceExtendedTests.cs
│   ├── Helpers/              # Test utilities and helpers
│   │   └── TestHelpers.cs
│   └── server.Tests.csproj   # Test project file
```

### Running Tests

#### Run All Tests

```bash
cd server
dotnet test Tests/server.Tests.csproj --configuration Release --verbosity normal
```

#### Run Tests with Coverage

```bash
cd server
dotnet test Tests/server.Tests.csproj \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory:./coverage
```

#### Run Specific Test Class

```bash
dotnet test Tests/server.Tests.csproj --filter "FullyQualifiedName~AccountControllerTests"
```

#### Run Specific Test Method

```bash
dotnet test Tests/server.Tests.csproj --filter "FullyQualifiedName~AccountControllerTests.Register_WithValidData_CreatesUser"
```

### Test Results

#### Current Test Status

✅ **All Tests Passing**: 117 tests, 0 failures

#### Test Coverage by Category

**Controller Tests (6 test classes, ~40 tests)**

- ✅ `AccountControllerTests` - Authentication, registration, profile management
- ✅ `VaultControllerTests` - Vault CRUD, member management, invites, policies
- ✅ `VaultItemControllerTests` - Item CRUD, permissions, restore operations
- ✅ `NotificationControllerTests` - Notification retrieval, marking as read, deletion
- ✅ `ContactControllerTests` - Contact form submission
- ✅ `HealthControllerTests` - Health check endpoints

**Service Tests (8 test classes, ~77 tests)**

- ✅ `TokenServiceTests` - JWT token generation, validation, refresh tokens
- ✅ `VaultServiceTests` - Vault business logic, permissions, CRUD operations
- ✅ `VaultServiceExtendedTests` - Advanced vault operations (invites, transfers, policies)
- ✅ `VaultItemServiceTests` - Item operations, permissions, encryption
- ✅ `VaultItemServiceExtendedTests` - Advanced item operations (restore, permissions)
- ✅ `NotificationServiceTests` - Notification creation, retrieval, updates
- ✅ `EncryptionServiceTests` - AES-256 encryption/decryption, Unicode support
- ✅ `DeviceServiceTests` - Device fingerprinting, verification, management

### CI/CD Integration

Tests are automatically run in **GitHub Actions** on every push and pull request:

```yaml
# .github/workflows/ci.yml
- name: Run tests
  run: dotnet test server/Tests/server.Tests.csproj --no-restore --configuration Release --verbosity normal
```

#### Test Execution in CI

- **Trigger**: Push to `main`, `develop`, `master` branches or pull requests
- **Environment**: Ubuntu Latest with .NET 9.0.x
- **Test Results**: Uploaded as artifacts for review
- **Status**: All tests must pass for CI to succeed

### Test Architecture

#### Test Patterns Used

1. **Arrange-Act-Assert (AAA)**: Standard test structure
2. **Mocking**: Moq framework for dependencies (database, external services)
3. **In-Memory Database**: EF Core InMemory provider for fast unit tests
4. **Test Fixtures**: Reusable test data and setup helpers
5. **Integration Tests**: Full controller tests with mocked services

#### Example Test Structure

```csharp
[Fact]
public async Task Register_WithValidData_CreatesUser()
{
    // Arrange
    var registerDto = new RegisterDTO { /* ... */ };

    // Act
    var result = await _controller.Register(registerDto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(200, ((ObjectResult)result).StatusCode);
}
```

### Test Data Management

- **Test Helpers**: `TestHelpers.cs` provides utilities for creating test data
- **Isolated Tests**: Each test is independent with its own database context
- **Cleanup**: Automatic cleanup after each test execution
- **Test Data**: Realistic test scenarios covering edge cases

### Coverage Goals

- ✅ **Controllers**: 100% endpoint coverage
- ✅ **Services**: Core business logic fully tested
- ✅ **Critical Paths**: Authentication, authorization, encryption
- 🔄 **Integration Tests**: API endpoint integration testing
- 🔄 **E2E Tests**: Full user workflow testing (planned)

### Running Tests Locally Before Push

Always run tests locally before pushing to ensure CI passes:

```bash
# Run all tests
cd server
dotnet test Tests/server.Tests.csproj --configuration Release

# Expected output: All 117 tests passing ✅
```

---

## 📦 Project Structure

```
Eloomen/
├── client/                          # Next.js frontend
│   ├── app/                        # App Router pages
│   │   ├── components/            # Reusable React components
│   │   │   ├── ContactModal.tsx
│   │   │   ├── CreateVaultItemModal.tsx
│   │   │   └── NotificationsModal.tsx
│   │   ├── contexts/              # React Context providers
│   │   │   └── AuthContext.tsx
│   │   ├── lib/                   # Utilities, API client
│   │   │   └── api.ts            # API client with JWT handling
│   │   ├── dashboard/             # Dashboard page
│   │   ├── vaults/[id]/          # Vault detail page
│   │   ├── account/              # Account management
│   │   ├── login/                # Authentication pages
│   │   └── [other routes]/
│   ├── public/                    # Static assets
│   │   └── icon.png             # Logo
│   └── package.json
│
├── server/                         # ASP.NET Core backend
│   ├── Controllers/              # API endpoints
│   │   ├── AccountController.cs
│   │   ├── VaultController.cs
│   │   ├── VaultItemController.cs
│   │   ├── NotificationController.cs
│   │   └── ContactController.cs
│   ├── Services/                 # Business logic layer
│   │   ├── TokenService.cs
│   │   ├── VaultService.cs
│   │   ├── VaultItemService.cs
│   │   ├── NotificationService.cs
│   │   ├── EmailService.cs
│   │   ├── EncryptionService.cs
│   │   ├── S3Service.cs
│   │   └── DeviceService.cs
│   ├── Interfaces/               # Service contracts
│   │   ├── ITokenService.cs
│   │   ├── IVaultService.cs
│   │   ├── IVaultItemService.cs
│   │   ├── INotificationService.cs
│   │   ├── IEmailService.cs
│   │   ├── IEncryptionService.cs
│   │   ├── IS3Service.cs
│   │   └── IDeviceService.cs
│   ├── Models/                   # Entity models (24 tables)
│   │   ├── User.cs
│   │   ├── Vault.cs
│   │   ├── VaultItem.cs
│   │   ├── Notification.cs
│   │   └── [other models]
│   ├── Dtos/                     # Data transfer objects
│   │   ├── Account/
│   │   ├── Vault/
│   │   ├── VaultItem/
│   │   └── Notification/
│   ├── Data/                     # DbContext, migrations
│   │   ├── ApplicationDBContext.cs
│   │   └── Migrations/
│   │       └── [migration files including triggers]
│   └── Program.cs                # Application startup
│
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- **Node.js** 20+ and npm
- **.NET 9 SDK**
- **PostgreSQL** 15+ (or Supabase account)
- **AWS S3** bucket (for file storage)
- **SendGrid** account (for emails)

### Environment Setup

1. **Backend Configuration** (`server/appsettings.json`):

   ```json
   {
     "ConnectionStrings": {
       "Default": "PostgreSQL connection string"
     },
     "Jwt": {
       "Issuer": "Eloomen",
       "Audience": "EloomenUsers",
       "SigningKey": "your-secret-key"
     },
     "S3": {
       "Endpoint": "your-s3-endpoint",
       "AccessKey": "your-access-key",
       "SecretKey": "your-secret-key",
       "BucketName": "your-bucket"
     },
     "SendGrid": {
       "ApiKey": "your-sendgrid-api-key",
       "FromEmail": "noreply@eloomen.com",
       "FromName": "Eloomen"
     },
     "App": {
       "BaseUrl": "http://localhost:3000"
     }
   }
   ```

2. **Frontend Configuration** (`.env.local`):
   ```
   NEXT_PUBLIC_API_URL=http://localhost:5000/api
   ```

### Running Locally

Run both server and client separately in different terminals:

```bash
# Backend
cd server
dotnet restore
dotnet watch run

# Frontend (new terminal)
cd client
npm install
npm run dev
```

---

## 🔄 CI/CD Pipeline

**GitHub Actions Workflow:**

1. **Backend Tests**: Run 117 unit and integration tests
   - Controller tests (Account, Vault, VaultItem, Notification, Contact, Health)
   - Service tests (Token, Vault, VaultItem, Notification, Encryption, Device)
   - Test results uploaded as artifacts
2. **Frontend Build & Lint**: Build Next.js app and run ESLint
3. **Build**: Compile .NET backend, build Next.js frontend
4. **Migrations**: Automatic database migrations on startup (Railway)
5. **Deploy**: Automated deployment via Railway and Vercel (connected via GitHub)

---

## 📈 Performance Metrics

- **API Response Time**: < 200ms (p95) for standard operations
- **Database Queries**: Optimized with proper indexing
- **Frontend Bundle**: Code-split, lazy-loaded components
- **File Upload**: Streaming uploads for large files
- **Notification Polling**: 30-second intervals for efficient updates

---

## 🔮 Future Enhancements

### Planned Features

- **Mobile Apps**: Native iOS and Android applications
- **Hardware Key Support**: FIDO2/WebAuthn integration
- **Encrypted Search**: Search over encrypted data
- **Enterprise Plans**: Team management, SSO, advanced policies
- **Advanced Analytics**: Usage dashboards, access reports
- **Real-time Updates**: WebSocket support for live notifications

### Technical Debt & Improvements

- [x] Unit test coverage (backend services) - ✅ 117 tests implemented
- [x] Integration tests (API endpoints) - ✅ Controller tests implemented
- [ ] E2E tests (Playwright/Cypress)
- [ ] Performance monitoring (Application Insights)
- [ ] Rate limiting (API throttling)
- [ ] Caching layer (Redis)
- [ ] WebSocket for real-time notifications

---

## 🤝 Contributing

This is a personal project showcasing full-stack development capabilities. Key areas of focus:

- **Security**: Industry-standard encryption and authentication
- **Scalability**: Architecture designed for growth
- **Maintainability**: Clean code, clear documentation
- **User Experience**: Intuitive UI, responsive design

---

## 📄 License

Proprietary - All rights reserved

---

## 👨‍💻 Engineering Highlights

### Technical Achievements

✅ **Full-Stack Development**: End-to-end implementation from database to UI  
✅ **Security-First Design**: Multi-layer security with encryption, RBAC, and audit logging  
✅ **Scalable Architecture**: Microservices-ready, cloud-native design  
✅ **Modern Tech Stack**: Latest versions of Next.js, .NET, React, TypeScript  
✅ **Production Practices**: CI/CD, automated migrations, error handling, logging  
✅ **Complex Business Logic**: Policy engine, time-based access, granular permissions  
✅ **API Design**: RESTful, well-documented, type-safe  
✅ **Database Design**: Normalized schema, proper relationships, migrations, triggers  
✅ **Notification System**: Comprehensive in-app and email notifications  
✅ **Change Tracking**: Detailed field-level change tracking for audit trails  
✅ **Test Coverage**: 117 comprehensive unit and integration tests

### Skills Demonstrated

- **Backend**: ASP.NET Core, Entity Framework Core, PostgreSQL, RESTful APIs
- **Frontend**: Next.js, React, TypeScript, TailwindCSS, State Management
- **Security**: JWT, Encryption, RBAC, Audit Logging, Device Verification
- **DevOps**: GitHub Actions, Docker, Database Migrations
- **Architecture**: Clean Architecture, DTO Pattern, Service Layer Pattern
- **Database**: PostgreSQL triggers, functions, complex relationships
- **Problem Solving**: Complex policy engine, granular permissions, time-based access
- **Integration**: SendGrid, S3, Supabase

---

<div align="center">

**Built with ❤️ using Next.js, ASP.NET Core, and PostgreSQL**

[Report Bug](https://github.com/hasanpeal/eloomen/issues) · [Request Feature](https://github.com/hasanpeal/eloomen/issues)

</div>
