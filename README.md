# Project Approval System (PAS)

A web-based **Project Approval System (PAS)** designed to facilitate the matching of **student research projects with academic supervisors**.

This system enables students to submit project proposals, allows supervisors to review and approve them, and helps academic staff manage the full approval workflow efficiently.

---

## Project Overview

The Project Approval System aims to streamline the process of:

- Student project proposal submission
- Supervisor allocation and matching
- Proposal review and approval
- Approval status tracking
- Communication between students and supervisors

This project is being developed as a **team-based academic software engineering project** using a modern full-stack architecture.

---

## Tech Stack

### Frontend
- **React**
- **Vite**
- JavaScript
- CSS / Tailwind CSS *(optional)*

### Backend
- **ASP.NET Core Web API**
- **.NET 10**
- Minimal APIs / REST APIs

### Database
- **Microsoft Azure SQL Database**
- **Entity Framework Core (EF Core)**
- EF Core Migrations

### DevOps / Tools
- **Docker**
- **Docker Compose**
- **Git & GitHub**
- **VS Code**

---

## Project Structure

```text
project-approval-system/
│
├── frontend/                  # React frontend
│   ├── src/
│   ├── public/
│   ├── Dockerfile
│   └── package.json
│
├── backend/
│   └── PAS.API/               # ASP.NET Core backend
│       ├── Controllers/
│       ├── Models/
│       ├── Data/
│       ├── Migrations/
│       ├── appsettings.json
│       ├── Dockerfile
│       ├── Program.cs
│       └── PAS.API.csproj
│
├── docker-compose.yml
├── .gitignore
└── README.md
```

---

## Prerequisites

Before running the project, install the following:

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js + npm](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## 🚀 Getting Started

Clone the repository:

```bash
git clone https://github.com/YOUR-ORG/project-approval-system.git
cd project-approval-system
```

---

# 🖥 Running Frontend

Navigate to frontend:

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on:

```text
http://localhost:5173
```

---

# ⚙️ Running Backend

Navigate to backend:

```bash
cd backend/PAS.API
dotnet restore
dotnet run
```

Backend runs on:

```text
https://localhost:xxxx
```

Swagger / OpenAPI:

```text
https://localhost:xxxx/swagger
```

---

# 🐳 Running with Docker

From project root:

```bash
docker compose up --build
```

This starts both:

- frontend
- backend

---

## 🗄 Database Setup (Azure SQL)

This project uses a **shared Microsoft Azure SQL Database** hosted in the cloud.  

All teammates connect to the **same database instance** using their local backend.

---

### 1️⃣ Firewall Access for Team Members

Since Azure SQL blocks unknown IPs, each teammate must provide **their public IPv4 address** to the database owner.  

Steps:

1. Search “what is my IP” in Google or use [https://whatismyipaddress.com](https://whatismyipaddress.com)
2. Send the IP to the DB admin
3. DB admin adds it in Azure Portal → SQL Server → Networking → Firewall rules

> **Note:** IPs may change if teammates use different networks.

---

### 2️⃣ Local Configuration for Teammates

Each teammate must create their **local configuration file** (ignored from Git) at:

```text
backend/PAS.API/appsettings.json
```

Use the template file at:

```json
backend/PAS.API/appsettings.example.json
```
Connection string will be shared privately.

---

### 3️⃣ Viewing Tables and Data

Teammates can view data **without Azure portal access** using **VS Code + MSSQL extension**:

1. Install **SQL Server (mssql)** extension in VS Code.
2. Create a new connection using:

```text
Server: pas-server-kalfox.database.windows.net
Database: PASDB
Username: pasadmin
Password: YOUR_PASSWORD
Port: 1433
```

3. Explore tables:

- Browse tables (e.g., `Students`)
- Run queries like:

```sql
SELECT * FROM Students;
```

> 🔹 Alternatively, teammates can always fetch data via the **backend API**. This is recommended for frontend devs.

---

### 4️⃣ EF Core Migrations (ONLY DB OWNER SHOULD RUN)

To avoid migration conflicts, **one person should create and apply migrations**:

```bash
cd backend/PAS.API
dotnet ef migrations add MigrationName
dotnet ef database update
```

After migration:

```bash
git add Migrations/
git commit -m "Create migration: MigrationName"
git push
```

Other teammates just **pull the latest code**.

---

## 👥 Team Git Workflow

### Pull latest changes

```bash
git pull origin main
```

### Create a feature branch

```bash
git checkout -b feature/your-feature
```

### Commit professionally

```bash
git commit -m "Add Student model and DbContext"
git commit -m "Implement supervisor approval endpoint"
git commit -m "Create EF migration for project schema"
```

### Push branch

```bash
git push origin feature/student-crud-api
```

### Open Pull Request

After completing a feature, open a **Pull Request** on GitHub.

---