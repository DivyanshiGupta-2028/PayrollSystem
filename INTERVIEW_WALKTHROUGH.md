# Technical Interview Walkthrough & Defense Guide
## Position: Full Stack Developer (.NET / SQL Server) — 2–3 Years Experience
## Project: Employee Payroll Run Module

This guide is designed for your **30–45 minute virtual technical interview**. It explains how every part of the system works, the reasoning behind each architectural decision, and provides sample answers to questions the interviewers are likely to ask.

---

## 1. High-Level Architecture (The 30-Second Elevator Pitch)

> *"I structured the solution around a Clean Layered Architecture with strict Separation of Concerns, following the exact flow outlined in the assessment brief:*
> 
> **UI (Vanilla JS + Responsive CSS + Print Layout)**  
> $\downarrow$ calls REST API via `fetch()`  
> **API Controllers (ASP.NET Core Web API with .NET 9)**  
> $\downarrow$ delegates to  
> **Service Layer (Domain Calculation & Business Validation)**  
> $\downarrow$ calls  
> **Repository Layer (Dapper + ADO.NET with `IDbConnectionFactory`)**  
> $\downarrow$ executes  
> **Stored Procedures (`usp_RunPayroll` wrapped in transactions)**  
> $\downarrow$ persists in  
> **SQL Server Database (Foreign keys, constraints, and immutability triggers)**
> 
> *The system guarantees immutability across three defense layers (API, Stored Procedure, and Database Triggers), handles financial rounding to the nearest rupee matching the Ravi Sharma specification, and includes 22 unit tests with xUnit."*

---

## 2. Deep Dive by Component

### A. Database Design & SQL Server (`database/`)

#### 1. Schema Tables (`01_Schema.sql`)
- **`Departments`**: Lookup table (`DepartmentId`, `DepartmentCode`, `DepartmentName`).
- **`Employees`**: Master records (`EmployeeCode`, `FirstName`, `LastName`, `Email`, `DepartmentId`, `BasicSalary`, `DateOfJoining`, `IsActive`).
  - Index on `DepartmentId` for efficient joins.
  - Index on `IsActive` filtered for active payroll processing.
  - `CHECK (BasicSalary >= 0)` prevents invalid salary data.
- **`MonthlyAttendance`**: Records monthly attendance per employee.
  - Columns: `EmployeeId`, `PayrollMonth`, `PayrollYear`, `TotalWorkingDays`, `DaysPresent`.
  - Composite Unique Constraint: `UQ_MonthlyAttendance_Emp_Month_Year (EmployeeId, PayrollMonth, PayrollYear)` prevents duplicate attendance entries for the same month.
  - Check Constraints: `PayrollMonth BETWEEN 1 AND 12`, `DaysPresent <= TotalWorkingDays`.
- **`PayrollRuns`** (Header):
  - Summary metadata: `PayrollMonth`, `PayrollYear`, `RunDate`, `TotalEmployees`, `TotalGrossPay`, `TotalPFDeduction`, `TotalProfessionalTax`, `TotalNetPay`, `Status`.
  - Unique Constraint: `UQ_PayrollRuns_Month_Year (PayrollMonth, PayrollYear)` guarantees at the database level that only one payroll run can ever exist for a given period.
- **`PayrollDetails`** (Line Items):
  - Snapshot of each employee's salary at the time of payroll execution: `BasicSalary`, `WorkingDays`, `DaysPresent`, `GrossPay`, `PFDeduction`, `ProfessionalTax`, `NetPay`.
  - Unique Constraint: `UQ_PayrollDetails_Run_Emp (PayrollRunId, EmployeeId)` prevents duplicate detail rows.

#### 2. How Immutability Is Enforced (3 Layers of Defense)
The assessment brief states: *"Once a payroll run is finalised, it cannot be edited or deleted."*
We enforce this at **three distinct layers**:
1. **Application Layer (`PayrollService.cs`)**: Checks `PayrollRunExistsAsync()` before starting, immediately throwing `PayrollConflictException` (mapped to HTTP 409 Conflict).
2. **Stored Procedure Layer (`usp_RunPayroll`)**: Checks `IF EXISTS (SELECT 1 FROM dbo.PayrollRuns WHERE PayrollMonth = @Month AND PayrollYear = @Year) THROW 51000...`
3. **Database Trigger Layer (`trg_EnforcePayrollRunsImmutability` & `trg_EnforcePayrollDetailsImmutability`)**:
   ```sql
   CREATE TRIGGER dbo.trg_EnforcePayrollRunsImmutability
   ON dbo.PayrollRuns
   AFTER UPDATE, DELETE
   AS
   BEGIN
       IF EXISTS (SELECT 1 FROM deleted WHERE Status = 'Finalized')
       BEGIN
           RAISERROR ('Immutability Violation: Finalized payroll runs cannot be edited or deleted.', 16, 1);
           ROLLBACK TRANSACTION;
       END
   END;
   ```
   *Even if someone connects directly to SQL Server Management Studio (SSMS) with administrative credentials and tries to run an `UPDATE` or `DELETE` query, the database engine aborts the transaction!*

#### 3. Stored Procedure: `dbo.usp_RunPayroll` (`03_StoredProcedures.sql`)
- Wrapped in an explicit transaction:
  ```sql
  SET XACT_ABORT ON;
  BEGIN TRANSACTION;
  ...
  COMMIT TRANSACTION;
  ```
- Validates `@Month BETWEEN 1 AND 12` and `@Year >= 2000`.
- Joins active employees (`IsActive = 1`) with `MonthlyAttendance` using a `LEFT JOIN`.
- Handles missing attendance gracefully using `COALESCE(att.TotalWorkingDays, 26)` and `COALESCE(att.DaysPresent, 0.0)`.
- Calculates Gross Pay, PF, PT, and Net Pay into a `#CalculatedPayroll` temporary table.
- Inserts the header into `PayrollRuns`, captures `SCOPE_IDENTITY()`, and inserts all line items into `PayrollDetails`.
- Returns the new `PayrollRunId`.

---

### B. Business Logic & Calculation Engine (`PayrollSystem.Core`)

#### The Formula Breakdown:
1. **Gross Pay:**
   $$\text{Gross Pay} = \left(\frac{\text{Basic Salary}}{\text{Total Working Days}}\right) \times \text{Days Present}$$
   - Rounded to nearest rupee using `ROUND(..., 0)` (`MidpointRounding.AwayFromZero`), exactly matching the Ravi Sharma example.
2. **PF Deduction:**
   $$\text{PF Deduction} = 12\% \times \text{Basic Salary}$$
3. **Professional Tax:**
   - Flat ₹200 per month (waived to ₹0 if employee has 0 attendance).
4. **Net Pay:**
   $$\text{Net Pay} = \text{Gross Pay} - \text{PF Deduction} - \text{Professional Tax}$$

#### Edge Cases Handled in Domain Service & Stored Procedure:
- **Zero Days Present:** Gross Pay = 0. Deductions are capped so Net Pay does not drop below ₹0 (preventing negative salary disbursement).
- **Missing Attendance Entry:** Defaulted to standard working days (26) and 0 days present.
- **Zero Working Days:** Guarded against division by zero.
- **Days Present > Working Days:** Blocked with an `ArgumentException`.
- **Negative Values:** Guarded with `ArgumentOutOfRangeException`.

---

### C. Data Access Layer (`PayrollSystem.Infrastructure`)

#### Why Dapper + ADO.NET?
- **Assessment Requirement:** Specifically listed as preferred technology.
- **Performance:** Micro-ORM with virtually zero overhead; maps raw query results directly to strongly-typed C# objects.
- **Direct Stored Procedure Support:** Handles multi-result sets cleanly using `QueryMultipleAsync` (reading both Header and Details in a single network round-trip).
- **Security:** Every query uses parameterized dynamic parameters (`DynamicParameters`), completely preventing SQL injection.

#### Connection Management:
- Uses `IDbConnectionFactory` and `SqlConnectionFactory`.
- Connections are created inside `using` blocks (`using var connection = _connectionFactory.CreateConnection();`), ensuring connections are returned to the ADO.NET connection pool immediately.

---

### D. Web API Layer (`PayrollSystem.Api`)

#### 1. REST Endpoints
- `GET /api/employees`: Returns 200 OK array of active employees.
- `POST /api/payroll/run`: Accepts `{ month, year, createdBy }`. Returns **201 Created** with the run summary, or **409 Conflict** if already finalized.
- `GET /api/payroll/{month}/{year}`: Returns **200 OK** with run and details, or **404 Not Found**. Supports optional pagination: `?page=1&pageSize=10`.
- `GET /api/payroll/{runId}/slip/{employeeId}`: Returns individual payslip including Net Pay converted to English words for printing.
- `GET /api/payroll/history`: Returns historical runs for the UI dropdown.

#### 2. Global Exception Handling Middleware (`GlobalExceptionHandlingMiddleware.cs`)
- Intercepts exceptions and transforms them into standard HTTP status codes:
  - `PayrollConflictException` $\rightarrow$ **409 Conflict**
  - `NotFoundException` $\rightarrow$ **404 Not Found**
  - `ValidationException` / `ArgumentException` $\rightarrow$ **400 Bad Request**
  - Unhandled exceptions $\rightarrow$ **500 Internal Server Error** (with logging, without leaking stack traces).

---

### E. Frontend Single-Page Interface (`wwwroot/`)

- Built with modern HTML5, responsive CSS, and Vanilla JavaScript (Fetch API).
- **No external heavy frameworks needed:** runs natively from ASP.NET Core `UseStaticFiles()`.
- **Interactive features:**
  - Month/Year selector with quick-select historical runs dropdown.
  - "Run Payroll" button with loading spinner and disabled state.
  - Alert banners for success, 404, and 409 Conflict.
  - Metric summary cards (Total Gross, Total PF, Total PT, Total Net).
  - Searchable, filterable (by department), and paginated data table.
  - **Printable Payslip Modal:** Formatted like a real salary slip with earnings, deductions, net salary in words, and signature block. Triggering `printPayslip()` uses `@media print` CSS to hide browser UI and print an A4 salary slip.

---

### F. Unit Testing (`tests/PayrollSystem.Tests`)

- **22 passing xUnit unit tests**:
  - `Calculate_RaviSharmaExample_MatchesBriefSpecificationExactly`: Verifies Basic ₹30,000, Working Days 26, Days Present 24 $\rightarrow$ Gross ₹27,692, PF ₹3,600, PT ₹200, Net ₹23,892.
  - `Calculate_FullAttendance_GrossMatchesBasicSalary`
  - `Calculate_ZeroDaysPresent_ReturnsZeroPaySafely`
  - `Calculate_ZeroWorkingDays_HandlesDivisionByZeroGracefully`
  - `Calculate_HalfDayAttendance_CalculatesAccurately`
  - `Calculate_NegativeBasicSalary_ThrowsArgumentOutOfRangeException`
  - `Calculate_DaysPresentExceedsWorkingDays_ThrowsArgumentException`
  - `ConvertAmountToWords_ConvertsCorrectly`
  - `RunPayrollAsync_DuplicateRun_ThrowsPayrollConflictException` (409 logic)
  - `GetPayrollRunAsync_WithPagination_ReturnsPaginatedItemsAndMetadata`
  - `GetEmployeePayslipAsync_NotFound_ThrowsNotFoundException`

---

## 3. Top Interview Questions & How to Answer Them

### Q1: *"Why did you use both C# and SQL Stored Procedures for the calculation?"*
**Your Answer:**
> *"The brief specified a stored procedure `usp_RunPayroll(@Month, @Year)` to calculate and save the payroll run. Putting the calculation in SQL ensures that the entire batch runs inside an atomic transaction right next to the data, avoiding network chat. At the same time, I also built a pure domain service in C# (`PayrollCalculationService`). This allows us to unit test the calculation logic in isolation using xUnit with lightning speed without requiring a live database connection."*

### Q2: *"Why did you choose Dapper over Entity Framework Core?"*
**Your Answer:**
> *"First, the assessment explicitly asked for Dapper + ADO.NET. Second, for payroll processing where calculations are executed via stored procedures and generate large result sets, Dapper is lightweight, has virtually zero mapping overhead, and gives complete control over SQL execution. It also supports `QueryMultipleAsync`, which lets us fetch both the Run Header and Employee Details in a single database round-trip."*

### Q3: *"How did you handle the Immutability requirement?"*
**Your Answer:**
> *"Immutability in financial systems is critical. I implemented defense-in-depth across three layers:
> 1. At the API service level, we check if a run exists and return HTTP 409 Conflict before doing work.
> 2. In the Stored Procedure, an `IF EXISTS` check raises an error.
> 3. At the database level, I added `AFTER UPDATE, DELETE` triggers on `PayrollRuns` and `PayrollDetails` that roll back any modification if `Status = 'Finalized'`. Even an administrator running manual SQL queries cannot alter finalized records."*

### Q4: *"How did you approach financial rounding?"*
**Your Answer:**
> *"In the assessment example for Ravi Sharma (Basic ₹30,000, 26 working days, 24 days present), the gross pay is given as ₹27,692. Mathematically, 30,000 ÷ 26 × 24 = 27,692.30769. Rounding to the nearest integer rupee (`ROUND(..., 0)` using `MidpointRounding.AwayFromZero`) produces ₹27,692 exactly. Deducting ₹3,600 PF and ₹200 PT gives ₹23,892. Both my SQL stored procedure and C# service use this rounding logic, while storing values in `DECIMAL(18,2)` columns for financial accuracy."*

### Q5: *"What edge cases did you consider?"*
**Your Answer:**
> *"I considered:
> 1. **Zero attendance:** An employee present 0 days gets ₹0 gross pay, and statutory deductions are guarded so net pay does not become negative.
> 2. **Missing attendance records:** `LEFT JOIN` with `COALESCE` defaults to 0 days present so the payroll run doesn't fail.
> 3. **Division by zero:** If working days is 0, the denominator is guarded.
> 4. **Concurrency:** `usp_RunPayroll` runs under `SET XACT_ABORT ON` within an explicit transaction to ensure atomic execution."*

---

## 4. Interview Checklist Before Joining Call
- [ ] Ensure SQL Server is running.
- [ ] Run `dotnet test` to confirm all 22 tests pass.
- [ ] Start the project: `dotnet run --project src/PayrollSystem.Api/PayrollSystem.Api.csproj --launch-profile http`.
- [ ] Open [http://localhost:5229](http://localhost:5229) in browser.
- [ ] Have [http://localhost:5229/scalar/v1](http://localhost:5229/scalar/v1) ready to showcase interactive OpenAPI documentation.

