/* Field labels / visibility per company, from NOMARO_Country_Field_Matrix_India_UAE.xlsx
   Run on NOMARO_GLOBAL. Set the company and its country below, run, then repeat for each company.
   - Adds a row per field to dbo.CompanyScreenAttributeConfig; an existing row (same company, screen, attribute)
     is updated to the matrix values, so the script is safe to run again.
   - Hidden fields ("-" in the matrix) get FieldVisibility = 0 and no label.
   - The table has no "mandatory" column, so the Mandatory columns of the matrix are not stored here.
   - Screen-level rows of the matrix (currency labels, reports, income tax, leave passage, master data) are not field
     attributes and are not included. */
DECLARE @CompanyCode VARCHAR(50) = 'GGHOSPITAL';  -- company code from NomaroInstanceConfig
DECLARE @Country     VARCHAR(3)  = 'IND';         -- 'IND' = India columns of the matrix, 'UAE' = UAE columns

IF @Country NOT IN ('IND', 'UAE')
BEGIN
    RAISERROR('@Country must be IND or UAE.', 16, 1);
    RETURN;
END

DECLARE @Matrix TABLE (
    ScreenCode VARCHAR(50), AttributeID VARCHAR(100),
    LabelIND NVARCHAR(100), LabelUAE NVARCHAR(100),
    VisibleIND BIT, VisibleUAE BIT,
    MaxLengthIND INT, MaxLengthUAE INT,
    DataType VARCHAR(50));

INSERT INTO @Matrix VALUES
    (N'EMPPROF', N'NationalIDNumber', N'Aadhaar Number', N'Emirates ID Number', 1, 1, 12, 18, N'text'),
    (N'EMPPROF', N'EmiratesIdExpiry', NULL, N'Emirates ID Expiry', 0, 1, NULL, NULL, N'date'),
    (N'EMPPROF', N'PassportNumber', N'Passport Number', N'Passport Number', 1, 1, 15, 15, N'text'),
    (N'EMPPROF', N'PassportExpiry', N'Passport Expiry', N'Passport Expiry', 1, 1, NULL, NULL, N'date'),
    (N'EMPPROF', N'WorkExpirationDate', N'Work Permit Expiry (foreign nationals)', N'Residence Visa Expiry', 1, 1, NULL, NULL, N'date'),
    (N'EMPPROF', N'VisaNumber', NULL, N'Residence Visa File No. / UID', 0, 1, NULL, 50, N'text'),
    (N'EMPPROF', N'VisaSponsor', NULL, N'Visa Sponsor', 0, 1, NULL, NULL, N'dropdown'),
    (N'EMPPROF', N'LabourCardNumber', NULL, N'Labour Card / MOHRE Person ID', 0, 1, NULL, 30, N'number'),
    (N'EMPPROF', N'LabourCardExpiry', NULL, N'Labour Card Expiry', 0, 1, NULL, NULL, N'date'),
    (N'EMPPROF', N'Nationality', N'Nationality', N'Nationality', 1, 1, NULL, NULL, N'dropdown'),
    (N'EMPPROF', N'CitizenShip', N'Citizenship', N'Citizenship', 1, 1, NULL, NULL, N'dropdown'),
    (N'EMPPROF', N'IdNumber', NULL, NULL, 0, 0, NULL, NULL, N'text'),
    (N'EMPPROF', N'TaxIdNumber', NULL, NULL, 0, 0, NULL, NULL, N'text'),
    (N'EMPPROF', N'PAN', N'PAN', NULL, 1, 0, 10, NULL, N'text'),
    (N'EMPPROF', N'UAN', N'UAN', NULL, 1, 0, 12, NULL, N'text'),
    (N'EMPPROF', N'PFNumber', N'PF Number', NULL, 1, 0, 30, NULL, N'text'),
    (N'EMPPROF', N'IsPFApplicable', N'PF Applicable', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'IsEPSApplicable', N'EPS Applicable', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'PFOnActualWage', N'PF on Actual Wage', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'VPFRate', N'VPF Rate (%)', NULL, 1, 0, NULL, NULL, N'number'),
    (N'EMPPROF', N'ESINumber', N'ESI Number', NULL, 1, 0, 17, NULL, N'text'),
    (N'EMPPROF', N'IsESIApplicable', N'ESI Applicable', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'IdPTState', N'PT State', NULL, 1, 0, NULL, NULL, N'dropdown'),
    (N'EMPPROF', N'IsPTApplicable', N'PT Applicable', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'IsLWFApplicable', N'LWF Applicable', NULL, 1, 0, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'IsDisabled', N'Person with Disability', N'Person of Determination', 1, 1, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'PensionApplicable', NULL, N'Pension Applicable (GPSSA / ADPF)', 0, 1, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'PensionScheme', NULL, N'Pension Authority', 0, 1, NULL, NULL, N'dropdown'),
    (N'EMPPROF', N'PensionRegistrationNumber', NULL, N'Pension Registration No.', 0, 1, NULL, 50, N'text'),
    (N'EMPPROF', N'UnemploymentInsuranceNo', NULL, N'Unemployment Insurance (ILOE) Policy No.', 0, 1, NULL, 50, N'text'),
    (N'EMPPROF', N'State', N'State', N'Emirate', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'ZipCode', N'PIN Code', N'PO Box', 1, 1, 6, 10, N'text'),
    (N'EMPPROF', N'City', N'City', N'City / Area', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'Address1', N'Address Line 1', N'Address Line 1', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'Address2', N'Address Line 2', N'Address Line 2', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'Address3', N'Address Line 3', N'Address Line 3', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'PhoneNumber1', N'Work Phone', N'Work Phone', 1, 1, 20, 20, N'text'),
    (N'EMPPROF', N'PhoneNumber2', N'Mobile Phone', N'Mobile Phone', 1, 1, 20, 20, N'text'),
    (N'EMPPROF', N'WhatsAppNumber', N'WhatsApp Number', N'WhatsApp Number', 1, 1, 20, 20, N'text'),
    (N'EMPPROF', N'CertificateNumber', N'Birth Certificate No.', N'Birth Certificate No.', 1, 1, 20, 20, N'text'),
    (N'EMPPROF', N'DivisionNumber', NULL, NULL, 0, 0, NULL, NULL, N'text'),
    (N'EMPPROF', N'IsNominee', N'Nominee', N'Beneficiary', 1, 1, NULL, NULL, N'checkbox'),
    (N'EMPPROF', N'NomineePercentage', N'Nominee share %', N'Share %', 1, 1, NULL, NULL, N'number'),
    (N'EMPPROF', N'DependentEmiratesId', NULL, N'Emirates ID (dependent)', 0, 1, NULL, 18, N'text'),
    (N'EMPPROF', N'BranchCode', N'IFSC Code', NULL, 1, 0, 11, NULL, N'text'),
    (N'EMPPROF', N'AccountNumber', N'Account Number', N'IBAN', 1, 1, 20, 23, N'text'),
    (N'EMPPROF', N'CurrencyCode', N'Currency (INR)', N'Currency (AED)', 1, 1, NULL, NULL, N'text'),
    (N'EMPPROF', N'PaymentMode', NULL, N'Payment Mode (Bank / Exchange house salary card)', 0, 1, NULL, NULL, N'dropdown'),
    (N'BNKBNC', N'IFSCCode', N'IFSC Code', N'Routing Code (WPS)', 1, 1, 11, 9, N'text'),
    (N'BNKBNC', N'BranchName', N'Branch Name', N'Branch Name', 1, 1, NULL, NULL, N'text'),
    (N'BNKBNC', N'BankAddress', N'Address', N'Address', 1, 1, NULL, NULL, N'text'),
    (N'BNKBNC', N'PhoneNumber', N'Phone', N'Phone', 1, 1, NULL, NULL, N'text'),
    (N'BNKBNC', N'SwiftCode', N'Swift Code', N'Swift Code', 1, 1, NULL, NULL, N'text'),
    (N'BNKBNC', N'RoutingCode', NULL, N'Bank Routing Code (WPS)', 0, 1, NULL, 9, N'number'),
    (N'OFFICEMGMT', N'State', N'State', N'Emirate', 1, 1, NULL, NULL, N'text'),
    (N'OFFICEMGMT', N'District', N'District', N'Area / Community', 1, 1, NULL, NULL, N'text'),
    (N'OFFICEMGMT', N'PinCode', N'Pin Code', N'PO Box', 1, 1, 6, 10, N'text'),
    (N'OFFICEMGMT', N'GSTIN', N'GSTIN', N'TRN (VAT Registration No.)', 1, 1, 15, 15, N'text'),
    (N'OFFICEMGMT', N'TradeLicenceNumber', NULL, N'Trade Licence No.', 0, 1, NULL, 50, N'text'),
    (N'OFFICEMGMT', N'TradeLicenceExpiry', NULL, N'Trade Licence Expiry', 0, 1, NULL, NULL, N'date'),
    (N'OFFICEMGMT', N'EstablishmentId', NULL, N'MOHRE Establishment ID (WPS Employer ID)', 0, 1, NULL, 20, N'number'),
    (N'OFFICEMGMT', N'PFEstablishmentCode', N'PF Establishment Code', NULL, 1, 0, 30, NULL, N'text'),
    (N'OFFICEMGMT', N'ESIEmployerCode', N'ESI Employer Code', NULL, 1, 0, 17, NULL, N'number'),
    (N'OFFICEMGMT', N'TAN', N'TAN (for TDS)', NULL, 1, 0, 10, NULL, N'text'),
    (N'OFFICEMGMT', N'PTRegistrationNumber', N'PT Registration No.', NULL, 1, 0, 50, NULL, N'text'),
    (N'SALHDS', N'Taxability', N'Taxability', NULL, 1, 0, NULL, NULL, N'dropdown'),
    (N'SALHDS', N'WageCeiling', N'Wage Ceiling', N'Wage Ceiling', 1, 1, NULL, NULL, N'number');

DECLARE @Rows TABLE (ScreenCode VARCHAR(50), AttributeID VARCHAR(100), DisplayLabel NVARCHAR(100), FieldVisibility BIT, MaxLength INT, DataType VARCHAR(50));
INSERT INTO @Rows
SELECT ScreenCode, AttributeID,
       CASE WHEN @Country = 'IND' THEN LabelIND ELSE LabelUAE END,
       CASE WHEN @Country = 'IND' THEN VisibleIND ELSE VisibleUAE END,
       CASE WHEN @Country = 'IND' THEN MaxLengthIND ELSE MaxLengthUAE END,
       DataType
FROM @Matrix;

BEGIN TRAN;

UPDATE c SET c.DisplayLabel = r.DisplayLabel, c.FieldVisibility = r.FieldVisibility, c.MaxLength = r.MaxLength, c.DataType = r.DataType
FROM dbo.CompanyScreenAttributeConfig c
JOIN @Rows r ON r.ScreenCode = c.ScreenCode AND r.AttributeID = c.AttributeID
WHERE c.CompanyCode = @CompanyCode;

INSERT INTO dbo.CompanyScreenAttributeConfig (CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
SELECT @CompanyCode, r.ScreenCode, r.AttributeID, r.DisplayLabel, r.FieldVisibility, r.MaxLength, r.DataType
FROM @Rows r
WHERE NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig c
                  WHERE c.CompanyCode = @CompanyCode AND c.ScreenCode = r.ScreenCode AND c.AttributeID = r.AttributeID);

COMMIT;

SELECT ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType
FROM dbo.CompanyScreenAttributeConfig
WHERE CompanyCode = @CompanyCode
ORDER BY ScreenCode, AttributeID;
