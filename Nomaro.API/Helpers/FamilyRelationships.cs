using Nomaro.API.Models;

namespace Nomaro.API.Helpers
{
    /// <summary>Relationship codes and the rules for an employee's family members.</summary>
    public static class FamilyRelationships
    {
        // code, label, default gender, only one allowed
        public static readonly (string Code, string Label, string? Gender, bool Single)[] All =
        {
            ("SPOUSE", "Spouse", null, true),
            ("SON", "Son", "MALE", false),
            ("DAUGHTER", "Daughter", "FEMALE", false),
            ("FATHER", "Father", "MALE", true),
            ("MOTHER", "Mother", "FEMALE", true),
            ("FATHERINLAW", "Father-in-law", "MALE", true),
            ("MOTHERINLAW", "Mother-in-law", "FEMALE", true),
            ("BROTHER", "Brother", "MALE", false),
            ("SISTER", "Sister", "FEMALE", false),
            ("OTHER", "Other", null, false),
        };

        public static readonly string[] ChildCodes = { "SON", "DAUGHTER" };

        // Date of birth is optional; when given for these relationships the person must be at least MinAdultAge today
        public static readonly string[] AdultCodes = { "SPOUSE", "FATHER", "MOTHER", "FATHERINLAW", "MOTHERINLAW" };
        public const int MinAdultAge = 16;

        public static bool IsValid(string? code) => All.Any(r => r.Code == code);

        public static string Label(string? code) => All.FirstOrDefault(r => r.Code == code).Label ?? code ?? "";

        public static string? DefaultGender(string? code) => All.FirstOrDefault(r => r.Code == code).Gender;

        /// <summary>Checks the whole family of one employee after a change. Throws InvalidOperationException.</summary>
        public static void Validate(IEnumerable<EmployeeFamilyMembers> family)
        {
            var list = family.ToList();
            foreach (var m in list)
            {
                if (!IsValid(m.Relationship)) throw new InvalidOperationException("Select a valid relationship.");
                if (string.IsNullOrWhiteSpace(m.MemberName)) throw new InvalidOperationException("Enter the family member's name.");
                if (m.MemberName.Trim().Length > 100) throw new InvalidOperationException("Name must not exceed 100 characters.");
                if (m.DateOfBirth.HasValue && m.DateOfBirth.Value.Date > DateTime.Today) throw new InvalidOperationException($"Date of birth of {m.MemberName} cannot be in the future.");
                if (m.DateOfBirth.HasValue && AdultCodes.Contains(m.Relationship) && m.DateOfBirth.Value.Date > DateTime.Today.AddYears(-MinAdultAge))
                    throw new InvalidOperationException($"{Label(m.Relationship)} must be at least {MinAdultAge} years old as on today. Check the date of birth of {m.MemberName}.");
                if (m.MarriageDate.HasValue && m.MarriageDate.Value.Date > DateTime.Today) throw new InvalidOperationException("Marriage date cannot be in the future.");
                if (m.NomineePercentage.HasValue && (m.NomineePercentage < 0 || m.NomineePercentage > 100)) throw new InvalidOperationException("Nominee % must be between 0 and 100.");
                if (!m.IsNominee) m.NomineePercentage = null;
                if (m.Relationship != "SPOUSE") m.MarriageDate = null;
            }

            foreach (var single in All.Where(r => r.Single))
                if (list.Count(m => m.Relationship == single.Code) > 1)
                    throw new InvalidOperationException($"Only one {single.Label.ToLowerInvariant()} can be added.");

            var nomineeTotal = list.Where(m => m.IsNominee).Sum(m => m.NomineePercentage ?? 0);
            if (nomineeTotal > 100)
                throw new InvalidOperationException($"Nominee shares add up to {nomineeTotal:0.##}%. The total cannot be more than 100%.");
        }
    }
}
