# Employee Payroll Run Module

Practical assessment submission for the Full Stack Developer (.NET / SQL Server) role.

- Candidate: Divyanshi Gupta
- Repository: https://github.com/DivyanshiGupta-2028/PayrollSystem
- Live Demo: https://divyanshigupta-2028.github.io/PayrollSystem/

---

## 1. System Architecture

The application follows the architecture requested in the technical brief:

```
UI Layer (HTML / JavaScript / CSS)
  ↓
API Controllers (ASP.NET Core Web API)
  ↓
Service Layer (Business Logic & Calculations)
  ↓
Repository Layer (Dapper / SQL Server Access)
  ↓
Stored Procedures (usp_RunPayroll, usp_GetPayrollRun, etc.)
  ↓
SQL Server Database (TymLabs)
```

### Project Structure
- PayrollSystem.Core: Domain models, DTOs, interfaces, validation, and salary calculation logic.
- PayrollSystem.Infrastructure: Dapper repositories, database connection factory, and stored procedure calls.
- PayrollSystem.Api: REST API controllers, global exception middleware, and frontend interface in wwwroot.
- PayrollSystem.Tests: xUnit test suite (22 unit tests) covering domain calculation, statutory deductions, and edge cases.
- database/: SQL scripts for schema, seed data, and stored procedures.

---

## 2. Business Rules & Formula Verification

Calculations implemented per specification:
- Gross Pay = (Basic Salary / Total Working Days) * Days Present (rounded to nearest integer rupee)
- PF Deduction = 12% of Basic Salary
- Professional Tax = Flat ₹200 per month
- Net Pay = Gross Pay - PF Deduction - Professional Tax
- Immutability = Once finalized, a payroll run cannot be edited or deleted.

### Brief Example Verification (Ravi Sharma - EMP001)
- Basic Salary: ₹30,000
- Working Days: 26, Days Present: 24
- Gross Pay = (30,000 / 26) * 24 = ₹27,692
- PF Deduction = 30,000 * 12% = ₹3,600
- Professional Tax = ₹200
- Net Pay = 27,692 - 3,600 - 200 = ₹23,892

---

## 3. Database Setup Instructions

Target database: TymLabs on Microsoft SQL Server.

### Running SQL Scripts
Execute the three scripts in the database/ folder in sequence:

```powershell
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/01_Schema.sql
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/02_SeedData.sql
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/03_StoredProcedures.sql
```
Alternatively, open and run the scripts in SQL Server Management Studio (SSMS) or execute database/run_all.sql.

### Connection String Configuration
Configure the connection string in src/PayrollSystem.Api/appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DIVYANSHI_PC\\MSSQLSERVER03;Database=TymLabs;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

---

## 4. How to Set Up and Run Locally (Step by Step)

### Step 1: Clone the Repository
```powershell
git clone https://github.com/DivyanshiGupta-2028/PayrollSystem.git
cd PayrollSystem
```

### Step 2: Database Setup
Ensure SQL Server is running and execute the database scripts as described in Section 3 above.

### Step 3: Build the Solution and Run Tests
```powershell
dotnet build PayrollSystem.sln
dotnet test
```
All 22 unit tests will execute and verify statutory rules, conflict handling, and edge cases.

### Step 4: Run the API Application
```powershell
dotnet run --project src/PayrollSystem.Api/PayrollSystem.Api.csproj
```
The server will start listening at http://localhost:5229.

### Step 5: Open the Frontend
Open http://localhost:5229 in your browser to access the payroll interface:
- Select Month and Year, then click "Run Payroll" to trigger a run (returns 201 Created).
- Clicking "Run Payroll" again for the same period demonstrates 409 Conflict handling.
- Use the "Past Finalized Runs" dropdown to quickly load and inspect past runs.
- Click "Payslip" on any employee row to preview and print the official single-page payslip.

---

## 5. API Endpoints

| Method | Endpoint | Description | Status Codes |
|---|---|---|---|
| GET | /api/employees | List active employees | 200 OK |
| POST | /api/payroll/run | Process monthly payroll run | 201 Created, 400 Bad Request, 409 Conflict |
| GET | /api/payroll/{month}/{year} | Get payroll run results (supports ?page=1&pageSize=10) | 200 OK, 400 Bad Request, 404 Not Found |
| GET | /api/payroll/{runId}/payslip/{employeeId} | Get single employee payslip | 200 OK, 400 Bad Request, 404 Not Found |
| GET | /api/payroll/history | List historical finalized runs | 200 OK |

---

## 6. Assumptions & Edge Cases Handled

1. Rounding: Financial figures are rounded to the nearest integer rupee using midpoint rounding away from zero (Math.Round(..., 0, MidpointRounding.AwayFromZero)), matching standard Indian payroll practice and the Ravi Sharma example.
2. Missing Attendance: If an employee has no attendance record for a given month, days present defaults to 0 to prevent unverified salary disbursement.
3. Zero Attendance: When days present is 0, Gross Pay is 0 and statutory deductions do not make Net Pay negative (Net Pay has a floor of 0).
4. Division by Zero: Guarded in both C# (validation error) and SQL (NULLIF / CASE expression) when total working days is 0.
5. Immutability: Enforced at three levels:
   - Application Service: Validates prior existence before calling stored procedure.
   - Stored Procedure: usp_RunPayroll checks for existing finalized run and throws error 51000.
   - Database Triggers: trg_EnforcePayrollRunsImmutability and trg_EnforcePayrollDetailsImmutability prevent direct SQL UPDATE or DELETE on finalized records.
6. Concurrency: usp_RunPayroll runs within an explicit transaction with SET XACT_ABORT ON.

---

## 7. Bonus Deliverables Implemented

- HTTP 409 Conflict: Returns descriptive conflict response when trying to re-run a finalized period.
- Comprehensive Unit Tests: 22 xUnit unit tests verifying domain calculations, statutory deductions, service validation, and pagination.
- Server-side Pagination: Implemented on GET /api/payroll/{month}/{year}?page=1&pageSize=10 with totalPages, hasNext, and hasPrevious metadata.
- Printable Payslip View: Formatted modal with dedicated print rendering ensuring clean single-page printing.

---

## 8. What Was Skipped & Future Enhancements

Given additional time for an expanded payroll platform, next priority items would be:
1. Dynamic Salary Components: Configurable allowances (HRA, Special Allowance, Conveyance) and deductions beyond basic salary.
2. Automated PDF Generation: Background service to batch-generate PDF payslips and email them to employees.
3. Role-Based Authentication: Differentiating HR Admin actions from Employee self-service payslip viewing.
4. Income Tax TDS Declarations: Handling Section 80C, 80D investments and annual TDS deductions.
