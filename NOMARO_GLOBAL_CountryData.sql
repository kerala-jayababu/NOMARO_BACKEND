/* =====================================================================
   NOMARO_GLOBAL - country master data for new companies
   ---------------------------------------------------------------------
   Idea: NOMARO_GLOBAL keeps, per country, a copy of master tables (leave types, financial years, holidays, banks ...).
   When a company is registered, every table listed in dbo.CountryDataTables is copied from the schema [cd]
   (rows of the company's country) into the new company database - same Ids, so links between the tables stay right.
   A table that already has rows in the new company is left alone.

     dbo.CountryDataTables         which tables are copied, in which order, and columns to clear when captured
     cd.<TableName>                same columns as the company table + CountryCode
     dbo.CaptureCountryData        fills cd.<TableName> for one country from an existing company database
                                   (run it again whenever that country's master data changes)

   Run on NOMARO_GLOBAL. Safe to run more than once.
   ===================================================================== */
-- wrong database: stop every batch below
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script against NOMARO_GLOBAL. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

IF SCHEMA_ID('cd') IS NULL EXEC ('CREATE SCHEMA cd');
GO

/* 1. Which tables are country data -------------------------------------------------- */
IF OBJECT_ID('dbo.CountryDataTables', 'U') IS NULL
    CREATE TABLE dbo.CountryDataTables
    (
        TableName    SYSNAME       NOT NULL CONSTRAINT PK_CountryDataTables PRIMARY KEY,   -- company table name (dbo schema)
        CopyOrder    INT           NOT NULL CONSTRAINT DF_CDT_Order DEFAULT (100),
        ClearColumns VARCHAR(400)  NULL,     -- comma separated columns set to NULL when captured (e.g. status of a live company)
        IsActive     BIT           NOT NULL CONSTRAINT DF_CDT_Active DEFAULT (1),
        Notes        NVARCHAR(200) NULL
    );
GO
INSERT INTO dbo.CountryDataTables (TableName, CopyOrder, ClearColumns, Notes)
SELECT v.* FROM (VALUES
 ('FinancialYears',          10, NULL,                     N'Financial / tax years (India Apr-Mar, UAE Jan-Dec)'),
 ('WorkYears',               11, NULL,                     N'Leave years'),
 ('SalaryMonths',            12, 'SalaryGenerationStatus', N'Salary months; generation status cleared'),
 ('LeaveTypes',              20, NULL,                     N'Leave types by labour law'),
 ('AnnualLeaveTypeConfig',   21, NULL,                     N'Leave type rules per year'),
 ('LeaveTemplates',          22, 'IdApprovedBy',           N'Leave templates'),
 ('LeaveTemplateDetails',    23, NULL,                     N'Leave template lines'),
 ('Holidays',                30, NULL,                     N'Public holidays'),
 ('Banks',                   40, NULL,                     N'Banks of the country'),
 ('BankBranches',            41, NULL,                     N'Bank branches / routing codes'),
 ('GeneralDocumentTypes',    50, NULL,                     N'Document types (Aadhaar, PAN / Emirates ID, Visa ...)'),
 ('GeneralDocumentSubtypes', 51, NULL,                     N'Document subtypes'),
 ('TaxRegimes',              60, NULL,                     N'India tax regimes'),
 ('TaxDeductionSections',    61, NULL,                     N'India 80C / 80D ... sections'),
 ('TaxYearConfigs',          62, NULL,                     N'Tax configuration per year and regime'),
 ('TaxSlabs',                63, NULL,                     N'Tax slabs'),
 ('TaxSurchargeSlabs',       64, NULL,                     N'Surcharge slabs'),
 ('NoticePeriodPolicies',    70, NULL,                     N'Notice period rules')
) AS v(TableName, CopyOrder, ClearColumns, Notes)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryDataTables c WHERE c.TableName = v.TableName);
GO

/* 2. Capture a country's data from an existing company database (same SQL Server) ---------------
   EXEC dbo.CaptureCountryData @CountryCode = 'IND', @SourceDatabase = 'NOMARO_GGHOSPITAL';               -- all tables
   EXEC dbo.CaptureCountryData @CountryCode = 'IND', @SourceDatabase = 'NOMARO_GGHOSPITAL', @TableName = 'Holidays';
   The country's previous rows in cd.<table> are replaced. cd.<table> is created on first use with the source columns
   (without identity). If a company table gets new columns later, drop cd.<table> and capture again. */
CREATE OR ALTER PROCEDURE dbo.CaptureCountryData
    @CountryCode    VARCHAR(5),
    @SourceDatabase SYSNAME,
    @TableName      SYSNAME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_ID(@SourceDatabase) IS NULL
    BEGIN
        RAISERROR('Database %s not found on this server.', 16, 1, @SourceDatabase);
        RETURN;
    END

    DECLARE @t SYSNAME, @clear VARCHAR(400), @src NVARCHAR(400), @dst NVARCHAR(300), @sql NVARCHAR(MAX), @cols NVARCHAR(MAX), @n INT;

    DECLARE tabs CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ClearColumns FROM dbo.CountryDataTables
        WHERE IsActive = 1 AND (@TableName IS NULL OR TableName = @TableName)
        ORDER BY CopyOrder, TableName;
    OPEN tabs;
    FETCH NEXT FROM tabs INTO @t, @clear;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @src = QUOTENAME(@SourceDatabase) + N'.dbo.' + QUOTENAME(@t);
        SET @dst = N'cd.' + QUOTENAME(@t);

        IF OBJECT_ID(@src) IS NULL
            PRINT 'Skipped ' + @t + ': not in ' + @SourceDatabase + '.';
        ELSE
        BEGIN
            -- create cd.<table> with the source columns; UNION ALL drops the identity property
            IF OBJECT_ID(@dst) IS NULL
            BEGIN
                SET @sql = N'SELECT TOP 0 * INTO ' + @dst + N' FROM ' + @src + N' UNION ALL SELECT TOP 0 * FROM ' + @src + N';'
                         + N'ALTER TABLE ' + @dst + N' ADD CountryCode VARCHAR(5) NOT NULL CONSTRAINT ' + QUOTENAME('DF_cd_' + @t + '_Country') + N' DEFAULT ('''');';
                EXEC sp_executesql @sql;
            END

            -- columns in both (cd table without CountryCode)
            SET @sql = N'SELECT @c = STRING_AGG(QUOTENAME(d.name), '','') WITHIN GROUP (ORDER BY d.column_id)
                         FROM sys.columns d
                         JOIN ' + QUOTENAME(@SourceDatabase) + N'.sys.columns s
                              ON s.object_id = OBJECT_ID(@src) AND s.name = d.name AND s.is_computed = 0
                         WHERE d.object_id = OBJECT_ID(@dst) AND d.name <> ''CountryCode''
                           AND TYPE_NAME(d.system_type_id) NOT IN (''timestamp'', ''rowversion'');';
            EXEC sp_executesql @sql, N'@c NVARCHAR(MAX) OUTPUT, @src NVARCHAR(400), @dst NVARCHAR(300)', @c = @cols OUTPUT, @src = @src, @dst = @dst;

            BEGIN TRY
                BEGIN TRAN;
                SET @sql = N'DELETE FROM ' + @dst + N' WHERE CountryCode = @cc;'
                         + N'INSERT INTO ' + @dst + N' (' + @cols + N', CountryCode) SELECT ' + @cols + N', @cc FROM ' + @src + N';'
                         + N'SET @n = @@ROWCOUNT;';
                EXEC sp_executesql @sql, N'@cc VARCHAR(5), @n INT OUTPUT', @cc = @CountryCode, @n = @n OUTPUT;

                -- clear columns of a live company (e.g. salary generation status), only nullable ones
                IF @clear IS NOT NULL
                BEGIN
                    SET @sql = NULL;
                    SELECT @sql = STRING_AGG(CAST(QUOTENAME(c.name) + N' = NULL' AS NVARCHAR(MAX)), N', ')
                    FROM sys.columns c
                    WHERE c.object_id = OBJECT_ID(@dst) AND c.is_nullable = 1
                      AND c.name IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@clear, ','));
                    IF @sql IS NOT NULL
                    BEGIN
                        SET @sql = N'UPDATE ' + @dst + N' SET ' + @sql + N' WHERE CountryCode = @cc;';
                        EXEC sp_executesql @sql, N'@cc VARCHAR(5)', @cc = @CountryCode;
                    END
                END
                COMMIT;
                PRINT CONCAT(@t, ': ', @n, ' rows captured for ', @CountryCode, '.');
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK;
                PRINT 'FAILED ' + @t + ': ' + ERROR_MESSAGE();
            END CATCH
        END
        FETCH NEXT FROM tabs INTO @t, @clear;
    END
    CLOSE tabs;
    DEALLOCATE tabs;
END
GO

/* 3. Capture now (edit and run) ------------------------------------------------------------
EXEC dbo.CaptureCountryData @CountryCode = 'IND', @SourceDatabase = 'NOMARO_GGHOSPITAL';
EXEC dbo.CaptureCountryData @CountryCode = 'UAE', @SourceDatabase = 'NOMARO_<UAE company>';
*/

/* What is captured per country */
SELECT t.TableName, t.CopyOrder, t.IsActive,
       CASE WHEN OBJECT_ID('cd.' + QUOTENAME(t.TableName)) IS NULL THEN 'not captured yet' ELSE 'captured' END AS Status
FROM dbo.CountryDataTables t ORDER BY t.CopyOrder;
GO
SET NOEXEC OFF;
GO
