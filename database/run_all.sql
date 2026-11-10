-- Master script to setup schema, seed data, and stored procedures in sequence
:setvar DatabasePath ""

PRINT 'Starting TymLabs database setup...';
GO

-- 1. Tables and constraints
:r 01_Schema.sql
GO

-- 2. Seed data
:r 02_SeedData.sql
GO

-- 3. Stored procedures
:r 03_StoredProcedures.sql
GO

PRINT 'TymLabs database setup completed.';
GO
