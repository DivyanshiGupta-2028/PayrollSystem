-- Database Schema for Payroll System
IF DB_ID('TymLabs') IS NULL
BEGIN
    CREATE DATABASE TymLabs;
END
GO

USE TymLabs;
GO

-- Drop tables if rebuilding
IF OBJECT_ID('dbo.PayrollDetails', 'U') IS NOT NULL DROP TABLE dbo.PayrollDetails;
IF OBJECT_ID('dbo.PayrollRuns', 'U') IS NOT NULL DROP TABLE dbo.PayrollRuns;
IF OBJECT_ID('dbo.MonthlyAttendance', 'U') IS NOT NULL DROP TABLE dbo.MonthlyAttendance;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.Departments', 'U') IS NOT NULL DROP TABLE dbo.Departments;
GO

-- 1. Departments Table
CREATE TABLE dbo.Departments (
    DepartmentId    INT IDENTITY(1,1)   NOT NULL,
    DepartmentCode  NVARCHAR(20)        NOT NULL,
    DepartmentName  NVARCHAR(100)       NOT NULL,
    CreatedAt       DATETIME2(7)        NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (DepartmentId),
    CONSTRAINT UQ_Departments_Code UNIQUE (DepartmentCode)
);
GO

-- 2. Employees Table
CREATE TABLE dbo.Employees (
    EmployeeId      INT IDENTITY(1,1)   NOT NULL,
    EmployeeCode    NVARCHAR(20)        NOT NULL,
    FirstName       NVARCHAR(50)        NOT NULL,
    LastName        NVARCHAR(50)        NOT NULL,
    Email           NVARCHAR(120)       NOT NULL,
    DepartmentId    INT                 NOT NULL,
    Designation     NVARCHAR(100)       NOT NULL,
    BasicSalary     DECIMAL(18,2)       NOT NULL,
    DateOfJoining   DATE                NOT NULL,
    IsActive        BIT                 NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1,
    CreatedAt       DATETIME2(7)        NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2(7)        NULL,
    CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED (EmployeeId),
    CONSTRAINT UQ_Employees_Code UNIQUE (EmployeeCode),
    CONSTRAINT UQ_Employees_Email UNIQUE (Email),
    CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
    CONSTRAINT CK_Employees_BasicSalary CHECK (BasicSalary >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Employees_DepartmentId ON dbo.Employees(DepartmentId);
CREATE NONCLUSTERED INDEX IX_Employees_IsActive ON dbo.Employees(IsActive) INCLUDE (EmployeeCode, BasicSalary);
GO

-- 3. MonthlyAttendance Table
CREATE TABLE dbo.MonthlyAttendance (
    AttendanceId        INT IDENTITY(1,1)   NOT NULL,
    EmployeeId          INT                 NOT NULL,
    PayrollMonth        INT                 NOT NULL,
    PayrollYear         INT                 NOT NULL,
    TotalWorkingDays    INT                 NOT NULL,
    DaysPresent         DECIMAL(5,2)        NOT NULL,
    Remarks             NVARCHAR(250)       NULL,
    CreatedAt           DATETIME2(7)        NOT NULL CONSTRAINT DF_MonthlyAttendance_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MonthlyAttendance PRIMARY KEY CLUSTERED (AttendanceId),
    CONSTRAINT FK_MonthlyAttendance_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
    CONSTRAINT UQ_MonthlyAttendance_Emp_Month_Year UNIQUE (EmployeeId, PayrollMonth, PayrollYear),
    CONSTRAINT CK_MonthlyAttendance_Month CHECK (PayrollMonth BETWEEN 1 AND 12),
    CONSTRAINT CK_MonthlyAttendance_Year CHECK (PayrollYear >= 2000),
    CONSTRAINT CK_MonthlyAttendance_WorkingDays CHECK (TotalWorkingDays >= 0 AND TotalWorkingDays <= 31),
    CONSTRAINT CK_MonthlyAttendance_DaysPresent CHECK (DaysPresent >= 0.0 AND DaysPresent <= TotalWorkingDays)
);
GO

CREATE NONCLUSTERED INDEX IX_MonthlyAttendance_Period ON dbo.MonthlyAttendance(PayrollMonth, PayrollYear) INCLUDE (EmployeeId, TotalWorkingDays, DaysPresent);
GO

-- 4. PayrollRuns Table
CREATE TABLE dbo.PayrollRuns (
    PayrollRunId            INT IDENTITY(1,1)   NOT NULL,
    PayrollMonth            INT                 NOT NULL,
    PayrollYear             INT                 NOT NULL,
    RunDate                 DATETIME2(7)        NOT NULL CONSTRAINT DF_PayrollRuns_RunDate DEFAULT SYSUTCDATETIME(),
    TotalEmployees          INT                 NOT NULL,
    TotalGrossPay           DECIMAL(18,2)       NOT NULL,
    TotalPFDeduction        DECIMAL(18,2)       NOT NULL,
    TotalProfessionalTax    DECIMAL(18,2)       NOT NULL,
    TotalNetPay             DECIMAL(18,2)       NOT NULL,
    Status                  NVARCHAR(30)        NOT NULL CONSTRAINT DF_PayrollRuns_Status DEFAULT 'Finalized',
    CreatedBy               NVARCHAR(100)       NOT NULL CONSTRAINT DF_PayrollRuns_CreatedBy DEFAULT 'SYSTEM',
    CreatedAt               DATETIME2(7)        NOT NULL CONSTRAINT DF_PayrollRuns_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PayrollRuns PRIMARY KEY CLUSTERED (PayrollRunId),
    CONSTRAINT UQ_PayrollRuns_Period UNIQUE (PayrollMonth, PayrollYear),
    CONSTRAINT CK_PayrollRuns_Month CHECK (PayrollMonth BETWEEN 1 AND 12),
    CONSTRAINT CK_PayrollRuns_Year CHECK (PayrollYear >= 2000),
    CONSTRAINT CK_PayrollRuns_Status CHECK (Status IN ('Draft', 'Finalized', 'Voided'))
);
GO

CREATE NONCLUSTERED INDEX IX_PayrollRuns_Period ON dbo.PayrollRuns(PayrollYear DESC, PayrollMonth DESC);
GO

-- 5. PayrollDetails Table
CREATE TABLE dbo.PayrollDetails (
    PayrollDetailId     INT IDENTITY(1,1)   NOT NULL,
    PayrollRunId        INT                 NOT NULL,
    EmployeeId          INT                 NOT NULL,
    BasicSalary         DECIMAL(18,2)       NOT NULL,
    WorkingDays         INT                 NOT NULL,
    DaysPresent         DECIMAL(5,2)        NOT NULL,
    GrossPay            DECIMAL(18,2)       NOT NULL,
    PFDeduction         DECIMAL(18,2)       NOT NULL,
    ProfessionalTax     DECIMAL(18,2)       NOT NULL,
    NetPay              DECIMAL(18,2)       NOT NULL,
    CreatedAt           DATETIME2(7)        NOT NULL CONSTRAINT DF_PayrollDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PayrollDetails PRIMARY KEY CLUSTERED (PayrollDetailId),
    CONSTRAINT FK_PayrollDetails_PayrollRuns FOREIGN KEY (PayrollRunId) REFERENCES dbo.PayrollRuns(PayrollRunId),
    CONSTRAINT FK_PayrollDetails_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
    CONSTRAINT UQ_PayrollDetails_Run_Emp UNIQUE (PayrollRunId, EmployeeId),
    CONSTRAINT CK_PayrollDetails_GrossPay CHECK (GrossPay >= 0),
    CONSTRAINT CK_PayrollDetails_PF CHECK (PFDeduction >= 0),
    CONSTRAINT CK_PayrollDetails_PT CHECK (ProfessionalTax >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_PayrollDetails_RunId ON dbo.PayrollDetails(PayrollRunId) INCLUDE (EmployeeId, BasicSalary, GrossPay, NetPay);
CREATE NONCLUSTERED INDEX IX_PayrollDetails_EmployeeId ON dbo.PayrollDetails(EmployeeId);
GO

-- Immutability Triggers
CREATE OR ALTER TRIGGER dbo.trg_EnforcePayrollRunsImmutability
ON dbo.PayrollRuns
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE Status = 'Finalized')
    BEGIN
        RAISERROR ('Immutability Violation: Finalized payroll runs cannot be edited or deleted.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_EnforcePayrollDetailsImmutability
ON dbo.PayrollDetails
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 
        FROM deleted d
        INNER JOIN dbo.PayrollRuns pr ON d.PayrollRunId = pr.PayrollRunId
        WHERE pr.Status = 'Finalized'
    )
    BEGIN
        RAISERROR ('Immutability Violation: Line items of a finalized payroll run cannot be edited or deleted.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
