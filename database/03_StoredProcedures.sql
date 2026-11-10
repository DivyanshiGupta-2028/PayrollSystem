-- ============================================================================
-- File: 03_StoredProcedures.sql
-- Description: Stored procedures for Payroll calculations, retrievals, 
--              and payslip generation.
-- Database: TymLabs
-- ============================================================================

USE TymLabs;
GO

-- ----------------------------------------------------------------------------
-- 1. usp_RunPayroll
-- Calculates and saves the payroll run for the given month and year.
-- Enforces immutability: returns error code if already processed.
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_RunPayroll
    @Month      INT,
    @Year       INT,
    @CreatedBy  NVARCHAR(100) = 'HR System'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Parameter Validation
    IF @Month IS NULL OR @Month < 1 OR @Month > 12
    BEGIN
        THROW 50001, 'Invalid Month: Month must be between 1 and 12.', 1;
    END

    IF @Year IS NULL OR @Year < 2000 OR @Year > 2100
    BEGIN
        THROW 50002, 'Invalid Year: Year must be a valid four-digit year (>= 2000).', 1;
    END

    -- Check if payroll run already exists for this Month/Year (Immutability rule)
    IF EXISTS (SELECT 1 FROM dbo.PayrollRuns WHERE PayrollMonth = @Month AND PayrollYear = @Year)
    BEGIN
        THROW 51000, 'PAYROLL_ALREADY_EXISTS: A finalized payroll run already exists for this month and year.', 1;
    END

    -- Check if any active employees exist
    IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE IsActive = 1)
    BEGIN
        THROW 50003, 'No active employees found to process payroll.', 1;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Temporary table to hold computed payroll calculations
        CREATE TABLE #CalculatedPayroll (
            EmployeeId       INT             NOT NULL,
            BasicSalary      DECIMAL(18,2)   NOT NULL,
            WorkingDays      INT             NOT NULL,
            DaysPresent      DECIMAL(5,2)    NOT NULL,
            GrossPay         DECIMAL(18,2)   NOT NULL,
            PFDeduction      DECIMAL(18,2)   NOT NULL,
            ProfessionalTax  DECIMAL(18,2)   NOT NULL,
            NetPay           DECIMAL(18,2)   NOT NULL
        );

        -- Perform Calculation based on business rules:
        -- Gross Pay = (Basic Salary / Total Working Days) * Days Present
        -- PF Deduction = 12% of Basic Salary
        -- Professional Tax = Flat 200
        -- Net Pay = Gross Pay - PF - Professional Tax
        -- (Uses ROUND(..., 0) to align with nearest rupee specification in brief)
        INSERT INTO #CalculatedPayroll (
            EmployeeId,
            BasicSalary,
            WorkingDays,
            DaysPresent,
            GrossPay,
            PFDeduction,
            ProfessionalTax,
            NetPay
        )
        SELECT 
            e.EmployeeId,
            e.BasicSalary,
            COALESCE(att.TotalWorkingDays, 26) AS WorkingDays,
            COALESCE(att.DaysPresent, 0.0)    AS DaysPresent,
            
            -- Gross Pay
            CASE 
                WHEN COALESCE(att.TotalWorkingDays, 26) = 0 THEN 0.00
                ELSE ROUND((e.BasicSalary / CAST(COALESCE(att.TotalWorkingDays, 26) AS DECIMAL(18,4))) * COALESCE(att.DaysPresent, 0.0), 0)
            END AS GrossPay,

            -- PF Deduction: 12% of Basic Salary
            ROUND(e.BasicSalary * 0.12, 0) AS PFDeduction,

            -- Professional Tax: Flat 200 (if gross pay > 0, otherwise 0)
            CASE 
                WHEN COALESCE(att.DaysPresent, 0.0) = 0 THEN 0.00
                ELSE 200.00
            END AS ProfessionalTax,

            -- Net Pay: Gross Pay - PF - Professional Tax
            -- Guarded: net pay does not drop below 0 for absent employees
            CASE 
                WHEN (
                    CASE 
                        WHEN COALESCE(att.TotalWorkingDays, 26) = 0 THEN 0.00
                        ELSE ROUND((e.BasicSalary / CAST(COALESCE(att.TotalWorkingDays, 26) AS DECIMAL(18,4))) * COALESCE(att.DaysPresent, 0.0), 0)
                    END 
                    - ROUND(e.BasicSalary * 0.12, 0) 
                    - (CASE WHEN COALESCE(att.DaysPresent, 0.0) = 0 THEN 0.00 ELSE 200.00 END)
                ) < 0.00 THEN 0.00
                ELSE (
                    CASE 
                        WHEN COALESCE(att.TotalWorkingDays, 26) = 0 THEN 0.00
                        ELSE ROUND((e.BasicSalary / CAST(COALESCE(att.TotalWorkingDays, 26) AS DECIMAL(18,4))) * COALESCE(att.DaysPresent, 0.0), 0)
                    END 
                    - ROUND(e.BasicSalary * 0.12, 0) 
                    - (CASE WHEN COALESCE(att.DaysPresent, 0.0) = 0 THEN 0.00 ELSE 200.00 END)
                )
            END AS NetPay
        FROM dbo.Employees e
        LEFT JOIN dbo.MonthlyAttendance att 
            ON e.EmployeeId = att.EmployeeId 
            AND att.PayrollMonth = @Month 
            AND att.PayrollYear = @Year
        WHERE e.IsActive = 1;

        -- Insert Header into PayrollRuns
        DECLARE @NewRunId INT;
        INSERT INTO dbo.PayrollRuns (
            PayrollMonth,
            PayrollYear,
            RunDate,
            TotalEmployees,
            TotalGrossPay,
            TotalPFDeduction,
            TotalProfessionalTax,
            TotalNetPay,
            Status,
            CreatedBy
        )
        SELECT 
            @Month,
            @Year,
            SYSUTCDATETIME(),
            COUNT(*),
            COALESCE(SUM(GrossPay), 0),
            COALESCE(SUM(PFDeduction), 0),
            COALESCE(SUM(ProfessionalTax), 0),
            COALESCE(SUM(NetPay), 0),
            'Finalized',
            @CreatedBy
        FROM #CalculatedPayroll;

        SET @NewRunId = SCOPE_IDENTITY();

        -- Insert Detail records
        INSERT INTO dbo.PayrollDetails (
            PayrollRunId,
            EmployeeId,
            BasicSalary,
            WorkingDays,
            DaysPresent,
            GrossPay,
            PFDeduction,
            ProfessionalTax,
            NetPay
        )
        SELECT 
            @NewRunId,
            EmployeeId,
            BasicSalary,
            WorkingDays,
            DaysPresent,
            GrossPay,
            PFDeduction,
            ProfessionalTax,
            NetPay
        FROM #CalculatedPayroll;

        DROP TABLE #CalculatedPayroll;

        COMMIT TRANSACTION;

        -- Return the newly created RunId
        SELECT @NewRunId AS PayrollRunId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- ----------------------------------------------------------------------------
-- 2. usp_GetPayrollRun
-- Returns the saved payroll run summary and details for a given Month and Year.
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_GetPayrollRun
    @Month INT,
    @Year  INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Header Summary
    SELECT 
        pr.PayrollRunId,
        pr.PayrollMonth,
        pr.PayrollYear,
        pr.RunDate,
        pr.TotalEmployees,
        pr.TotalGrossPay,
        pr.TotalPFDeduction,
        pr.TotalProfessionalTax,
        pr.TotalNetPay,
        pr.Status,
        pr.CreatedBy
    FROM dbo.PayrollRuns pr
    WHERE pr.PayrollMonth = @Month AND pr.PayrollYear = @Year;

    -- Result Set 2: Employee Line Items
    SELECT 
        pd.PayrollDetailId,
        pd.PayrollRunId,
        pd.EmployeeId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        (e.FirstName + ' ' + e.LastName) AS Name,
        e.Email,
        d.DepartmentName,
        e.Designation,
        pd.BasicSalary,
        pd.WorkingDays,
        pd.DaysPresent,
        pd.GrossPay,
        pd.PFDeduction,
        pd.ProfessionalTax,
        pd.NetPay
    FROM dbo.PayrollRuns pr
    INNER JOIN dbo.PayrollDetails pd ON pr.PayrollRunId = pd.PayrollRunId
    INNER JOIN dbo.Employees e ON pd.EmployeeId = e.EmployeeId
    INNER JOIN dbo.Departments d ON e.DepartmentId = d.DepartmentId
    WHERE pr.PayrollMonth = @Month AND pr.PayrollYear = @Year
    ORDER BY e.EmployeeId ASC;
END;
GO

-- ----------------------------------------------------------------------------
-- 3. usp_GetEmployeePayslip
-- Returns individual payslip details for an employee in a given run.
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_GetEmployeePayslip
    @RunId      INT,
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        pr.PayrollRunId,
        pr.PayrollMonth,
        pr.PayrollYear,
        pr.RunDate,
        pr.Status,
        e.EmployeeId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        (e.FirstName + ' ' + e.LastName) AS Name,
        e.Email,
        d.DepartmentName,
        e.Designation,
        e.DateOfJoining,
        pd.BasicSalary,
        pd.WorkingDays,
        pd.DaysPresent,
        (pd.WorkingDays - pd.DaysPresent) AS DaysAbsent,
        pd.GrossPay,
        pd.PFDeduction,
        pd.ProfessionalTax,
        (pd.PFDeduction + pd.ProfessionalTax) AS TotalDeductions,
        pd.NetPay
    FROM dbo.PayrollRuns pr
    INNER JOIN dbo.PayrollDetails pd ON pr.PayrollRunId = pd.PayrollRunId
    INNER JOIN dbo.Employees e ON pd.EmployeeId = e.EmployeeId
    INNER JOIN dbo.Departments d ON e.DepartmentId = d.DepartmentId
    WHERE pr.PayrollRunId = @RunId AND e.EmployeeId = @EmployeeId;
END;
GO

-- ----------------------------------------------------------------------------
-- 4. usp_GetEmployees
-- Returns list of all active employees with department information.
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_GetEmployees
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        e.EmployeeId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        (e.FirstName + ' ' + e.LastName) AS Name,
        e.Email,
        e.DepartmentId,
        d.DepartmentName,
        e.Designation,
        e.BasicSalary,
        e.DateOfJoining,
        e.IsActive,
        e.CreatedAt
    FROM dbo.Employees e
    INNER JOIN dbo.Departments d ON e.DepartmentId = d.DepartmentId
    WHERE e.IsActive = 1
    ORDER BY e.EmployeeId ASC;
END;
GO

-- ----------------------------------------------------------------------------
-- 5. usp_GetAllPayrollRuns
-- Returns list of all historical runs for easy dashboard navigation.
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_GetAllPayrollRuns
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        pr.PayrollRunId,
        pr.PayrollMonth,
        pr.PayrollYear,
        pr.RunDate,
        pr.TotalEmployees,
        pr.TotalGrossPay,
        pr.TotalPFDeduction,
        pr.TotalProfessionalTax,
        pr.TotalNetPay,
        pr.Status,
        pr.CreatedBy
    FROM dbo.PayrollRuns pr
    ORDER BY pr.PayrollYear DESC, pr.PayrollMonth DESC;
END;
GO

PRINT 'Stored procedures created successfully.';
GO

