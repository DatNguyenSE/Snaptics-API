# 🏗️ SNAPTICS API - AWS CLOUD ARCHITECTURE

**Version:** 2.0  
**Last Updated:** July 31, 2026  
**Region:** ap-southeast-1 (Singapore)  
**Environment:** Production

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Complete Architecture Diagram](#complete-architecture-diagram)
3. [Security Layer (Critical)](#security-layer)
4. [Network Architecture](#network-architecture)
5. [Application Layer](#application-layer)
6. [Data Layer](#data-layer)
7. [SQS Asynchronous Processing](#sqs-architecture)
8. [Multi-Cloud Integration](#multi-cloud-integration)
9. [CI/CD Pipeline](#cicd-pipeline)
10. [Monitoring & Observability](#monitoring)
11. [Traffic Flow Details](#traffic-flow)
12. [Security Groups Configuration](#security-groups)

---

## 🎯 Overview

Snaptics API là hệ thống quản lý tài chính cá nhân được deploy trên AWS với:
- **Architecture Pattern:** Multi-tier, Microservices-ready
- **Deployment:** ECS Fargate (Serverless Containers)
- **Database:** RDS SQL Server với Multi-AZ replication
- **AI Integration:** Hybrid cloud (AWS + Azure + Google)

---

## 🏛️ Complete Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    INTERNET                                             │
│                              (Public Network)                                           │
└──────────────────────────────────────┬──────────────────────────────────────────────────┘
                                       │
                                       │ DNS Query
                                       ▼
                          ┌────────────────────────────┐
                          │      Route 53 (DNS)        │
                          │   snaptics-api.com         │
                          └──────────────┬─────────────┘
                                       │
                                       │ HTTP/HTTPS Traffic
                                       ▼
                          ┌────────────────────────────┐
                          │  AWS WAF                   │
                          │  (Web Application Firewall)│
                          │  • SQL Injection Block     │
                          │  • XSS Protection          │
                          │  • Rate Limiting           │
                          │  • Geo-blocking            │
                          └──────────────┬─────────────┘
                                       │
                                       ▼
               ╔══════════════════════════════════════════════╗
               ║         AWS Shield (DDoS Protection)         ║
               ║  ┌────────────────────────────────────┐      ║
               ║  │        CloudFront (CDN)            │      ║
               ║  │  • Global Edge Locations           │      ║
               ║  │  • SSL/TLS Termination             │      ║
               ║  │  • Cache Static Assets             │      ║
               ║  └──────────────┬─────────────────────┘      ║
               ╚═════════════════╪════════════════════════════╝
                                 │
                                 │ HTTPS (443)
                                 ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                    AWS VPC                                             │
│                              (Virtual Private Cloud)                                   │
│                            CIDR: 10.0.0.0/16                                           │
│                                                                                        │
│  ┌───────────────────────────────────────────────────────────────────────────────┐    │
│  │                           PUBLIC SUBNET (AZ-A)                                │    │
│  │                            CIDR: 10.0.1.0/24                                  │    │
│  │                                                                               │    │
│  │   ┌─────────────────────────────────────────────────────────────┐            │    │
│  │   │  Security Group: SG-ALB                                     │            │    │
│  │   │  Inbound Rules:                                             │            │    │
│  │   │   ✅ HTTPS (443) from CloudFront IPs only                   │            │    │
│  │   │   ✅ HTTP (80) redirect to 443                              │            │    │
│  │   │   ❌ All other traffic DENIED                               │            │    │
│  │   └───────────────────────────┬─────────────────────────────────┘            │    │
│  │                               ▼                                              │    │
│  │                  ┌─────────────────────────────┐                             │    │
│  │                  │  Application Load Balancer  │                             │    │
│  │                  │  • Health Check: /health    │                             │    │
│  │                  │  • Target: Fargate Tasks    │                             │    │
│  │                  │  • Sticky Sessions          │                             │    │
│  │                  └──────────────┬──────────────┘                             │    │
│  └──────────────────────────────────┼─────────────────────────────────────────────┘  │
│                                     │                                                │
│  ┌───────────────────────────────────────────────────────────────────────────────┐    │
│  │                           PUBLIC SUBNET (AZ-B)                                │    │
│  │                            CIDR: 10.0.2.0/24                                  │    │
│  │                                                                               │    │
│  │                  ┌─────────────────────────────┐                             │    │
│  │                  │      NAT Gateway (AZ-A)     │                             │    │
│  │                  │  • Elastic IP attached      │                             │    │
│  │                  │  • High availability        │                             │    │
│  │                  └──────────────┬──────────────┘                             │    │
│  └──────────────────────────────────┼─────────────────────────────────────────────┘  │
│                                     │                                                │
│                                     │ Outbound Internet Access                       │
│                                     ▼                                                │
│  ┌───────────────────────────────────────────────────────────────────────────────┐    │
│  │                          PRIVATE SUBNET (AZ-A)                                │    │
│  │                            CIDR: 10.0.10.0/24                                 │    │
│  │                                                                               │    │
│  │   ┌────────────────────────────────────────────────────────┐                 │    │
│  │   │  Security Group: SG-Fargate                            │                 │    │
│  │   │  Inbound Rules:                                        │                 │    │
│  │   │   ✅ Port 8080 from SG-ALB only                        │                 │    │
│  │   │   ❌ No direct internet access                         │                 │    │
│  │   │  Outbound Rules:                                       │                 │    │
│  │   │   ✅ HTTPS (443) to NAT Gateway                        │                 │    │
│  │   │   ✅ Port 1433 to SG-RDS                               │                 │    │
│  │   └──────────────────────────┬─────────────────────────────┘                 │    │
│  │                              ▼                                               │    │
│  │           ┌──────────────────────────────────────────────────┐                │    │
│  │           │         ECS Fargate (Auto Scaling)           │                │    │
│  │           │                                              │                │    │
│  │           │  ┌─────────────┐    ┌─────────────┐         │                │    │
│  │           │  │  Task 1     │    │  Task 2     │         │                │    │
│  │           │  │  (API)      │    │  (API)      │         │                │    │
│  │           │  │  Port: 8080 │    │  Port: 8080 │         │                │    │
│  │           │  └─────────────┘    └─────────────┘         │                │    │
│  │           │                                              │                │    │
│  │           │  ┌─────────────┐                            │                │    │
│  │           │  │  Task 3     │                            │                │    │
│  │           │  │(Background) │◄───── SQS Consumer         │                │    │
│  │           │  │  Service    │                            │                │    │
│  │           │  └─────────────┘                            │                │    │
│  │           │                                              │                │    │
│  │           │  Auto Scaling Policy:                        │                │    │
│  │           │  • Min: 2 tasks, Max: 10 tasks              │                │    │
│  │           │  • Trigger: CPU > 70% or Memory > 80%       │                │    │
│  │           └──────────┬───────────────┬───────────────────┘                │    │
│  │                      │               │                                    │    │
│  │                      │               │ Fetch Secrets                      │    │
│  │                      │               └────────────────────┐               │    │
│  └──────────────────────┼──────────────────────────────────────┼─────────────┘  │
│                         │                                     │                  │
│  ┌───────────────────────────────────────────────────────────────────────────────┐    │
│  │                          PRIVATE SUBNET (AZ-B)                                │    │
│  │                            CIDR: 10.0.11.0/24                                 │    │
│  │                                                                               │    │
│  │   ┌────────────────────────────────────────────────────────┐                 │    │
│  │   │  Security Group: SG-RDS                                │                 │    │
│  │   │  Inbound Rules:                                        │                 │    │
│  │   │   ✅ Port 1433 (SQL Server) from SG-Fargate only      │                 │    │
│  │   │   ❌ No public access                                  │                 │    │
│  │   │   ❌ No direct internet                                │                 │    │
│  │   └──────────────────────────┬─────────────────────────────┘                 │    │
│  │                              ▼                                               │    │
│  │           ┌──────────────────────────────────────────────────┐               │    │
│  │           │      RDS SQL Server (Multi-AZ)                  │               │    │
│  │           │                                                  │               │    │
│  │           │  ┌──────────────┐      ┌──────────────┐         │               │    │
│  │           │  │   Primary    │─────→│   Replica    │         │               │    │
│  │           │  │   (AZ-A)     │ sync │   (AZ-B)     │         │               │    │
│  │           │  │ Read/Write   │      │  Read Only   │         │               │    │
│  │           │  └──────────────┘      └──────────────┘         │               │    │
│  │           │                                                  │               │    │
│  │           │  • Automated Backups (7 days retention)         │               │    │
│  │           │  • Encryption at Rest (KMS)                     │               │    │
│  │           │  • Hangfire Jobs Database                       │               │    │
│  │           └──────────────────────────────────────────────────┘               │    │
│  └───────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘

         ┌──────────────────────────────────────────────────────────────┐
         │              AWS MANAGED SERVICES                            │
         │          (Outside VPC - Managed by AWS)                      │
         │                                                              │
         │  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐        │
         │  │     S3      │  │    SQS      │  │     SNS      │        │
         │  │   Bucket    │  │   Queue     │  │    Topic     │        │
         │  │             │  │             │  │              │        │
         │  │ • Images    │  │ • AI Tasks  │  │ • Alerts     │        │
         │  │ • Receipts  │  │ • DLQ       │  │ • Monitoring │        │
         │  └──────┬──────┘  └──────┬──────┘  └──────┬───────┘        │
         │         │                │                │                 │
         └─────────┼────────────────┼────────────────┼─────────────────┘
                   │                │                │
                   │                │                │
         ┌─────────┴────────────────┴────────────────┴─────────────────┐
         │          MANAGEMENT & OBSERVABILITY                          │
         │                                                              │
         │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
         │  │  CloudWatch  │  │   Secrets    │  │  Parameter   │      │
         │  │              │  │   Manager    │  │    Store     │      │
         │  │ • Logs       │  │              │  │              │      │
         │  │ • Metrics    │  │ • DB Pass    │  │ • App Config │      │
         │  │ • Alarms     │  │ • API Keys   │  │              │      │
         │  │ • Dashboards │  │ • JWT Secret │  │              │      │
         │  └──────────────┘  └──────────────┘  └──────────────┘      │
         │                                                              │
         │  ┌──────────────┐  ┌──────────────┐                         │
         │  │   X-Ray      │  │     ECR      │                         │
         │  │  (Tracing)   │  │ (Container   │                         │
         │  │              │  │  Registry)   │                         │
         │  └──────────────┘  └──────────────┘                         │
         └──────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│                      EXTERNAL SERVICES (Multi-Cloud)                           │
│                         (Outside AWS - HTTPS APIs)                             │
│                                                                                │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐    │
│  │   Google Cloud (GCP) │  │   Azure Cloud        │  │   Gmail SMTP     │    │
│  │                      │  │                      │  │                  │    │
│  │  ┌────────────────┐  │  │  ┌────────────────┐ │  │  Port: 587       │    │
│  │  │  Gemini Flash  │  │  │  │  Document      │ │  │  TLS Encryption  │    │
│  │  │  Lite API      │  │  │  │  Intelligence  │ │  │                  │    │
│  │  │  (Vision AI)   │  │  │  │  (OCR)         │ │  └──────────────────┘    │
│  │  └────────────────┘  │  │  └────────────────┘ │                           │
│  │                      │  │                      │                           │
│  │  Purpose:            │  │  ┌────────────────┐ │                           │
│  │  • Analyze Images    │  │  │  Azure OpenAI  │ │                           │
│  │  • Detect Objects    │  │  │  GPT-4o-mini   │ │                           │
│  │  • Price Estimation  │  │  │  (Chat)        │ │                           │
│  │                      │  │  └────────────────┘ │                           │
│  └──────────────────────┘  └──────────────────────┘                           │
│                                                                                │
│  🔐 API Keys stored in AWS Secrets Manager                                    │
│  🔄 Retry Logic: 3 attempts with exponential backoff                          │
│  ⏱️ Timeout: 30 seconds per request                                           │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔒 Security Layer (Critical)

### 1. AWS WAF (Web Application Firewall)

**Vị trí:** Đặt trước CloudFront

**Rules được áp dụng:**
- ✅ **SQL Injection Protection** - Block patterns: `' OR '1'='1`, `UNION SELECT`, etc.
- ✅ **XSS Protection** - Block `<script>`, `javascript:`, `onerror=` patterns
- ✅ **Rate Limiting** - 2000 requests per 5 minutes per IP
- ✅ **Geo-blocking** - Allow only specific countries (configurable)
- ✅ **Known Bot Protection** - Block bad bots using AWS Managed Rules

**Cách vẽ:**
```
┌────────────────────┐
│      AWS WAF       │
│  ┌──────────────┐  │
│  │ Rule: SQL    │  │
│  │ Rule: XSS    │  │
│  │ Rule: Rate   │  │
│  └──────────────┘  │
└────────────────────┘
         │
         ▼ [Mũi tên màu đỏ, nét liền]
    CloudFront
```

---

### 2. AWS Shield

**Loại:** Shield Standard (Free) - đã được enable mặc định

**Chức năng:**
- Tự động chống DDoS attacks (Layer 3, 4, 7)
- Protection cho CloudFront và Route 53
- Real-time attack visibility

**Cách vẽ:** Vẽ như "lớp bao bọc" xung quanh CloudFront
```
┌────────────────────────┐
│    AWS Shield          │ ← Label ở trên
│  ╔══════════════════╗  │
│  ║   CloudFront     ║  │
│  ╚══════════════════╝  │
└────────────────────────┘
```

**Không có mũi tên** - Shield là protection layer, không phải traffic flow

---

### 3. Security Groups Configuration

#### SG-ALB (Application Load Balancer Security Group)

```
┌─────────────────────────────────────────────────────┐
│  Security Group: SG-ALB                             │
│  ID: sg-0abc123def456                               │
│                                                     │
│  INBOUND RULES:                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Source          │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ HTTPS   │ 443   │ CloudFront IPs  │ Allow   │   │
│  │ HTTP    │ 80    │ CloudFront IPs  │ Redirect│   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  OUTBOUND RULES:                                    │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Destination     │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ Custom  │ 8080  │ SG-Fargate      │ Forward │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**Mũi tên:**
- CloudFront → [🔒 màu xanh, nét liền] → SG-ALB → ALB

---

#### SG-Fargate (ECS Fargate Security Group)

```
┌─────────────────────────────────────────────────────┐
│  Security Group: SG-Fargate                         │
│  ID: sg-0def456ghi789                               │
│                                                     │
│  INBOUND RULES:                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Source          │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ Custom  │ 8080  │ SG-ALB          │ API     │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  OUTBOUND RULES:                                    │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Destination     │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ HTTPS   │ 443   │ 0.0.0.0/0       │ APIs    │   │
│  │ Custom  │ 1433  │ SG-RDS          │ Database│   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**Mũi tên:**
- ALB → [🔒 màu xanh, nét liền] → SG-Fargate → Fargate Tasks

---
#### SG-RDS (Database Security Group)

```
┌─────────────────────────────────────────────────────┐
│  Security Group: SG-RDS                             │
│  ID: sg-0ghi789jkl012                               │
│                                                     │
│  INBOUND RULES:                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Source          │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ MSSQL   │ 1433  │ SG-Fargate      │ Only App│   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  OUTBOUND RULES:                                    │
│  ┌─────────────────────────────────────────────┐   │
│  │ Type    │ Port  │ Destination     │ Purpose │   │
│  ├─────────────────────────────────────────────┤   │
│  │ All     │ All   │ DENY            │ No Out  │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  🚫 NO PUBLIC ACCESS                                │
│  🚫 NO INTERNET ACCESS                              │
└─────────────────────────────────────────────────────┘
```

**Mũi tên:**
- Fargate → [🔒 màu xanh với database icon] → SG-RDS → RDS

---

### 4. AWS Secrets Manager

**Vị trí:** Management & Observability section

**Secrets được lưu trữ:**
```
┌─────────────────────────────────────────────┐
│        AWS Secrets Manager                  │
│                                             │
│  Secrets:                                   │
│  ├─ snaptics/db/connection-string           │
│  ├─ snaptics/jwt/token-key                  │
│  ├─ snaptics/gemini/api-key                 │
│  ├─ snaptics/azure/doc-intel-key            │
│  ├─ snaptics/azure/openai-key               │
│  ├─ snaptics/email/password                 │
│  └─ snaptics/aws/access-keys                │
│                                             │
│  Features:                                  │
│  ✅ Automatic rotation (30 days)            │
│  ✅ Encryption at rest (KMS)                │
│  ✅ Audit logging (CloudTrail)              │
│  ✅ Version history                         │
└─────────────────────────────────────────────┘
```

**Mũi tên:**
```
Fargate Tasks → [⚙️ màu xám, nét chấm] → Secrets Manager
(Label: "Fetch secrets at container startup")
```

---

## 📡 SQS Architecture (Asynchronous Processing)

### Complete SQS Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                       SQS ASYNCHRONOUS AI PROCESSING                                │
│                                                                                     │
│                                                                                     │
│  [1] User uploads image via API                                                    │
│       │                                                                             │
│       ▼                                                                             │
│  ┌─────────────────┐                                                               │
│  │ Fargate API     │  POST /ai/analyze                                             │
│  │ (Producer)      │                                                               │
│  └────────┬────────┘                                                               │
│           │                                                                         │
│           │ (1) Upload to S3                                                       │
│           ▼                                                                         │
│  ┌─────────────────┐                                                               │
│  │   S3 Bucket     │  s3-bucket-snaptics/images/{userId}/{filename}                │
│  └────────┬────────┘                                                               │
│           │                                                                         │
│           │ (2) Send message to SQS                                                │
│           ▼                                                                         │
│  ┌──────────────────────────────────────────────────────┐                          │
│  │              SQS Queue                               │                          │
│  │         Name: snaptics-ai-queue                      │                          │
│  │                                                      │                          │
│  │  Message Format:                                     │                          │
│  │  {                                                   │                          │
│  │    "TaskType": "AnalyzeImage",                       │                          │
│  │    "S3ObjectKey": "images/123/receipt.jpg",          │                          │
│  │    "UserId": "123",                                  │                          │
│  │    "ContentType": "image/jpeg",                      │                          │
│  │    "EstimatePrice": true                             │                          │
│  │  }                                                   │                          │
│  │                                                      │                          │
│  │  Settings:                                           │                          │
│  │  • Visibility Timeout: 60 seconds                    │                          │
│  │  • Message Retention: 4 days                         │
│  │  • Max Receive Count: 3                              │                          │
│  └────────┬───────────────────────────────────┬─────────┘                          │
│           │                                   │ (Failed after 3 retries)           │
│           │                                   ▼                                    │
│           │                          ┌─────────────────┐                           │
│           │                          │  Dead Letter    │                           │
│           │                          │  Queue (DLQ)    │                           │
│           │                          │  Name: snaptics │                           │
│           │                          │  -ai-queue-dlq  │                           │
│           │                          └─────────────────┘                           │
│           │ (3) Long polling (10 seconds)                                          │
│           ▼                                                                         │
│  ┌─────────────────────────────────┐                                               │
│  │  SqsConsumerService             │                                               │
│  │  (Background Service)           │                                               │
│  │                                 │                                               │
│  │  while (!stoppingToken)         │                                               │
│  │  {                              │                                               │
│  │    ReceiveMessageAsync()        │                                               │
│  │    ProcessMessageAsync()        │                                               │
│  │    DeleteMessageAsync()         │                                               │
│  │  }                              │                                               │
│  └───────────┬─────────────────────┘                                               │
│              │                                                                     │
│              │ (4) Download image from S3                                          │
│              ▼                                                                     │
│         [S3 Service]                                                               │
│              │                                                                     │
│              │ (5) Call AI APIs                                                    │
│              ▼                                                                     │
│    ┌─────────────────────┐                                                        │
│    │  External AI APIs   │                                                        │
│    │  • Gemini (Vision)  │                                                        │
│    │  • Azure Doc Intel  │                                                        │
│    └─────────┬───────────┘                                                        │
│              │ (6) AI Response                                                    │
│              ▼                                                                     │
│    ┌─────────────────────┐                                                        │
│    │  SignalR Hub        │                                                        │
│    │  NotificationHub    │                                                        │
│    └─────────┬───────────┘                                                        │
│              │ (7) Push result to user's browser                                  │
│              ▼                                                                     │
│         [User Browser]                                                             │
│          WebSocket connection                                                      │
│          Receives: { result: {...}, status: "completed" }                         │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Mũi tên chi tiết cho SQS:

1. **Fargate API → S3**
   - Màu: Cam
   - Kiểu: Nét liền
   - Label: "1. Upload image"

2. **Fargate API → SQS Queue**
   - Màu: Xanh dương
   - Kiểu: Nét liền
   - Label: "2. SendMessage (task metadata)"

3. **SQS Queue → SqsConsumerService**
   - Màu: Tím
   - Kiểu: Mũi tên 2 chiều (←→)
   - Label: "3. ReceiveMessage (Long poll 10s)"

4. **SqsConsumerService → S3**
   - Màu: Cam
   - Kiểu: Nét chấm
   - Label: "4. Download image"

5. **SqsConsumerService → External AI**
   - Màu: Xanh lá
   - Kiểu: Nét chấm (ra ngoài VPC)
   - Label: "5. AI Processing"

6. **SqsConsumerService → SignalR Hub**
   - Màu: Đỏ
   - Kiểu: Nét liền
   - Label: "6. Push result"

7. **SQS → DLQ**
   - Màu: Đỏ
   - Kiểu: Nét đứt
   - Label: "Failed after 3 retries"

---

## 🌐 Multi-Cloud Integration

### External Services Architecture

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                          MULTI-CLOUD ARCHITECTURE                                │
│                                                                                  │
│                                                                                  │
│                     ┌────────────────────────────────┐                           │
│                     │    AWS VPC (Private Subnet)    │                           │
│                     │                                │                           │
│                     │    ┌──────────────────┐        │                           │
│                     │    │  Fargate Tasks   │        │                           │
│                     │    └────────┬─────────┘        │                           │
│                     │             │                  │                           │
│                     │             ▼                  │                           │
│                     │    ┌──────────────────┐        │                           │
│                     │    │  NAT Gateway     │        │                           │
│                     │    └────────┬─────────┘        │                           │
│                     │             │                  │                           │
│                     └─────────────┼──────────────────┘                           │
│                                   │                                              │
│                                   │ HTTPS (TLS 1.3)                              │
│                                   ▼                                              │
│                        ┌──────────────────┐                                      │
│                        │ Internet Gateway │                                      │
│                        └────────┬─────────┘                                      │
│                                 │                                                │
│              ┌──────────────────┼──────────────────┐                             │
│              │                  │                  │                             │
│              ▼                  ▼                  ▼                             │
│   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐                  │
│   │  Google Cloud   │ │   Azure Cloud   │ │   Gmail SMTP    │                  │
│   │      (GCP)      │ │                 │ │                 │                  │
│   └─────────────────┘ └─────────────────┘ └─────────────────┘                  │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### 1. Google Gemini AI (Vision API)

**Endpoint:** `https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest`

**Use Cases:**
- Analyze receipt/invoice images
- Detect items and prices
- Estimate total amount
- Category classification

**Request Flow:**
```
Fargate → NAT → Internet → GCP
```

**Mũi tên:**
- Màu: Xanh lá (#34A853 - Google brand color)
- Kiểu: Nét chấm (......→)
- Label: "AI Image Analysis"

**Configuration:**
```json
{
  "ApiKey": "from Secrets Manager",
  "Model": "gemini-flash-lite-latest",
  "MaxTokens": 2048,
  "Temperature": 0.3
}
```

---
### 2. Azure Document Intelligence (OCR)

**Endpoint:** `https://snaptics-doc-intel.cognitiveservices.azure.com/`

**Use Cases:**
- Extract text from bills/receipts
- Parse structured data
- Table extraction
- Multi-language support

**Request Flow:**
```
Fargate → NAT → Internet → Azure
```

**Mũi tên:**
- Màu: Xanh dương (#0078D4 - Azure brand color)
- Kiểu: Nét chấm (......→)
- Label: "OCR Document Processing"

**Configuration:**
```json
{
  "Endpoint": "https://snaptics-doc-intel.cognitiveservices.azure.com/",
  "ApiKey": "from Secrets Manager",
  "ApiVersion": "2023-07-31"
}
```

---

### 3. Azure OpenAI (GPT-4o-mini)

**Endpoint:** `https://models.inference.ai.azure.com/chat/completions`

**Use Cases:**
- AI Assistant chatbot
- Budget advice
- Financial tips
- Natural language queries

**Request Flow:**
```
Fargate → NAT → Internet → Azure OpenAI
```

**Mũi tên:**
- Màu: Tím (#8B5CF6 - OpenAI brand color)
- Kiểu: Nét chấm (......→)
- Label: "AI Chat Assistant"

**Configuration:**
```json
{
  "Endpoint": "https://models.inference.ai.azure.com/chat/completions",
  "ApiKey": "from Secrets Manager",
  "ModelName": "gpt-4o-mini",
  "MaxTokens": 1024
}
```

---

### 4. Gmail SMTP (Email Notifications)

**Server:** `smtp.gmail.com:587`

**Use Cases:**
- Welcome emails
- Password reset
- Budget alerts
- Monthly reports

**Request Flow:**
```
Fargate → NAT → Internet → Gmail SMTP
```

**Mũi tên:**
- Màu: Đỏ (#EA4335 - Gmail brand color)
- Kiểu: Nét chấm (......→)
- Label: "Email Notifications"

**Configuration:**
```json
{
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Email": "from Secrets Manager",
  "Password": "from Secrets Manager"
}
```

---

### Security Best Practices cho External APIs:

```
┌────────────────────────────────────────────────────┐
│  External API Security Checklist                  │
│                                                    │
│  ✅ All API keys stored in Secrets Manager         │
│  ✅ TLS 1.3 encryption for all connections        │
│  ✅ Retry logic with exponential backoff          │
│  ✅ Circuit breaker pattern implemented           │
│  ✅ Request timeout: 30 seconds                   │
│  ✅ Rate limiting per API                         │
│  ✅ Error logging to CloudWatch                   │
│  ✅ Cost tracking per API call                    │
└────────────────────────────────────────────────────┘
```

---

## 🚀 CI/CD Pipeline (GitHub Actions)

### Complete CI/CD Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           CI/CD PIPELINE - GITHUB ACTIONS                           │
│                                                                                     │
│                                                                                     │
│  [Developer] 👨‍💻                                                                     │
│       │                                                                             │
│       │ (1) git add .                                                              │
│       │     git commit -m "feature: add new endpoint"                              │
│       │     git push origin main                                                   │
│       ▼                                                                             │
│  ┌──────────────────────┐                                                          │
│  │   GitHub Repository  │                                                          │
│  │   Snaptics-API       │                                                          │
│  │                      │                                                          │
│  │  Branches:           │                                                          │
│  │  ├─ main (protected) │                                                          │
│  │  ├─ develop          │                                                          │
│  │  └─ feature/*        │                                                          │
│  └──────────┬───────────┘                                                          │
│             │                                                                       │
│             │ (2) Webhook trigger on push                                          │
│             ▼                                                                       │
│  ┌─────────────────────────────────────────────────────────┐                       │
│  │         GitHub Actions Runner (ubuntu-latest)           │                       │
│  │                                                         │                       │
│  │  Step 1: Checkout Code                                  │                       │
│  │  └─ uses: actions/checkout@v4                           │                       │
│  │                                                         │                       │
│  │  Step 2: Configure AWS Credentials                      │                       │
│  │  └─ uses: aws-actions/configure-aws-credentials@v4     │                       │
│  │     with:                                               │                       │
│  │       aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY }}  │                       │
│  │       aws-region: ap-southeast-1                        │                       │
│  │                                                         │                       │
│  │  Step 3: Login to Amazon ECR                            │                       │
│  │  └─ uses: aws-actions/amazon-ecr-login@v2              │                       │
│  │                                                         │                       │
│  │  Step 4: Build Docker Image                             │                       │
│  │  └─ docker build -t snaptics-api:latest .              │                       │
│  │     • Multi-stage build (SDK → Runtime)                 │                       │
│  │     • Layer caching enabled                             │                       │
│  │     • Build time: ~3-4 minutes                          │                       │
│  │                                                         │                       │
│  │  Step 5: Tag & Push to ECR                              │                       │
│  │  └─ docker tag snaptics-api:latest                      │                       │
│  │       769137004257.dkr.ecr.ap-southeast-1.              │                       │
│  │       amazonaws.com/snaptics-api:latest                 │                       │
│  │     docker push ...                                     │                       │
│  │     • Push time: ~2-3 minutes                           │                       │
│  │                                                         │                       │
│  │  Step 6: Update ECS Service                             │                       │
│  │  └─ aws ecs update-service                              │                       │
│  │       --cluster Snaptics-Cluster                        │                       │
│  │       --service snaptics-backend-service                │                       │
│  │       --force-new-deployment                            │                       │
│  │                                                         │                       │
│  └─────────────┬───────────────────────────────────────────┘                       │
│                │                                                                   │
│                │ (3) docker push                                                   │
│                ▼                                                                   │
│  ┌──────────────────────────────────┐                                              │
│  │  Amazon ECR (Container Registry) │                                              │
│  │                                  │                                              │
│  │  Repository: snaptics-api        │                                              │
│  │  Latest tag: <commit-sha>        │                                              │
│  │  Image size: ~350 MB             │                                              │
│  │                                  │                                              │
│  │  Lifecycle Policy:               │                                              │
│  │  • Keep last 10 images           │                                              │
│  │  • Auto-delete untagged images   │                                              │
│  └──────────────┬───────────────────┘                                              │
│                 │                                                                  │
│                 │ (4) ECS pulls new image                                          │
│                 ▼                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐                 │
│  │         ECS Fargate - Rolling Update                         │                 │
│  │                                                              │                 │
│  │  Current State (Before Deploy):                              │                 │
│  │  ┌──────┐  ┌──────┐                                          │                 │
│  │  │Task 1│  │Task 2│  (Old version)                           │                 │
│  │  └──────┘  └──────┘                                          │                 │
│  │                                                              │                 │
│  │  Rolling Update Process:                                     │                 │
│  │  ───────────────────────────────────────────────────────────│                 │
│  │                                                              │                 │
│  │  Phase 1: Start new task                                     │                 │
│  │  ┌──────┐  ┌──────┐  ┌──────┐                                │                 │
│  │  │Task 1│  │Task 2│  │Task 3│ ← New version starting         │                 │
│  │  └──────┘  └──────┘  └──┬───┘                                │                 │
│  │                         │                                    │                 │
│  │                         └─ Health check (30s)                │                 │
│  │                            • HTTP GET /health                │                 │
│  │                            • Expected: 200 OK                │                 │
│  │                                                              │                 │
│  │  Phase 2: Register with ALB                                  │                 │
│  │  ┌──────┐  ┌──────┐  ┌──────┐                                │                 │
│  │  │Task 1│  │Task 2│  │Task 3│ ✅ Healthy                      │                 │
│  │  └──────┘  └──────┘  └──────┘                                │                 │
│  │     │        │          │                                    │                 │
│  │     └────────┴──────────┴─→ ALB routes traffic               │                 │
│  │                                                              │                 │
│  │  Phase 3: Stop old tasks                                     │                 │
│  │             ┌──────┐  ┌──────┐                                │                 │
│  │             │Task 2│  │Task 3│ (All new version)             │                 │
│  │             └──────┘  └──────┘                                │                 │
│  │                                                              │                 │
│  │  ✅ Zero downtime deployment                                 │                 │
│  │  ⏱️ Total deployment time: ~3-5 minutes                      │                 │
│  └──────────────────────────────────────────────────────────────┘                 │
│                                                                                   │
│                                                                                   │
│  📊 Deployment Metrics:                                                           │
│  ┌─────────────────────────────────────────────────────────────┐                 │
│  │ Total pipeline time: ~5-7 minutes                           │                 │
│  │ Build time: ~3-4 min                                        │                 │
│  │ Push to ECR: ~2-3 min                                       │                 │
│  │ ECS deployment: ~3-5 min                                    │                 │
│  │ Success rate: 95%+                                          │                 │
│  └─────────────────────────────────────────────────────────────┘                 │
│                                                                                   │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Mũi tên chi tiết cho CI/CD:

1. **Developer → GitHub**
   - Màu: Xám (#24292e - GitHub brand)
   - Kiểu: Nét liền (────→)
   - Label: "git push origin main"

2. **GitHub → GitHub Actions**
   - Màu: Xanh dương (#2088FF)
   - Kiểu: Nét liền (────→)
   - Label: "Webhook trigger (on push)"

3. **GitHub Actions → ECR**
   - Màu: Cam (#FF9900 - AWS brand)
   - Kiểu: Nét liền (────→)
   - Label: "docker push (latest tag)"

4. **GitHub Actions → ECS**
   - Màu: Xanh lá (#1E8900)
   - Kiểu: Nét liền (────→)
   - Label: "aws ecs update-service --force-new-deployment"

5. **ECS → ECR**
   - Màu: Tím (#8B5CF6)
   - Kiểu: Nét chấm (......→)
   - Label: "Pull new image"

6. **ECS → ALB**
   - Màu: Xanh dương
   - Kiểu: Nét chấm (......→)
   - Label: "Register new target"

---

### Rollback Strategy

```
┌────────────────────────────────────────────────────┐
│             ROLLBACK PROCEDURE                     │
│                                                    │
│  Manual Rollback:                                  │
│  1. Go to ECR Console                              │
│  2. Find previous image tag                        │
│  3. Update ECS Task Definition                     │
│  4. Force new deployment with old tag              │
│                                                    │
│  OR use AWS CLI:                                   │
│  aws ecs update-service \                          │
│    --cluster Snaptics-Cluster \                    │
│    --service snaptics-backend-service \            │
│    --task-definition snaptics-api:123 \            │
│    --force-new-deployment                          │
│                                                    │
│  ⏱️ Rollback time: ~2-3 minutes                    │
└────────────────────────────────────────────────────┘
```

**Thêm mũi tên rollback:**
- GitHub Actions ←─ [nút đỏ] ─← "Manual Rollback"
- Màu: Đỏ (#DC2626)
- Kiểu: Nét đứt (←- -←)
- Label: "Rollback to previous stable version"

---

## 📊 Monitoring & Observability

### CloudWatch Logs & Metrics

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          MONITORING ARCHITECTURE                                    │
│                                                                                     │
│  ┌────────────────┐                                                                 │
│  │  Fargate Tasks │                                                                 │
│  └────────┬───────┘                                                                 │
│           │                                                                         │
│           │ (1) Application logs (stdout/stderr)                                   │
│           ▼                                                                         │
│  ┌──────────────────────────────────────────────────┐                              │
│  │         CloudWatch Logs                          │                              │
│  │                                                  │                              │
│  │  Log Groups:                                     │                              │
│  │  ├─ /ecs/snaptics-api-logs                       │                              │
│  │  ├─ /aws/ecs/fargate/snaptics                    │                              │
│  │  └─ /aws/lambda/* (if using Lambda)              │                              │
│  │                                                  │                              │
│  │  Log Streams: One per task                       │                              │
│  │  Retention: 7 days                               │                              │
│  │  Size: ~500 MB/day                               │                              │
│  └──────────────────┬───────────────────────────────┘                              │
│                     │                                                               │
│                     │ (2) Metrics extraction                                        │
│                     ▼                                                               │
│  ┌──────────────────────────────────────────────────┐                              │
│  │         CloudWatch Metrics                       │                              │
│  │                                                  │                              │
│  │  Custom Metrics:                                 │                              │
│  │  ├─ API_Request_Count                            │                              │
│  │  ├─ API_Response_Time_ms                         │                              │
│  │  ├─ AI_Processing_Time_ms                        │                              │
│  │  ├─ SQS_Message_Age_seconds                      │                              │
│  │  ├─ Database_Connection_Pool_Size                │                              │
│  │  └─ Error_Rate_5xx                               │                              │
│  │                                                  │                              │
│  │  AWS Metrics (auto-collected):                   │                              │
│  │  ├─ ECS: CPUUtilization, MemoryUtilization       │                              │
│  │  ├─ ALB: TargetResponseTime, HTTPCode_Target_5XX │                              │
│  │  ├─ RDS: CPUUtilization, DatabaseConnections     │                              │
│  │  └─ SQS: NumberOfMessagesSent, ApproximateAge    │                              │
│  └──────────────────┬───────────────────────────────┘                              │
│                     │                                                               │
│                     │ (3) Trigger alarms                                            │
│                     ▼                                                               │
│  ┌──────────────────────────────────────────────────┐                              │
│  │         CloudWatch Alarms                        │                              │
│  │                                                  │                              │
│  │  Critical Alarms (P0):                           │                              │
│  │  ┌────────────────────────────────────────────┐  │                              │
│  │  │ Name: High-Error-Rate                      │  │                              │
│  │  │ Metric: Error_Rate_5xx > 5%                │  │                              │
│  │  │ Evaluation: 2 consecutive periods (2 min)  │  │                              │
│  │  │ Action: SNS → PagerDuty → On-call engineer │  │                              │
│  │  └────────────────────────────────────────────┘  │                              │
│  │                                                  │                              │
│  │  ┌────────────────────────────────────────────┐  │                              │
│  │  │ Name: RDS-CPU-High                         │  │                              │
│  │  │ Metric: RDS CPUUtilization > 80%           │  │                              │
│  │  │ Evaluation: 3 consecutive periods (3 min)  │  │                              │
│  │  │ Action: SNS → Email + Slack                │  │                              │
│  │  └────────────────────────────────────────────┘  │                              │
│  │                                                  │                              │
│  │  Warning Alarms (P1):                            │                              │
│  │  ┌────────────────────────────────────────────┐  │                              │
│  │  │ Name: Slow-Response-Time                   │  │                              │
│  │  │ Metric: API_Response_Time > 2000ms         │  │                              │
│  │  │ Evaluation: 5 consecutive periods (5 min)  │  │                              │
│  │  │ Action: SNS → Slack #monitoring            │  │                              │
│  │  └────────────────────────────────────────────┘  │                              │
│  └──────────────────┬───────────────────────────────┘                              │
│                     │                                                               │
│                     │ (4) Send notifications                                        │
│                     ▼                                                               │
│  ┌──────────────────────────────────────────────────┐                              │
│  │         SNS Topic                                │                              │
│  │  ARN: arn:aws:sns:ap-southeast-1:               │                              │
│  │       769137004257:snaptics-alerts               │                              │
│  │                                                  │                              │
│  │  Subscriptions:                                  │                              │
│  │  ├─ Email: admin@snaptics.com                    │                              │
│  │  ├─ SMS: +84-xxx-xxx-xxx                         │                              │
│  │  └─ Lambda: Forward to Slack webhook             │                              │
│  └──────────────────────────────────────────────────┘                              │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Mũi tên cho Monitoring:

1. **Fargate → CloudWatch Logs**
   - Màu: Xanh dương
   - Kiểu: Nét liền (────→)
   - Label: "Stream logs (real-time)"

2. **CloudWatch Logs → CloudWatch Metrics**
   - Màu: Tím
   - Kiểu: Nét liền (────→)
   - Label: "Extract metrics"

3. **CloudWatch Metrics → CloudWatch Alarms**
   - Màu: Cam
   - Kiểu: Nét liền (────→)
   - Label: "Trigger when threshold exceeded"

4. **CloudWatch Alarms → SNS**
   - Màu: Đỏ
   - Kiểu: Nét liền (────→)
   - Label: "Send alert notification"

---

### CloudWatch Dashboard Example

```
┌────────────────────────────────────────────────────────────┐
│          Snaptics API - Production Dashboard              │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   API Requests  │  │  Response Time  │                 │
│  │   (Last 1h)     │  │  (p95)          │                 │
│  │   ▁▂▃▅▇█▇▅▃▂▁   │  │  245ms          │                 │
│  │   12,453        │  │  ↓ 12% vs 24h   │                 │
│  └─────────────────┘  └─────────────────┘                 │
│                                                            │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   Error Rate    │  │  CPU Usage      │                 │
│  │   (5xx)         │  │  (Fargate)      │                 │
│  │   0.12%         │  │  ▁▂▃▄▅▆▅▄▃▂▁   │                 │
│  │   ✅ Normal     │  │  45%            │                 │
│  └─────────────────┘  └─────────────────┘                 │
│                                                            │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   RDS           │  │  SQS Queue      │                 │
│  │   Connections   │  │  Messages       │                 │
│  │   23 / 100      │  │  5 in queue     │                 │
│  │   ✅ Healthy    │  │  ✅ Processing  │                 │
│  └─────────────────┘  └─────────────────┘                 │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 🔄 Traffic Flow Details

### Complete Request Flow (User → Database)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          COMPLETE REQUEST FLOW                                      │
│                                                                                     │
│  [User Browser] 🌐                                                                  │
│       │                                                                             │
│       │ (1) HTTPS Request                                                           │
│       │     GET https://snaptics-api.com/api/transactions                           │
│       │     Headers: Authorization: Bearer <jwt_token>                              │
│       ▼                                                                             │
│  ┌──────────────┐                                                                   │
│  │  Route 53    │  DNS Resolution                                                   │
│  │  (DNS)       │  snaptics-api.com → CloudFront Distribution ID                    │
│  └──────┬───────┘                                                                   │
│         │                                                                           │
│         │ (2) Route to nearest edge location                                        │
│         ▼                                                                           │
│  ╔══════════════════════════╗                                                       │
│  ║      AWS Shield          ║  DDoS Protection (Layer 3/4/7)                        │
│  ║  ┌────────────────────┐  ║                                                       │
│  ║  │    AWS WAF         │  ║  SQL Injection / XSS / Rate Limit Check               │
│  ║  └────────┬───────────┘  ║                                                       │
│  ║           │              ║                                                       │
│  ║           ▼              ║                                                       │
│  ║  ┌────────────────────┐  ║                                                       │
│  ║  │   CloudFront       │  ║  CDN + SSL/TLS Termination                            │
│  ║  │   (Edge Cache)     │  ║  • Check cache (HIT or MISS)                          │
│  ║  │                    │  ║  • If MISS → forward to origin (ALB)                  │
│  ║  └────────┬───────────┘  ║                                                       │
│  ╚═══════════╪══════════════╝                                                       │
│              │                                                                       │
│              │ (3) Forward to origin (if cache MISS)                                │
│              │     X-Forwarded-For: <client_ip>                                     │
│              ▼                                                                       │
│  ┌──────────────────────────────────────────────┐                                   │
│  │           VPC (10.0.0.0/16)                  │                                   │
│  │                                              │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  Security Group: SG-ALB            │     │                                   │
│  │   │  ✅ Allow HTTPS from CloudFront    │     │                                   │
│  │   └──────────┬─────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              ▼                                │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  Application Load Balancer (ALB)   │     │                                   │
│  │   │  • Health check: /health           │     │                                   │
│  │   │  • Sticky sessions enabled         │     │                                   │
│  │   │  • Select target (round-robin)     │     │                                   │
│  │   └──────────┬─────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              │ (4) Forward to Fargate task    │                                   │
│  │              │     Port: 8080                 │                                   │
│  │              ▼                                │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  Security Group: SG-Fargate        │     │                                   │
│  │   │  ✅ Allow 8080 from SG-ALB         │     │                                   │
│  │   └──────────┬─────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              ▼                                │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  ECS Fargate Task (API Container)  │     │                                   │
│  │   │                                    │     │                                   │
│  │   │  (5) Process request:              │     │                                   │
│  │   │  • Validate JWT token              │     │                                   │
│  │   │  • Check user permissions          │     │                                   │
│  │   │  • Query database                  │     │                                   │
│  │   └──────────┬─────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              │ (6) Database query             │                                   │
│  │              │     SELECT * FROM Transactions │                                   │
│  │              │     WHERE UserId = @userId     │                                   │
│  │              ▼                                │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  Security Group: SG-RDS            │     │                                   │
│  │   │  ✅ Allow 1433 from SG-Fargate     │     │                                   │
│  │   └──────────┬─────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              ▼                                │                                   │
│  │   ┌────────────────────────────────────┐     │                                   │
│  │   │  RDS SQL Server (Primary)          │     │                                   │
│  │   │  • Execute query                   │     │                                   │
│  │   │  • Return results                  │     │                                   │
│  │   └────────────────────────────────────┘     │                                   │
│  │              │                                │                                   │
│  │              │ (7) Database response          │                                   │
│  │              │     [{"id": 1, "amount": 50}]  │                                   │
│  │              ▼                                │                                   │
│  │   [Fargate serializes to JSON]               │                                   │
│  │              │                                │                                   │
│  └──────────────┼────────────────────────────────┘                                   │
│                 │                                                                   │
│                 │ (8) HTTP 200 OK                                                   │
│                 │     Content-Type: application/json                                │
│                 │     [{"id": 1, "amount": 50, ...}]                                │
│                 ▼                                                                   │
│            [User Browser receives response]                                         │
│                                                                                     │
│  ⏱️ Total time: ~200-500ms                                                          │
│  ├─ DNS resolution: ~10ms                                                           │
│  ├─ CloudFront: ~20ms (if cached: 0ms)                                             │
│  ├─ ALB: ~5ms                                                                       │
│  ├─ Fargate processing: ~50-100ms                                                   │
│  ├─ Database query: ~100-300ms                                                      │
│  └─ Response serialization: ~10ms                                                   │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎨 Cách vẽ Icons và Màu sắc

### AWS Service Icons

```
Route 53:       [R53] (icon hình địa cầu màu tím)
WAF:            [🛡️] (icon lá chắn màu đỏ)
CloudFront:     [☁️] (icon mây màu cam)
Shield:         [🛡️] (icon khiên màu xanh dương)
ALB:            [⚖️] (icon cân bằng màu cam)
VPC:            [🏢] (hình chữ nhật viền xanh dương)
NAT Gateway:    [🌐] (icon gateway màu xanh lá)
Fargate:        [🐳] (icon container màu tím)
RDS:            [🗄️] (icon database màu xanh dương)
S3:             [📦] (icon bucket màu cam)
SQS:            [📨] (icon message queue màu hồng)
SNS:            [📢] (icon notification màu đỏ)
CloudWatch:     [📊] (icon chart màu đỏ)
Secrets Manager:[🔐] (icon key màu cam)
ECR:            [📦] (icon container registry màu cam)
```

### Màu sắc cho mũi tên:

```
Traffic flow (request):     Xanh dương (#2196F3)
Security boundaries:        Xanh lá (#4CAF50)
Database connections:       Tím (#9C27B0)
External API calls:         Cam (#FF9800)
Monitoring/Logging:         Đỏ (#F44336)
Cache/CDN:                  Xanh lá nhạt (#8BC34A)
Error/Alert:                Đỏ đậm (#D32F2F)
CI/CD Pipeline:             Xanh dương đậm (#1976D2)
```

### Kiểu nét vẽ:

```
Nét liền:     ────→  (traffic flow chính)
Nét chấm:     ····→  (traffic ra ngoài VPC)
Nét đứt:      ----→  (backup/fallback)
Nét đôi:      ════→  (high bandwidth)
Mũi tên 2 chiều: ←→  (bi-directional communication)
```

---

## 📝 Summary & Best Practices

### ✅ Architecture Checklist

#### Security (P0 - Critical)
- [x] WAF enabled trước CloudFront
- [x] AWS Shield Standard enabled
- [x] Security Groups phân tầng rõ ràng (ALB, Fargate, RDS)
- [x] Secrets Manager cho sensitive data
- [x] TLS 1.3 cho tất cả connections
- [x] No public access to database
- [x] IAM roles với least privilege principle

#### High Availability (P0 - Critical)
- [x] Multi-AZ deployment (VPC spanning 2 AZs)
- [x] Auto Scaling cho Fargate (min 2, max 10)
- [x] RDS Multi-AZ với automatic failover
- [x] ALB health checks enabled
- [x] CloudFront global edge locations
- [x] NAT Gateway trong mỗi AZ

#### Performance (P1 - Important)
- [x] CloudFront CDN cho static content
- [x] Database read replicas
- [ ] ElastiCache Redis cho session caching (THIẾU - nên thêm)
- [x] Connection pooling cho database
- [x] Async processing với SQS
- [x] Container image optimization (multi-stage build)

#### Monitoring & Observability (P1 - Important)
- [x] CloudWatch Logs cho tất cả services
- [x] CloudWatch Metrics + Custom metrics
- [x] CloudWatch Alarms với SNS notifications
- [ ] X-Ray distributed tracing (THIẾU - nên thêm)
- [x] CloudWatch Dashboard
- [x] Log retention policy (7 days)

#### Cost Optimization (P2 - Nice to have)
- [x] Fargate Spot instances cho non-critical tasks
- [x] S3 lifecycle policies
- [x] ECR image cleanup policy
- [x] RDS automated backups (7 days)
- [ ] CloudWatch Logs insights query optimization
- [ ] Cost anomaly detection alerts

#### Disaster Recovery (P2 - Nice to have)
- [x] Automated RDS backups (7 days retention)
- [x] Point-in-time recovery enabled
- [ ] Cross-region backup replication (THIẾU)
- [ ] Documented runbook for disaster recovery
- [x] Blue/green deployment capability
- [x] Rollback procedure documented

---

### 🚨 Critical Issues Found & Fixed

| Issue | Severity | Status | Fix |
|-------|----------|--------|-----|
| Route 53 → ALB trực tiếp | 🔴 Critical | ✅ Fixed | Thêm CloudFront làm entry point |
| WAF missing | 🔴 Critical | ✅ Fixed | Thêm WAF trước CloudFront |
| Security Groups không rõ ràng | 🔴 Critical | ✅ Fixed | Vẽ 3 SGs riêng biệt với rules chi tiết |
| Secrets hardcoded | 🔴 Critical | ✅ Fixed | Chuyển sang Secrets Manager |
| SQS architecture không đầy đủ | 🟡 High | ✅ Fixed | Thêm DLQ, Consumer Service, flow diagram |
| Multi-cloud không được thể hiện | 🟡 High | ✅ Fixed | Vẽ External Services với mũi tên rõ ràng |
| CI/CD pipeline thiếu detail | 🟡 High | ✅ Fixed | Vẽ đầy đủ 6 steps + rollback |
| Missing X-Ray tracing | 🟢 Medium | ⚠️ TODO | Cần implement |
| Missing ElastiCache | 🟢 Medium | ⚠️ TODO | Cần thêm Redis layer |
| No cross-region backup | 🟢 Low | ⚠️ TODO | Future enhancement |

---

### 📊 Estimated Monthly Cost (Production)

```
┌────────────────────────────────────────────────────────┐
│  AWS Service Breakdown (ap-southeast-1)                │
├────────────────────────────────────────────────────────┤
│  Fargate (2 tasks x 0.5 vCPU, 1GB RAM):    $30/month  │
│  RDS SQL Server (db.t3.medium, Multi-AZ):  $180/month │
│  ALB (with data transfer):                 $25/month  │
│  NAT Gateway (2 AZs):                      $70/month  │
│  CloudFront (100GB data transfer):         $15/month  │
│  S3 (500GB storage):                       $12/month  │
│  ECR (100GB images):                       $10/month  │
│  CloudWatch Logs (10GB/month):             $5/month   │
│  Secrets Manager (10 secrets):             $5/month   │
│  SQS (1M requests/month):                  $1/month   │
│  SNS (1000 notifications):                 $0.50/mo   │
│  Route 53 (1 hosted zone):                 $0.50/mo   │
│  WAF (Basic rules):                        $15/month  │
├────────────────────────────────────────────────────────┤
│  TOTAL ESTIMATED:                          $368/month │
├────────────────────────────────────────────────────────┤
│  External Services:                                    │
│  • Gemini API:                             $20/month  │
│  • Azure Document Intelligence:            $50/month  │
│  • Azure OpenAI:                           $30/month  │
├────────────────────────────────────────────────────────┤
│  GRAND TOTAL:                              $468/month │
└────────────────────────────────────────────────────────┘

💡 Cost Optimization Tips:
1. Use Reserved Instances cho RDS (-40% cost)
2. Enable Fargate Spot cho background tasks (-70% cost)
3. Implement S3 Intelligent-Tiering
4. Optimize CloudWatch Logs retention
5. Use CloudFront caching aggressively
```

---

### 🔗 References & Tools

**Diagram Tools:**
- draw.io (diagrams.net) - Free online tool
- Lucidchart - Professional diagramming
- AWS Architecture Icons: https://aws.amazon.com/architecture/icons/
- Cloudcraft - AWS architecture visualization

**Documentation:**
- AWS Well-Architected Framework: https://aws.amazon.com/architecture/well-architected/
- ECS Best Practices: https://docs.aws.amazon.com/AmazonECS/latest/bestpracticesguide/
- RDS Security: https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_Security.html

**Monitoring:**
- CloudWatch Dashboard templates
- X-Ray service map
- Cost Explorer reports

---

## 🎯 Next Steps (Action Items)

### Phase 1: Security Hardening (Week 1)
1. ✅ Deploy WAF với AWS Managed Rules
2. ✅ Migrate secrets từ Parameter Store → Secrets Manager
3. ✅ Review và update Security Groups
4. ✅ Enable CloudTrail logging
5. ✅ Setup SNS alerts cho critical alarms

### Phase 2: Performance Optimization (Week 2-3)
1. ⚠️ Add ElastiCache Redis cluster
2. ⚠️ Implement X-Ray distributed tracing
3. ⚠️ Optimize database queries (add indexes)
4. ⚠️ Setup CloudFront caching rules
5. ⚠️ Implement API response compression

### Phase 3: Observability (Week 4)
1. ⚠️ Create comprehensive CloudWatch Dashboard
2. ⚠️ Setup log aggregation và analysis
3. ⚠️ Implement custom metrics
4. ⚠️ Setup cost anomaly detection
5. ⚠️ Document runbooks

### Phase 4: Disaster Recovery (Week 5-6)
1. ⚠️ Test RDS failover procedure
2. ⚠️ Setup cross-region backup
3. ⚠️ Document rollback procedures
4. ⚠️ Conduct disaster recovery drill
5. ⚠️ Create incident response playbook

---

**Document Version:** 2.0  
**Last Updated:** July 31, 2026  
**Maintained by:** DevOps Team  
**Review Cycle:** Monthly

---

*Lưu ý: File này nên được review và update định kỳ khi có thay đổi infrastructure*
