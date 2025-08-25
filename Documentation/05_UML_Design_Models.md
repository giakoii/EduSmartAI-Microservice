# UML 2.0 Design Models
## EduSmartAI Backend System

### 1. Use Case Diagram

```plantuml
@startuml EduSmartAI_UseCases
!define RECTANGLE class

actor Student
actor Teacher  
actor Admin
actor "AI Service" as AI
actor "External Services" as External

rectangle "EduSmartAI System" {
    usecase "Register Account" as UC1
    usecase "Login" as UC2
    usecase "Verify Email" as UC3
    usecase "Reset Password" as UC4
    usecase "Manage Profile" as UC5
    
    usecase "Upload Content" as UC6
    usecase "Search Content" as UC7
    usecase "Categorize Content" as UC8
    
    usecase "Process Images" as UC9
    usecase "Analyze Speech" as UC10
    usecase "Extract Text" as UC11
    
    usecase "Manage Users" as UC12
    usecase "System Configuration" as UC13
    usecase "View Reports" as UC14
}

' Student interactions
Student --> UC1
Student --> UC2
Student --> UC3
Student --> UC4
Student --> UC5
Student --> UC7

' Teacher interactions  
Teacher --> UC1
Teacher --> UC2
Teacher --> UC3
Teacher --> UC4
Teacher --> UC5
Teacher --> UC6
Teacher --> UC7
Teacher --> UC8

' Admin interactions
Admin --> UC12
Admin --> UC13
Admin --> UC14

' AI Service interactions
AI --> UC9
AI --> UC10
AI --> UC11

' External service dependencies
External --> UC9
External --> UC10
External --> UC11

' Include relationships
UC6 ..> UC9 : <<include>>
UC6 ..> UC10 : <<include>>
UC8 ..> UC11 : <<include>>

@enduml
```

### 2. Class Diagram - Domain Model

```plantuml
@startuml EduSmartAI_DomainModel

package "AuthService.Domain" {
    class Account {
        +AccountId: Guid
        +RoleId: Guid
        +Email: string
        +EmailConfirmed: bool
        +PasswordHash: string
        +LockoutEnd: DateTimeOffset?
        +CreatedAt: DateTime
        +CreatedBy: string
        +IsActive: bool
        +UpdatedAt: DateTime
        +UpdatedBy: string
        +AccessFailedCount: int
        +Key: string?
        
        +LockAccount(minutes: int): void
        +IsLockedOut(): bool
        +ResetFailedAccessCount(): void
        +IncrementAccessFailedCount(): void
    }
    
    class Role {
        +Id: Guid
        +Name: string
        +NormalizedName: string
        
        +IsSystemRole(): bool
    }
    
    Account }|--|| Role : belongs to
    Role ||--o{ Account : has many
}

package "BuildingBlocks.CQRS" {
    interface ICommand<TResponse> {
        
    }
    
    interface IQuery<TResponse> {
        
    }
    
    interface ICommandHandler<TCommand, TResponse> {
        +Handle(command: TCommand, token: CancellationToken): Task<TResponse>
    }
    
    interface IQueryHandler<TQuery, TResponse> {
        +Handle(query: TQuery, token: CancellationToken): Task<TResponse>
    }
    
    ICommandHandler ..> ICommand : handles
    IQueryHandler ..> IQuery : handles
}

package "AuthService.Application" {
    class CreateAccountCommand {
        +Email: string
        +Password: string
        +RoleId: Guid
        +CreatedBy: string
    }
    
    class GetAccountByEmailQuery {
        +Email: string
    }
    
    class CreateAccountCommandHandler {
        -_accountRepository: IAccountRepository
        -_passwordHasher: IPasswordHasher
        -_emailService: IEmailService
        -_unitOfWork: IUnitOfWork
        
        +Handle(command: CreateAccountCommand, token: CancellationToken): Task<CreateAccountResponse>
    }
    
    CreateAccountCommand ..|> ICommand
    GetAccountByEmailQuery ..|> IQuery
    CreateAccountCommandHandler ..|> ICommandHandler
}

@enduml
```

### 3. Sequence Diagram - Authentication Flow

```plantuml
@startuml EduSmartAI_AuthSequence

participant "Client App" as Client
participant "AuthService API" as API
participant "AuthService Handler" as Handler
participant "Account Repository" as Repo
participant "Password Hasher" as Hasher
participant "JWT Service" as JWT
participant "Database" as DB
participant "Redis Cache" as Cache

Client -> API: POST /api/auth/login\n{email, password}
activate API

API -> Handler: LoginCommand
activate Handler

Handler -> Repo: GetByEmailAsync(email)
activate Repo

Repo -> DB: SELECT * FROM Accounts WHERE Email = ?
activate DB
DB --> Repo: Account data
deactivate DB

Repo --> Handler: Account entity
deactivate Repo

alt Account exists and is active
    Handler -> Hasher: VerifyPassword(password, hash)
    activate Hasher
    Hasher --> Handler: true/false
    deactivate Hasher
    
    alt Password valid
        Handler -> Repo: ResetFailedAccessCount()
        Handler -> JWT: GenerateAccessToken(account)
        activate JWT
        JWT --> Handler: JWT access token
        deactivate JWT
        
        Handler -> JWT: GenerateRefreshToken(accountId)
        activate JWT
        JWT -> Cache: Store refresh token
        activate Cache
        Cache --> JWT: Success
        deactivate Cache
        JWT --> Handler: Refresh token
        deactivate JWT
        
        Handler --> API: LoginResponse{accessToken, refreshToken, user}
        API --> Client: 200 OK + tokens
    else Password invalid
        Handler -> Repo: IncrementAccessFailedCount()
        Handler --> API: UnauthorizedException
        API --> Client: 401 Unauthorized
    end
else Account not found/inactive
    Handler --> API: UnauthorizedException  
    API --> Client: 401 Unauthorized
end

deactivate Handler
deactivate API

@enduml
```

### 4. Component Diagram - System Architecture

```plantuml
@startuml EduSmartAI_Components

package "Presentation Layer" {
    component [AuthService.API] as AuthAPI
    component [Shared.API] as SharedAPI
}

package "Application Layer" {
    component [AuthService.Application] as AuthApp
    component [Shared.Application] as SharedApp
    
    package "CQRS Components" {
        component [Commands] as Cmd
        component [Queries] as Query
        component [Handlers] as Handler
    }
}

package "Domain Layer" {
    component [AuthService.Domain] as AuthDomain
    component [BuildingBlocks] as BB
    
    package "Domain Models" {
        component [Entities] as Entity
        component [Value Objects] as VO
        component [Domain Services] as DS
    }
}

package "Infrastructure Layer" {
    component [AuthService.Infrastructure] as AuthInfra
    component [Shared.Infrastructure] as SharedInfra
    
    package "Data Access" {
        component [DbContext] as DbCtx
        component [Repositories] as Repo
        component [Migrations] as Mig
    }
}

package "External Services" {
    component [PostgreSQL] as DB
    component [Redis] as Cache
    component [ElasticSearch] as ES
    component [Cloudinary] as Cloud
    component [Ultralytics HUB] as Ultra
    component [Azure Speech] as Azure
    component [Google Voice] as Google
}

' Dependencies
AuthAPI --> AuthApp
AuthAPI --> SharedAPI
AuthApp --> AuthDomain
AuthApp --> BB
AuthInfra --> AuthDomain
AuthInfra --> SharedInfra

' CQRS Flow
AuthAPI --> Cmd
AuthAPI --> Query
Cmd --> Handler
Query --> Handler
Handler --> Repo
Repo --> DbCtx

' External connections
DbCtx --> DB
SharedInfra --> Cache
SharedInfra --> ES
SharedInfra --> Cloud
SharedApp --> Ultra
SharedApp --> Azure
SharedApp --> Google

@enduml
```

### 5. Deployment Diagram - Google Cloud Platform

```plantuml
@startuml EduSmartAI_Deployment

node "Google Cloud Platform" {
    
    node "Google Kubernetes Engine" as GKE {
        node "Load Balancer" as LB {
            component [Google Cloud Load Balancer]
        }
        
        node "AuthService Cluster" {
            node "Pod 1" {
                component [AuthService.API]
                component [AuthService.App]
            }
            node "Pod 2" {
                component [AuthService.API]
                component [AuthService.App]  
            }
            node "Pod 3" {
                component [AuthService.API]
                component [AuthService.App]
            }
        }
        
        node "Shared Services Cluster" {
            node "Pod 1" {
                component [Shared.API]
                component [AI Services]
            }
            node "Pod 2" {
                component [Shared.API] 
                component [AI Services]
            }
        }
    }
    
    node "Cloud SQL" {
        database "Primary DB" as PrimaryDB {
            component [PostgreSQL Primary]
        }
        database "Read Replica" as ReplicaDB {
            component [PostgreSQL Replica]
        }
    }
    
    node "Memorystore" {
        database "Redis Cluster" as RedisCluster {
            component [Redis Node 1]
            component [Redis Node 2] 
            component [Redis Node 3]
        }
    }
    
    node "Cloud Storage" {
        storage "Media Storage" as MediaStorage
        storage "Backup Storage" as BackupStorage
    }
}

node "External Services" {
    cloud "Ultralytics HUB" as UltraCloud
    cloud "Azure Services" as AzureCloud
    cloud "Google AI Services" as GoogleAI
    cloud "Cloudinary CDN" as CloudinaryCDN
}

' Connections
LB --> AuthService Cluster
LB --> "Shared Services Cluster"
AuthService Cluster --> PrimaryDB
AuthService Cluster --> RedisCluster
"Shared Services Cluster" --> ReplicaDB
"Shared Services Cluster" --> UltraCloud
"Shared Services Cluster" --> AzureCloud
"Shared Services Cluster" --> GoogleAI
"Shared Services Cluster" --> CloudinaryCDN

PrimaryDB --> ReplicaDB : replication
MediaStorage --> CloudinaryCDN

@enduml
```

### 6. Activity Diagram - Account Registration Process

```plantuml
@startuml EduSmartAI_Registration

start

:User submits registration form;

:Validate input data;

if (Is email format valid?) then (no)
    :Return validation error;
    stop
else (yes)
endif

if (Is password strong enough?) then (no)
    :Return password requirements;
    stop
else (yes)
endif

:Check email uniqueness in database;

if (Email already exists?) then (yes)
    :Return duplicate email error;
    stop
else (no)
endif

:Hash password with salt;

:Generate verification key;

:Create new account record;

:Save account to database;

fork
    :Send verification email;
fork again
    :Log registration event;
end fork

:Return success response;

stop

@enduml
```

### 7. State Diagram - Account Status

```plantuml
@startuml EduSmartAI_AccountStates

[*] --> Created : Register

state Created {
    Created : EmailConfirmed = false
    Created : IsActive = true
    Created : AccessFailedCount = 0
}

Created --> EmailVerified : Confirm Email
Created --> Inactive : Deactivate Account

state EmailVerified {
    EmailVerified : EmailConfirmed = true
    EmailVerified : IsActive = true
}

EmailVerified --> LockedOut : Multiple Failed Logins
EmailVerified --> Inactive : Admin Deactivation

state LockedOut {
    LockedOut : LockoutEnd = DateTime + 30min
    LockedOut : AccessFailedCount >= 5
}

LockedOut --> EmailVerified : Lockout Expires
LockedOut --> Inactive : Admin Action

state Inactive {
    Inactive : IsActive = false
}

Inactive --> EmailVerified : Admin Reactivation
Inactive --> [*] : Delete Account

EmailVerified --> [*] : Delete Account

@enduml
```
