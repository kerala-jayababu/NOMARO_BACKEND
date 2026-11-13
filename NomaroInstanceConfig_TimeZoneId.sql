-- wrong database: stop every batch below (THROW alone stops only this batch)
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script against NOMARO_GLOBAL. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END

IF COL_LENGTH('dbo.NomaroInstanceConfig', 'TimeZoneId') IS NULL
BEGIN
    ALTER TABLE dbo.NomaroInstanceConfig
    ADD TimeZoneId nvarchar(100) NULL;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF (
        SELECT COUNT(*)
        FROM dbo.NomaroInstanceConfig
        WHERE (CompanyCode = 'GIA' AND CountryCode = 'GY')
           OR (CompanyCode = 'GIATEST' AND CountryCode = 'GY')
           OR (CompanyCode = 'US' AND CountryCode = 'US')
    ) <> 3
        THROW 51001, 'Expected GIA, GIATEST, and US rows with the specified country codes.', 1;

    UPDATE dbo.NomaroInstanceConfig
    SET TimeZoneId = CASE CompanyCode
        WHEN 'GIA' THEN 'America/Guyana'
        WHEN 'GIATEST' THEN 'America/Guyana'
        WHEN 'US' THEN 'America/New_York'
    END
    WHERE (CompanyCode = 'GIA' AND CountryCode = 'GY')
       OR (CompanyCode = 'GIATEST' AND CountryCode = 'GY')
       OR (CompanyCode = 'US' AND CountryCode = 'US');

    IF @@ROWCOUNT <> 3
        THROW 51002, 'Expected exactly three timezone rows to be updated.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT CompanyCode, CountryCode, TimeZoneId
FROM dbo.NomaroInstanceConfig
WHERE CompanyCode IN ('GIA', 'GIATEST', 'US')
ORDER BY CompanyCode;
GO
GO
SET NOEXEC OFF;
GO
