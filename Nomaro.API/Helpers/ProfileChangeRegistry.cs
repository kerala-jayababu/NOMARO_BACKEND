namespace Nomaro.API.Helpers
{
    // A master-table column an employee may request a change to.
    public record ProfileChangeField(
        string Column,
        string Label,
        string DataType,          // text | date | email | phone | gender | marital | relationship | yesno | bank | branch | doctype | decimal | pan | uan | esi
        int MaxLength,
        bool Sensitive = false,
        bool Required = false);

    // A request type: which table it changes, how, and which columns.
    public record ProfileChangeTypeDef(
        string TypeCode,
        string TableName,
        string Operation,          // UPDATE | ADD | REMOVE
        bool NeedsRecord,          // true when the employee picks an existing row (child, account, document)
        bool RequiresFile,         // the uploaded file itself is the change (documents)
        ProfileChangeField[] Fields);

    // Whitelist of everything the self-service module may change. Nothing outside this list can be written.
    public static class ProfileChangeRegistry
    {
        public const string TEmployees = "Employees";
        public const string TStatutory = "EmployeeStatutoryDetails";
        public const string TFamily = "EmployeeFamilyMembers";
        public const string TBank = "EmployeeBankAccounts";
        public const string TDocuments = "EmployeeDocuments";

        public const string Draft = "DRAFT";
        public const string Submitted = "SUBMITTED";
        public const string Interim = "INTERIM APPROVED";
        public const string Clarification = "CLARIFICATION REQUIRED";
        public const string FinalApproved = "FINAL APPROVED";
        public const string Rejected = "REJECTED";
        public const string Cancelled = "CANCELLED";

        public static readonly string[] OpenStatuses = { Submitted, Interim, Clarification };
        public static readonly string[] WithApproverStatuses = { Submitted, Interim };

        public static readonly string[] Genders = { "MALE", "FEMALE", "OTHER" };
        public static readonly string[] MaritalStatuses = { "Single", "Married", "Widowed", "Separated", "Divorced", "Other" };

        private static readonly ProfileChangeField[] FamilyFields =
        {
            new("Relationship", "Relationship", "relationship", 20, Required: true),
            new("MemberName", "Name", "text", 100, Required: true),
            new("Gender", "Gender", "gender", 10),
            new("DateOfBirth", "Date of Birth", "date", 10),
            new("ContactNumber", "Contact Number", "phone", 20),
            new("Occupation", "Occupation", "text", 100),
            new("IsDependent", "Dependent", "yesno", 5),
            new("IsNominee", "Nominee", "yesno", 5),
            new("NomineePercentage", "Nominee %", "decimal", 6),
            new("MarriageDate", "Marriage Date (spouse)", "date", 10),
            new("CertificateNumber", "Birth Certificate No.", "text", 20),
        };

        private static readonly ProfileChangeField[] BankFields =
        {
            new("IdBank", "Bank", "bank", 10, Required: true),
            new("IdBankBranch", "Branch", "branch", 10),
            new("AccountNumber", "Account Number", "account", 25, Sensitive: true, Required: true),
            new("BranchCode", "IFSC / Branch Code", "text", 20),
            new("CurrencyCode", "Currency", "text", 3),
            new("SalaryPercentageDistributed", "Salary %", "decimal", 6, Required: true),
        };

        public static readonly ProfileChangeTypeDef[] Types =
        {
            new("NAME_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("FirstName", "First Name", "text", 50, Required: true),
                new("MiddleName", "Middle Name", "text", 50),
                new("LastName", "Last Name", "text", 50) }),
            new("DOB_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("DateOfBirth", "Date of Birth", "date", 10, Required: true) }),
            new("GENDER_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("Gender", "Gender", "gender", 10, Required: true) }),
            new("MARITAL_STATUS", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("MaritalStatus", "Marital Status", "marital", 20, Required: true) }),
            new("NATIONALITY_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("Nationality", "Nationality", "text", 50),
                new("CitizenShip", "Citizenship", "text", 50) }),
            new("CONTACT_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("PhoneNumber1", "Mobile Number", "phone", 20),
                new("PhoneNumber2", "Alternate Number", "phone", 20),
                new("WhatsAppNumber", "WhatsApp Number", "phone", 20),
                new("HomeEmail", "Personal Email", "email", 100) }),
            new("ADDRESS_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("Address1", "Address Line 1", "text", 50),
                new("Address2", "Address Line 2", "text", 50),
                new("Address3", "Address Line 3", "text", 50),
                new("City", "City", "text", 50),
                new("State", "State", "text", 50),
                new("ZipCode", "PIN / Postal Code", "text", 10) }),
            new("EMERGENCY_CONTACT", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("EmergencyContactPersonName", "Emergency Contact Person", "text", 50),
                new("EmergencyContactNumbers", "Emergency Contact Number", "phone", 20) }),
            new("IDENTITY_CHANGE", TEmployees, "UPDATE", false, false, new ProfileChangeField[] {
                new("PassportNumber", "Passport Number", "text", 15, Sensitive: true),
                new("NationalIDNumber", "National ID Number", "text", 20, Sensitive: true),
                new("TaxIdNumber", "Tax ID Number", "text", 20, Sensitive: true),
                new("IdNumber", "ID Number", "text", 20, Sensitive: true),
                new("WorkExpirationDate", "Work Permit / Visa Expiry", "date", 10) }),
            new("STATUTORY_CHANGE", TStatutory, "UPDATE", false, false, new ProfileChangeField[] {
                new("PAN", "PAN", "pan", 10, Sensitive: true),
                new("UAN", "UAN", "uan", 12, Sensitive: true),
                new("PFNumber", "PF Number", "text", 30, Sensitive: true),
                new("ESINumber", "ESI Number", "esi", 17, Sensitive: true) }),
            new("FAMILY_ADD", TFamily, "ADD", false, false, FamilyFields),
            new("FAMILY_EDIT", TFamily, "UPDATE", true, false, FamilyFields),
            new("FAMILY_REMOVE", TFamily, "REMOVE", true, false, Array.Empty<ProfileChangeField>()),
            new("BANK_ADD", TBank, "ADD", false, false, BankFields),
            new("BANK_EDIT", TBank, "UPDATE", true, false, BankFields),
            new("BANK_REMOVE", TBank, "REMOVE", true, false, Array.Empty<ProfileChangeField>()),
            new("DOC_ADD", TDocuments, "ADD", false, true, new ProfileChangeField[] {
                new("IdDocumentType", "Document Type", "doctype", 10, Required: true),
                new("DocumentValidTill", "Valid Till", "date", 10),
                new("Remarks", "Remarks", "text", 250) }),
            new("DOC_REPLACE", TDocuments, "UPDATE", true, true, new ProfileChangeField[] {
                new("DocumentValidTill", "Valid Till", "date", 10),
                new("Remarks", "Remarks", "text", 250) }),
        };

        public static ProfileChangeTypeDef? GetType(string? typeCode) =>
            Types.FirstOrDefault(t => string.Equals(t.TypeCode, typeCode, StringComparison.OrdinalIgnoreCase));

        public static ProfileChangeField? FindField(string table, string column) =>
            Types.Where(t => t.TableName == table)
                 .SelectMany(t => t.Fields)
                 .FirstOrDefault(f => f.Column == column);

        public static bool IsSensitive(string table, string column) => FindField(table, column)?.Sensitive ?? false;

        public static string? Mask(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var v = value.Trim();
            if (v.Length <= 4) return new string('X', v.Length);
            return new string('X', v.Length - 4) + v[^4..];
        }
    }
}
