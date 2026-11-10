# Employee Payroll System

Practical assessment submission for Full Stack Developer (.NET 9 / SQL Server).

- Candidate: Divyanshi Gupta
- Live Demo: https://divyanshigupta-2028.github.io/PayrollSystem/
- Repository: https://github.com/DivyanshiGupta-2028/PayrollSystem

---

## 1. How to Run Locally (Step by Step)

### Prerequisites
- .NET 9 SDK
- Microsoft SQL Server (LocalDB, Express, or Developer instance)

### Step 1: Clone the repository
```bash
git clone https://github.com/DivyanshiGupta-2028/PayrollSystem.git
cd PayrollSystem
```

### Step 2: Database setup
Run the three SQL scripts located in the `database/` folder in order against your SQL Server instance (see section 2 for details).

### Step 3: Run unit tests
```bash
dotnet test
```
All 22 unit tests will run and verify calculation logic, edge cases, and service validations.

### Step 4: Start the application
```bash
dotnet run --project src/PayrollSystem.Api/PayrollSystem.Api.csproj
```
The API and frontend will start at: `http://localhost:5229`

Open `http://localhost:5229` in any modern web browser to access the payroll portal.

---

## 2. Database Setup Instructions

The database name is `TymLabs`.

### Scripts Execution Order
Execute the three SQL files in the `database/` directory in sequence using SQL Server Management Studio (SSMS), Azure Data Studio, or `sqlcmd`:

1. `database/01_Schema.sql` - Creates the database, tables (`Departments`, `Employees`, `MonthlyAttendance`, `PayrollRuns`, `PayrollDetails`), indexes, and immutability triggers.
2. `database/02_SeedData.sql` - Seeds initial departments, 6 employees, and monthly attendance for October and November 2026.
3. `database/03_StoredProcedures.sql` - Creates stored procedures: `usp_RunPayroll`, `usp_GetPayrollRun`, `usp_GetEmployeePayslip`, `usp_GetEmployees`, and `usp_GetAllPayrollRuns`.

Quick execution via `sqlcmd`:
```powershell
sqlcmd -S "YOUR_SERVER_INSTANCE" -E -i database/01_Schema.sql
sqlcmd -S "YOUR_SERVER_INSTANCE" -E -i database/02_SeedData.sql
sqlcmd -S "YOUR_SERVER_INSTANCE" -E -i database/03_StoredProcedures.sql
```

### Connection String Configuration
Update the connection string in `src/PayrollSystem.Api/appsettings.json` with your server instance name:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_INSTANCE;Database=TymLabs;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

---

## 3. Assumptions & Edge Cases Handled

1. **Rounding:** Rupee values are rounded to the nearest integer using standard midpoint rounding (`Math.Round(..., 0, MidpointRounding.AwayFromZero)`), matching the Ravi Sharma example in the brief.
2. **Missing Attendance:** If an employee has no attendance record submitted for the selected month, days present defaults to 0 to prevent unverified salary disbursement.
3. **Zero Attendance:** When days present is 0, Gross Pay is 0 and Net Pay is floored at 0 so that statutory deductions do not create a negative payout.
4. **Division by Zero:** Guarded in both C# validation and SQL (`NULLIF` / `CASE`) to prevent runtime crashes if working days are passed as 0.
5. **Run Immutability:** Enforced at three levels:
   - Application Service: Checks if month/year has already been processed before invoking the stored procedure.
   - Stored Procedure: `usp_RunPayroll` checks existing finalized status and raises custom error 51000.
   - Database Triggers: `trg_EnforcePayrollRunsImmutability` and `trg_EnforcePayrollDetailsImmutability` block direct `UPDATE` or `DELETE` statements on finalized records.
6. **Concurrency:** `usp_RunPayroll` executes within an explicit SQL transaction with `SET XACT_ABORT ON` to prevent partial payroll commits.
7. **Duplicate Run Protection:** Attempting to process an already finalized payroll run returns an HTTP 409 Conflict with a clear error message.

---

## 4. What Was Not Completed & Future Work

Given the time constraints of the assessment, the following items were intentionally scoped out and would be implemented next:

1. **Configurable Salary Structure:** Support for dynamic allowances (HRA, Special Allowance, Medical) and custom deduction heads instead of a single basic salary field.
2. **Automated PDF Payslip Generation & Emailing:** Server-side PDF generation (e.g., using QuestPDF or Puppeteer) with background queuing to email payslips to employees upon finalization.
3. **Role-Based Access Control (RBAC):** Authentication (JWT) separating HR admin actions (run payroll, modify attendance) from employee actions (view personal payslip only).
4. **Tax Calculations (TDS):** Dynamic income tax computation under old vs. new tax regimes including standard deduction and investment declarations.
