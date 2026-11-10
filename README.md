# Employee Payroll Run Module

Practical assessment submission for the Full Stack Developer (.NET / SQL Server) role.

- **Candidate:** Divyanshi Gupta
- **Repository:** [https://github.com/DivyanshiGupta-2028/PayrollSystem](https://github.com/DivyanshiGupta-2028/PayrollSystem)
- **Live Demo:** [https://divyanshigupta-2028.github.io/PayrollSystem/](https://divyanshigupta-2028.github.io/PayrollSystem/)

---

## Architecture Overview

The application follows the architecture requested in the brief:

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

### Projects Structure
- **PayrollSystem.Core**: Domain models, DTOs, service interfaces, validation, and payroll calculation logic.
- **PayrollSystem.Infrastructure**: Dapper repositories, database connection factory, and stored procedure calls.
- **PayrollSystem.Api**: REST API controllers, global exception handling, and frontend assets in `wwwroot`.
- **PayrollSystem.Tests**: xUnit unit tests for calculation formulas, validation, and edge cases.
- **database/**: SQL scripts for schema, seed data, and stored procedures.

---

## Business Rules & Calculation

Calculations follow the specification:
- **Gross Pay** = `(Basic Salary / Total Working Days) * Days Present` (rounded to nearest rupee)
- **PF Deduction** = `12% of Basic Salary`
- **Professional Tax** = Flat `₹200` per month
- **Net Pay** = `Gross Pay - PF Deduction - Professional Tax`
- **Immutability** = Once finalized, a payroll run cannot be edited or deleted.

### Verification (Ravi Sharma - EMP001)
- Basic Salary: ₹30,000
- Working Days: 26, Days Present: 24
- Gross Pay = (30,000 / 26) * 24 = ₹27,692
- PF Deduction = 30,000 * 12% = ₹3,600
- Professional Tax = ₹200
- Net Pay = 27,692 - 3,600 - 200 = **₹23,892**

---

## Database Setup

Target database: **TymLabs** on SQL Server.

### Using sqlcmd:
```powershell
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/01_Schema.sql
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/02_SeedData.sql
sqlcmd -S "DIVYANSHI_PC\MSSQLSERVER03" -E -C -i database/03_StoredProcedures.sql
```
*(Or open and run the scripts in SSMS / Azure Data Studio).*

Update `src/PayrollSystem.Api/appsettings.json` with your connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DIVYANSHI_PC\\MSSQLSERVER03;Database=TymLabs;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

---

## How to Run

### 1. Build and Test
```powershell
dotnet build PayrollSystem.sln
dotnet test
```

### 2. Run API Server
```powershell
dotnet run --project src/PayrollSystem.Api/PayrollSystem.Api.csproj
```
The application will be available at `http://localhost:5229`.

---

## API Endpoints

| Method | Endpoint | Description | Status Codes |
|---|---|---|---|
| `GET` | `/api/employees` | List active employees | `200` |
| `POST` | `/api/payroll/run` | Process monthly payroll | `201`, `400`, `409` |
| `GET` | `/api/payroll/{month}/{year}` | Get payroll run results (supports `?page=1&pageSize=10`) | `200`, `400`, `404` |
| `GET` | `/api/payroll/{runId}/payslip/{employeeId}` | Get single employee payslip | `200`, `400`, `404` |
| `GET` | `/api/payroll/history` | List past finalized runs | `200` |

---

## Assumptions & Edge Cases Handled

1. **Rounding:** Rounded to nearest integer rupee using away-from-zero midpoint rounding, matching the Ravi Sharma example.
2. **Missing Attendance:** If an employee has no attendance row for a given month, defaults to 0 days present so unrecorded employees are not paid incorrectly.
3. **Zero Attendance:** When days present is 0, Gross Pay is 0 and statutory deductions do not make Net Pay negative.
4. **Division by Zero:** Guarded against 0 total working days in both SQL and C#.
5. **Immutability:** Guarded at multiple levels:
   - Application service checks before running.
   - Stored procedure returns an error if already finalized.
   - SQL triggers (`trg_EnforcePayrollRunsImmutability` and `trg_EnforcePayrollDetailsImmutability`) prevent direct `UPDATE`/`DELETE` on finalized records in database.
6. **Concurrency:** `usp_RunPayroll` runs in an explicit transaction with `XACT_ABORT ON` to prevent partial commits.

---

## Bonus Features Implemented

- **409 Conflict:** If a payroll run for the given month/year already exists, the API returns HTTP 409 Conflict with a clear message.
- **Unit Tests:** 22 unit tests using xUnit covering domain calculation, statutory deductions, service layer validation, pagination, and conflict scenarios.
- **Pagination:** Supported on `GET /api/payroll/{month}/{year}?page=1&pageSize=10`.
- **Printable Payslip:** Frontend modal formatted for printing with `@media print` CSS including earnings, deductions, net salary, and amount in words.

---

## Future Enhancements

With more time, next steps would include:
- Dynamic salary components (HRA, allowances, variable pay).
- PDF generation and automated email distribution of payslips.
- Role-based authentication (Admin vs Employee).
- Income tax TDS deduction support.
