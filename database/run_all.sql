-- ============================================================================
-- File: run_all.sql
-- Description: Master deployment script that creates the database, schema,
--              seeds initial data, and creates stored procedures in sequence.
-- Execution: Run in SQL Server Management Studio (SSMS) or via sqlcmd:
--            sqlcmd -S . -E -i database/run_all.sql
-- ============================================================================

:setvar DatabasePath ""

PRINT '==================================================';
PRINT 'Starting Payroll System Database Setup';
PRINT '==================================================';
GO

-- 1. Create Schema and Tables
:r 01_Schema.sql
GO

-- 2. Seed Initial Employees, Departments, and Attendance
:r 02_SeedData.sql
GO

-- 3. Create Stored Procedures
:r 03_StoredProcedures.sql
GO

PRINT '==================================================';
PRINT 'Payroll System Database Setup Completed Successfully!';
PRINT '==================================================';
GO

