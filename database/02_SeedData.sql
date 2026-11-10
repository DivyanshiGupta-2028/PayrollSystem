-- ============================================================================
-- File: 02_SeedData.sql
-- Description: Inserts initial seed data (Departments, Employees, Attendance)
-- Database: TymLabs
-- ============================================================================

USE TymLabs;
GO

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. Seed Departments (3 departments)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentCode = 'ENG')
    INSERT INTO dbo.Departments (DepartmentCode, DepartmentName) VALUES ('ENG', 'Engineering');

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentCode = 'HR')
    INSERT INTO dbo.Departments (DepartmentCode, DepartmentName) VALUES ('HR', 'Human Resources');

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentCode = 'FIN')
    INSERT INTO dbo.Departments (DepartmentCode, DepartmentName) VALUES ('FIN', 'Finance');
GO

DECLARE @EngDeptId INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentCode = 'ENG');
DECLARE @HrDeptId  INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentCode = 'HR');
DECLARE @FinDeptId INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentCode = 'FIN');

-- ----------------------------------------------------------------------------
-- 2. Seed Employees (6 employees across 3 departments)
-- Includes "Ravi Sharma" matching the technical assessment brief example:
-- Basic: ₹30,000, Working days: 26, Days present: 24
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP001')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP001', 'Ravi', 'Sharma', 'ravi.sharma@company.com', @EngDeptId, 'Senior Software Engineer', 30000.00, '2023-01-15', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP002')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP002', 'Priya', 'Patel', 'priya.patel@company.com', @EngDeptId, 'Frontend Developer', 45000.00, '2023-04-10', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP003')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP003', 'Amit', 'Verma', 'amit.verma@company.com', @HrDeptId, 'HR Specialist', 28000.00, '2022-08-01', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP004')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP004', 'Sneha', 'Reddy', 'sneha.reddy@company.com', @FinDeptId, 'Financial Analyst', 52000.00, '2021-11-20', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP005')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP005', 'Vikram', 'Singh', 'vikram.singh@company.com', @EngDeptId, 'QA Engineer', 35000.00, '2023-09-05', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = 'EMP006')
    INSERT INTO dbo.Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, Designation, BasicSalary, DateOfJoining, IsActive)
    VALUES ('EMP006', 'Ananya', 'Gupta', 'ananya.gupta@company.com', @HrDeptId, 'Talent Acquisition Lead', 40000.00, '2024-02-15', 1);
GO

-- ----------------------------------------------------------------------------
-- 3. Seed Monthly Attendance
-- We seed attendance for:
--   - Current Month (11/2026 - November 2026)
--   - Previous Month (10/2026 - October 2026)
-- ----------------------------------------------------------------------------
DECLARE @Emp1 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP001');
DECLARE @Emp2 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP002');
DECLARE @Emp3 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP003');
DECLARE @Emp4 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP004');
DECLARE @Emp5 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP005');
DECLARE @Emp6 INT = (SELECT EmployeeId FROM dbo.Employees WHERE EmployeeCode = 'EMP006');

-- Current Month: November 2026 (Month 11, Year 2026)
-- EMP001 (Ravi Sharma): 26 working days, 24 days present -> exactly matches the brief!
IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp1 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp1, 11, 2026, 26, 24.0, '2 days casual leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp2 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp2, 11, 2026, 26, 26.0, 'Full attendance');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp3 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp3, 11, 2026, 26, 22.0, '4 days medical leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp4 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp4, 11, 2026, 26, 25.0, '1 day leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp5 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp5, 11, 2026, 26, 20.0, '6 days leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp6 AND PayrollMonth = 11 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp6, 11, 2026, 26, 26.0, 'Full attendance');


-- Previous Month: October 2026 (Month 10, Year 2026)
IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp1 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp1, 10, 2026, 25, 25.0, 'Full attendance');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp2 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp2, 10, 2026, 25, 24.0, '1 day leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp3 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp3, 10, 2026, 25, 25.0, 'Full attendance');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp4 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp4, 10, 2026, 25, 23.0, '2 days leave');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp5 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp5, 10, 2026, 25, 25.0, 'Full attendance');

IF NOT EXISTS (SELECT 1 FROM dbo.MonthlyAttendance WHERE EmployeeId = @Emp6 AND PayrollMonth = 10 AND PayrollYear = 2026)
    INSERT INTO dbo.MonthlyAttendance (EmployeeId, PayrollMonth, PayrollYear, TotalWorkingDays, DaysPresent, Remarks)
    VALUES (@Emp6, 10, 2026, 25, 25.0, 'Full attendance');
GO

PRINT 'Seed data inserted successfully.';
GO

