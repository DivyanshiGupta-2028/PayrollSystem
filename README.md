# Employee Payroll Run Module — Full Stack (.NET / SQL Server)

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Dapper](https://img.shields.io/badge/ORM-Dapper%20%2B%20ADO.NET-orange.svg)](https://github.com/DapperLib/Dapper)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red.svg)](https://www.microsoft.com/sql-server)
[![Tests](https://img.shields.io/badge/Tests-xUnit%20(22%20passed)-brightgreen.svg)](https://xunit.net/)

> **Technical Assessment Submission**  
> **Position:** Full Stack Developer (.NET / SQL Server) — 2–3 Years Experience  
> **Feature:** Monthly Employee Payroll Run, Validation, Calculation, and Payslip Generation

---

## 1. System Architecture & Design

The solution follows a clean, decoupled **Layered Architecture** adhering to Separation of Concerns (SoC) and the exact specification in the assessment brief:

```
UI Layer (HTML5 / Modern CSS / Vanilla JavaScript / Print Styles)
  ↓
API Controllers (ASP.NET Core Web API, RESTful Endpoints, RFC 7807 Error Responses)
  ↓
Service Layer (Business Logic, Statutory Calculations, DTO Mapping, Validation)
  ↓
Repository Layer (Dapper + ADO.NET Data Access, Parameterized Queries)
  ↓
Stored Procedures (Transactional Payroll Execution: usp_RunPayroll)
  ↓
SQL Server Database (Relational Schema, Constraints, Indexes, Immutability Triggers)
```

### Project Structure

```
TymLabs/
├── database/
│   ├── 01_Schema.sql             # Tables, constraints, foreign keys, indexes, triggers
│   ├── 02_SeedData.sql           # Departments, employees, and monthly attendance
│   ├── 03_StoredProcedures.sql   # usp_RunPayroll, usp_GetPayrollRun, usp_GetEmployeePayslip
│   └── run_all.sql               # Single-click master execution script
├── src/
│   ├── PayrollSystem.Core/       # Domain Entities, DTOs, Business Rules, Interfaces, Services
│   │   ├── DTOs/                 # PayrollItemDto, PayrollRunSummaryDto, PayslipDto, etc.
│   │   ├── Exceptions/           # PayrollConflictException (409), NotFoundException (404), etc.
│   │   ├── Interfaces/           # IPayrollService, IPayrollRepository, IPayrollCalculationService
│   │   ├── Models/               # Department, Employee, MonthlyAttendance, PayrollRun, PayrollDetail
│   │   └── Services/             # Pure Calculation Engine & Application Services
│   ├── PayrollSystem.Infrastructure/ # Data Access Implementation
│   │   ├── Data/                 # IDbConnectionFactory & SqlConnectionFactory
│   │   └── Repositories/         # Dapper Repositories (PayrollRepository, EmployeeRepository)
│   └── PayrollSystem.Api/        # ASP.NET Core Web API & Frontend Presentation
│       ├── Controllers/          # EmployeesController, PayrollController
│       ├── Middleware/           # GlobalExceptionHandlingMiddleware
│       ├── wwwroot/              # Frontend Single-Page Interface
│       │   ├── css/styles.css    # Responsive styles & official @media print payslip layout
│       │   ├── js/app.js         # Fetch client, filters, pagination, interactive UI
│       │   └── index.html        # Single-page dashboard & printable payslip modal
│       ├── appsettings.json      # Connection string configuration
│       └── Program.cs            # Dependency Injection, Middleware, and Route mappings
├── tests/
│   └── PayrollSystem.Tests/      # xUnit Test Suite (Calculation Engine & Service Unit Tests)
├── PayrollSystem.sln             # Visual Studio Solution File
└── README.md                     # Comprehensive Setup & Architecture Documentation
```

---

## 2. Business Rules & Calculation Formula

Monthly payroll calculates each active employee's take-home salary based on working days and recorded attendance:

| Component | Rule / Formula |
|---|---|
| **Gross Pay** | `(Basic Salary ÷ Total Working Days) × Days Present` *(rounded to nearest integer ₹)* |
| **PF Deduction** | `12% of Basic Salary` |
| **Professional Tax** | Flat `₹200` per month *(waived if employee is absent the entire month)* |
| **Net Pay** | `Gross Pay − PF Deduction − Professional Tax` |
| **Immutability** | Once finalized, a payroll run cannot be edited, re-run, or deleted. |

### Verification Against Assessment Brief Example
For **Ravi Sharma** (EMP001):
- Basic Salary: **₹30,000**
- Total Working Days: **26**
- Days Present: **24**

$$\text{Gross Pay} = \left(\frac{30000}{26}\right) \times 24 = 27692.307\dots \xrightarrow{\text{round}} \mathbf{₹27,692}$$
$$\text{PF Deduction} = 30000 \times 12\% = \mathbf{₹3,600}$$
$$\text{Professional Tax} = \mathbf{₹200}$$
$$\text{Net Pay} = 27692 - 3600 - 200 = \mathbf{₹23,892}$$

*Both our SQL Stored Procedure and C# domain service compute and verify these exact numbers.*

---

## 3. Database Setup Instructions

### Prerequisites
- Microsoft SQL Server 2016+ or LocalDB / SQL Server Express / Developer Edition
- Command-line tool `sqlcmd` OR SQL Server Management Studio (SSMS) / Azure Data Studio

### Option A: Setup Using `sqlcmd` (Command Prompt / PowerShell)
From the repository root folder, run:

```powershell
# 1. Create Schema and Constraints
sqlcmd -S . -E -C -i database/01_Schema.sql

# 2. Insert Seed Data (Employees & Attendance)
sqlcmd -S . -E -C -i database/02_SeedData.sql

# 3. Create Stored Procedures
sqlcmd -S . -E -C -i database/03_StoredProcedures.sql
```

*(Replace `-S .` with `-S (localdb)\MSSQLLocalDB` or `-S .\SQLEXPRESS` if using a named instance).*

### Option B: Setup Using SQL Server Management Studio (SSMS)
1. Open SSMS and connect to your SQL Server instance.
2. Open and execute `database/01_Schema.sql`.
3. Open and execute `database/02_SeedData.sql`.
4. Open and execute `database/03_StoredProcedures.sql`.

### Connection String Configuration
Update the connection string in `src/PayrollSystem.Api/appsettings.json` if your server instance differs:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PayrollDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

---

## 4. How to Run the Project Locally

### 1. Build Solution
```powershell
dotnet build PayrollSystem.sln
```

### 2. Run All Unit Tests
```powershell
dotnet test
```
*Expected: 22 tests passing covering edge cases, statutory calculations, Ravi Sharma brief example, and conflict handling.*

### 3. Launch Web API and Frontend
```powershell
dotnet run --project src/PayrollSystem.Api/PayrollSystem.Api.csproj --launch-profile http
```

### 4. Open Application in Browser
- **Frontend Dashboard:** [http://localhost:5229](http://localhost:5229)
- **OpenAPI / Swagger Documentation:** [http://localhost:5229/openapi/v1.json](http://localhost:5229/openapi/v1.json)

---

## 5. API Endpoints Reference

All endpoints return JSON responses with standard HTTP status codes.

| Method | Endpoint | Description | Status Codes |
|---|---|---|---|
| `GET` | `/api/employees` | Return list of active employees | `200 OK` |
| `POST` | `/api/payroll/run` | Process monthly payroll run | `201 Created`, `400 Bad Request`, `409 Conflict` |
| `GET` | `/api/payroll/{month}/{year}` | Get saved run for period *(supports `?page=1&pageSize=10`)* | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `GET` | `/api/payroll/{runId}/slip/{employeeId}` | Get printable payslip for an employee | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `GET` | `/api/payroll/history` | List historical finalized runs | `200 OK` |

### Sample Payloads & Responses

#### `POST /api/payroll/run`
**Request Body:**
```json
{
  "month": 11,
  "year": 2026,
  "createdBy": "HR Admin"
}
```

**Response (201 Created):**
```json
{
  "payrollRunId": 1,
  "month": 11,
  "year": 2026,
  "runDate": "2026-11-09T12:17:26Z",
  "totalEmployees": 6,
  "totalGrossPay": 221707.00,
  "totalPFDeduction": 36000.00,
  "totalProfessionalTax": 1200.00,
  "totalDeductions": 37200.00,
  "totalNetPay": 184507.00,
  "status": "Finalized",
  "createdBy": "HR Admin",
  "items": [
    {
      "employeeId": 1,
      "employeeCode": "EMP001",
      "name": "Ravi Sharma",
      "departmentName": "Engineering",
      "designation": "Senior Software Engineer",
      "basicSalary": 30000.00,
      "workingDays": 26,
      "daysPresent": 24.00,
      "grossPay": 27692.00,
      "pfDeduction": 3600.00,
      "professionalTax": 200.00,
      "netPay": 23892.00
    }
  ]
}
```

#### Re-run Conflict Response (409 Conflict):
```json
{
  "statusCode": 409,
  "success": false,
  "message": "A finalized payroll run already exists for 11/2026. Finalized payroll runs cannot be modified or re-run.",
  "timestamp": "2026-11-09T12:20:00Z"
}
```

---

## 6. Assumptions & Edge Cases Handled

1. **Rounding Precision:**
   - In accordance with standard Indian Payroll practices and the Ravi Sharma example (`₹27,692`, `₹3,600`, `₹200`, `₹23,892`), figures are rounded to the nearest integer rupee using `ROUND(..., 0)` (`MidpointRounding.AwayFromZero`).
   - Columns are stored as `DECIMAL(18,2)` to maintain financial precision and support paise if needed.
2. **Missing Attendance Record:**
   - If an employee does not have an attendance entry for the month, the stored procedure defaults `TotalWorkingDays` to standard working days (26) and `DaysPresent` to `0.0`.
3. **Zero Attendance & Negative Net Pay Protection:**
   - If an employee has 0 days present (e.g. unpaid sabbatical), Gross Pay is `0`.
   - To prevent negative take-home salary disbursement when Gross Pay is zero, statutory deductions are guarded so Net Pay does not drop below `0`.
4. **Division by Zero Protection:**
   - Guarded with `NULLIF(TotalWorkingDays, 0)` in SQL and validation checks in C#.
5. **Database Immutability Enforcement:**
   - In addition to API-level checks, two SQL triggers (`trg_EnforcePayrollRunsImmutability` and `trg_EnforcePayrollDetailsImmutability`) block `UPDATE` and `DELETE` operations on any record associated with `Status = 'Finalized'`.
6. **Concurrency & Atomicity:**
   - `usp_RunPayroll` wraps calculation, header insertion, and line item insertion inside an explicit transaction (`BEGIN TRANSACTION ... COMMIT TRANSACTION`) with `SET XACT_ABORT ON`.

---

## 7. Bonus Deliverables Implemented

- [x] **HTTP 409 Conflict Handling:** Prevents duplicate runs at both the API and database levels, returning informative JSON feedback.
- [x] **Comprehensive xUnit Suite (22 Unit Tests):** Verifies the calculation engine, Ravi Sharma numbers, zero-attendance safety, exception handling, and service logic.
- [x] **Pagination Support:** Built into `GET /api/payroll/{month}/{year}?page=1&pageSize=10` with metadata (`totalPages`, `hasNext`, `hasPrevious`).
- [x] **Printable Payslip View:** Dedicated modal and `@media print` layout formatting an official corporate salary slip with earnings, deductions, net salary in words, and signature block.

---

## 8. What I Would Add Next with More Time

Given additional time for an enterprise deployment, I would prioritize:
1. **Dynamic Salary Components:** Support for HRA, Special Allowances, Variable Pay, and Voluntary PF (VPF) via a flexible `SalaryStructure` table.
2. **Batch Payslip PDF Generation:** Background worker generating password-protected PDF payslips (using QuestPDF) emailed automatically to employees.
3. **Role-Based Access Control (RBAC):** ASP.NET Core Identity with JWT authentication differentiating `Payroll Administrator` vs `Employee Self-Service`.
4. **Tax Exemption Declarations:** Section 80C, 80D, and Old vs. New Tax Regime annual income tax TDS deductions.
5. **Docker Containerization:** `Dockerfile` and `docker-compose.yml` to spin up SQL Server and the API with zero local setup.

