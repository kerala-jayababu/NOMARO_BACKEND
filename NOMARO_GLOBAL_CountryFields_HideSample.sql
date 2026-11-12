/* OPTIONAL sample - hide the new country fields that do not apply to a company (CompanyScreenAttributeConfig).
   Run on NOMARO_GLOBAL. Set the two values below. Fields of the company's own country are left alone (shown, default label).
   Passport Expiry applies to both countries and is not listed. Safe to run more than once. */
DECLARE @CompanyCode VARCHAR(50) = 'GGHOSPITAL';   -- company to configure
DECLARE @Country     VARCHAR(3)  = 'IND';          -- 'IND' (hides UAE fields) or 'UAE' (hides India fields)

INSERT INTO dbo.CompanyScreenAttributeConfig (CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
SELECT @CompanyCode, f.ScreenCode, f.AttributeID, NULL, 0, NULL, NULL
FROM (VALUES
    ('EMPPROF', 'EmiratesIdExpiry', 'UAE'),
    ('EMPPROF', 'VisaNumber', 'UAE'),
    ('EMPPROF', 'VisaSponsor', 'UAE'),
    ('EMPPROF', 'LabourCardNumber', 'UAE'),
    ('EMPPROF', 'LabourCardExpiry', 'UAE'),
    ('EMPPROF', 'PensionApplicable', 'UAE'),
    ('EMPPROF', 'PensionScheme', 'UAE'),
    ('EMPPROF', 'PensionRegistrationNumber', 'UAE'),
    ('EMPPROF', 'UnemploymentInsuranceNo', 'UAE'),
    ('EMPPROF', 'DependentEmiratesId', 'UAE'),
    ('EMPPROF', 'PaymentMode', 'UAE'),
    ('BNKBNC', 'RoutingCode', 'UAE'),
    ('OFFICEMGMT', 'TradeLicenceNumber', 'UAE'),
    ('OFFICEMGMT', 'TradeLicenceExpiry', 'UAE'),
    ('OFFICEMGMT', 'EstablishmentId', 'UAE'),
    ('OFFICEMGMT', 'PFEstablishmentCode', 'IND'),
    ('OFFICEMGMT', 'ESIEmployerCode', 'IND'),
    ('OFFICEMGMT', 'TAN', 'IND'),
    ('OFFICEMGMT', 'PTRegistrationNumber', 'IND')
) AS f(ScreenCode, AttributeID, ForCountry)
WHERE f.ForCountry <> @Country
  AND NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig c
                  WHERE c.CompanyCode = @CompanyCode AND c.ScreenCode = f.ScreenCode AND c.AttributeID = f.AttributeID);

SELECT ScreenCode, AttributeID, DisplayLabel, FieldVisibility FROM dbo.CompanyScreenAttributeConfig
WHERE CompanyCode = @CompanyCode ORDER BY ScreenCode, AttributeID;
