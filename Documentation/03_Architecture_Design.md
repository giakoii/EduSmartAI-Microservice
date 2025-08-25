# Architecture Design Document
## EduSmartAI Backend System

### 1. Architectural Overview

#### 1.1 Architecture Style
The EduSmartAI backend follows **Clean Architecture** principles combined with **Microservices Architecture** to ensure maintainability, testability, and scalability.

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   AuthService   │  │   SharedAPI     │  │  Future     │ │
│  │      API        │  │   Controllers   │  │  Services   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   AuthService   │  │     Shared      │                 │
│  │  Application    │  │  Application    │                 │
│  │  (CQRS/MediatR) │  │    Services     │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   AuthService   │  │  BuildingBlocks │                 │
│  │     Domain      │  │   (CQRS Base)   │                 │
│  │ (Entities, VOs) │  │                 │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────┐
│                Infrastructure Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   AuthService   │  │     Shared      │                 │
│  │ Infrastructure  │  │ Infrastructure  │                 │
│  │(EF, Repos, DB)  │  │ (Common Utils)  │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

#### 1.2 CQRS Implementation
The system implements **Command Query Responsibility Segregation** using MediatR:

```csharp
// Command Pattern
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

// Query Pattern  
public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : notnull { }
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
```

### 2. Service Architecture

#### 2.1 AuthService Architecture
```
AuthService.API
├── Controllers (REST endpoints)
├── Middleware (Auth, Validation, Error handling)
└── Program.cs (DI configuration)

AuthService.Application
├── Commands (Write operations)
│   ├── CreateAccount
│   ├── UpdateAccount
│   └── DeleteAccount
├── Queries (Read operations)
│   ├── GetAccountById
│   └── GetAccountByEmail
└── Handlers (Business logic)

AuthService.Domain
├── WriteModels
│   ├── Account.cs
│   └── Role.cs
├── ReadModels (DTOs)
└── ValueObjects

AuthService.Infrastructure
├── Context (EF DbContext)
├── Repositories (Data access)
├── Configurations (EF mapping)
└── Migrations
```

#### 2.2 Shared Components Architecture
```
Shared.API
├── AbstractControllers (Base controllers)
├── Middleware (Common middleware)
└── Extensions (Service registration)

Shared.Application
├── Common (Shared DTOs, responses)
├── Helpers (Utility functions)
├── Interfaces (Common contracts)
└── Utils (Common utilities)

Shared.Infrastructure
├── Contexts (Base DbContext)
├── Identities (User management)
├── Logics (Business rules)
└── Repositories (Generic repository pattern)

BuildingBlocks
└── CQRS (Base interfaces for commands/queries)
```

### 3. Data Architecture

#### 3.1 Database Design (PostgreSQL)
```sql
-- Core Authentication Tables
CREATE TABLE Roles (
    Id UUID PRIMARY KEY,
    Name VARCHAR(256) NOT NULL,
    NormalizedName VARCHAR(256) NOT NULL UNIQUE
);

CREATE TABLE Accounts (
    AccountId UUID PRIMARY KEY,
    RoleId UUID NOT NULL REFERENCES Roles(Id),
    Email VARCHAR(256) NOT NULL UNIQUE,
    EmailConfirmed BOOLEAN DEFAULT FALSE,
    PasswordHash VARCHAR(512),
    LockoutEnd TIMESTAMPTZ,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(256) NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(256) NOT NULL,
    AccessFailedCount INTEGER DEFAULT 0,
    Key VARCHAR(256)
);

-- Indexes for performance
CREATE INDEX IX_Accounts_Email ON Accounts(Email);
CREATE INDEX IX_Accounts_RoleId ON Accounts(RoleId);
CREATE INDEX IX_Accounts_IsActive ON Accounts(IsActive);
```

#### 3.2 Caching Strategy (Redis)
```
Cache Key Patterns:
- user:profile:{userId} - User profile data (TTL: 1 hour)
- auth:token:{tokenId} - JWT token validation (TTL: 15 minutes)
- session:{sessionId} - User session data (TTL: 24 hours)
- role:permissions:{roleId} - Role permissions (TTL: 6 hours)
```

### 4. Integration Architecture

#### 4.1 AI Services Integration
```
┌─────────────────┐    HTTP/REST    ┌─────────────────┐
│   AuthService   │ ────────────────▶│  Ultralytics   │
│      API        │                 │      HUB        │
└─────────────────┘                 └─────────────────┘
         │
         │ HTTP/REST
         ▼
┌─────────────────┐                 ┌─────────────────┐
│  Azure Speech   │                 │ Google Voice    │
│    Service      │                 │  Recognition    │
└─────────────────┘                 └─────────────────┘
         │
         │ HTTP/REST
         ▼
┌─────────────────┐                 ┌─────────────────┐
│ Azure Custom    │                 │   Cloudinary    │
│    Vision       │                 │   Media API     │
└─────────────────┘                 └─────────────────┘
```

#### 4.2 Search Architecture (ElasticSearch)
```json
{
  "mappings": {
    "properties": {
      "id": {"type": "keyword"},
      "title": {"type": "text", "analyzer": "standard"},
      "content": {"type": "text", "analyzer": "standard"},
      "tags": {"type": "keyword"},
      "createdAt": {"type": "date"},
      "userId": {"type": "keyword"},
      "category": {"type": "keyword"}
    }
  }
}
```

### 5. Security Architecture

#### 5.1 Authentication Flow
```
1. User Login Request → AuthService API
2. Validate Credentials → Database Query  
3. Generate JWT Tokens → Access + Refresh
4. Store Refresh Token → Redis Cache
5. Return Tokens → Client Application
6. Subsequent Requests → JWT Validation
7. Token Refresh → New Access Token
```

#### 5.2 Authorization Matrix
```
Role        | Endpoints Access
----------- | ----------------
Student     | GET /api/auth/profile, POST /api/content/search
Teacher     | All Student + POST /api/content/create
Admin       | All Teacher + DELETE /api/accounts/*
System      | Internal service-to-service communication
```

### 6. Deployment Architecture

#### 6.1 Google Cloud Platform Setup
```
Production Environment:
├── Google Kubernetes Engine (GKE)
│   ├── AuthService Pods (3 replicas)
│   ├── Shared Services Pods (2 replicas)
│   └── Load Balancer (Google Cloud Load Balancer)
├── Cloud SQL (PostgreSQL)
│   ├── Primary Instance (High Availability)
│   └── Read Replica (Query optimization)
├── Memorystore (Redis)
│   ├── Cache Cluster (3 nodes)
│   └── Session Store
└── Cloud Storage
    ├── Media Files (Cloudinary integration)
    └── Backup Storage
```

#### 6.2 CI/CD Pipeline
```
GitHub Repository
    │
    ├── Feature Branch → Pull Request
    │   ├── Unit Tests (xUnit)
    │   ├── Integration Tests
    │   └── Code Review
    │
    ├── Main Branch → Auto Deploy
    │   ├── Build Docker Images
    │   ├── Push to Container Registry
    │   ├── Deploy to Staging
    │   ├── Run E2E Tests
    │   └── Deploy to Production
```

### 7. Performance Architecture

#### 7.1 Scalability Patterns
- **Horizontal Scaling**: Multiple service instances behind load balancer
- **Database Sharding**: User-based partitioning for large datasets
- **Read Replicas**: Separate read/write database instances
- **CDN Integration**: Static asset delivery via Cloudinary

#### 7.2 Monitoring & Observability
```
Application Monitoring:
├── Health Checks (/health, /ready endpoints)
├── Metrics Collection (Prometheus/Grafana)
├── Distributed Tracing (OpenTelemetry)
├── Centralized Logging (Google Cloud Logging)
└── Alert Management (PagerDuty/Slack integration)
```
