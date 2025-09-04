# ZiraAI System Architecture Diagrams

This document provides comprehensive visual documentation of the ZiraAI system architecture, designed to accelerate onboarding, support business decisions, and enable effective technical discussions.

## Table of Contents
1. [System Context Diagram](#1-system-context-diagram)
2. [Component Architecture Diagram](#2-component-architecture-diagram)  
3. [Plant Analysis Processing Flow](#3-plant-analysis-processing-flow)
4. [Subscription Management Flow](#4-subscription-management-flow)
5. [Microservices Topology](#5-microservices-topology)
6. [Data Architecture (ERD)](#6-data-architecture-erd)

---

## 1. System Context Diagram

**Purpose**: High-level overview showing external actors, system boundaries, and key integrations.

```mermaid
graph TB
    subgraph "External Actors"
        F[👨‍🌾 Farmers<br/>Plant Analysis Users]
        S[🏢 Sponsors<br/>Subscription Purchasers]
        A[👤 Admins<br/>System Managers]
    end
    
    subgraph "ZiraAI Platform"
        API[🌐 Web API<br/>.NET 9.0]
        WS[⚙️ Worker Service<br/>Background Processing]
    end
    
    subgraph "Data Layer"
        PG[(🐘 PostgreSQL<br/>Primary Database)]
        RD[(⚡ Redis<br/>Cache Layer)]
    end
    
    subgraph "Message Queue"
        RMQ[🐰 RabbitMQ<br/>Async Processing]
    end
    
    subgraph "External Services"
        N8N[🤖 N8N AI Pipeline<br/>Plant Analysis ML]
        FIH[🖼️ FreeImageHost<br/>Image Storage]
        SMS[📱 SMS/WhatsApp<br/>Notifications]
        PAY[💳 Payment Gateway<br/>Subscriptions]
    end
    
    subgraph "Background Jobs"
        HF[📊 Hangfire<br/>Scheduled Tasks]
    end

    %% User Interactions
    F -->|JWT Authentication| API
    S -->|JWT Authentication| API  
    A -->|JWT Authentication| API
    
    %% API Interactions
    API --> PG
    API --> RD
    API --> RMQ
    API --> FIH
    API --> SMS
    API --> PAY
    API --> HF
    
    %% Worker Service Flow
    RMQ --> WS
    WS --> N8N
    WS --> PG
    WS --> FIH
    WS --> HF
    
    %% Styling
    classDef userClass fill:#e1f5fe,stroke:#0277bd,stroke-width:2px
    classDef platformClass fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px  
    classDef dataClass fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef externalClass fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    
    class F,S,A userClass
    class API,WS platformClass
    class PG,RD,RMQ,HF dataClass
    class N8N,FIH,SMS,PAY externalClass
```

**Key Features:**
- **Role-Based Access**: Farmers, Sponsors, and Admins with different permissions
- **Microservice Architecture**: Separate WebAPI and Worker Service for scaling
- **External AI Integration**: N8N pipeline for plant analysis processing
- **Multi-Provider Storage**: FreeImageHost for optimized image handling

---

## 2. Component Architecture Diagram

**Purpose**: Clean Architecture layers with CQRS implementation and service boundaries.

```mermaid
graph TB
    subgraph "Presentation Layer"
        API[Web API Controllers]
        MW[Middleware Pipeline]
    end
    
    subgraph "Business Layer"
        subgraph "CQRS Pattern"
            CMD[Commands]
            QRY[Queries] 
            MED[MediatR]
        end
        
        subgraph "Business Services"
            PAS[Plant Analysis Service]
            SBS[Subscription Service]
            IPS[Image Processing Service]
            AUS[Authentication Service]
            NOS[Notification Service]
        end
        
        subgraph "Domain Logic"
            ENT[Domain Entities]
            DTO[Data Transfer Objects]
            VAL[Validation Rules]
        end
    end
    
    subgraph "Infrastructure Layer"
        subgraph "Data Access"
            REP[Repository Pattern]
            EFC[Entity Framework Core]
            DBX[Database Context]
        end
        
        subgraph "External Services"
            N8N[N8N HTTP Client]
            RMQ[RabbitMQ Publisher]
            FIL[File Storage Clients]
            SMS[SMS/WhatsApp Clients]
        end
    end
    
    subgraph "Core Layer"
        subgraph "Cross-Cutting Concerns"
            IOC[IoC Container<br/>Autofac]
            LOG[Logging<br/>Serilog]
            SEC[Security Aspects]
            AOP[AOP Aspects]
        end
    end
    
    subgraph "Database"
        PG[(PostgreSQL<br/>Primary DB)]
        RD[(Redis Cache)]
    end

    %% Flow Connections
    API --> MW
    MW --> MED
    MED --> CMD
    MED --> QRY
    CMD --> PAS
    CMD --> SBS
    QRY --> PAS
    QRY --> SBS
    PAS --> IPS
    SBS --> AUS
    PAS --> NOS
    
    %% Service Dependencies
    PAS --> REP
    SBS --> REP
    REP --> EFC
    EFC --> DBX
    DBX --> PG
    
    %% External Dependencies
    PAS --> N8N
    PAS --> RMQ
    IPS --> FIL
    NOS --> SMS
    
    %% Cross-cutting
    IOC -.-> API
    LOG -.-> PAS
    SEC -.-> MW
    AOP -.-> CMD
    
    %% Cache
    SBS --> RD
    
    %% Styling
    classDef presentation fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef business fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef infrastructure fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef core fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    classDef database fill:#fce4ec,stroke:#c2185b,stroke-width:2px
    
    class API,MW presentation
    class CMD,QRY,MED,PAS,SBS,IPS,AUS,NOS,ENT,DTO,VAL business
    class REP,EFC,DBX,N8N,RMQ,FIL,SMS infrastructure
    class IOC,LOG,SEC,AOP core
    class PG,RD database
```

**Architecture Principles:**
- **Clean Architecture**: Clear separation of concerns across layers
- **CQRS Pattern**: Command/Query separation with MediatR
- **Dependency Inversion**: Abstract interfaces with concrete implementations
- **Cross-Cutting Concerns**: AOP aspects for logging, security, caching

---

## 3. Plant Analysis Processing Flow

**Purpose**: Core business process showing URL-based optimization and cost savings.

```mermaid
sequenceDiagram
    participant U as 👨‍🌾 Farmer
    participant A as 🌐 WebAPI
    participant S as 📊 Subscription Service
    participant I as 🖼️ Image Processing
    participant F as 🗃️ File Storage
    participant Q as 🐰 RabbitMQ
    participant W as ⚙️ Worker Service  
    participant N as 🤖 N8N AI Pipeline
    participant D as 🐘 Database

    Note over U,D: Plant Analysis Request Flow
    
    U->>A: POST /api/plantanalyses/analyze<br/>image: base64 (5MB)
    
    A->>S: Validate Subscription & Usage Quota
    alt No Active Subscription
        S-->>A: Error: No active subscription
        A-->>U: 403: Subscription required
    else Quota Exceeded  
        S-->>A: Error: Daily/Monthly limit reached
        A-->>U: 429: Quota exceeded
    else Valid Subscription
        S->>A: ✅ Subscription valid, quota available
        
        Note over A,I: URL-Based Processing (99.6% Token Reduction)
        A->>I: Optimize image for AI processing
        I->>I: Resize: 5MB → 100KB<br/>Quality: 85 → 70<br/>Format: PNG → JPEG
        I->>F: Upload optimized image
        F-->>I: Return public URL<br/>https://iili.io/abc123.jpg
        
        alt Synchronous Processing
            I->>N: Send image URL (not base64)<br/>Tokens: 1,500 vs 400,000<br/>Cost: $0.01 vs $12.00
            N-->>I: AI Analysis Response<br/>⚡ 10x faster processing
            I->>D: Save analysis result
            I-->>A: Return complete analysis
            A->>S: Increment usage counters
            A-->>U: 🎉 Analysis complete
            
        else Asynchronous Processing  
            A->>Q: Queue analysis request<br/>correlationId: async_123
            A-->>U: 📝 Request queued: async_123
            Q->>W: Consume message
            W->>N: Send image URL
            N-->>W: AI Analysis Response  
            W->>D: Save analysis result
            W->>S: Increment usage counters
            
            Note over W,U: Notification sent separately
        end
    end

    Note over U,D: Performance Impact
    Note right of N: Before: Base64 processing<br/>• 400,000 tokens<br/>• $12 per image<br/>• 20% success rate<br/>• Memory intensive
    Note right of N: After: URL processing<br/>• 1,500 tokens (99.6% ↓)<br/>• $0.01 per image (99.9% ↓)<br/>• 100% success rate<br/>• 10x faster
```

**Key Optimizations:**
- **Token Reduction**: 400,000 → 1,500 tokens (99.6% reduction)
- **Cost Savings**: $12 → $0.01 per image (99.9% reduction)  
- **Success Rate**: 20% → 100% (eliminated token limit errors)
- **Performance**: 10x faster processing with URL-based approach

---

## 4. Subscription Management Flow

**Purpose**: Complete user subscription lifecycle with tier management and usage tracking.

```mermaid
stateDiagram-v2
    [*] --> Registration
    
    Registration --> TrialSubscription : Auto-create Trial
    
    state TrialSubscription {
        [*] --> Active_Trial
        Active_Trial --> Trial_Usage : Daily usage (1/day, 30/month)
        Trial_Usage --> Active_Trial : Within limits
        Trial_Usage --> Trial_Quota_Exceeded : Limits reached
        Active_Trial --> Trial_Expired : 7 days elapsed
    }
    
    TrialSubscription --> PaidSubscription : Upgrade to S/M/L/XL
    
    state PaidSubscription {
        [*] --> Active_Paid
        
        state "Subscription Tiers" as Tiers {
            Small_S : S: 5 daily / 50 monthly - ₺99.99/month
            Medium_M : M: 20 daily / 200 monthly - ₺299.99/month  
            Large_L : L: 50 daily / 500 monthly - ₺599.99/month
            XL : XL: 200 daily / 2000 monthly - ₺1499.99/month
        }
        
        Active_Paid --> Usage_Validation : Each API request
        
        state Usage_Validation {
            [*] --> Check_Daily_Quota
            Check_Daily_Quota --> Check_Monthly_Quota : Daily OK
            Check_Monthly_Quota --> Allow_Request : Monthly OK
            Check_Daily_Quota --> Block_Request : Daily exceeded
            Check_Monthly_Quota --> Block_Request : Monthly exceeded
            Allow_Request --> Update_Counters
            Update_Counters --> Log_Usage
        }
        
        Usage_Validation --> Active_Paid : Request processed
        Active_Paid --> Renewal_Due : Monthly cycle
        
        state Renewal_Due {
            [*] --> Auto_Renewal_Check
            Auto_Renewal_Check --> Process_Payment : AutoRenew=true
            Auto_Renewal_Check --> Expired : AutoRenew=false
            Process_Payment --> Active_Paid : Payment successful
            Process_Payment --> Expired : Payment failed
        }
    }
    
    state Daily_Reset {
        [*] --> Reset_Daily_Counters : Midnight UTC
        Reset_Daily_Counters --> [*]
    }
    
    state Monthly_Reset {
        [*] --> Reset_Monthly_Counters : 1st of month
        Reset_Monthly_Counters --> [*]  
    }
    
    Trial_Expired --> [*]
    Trial_Quota_Exceeded --> PaidSubscription : Upgrade prompt
    Expired --> [*]
    
    PaidSubscription --> Daily_Reset : Scheduled job
    PaidSubscription --> Monthly_Reset : Scheduled job
```

**Subscription Features:**
- **Trial System**: Automatic 7-day trial with 1 daily analysis
- **Four Tiers**: Progressive limits from 5 to 200 daily requests
- **Real-Time Validation**: Usage checked before each API call
- **Automatic Resets**: Daily quotas reset at midnight, monthly on 1st
- **Revenue Model**: ₺99.99 to ₺1499.99 monthly pricing

---

## 5. Microservices Topology

**Purpose**: Production deployment architecture with scaling and fault tolerance.

```mermaid
graph TB
    subgraph "Load Balancer"
        LB[⚖️ Load Balancer<br/>HTTPS/SSL]
    end
    
    subgraph "WebAPI Cluster"
        API1[🌐 WebAPI Instance 1<br/>.NET 9.0]
        API2[🌐 WebAPI Instance 2<br/>.NET 9.0]  
        API3[🌐 WebAPI Instance 3<br/>.NET 9.0]
    end
    
    subgraph "Message Queue Infrastructure"
        RMQ1[🐰 RabbitMQ Primary<br/>plant-analysis-requests]
        RMQ2[🐰 RabbitMQ Replica<br/>Failover]
    end
    
    subgraph "Worker Service Cluster"
        WS1[⚙️ Worker Instance 1<br/>Background Processing]
        WS2[⚙️ Worker Instance 2<br/>Background Processing]
    end
    
    subgraph "Database Cluster"
        PG_M[(🐘 PostgreSQL Master<br/>Read/Write)]
        PG_S1[(🐘 PostgreSQL Replica 1<br/>Read Only)]
        PG_S2[(🐘 PostgreSQL Replica 2<br/>Read Only)]
    end
    
    subgraph "Cache Layer"
        RD_M[(⚡ Redis Master<br/>Session/Config Cache)]
        RD_S[(⚡ Redis Replica<br/>Failover)]
    end
    
    subgraph "Background Jobs"
        HF1[📊 Hangfire Dashboard 1<br/>Job Scheduling]
        HF2[📊 Hangfire Dashboard 2<br/>Job Processing]
    end
    
    subgraph "External Services"
        N8N[🤖 N8N AI Pipeline<br/>Load Balanced]
        FIH[🖼️ FreeImageHost<br/>CDN Global]
        SMS[📱 SMS/WhatsApp API<br/>Multi-Provider]
    end
    
    subgraph "Monitoring"
        MON[📊 Application Insights<br/>Health Checks]
        LOG[📝 Centralized Logging<br/>Elasticsearch/Serilog]
    end

    %% User Traffic Flow
    Users[👥 Users] --> LB
    LB --> API1
    LB --> API2  
    LB --> API3
    
    %% API Dependencies
    API1 --> RMQ1
    API2 --> RMQ1
    API3 --> RMQ1
    RMQ1 -.-> RMQ2
    
    API1 --> PG_M
    API2 --> PG_S1
    API3 --> PG_S2
    PG_M -.-> PG_S1
    PG_M -.-> PG_S2
    
    API1 --> RD_M
    API2 --> RD_M
    API3 --> RD_M
    RD_M -.-> RD_S
    
    %% Worker Service Flow
    RMQ1 --> WS1
    RMQ1 --> WS2
    WS1 --> N8N
    WS2 --> N8N
    WS1 --> PG_M
    WS2 --> PG_M
    WS1 --> FIH
    WS2 --> FIH
    
    %% Background Jobs
    API1 --> HF1
    WS1 --> HF2
    HF1 --> PG_M
    HF2 --> PG_M
    
    %% External Integrations
    API1 --> SMS
    WS1 --> SMS
    
    %% Monitoring
    API1 --> MON
    WS1 --> MON
    API1 --> LOG
    WS1 --> LOG
    
    %% Styling
    classDef webapi fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef worker fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef database fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef queue fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    classDef external fill:#fce4ec,stroke:#c2185b,stroke-width:2px
    classDef monitoring fill:#f1f8e9,stroke:#689f38,stroke-width:2px
    
    class API1,API2,API3 webapi
    class WS1,WS2 worker
    class PG_M,PG_S1,PG_S2,RD_M,RD_S database
    class RMQ1,RMQ2,HF1,HF2 queue
    class N8N,FIH,SMS external
    class MON,LOG monitoring
```

**Scaling & Reliability Features:**
- **Horizontal Scaling**: Multiple WebAPI and Worker instances
- **Database Replication**: Master-slave PostgreSQL with read replicas
- **Queue Redundancy**: RabbitMQ with failover capabilities
- **Cache Replication**: Redis master-replica for high availability
- **Load Distribution**: Smart routing based on service health

---

## 6. Data Architecture (ERD)

**Purpose**: Core business entities with relationships and constraints.

```mermaid
erDiagram
    %% User Management
    Users {
        int UserId PK
        string Email UK
        string FullName
        string MobilePhones
        string PasswordHash
        boolean Status
        datetime CreatedDate
        datetime UpdatedDate
    }
    
    Groups {
        int Id PK
        string GroupName UK
        datetime CreatedDate
    }
    
    UserGroups {
        int Id PK
        int UserId FK
        int GroupId FK
        datetime CreatedDate
    }
    
    OperationClaims {
        int Id PK
        string Name UK
        string Description
        datetime CreatedDate
    }
    
    UserClaims {
        int Id PK
        int UserId FK
        int ClaimId FK  
        datetime CreatedDate
    }
    
    %% Subscription System
    SubscriptionTiers {
        int Id PK
        string TierName UK "S,M,L,XL"
        string DisplayName
        int DailyRequestLimit
        int MonthlyRequestLimit
        decimal MonthlyPrice
        decimal YearlyPrice
        string Currency
        boolean IsActive
        datetime CreatedDate
    }
    
    UserSubscriptions {
        int Id PK
        int UserId FK
        int SubscriptionTierId FK
        datetime StartDate
        datetime EndDate
        boolean IsActive
        boolean AutoRenew
        int CurrentDailyUsage
        int CurrentMonthlyUsage
        string Status "Active,Expired,Cancelled,Suspended"
        string PaymentMethod
        string PaymentReference
        decimal PaidAmount
        boolean IsTrialSubscription
        datetime TrialEndDate
        datetime CreatedDate
        datetime UpdatedDate
    }
    
    SubscriptionUsageLogs {
        bigint Id PK
        int UserId FK
        int UserSubscriptionId FK
        int PlantAnalysisId FK
        string UsageType
        datetime UsageDate
        string RequestEndpoint
        string RequestMethod
        boolean IsSuccessful
        string ResponseStatus
        string ErrorMessage
        int QuotaUsed
        int QuotaLimit
        string IpAddress
        string UserAgent
        int ResponseTimeMs
        string RequestData
        datetime CreatedDate
    }
    
    %% Plant Analysis System
    PlantAnalyses {
        int Id PK
        int UserId FK
        int SponsorUserId FK
        int SponsorshipCodeId FK
        string FarmerId
        string CropType
        string Location
        text ImagePath
        text N8nResponse
        text PlantType
        text OverallHealth
        int OverallHealthScore
        text Diseases
        text Pests
        text ElementDeficiencies
        text Recommendations
        text StressIndicators
        text DiseaseSymptoms
        text NutrientStatus
        text TreatmentPlan
        text PreventiveMeasures
        string Status "Processing,Completed,Failed"
        datetime CreatedDate
        datetime UpdatedDate
    }
    
    %% Sponsorship System
    SponsorshipTiers {
        int Id PK
        string TierName
        string DisplayName
        decimal Price
        int AnalysisCount
        boolean IsActive
        datetime CreatedDate
    }
    
    SponsorshipPurchases {
        int Id PK
        int SponsorId FK
        int SubscriptionTierId FK
        int Quantity
        decimal TotalAmount
        string PaymentMethod
        string PaymentReference
        string CompanyName
        string InvoiceAddress
        string TaxNumber
        string Status
        datetime CreatedDate
    }
    
    SponsorshipCodes {
        int Id PK
        int PurchaseId FK
        int SponsorId FK
        string Code UK
        string FarmerName
        string FarmerPhone
        decimal Amount
        string Description
        boolean IsUsed
        int UsedByUserId FK
        datetime UsedDate
        datetime ExpiryDate
        datetime CreatedDate
    }
    
    %% Configuration System
    Configurations {
        int Id PK
        string Key UK
        string Value
        string Category
        string Description
        string DataType "string,int,decimal,bool"
        boolean IsActive
        datetime CreatedDate
        datetime UpdatedDate
    }

    %% Relationships
    Users ||--o{ UserGroups : "has roles"
    Groups ||--o{ UserGroups : "assigned to"
    Users ||--o{ UserClaims : "has permissions"
    OperationClaims ||--o{ UserClaims : "granted to"
    
    Users ||--o{ UserSubscriptions : "subscribes to"
    SubscriptionTiers ||--o{ UserSubscriptions : "tier type"
    UserSubscriptions ||--o{ SubscriptionUsageLogs : "usage tracking"
    
    Users ||--o{ PlantAnalyses : "requests analysis"
    Users ||--o{ PlantAnalyses : "sponsors analysis"
    SponsorshipCodes ||--o{ PlantAnalyses : "redeems for"
    PlantAnalyses ||--o{ SubscriptionUsageLogs : "consumes quota"
    
    Users ||--o{ SponsorshipPurchases : "purchases codes"
    SubscriptionTiers ||--o{ SponsorshipPurchases : "bulk tier"
    SponsorshipPurchases ||--o{ SponsorshipCodes : "generates codes"
    Users ||--o{ SponsorshipCodes : "redeems codes"
```

**Key Business Rules:**
- **User Authentication**: Email-based with role and claim-based authorization
- **Subscription Tiers**: Four tiers (S/M/L/XL) with progressive limits and pricing
- **Usage Tracking**: Real-time quota validation with detailed audit trails
- **Sponsorship Model**: Sponsors purchase bulk codes that farmers can redeem
- **Plant Analysis**: Core business entity with comprehensive AI response storage
- **Configuration**: Dynamic system settings with type-safe value retrieval

---

## 📊 Business Impact Summary

### Cost Optimization Achieved
- **Token Usage**: 99.6% reduction (400,000 → 1,500 tokens)
- **Processing Cost**: 99.9% reduction ($12 → $0.01 per image)
- **Success Rate**: Improved from 20% to 100%
- **Performance**: 10x faster processing with URL-based approach

### Architecture Benefits
- **Scalable Design**: Microservices with independent scaling
- **Fault Tolerance**: Redundancy and failover at every layer
- **Security**: Role-based access with comprehensive audit trails
- **Maintainability**: Clean Architecture with clear separation of concerns

### Business Value
- **Revenue Model**: Four-tier subscription system (₺99.99 - ₺1499.99/month)
- **Market Position**: Enterprise-grade AI plant analysis platform
- **Competitive Advantage**: Industry-leading cost optimization and performance
- **Growth Ready**: Architecture supports rapid scaling and feature expansion

---

*Generated: December 2024 | Version: 1.0 | Last Updated: Architecture analysis based on production codebase*