using Microsoft.EntityFrameworkCore;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>Employee family members (spouse, children, parents...). Keeps Employees.ChildrenCount up to date.</summary>
    public class EmployeeFamilyService : IEmployeeFamilyService
    {
        private readonly ApplicationDBContext _db;
        private readonly IAuditService _audit;

        public EmployeeFamilyService(ApplicationDBContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        private static readonly Dictionary<string, int> Order =
            FamilyRelationships.All.Select((r, i) => (r.Code, i)).ToDictionary(x => x.Code, x => x.i);

        public static EmployeeFamilyMemberDto ToDto(EmployeeFamilyMembers m) => new()
        {
            IdEmployeeFamilyMember = m.IdEmployeeFamilyMember,
            IdEmployee = m.IdEmployee,
            Relationship = m.Relationship,
            RelationshipLabel = FamilyRelationships.Label(m.Relationship),
            MemberName = m.MemberName,
            Gender = m.Gender,
            DateOfBirth = m.DateOfBirth,
            ContactNumber = m.ContactNumber,
            Occupation = m.Occupation,
            IsDependent = m.IsDependent,
            IsNominee = m.IsNominee,
            NomineePercentage = m.NomineePercentage,
            MarriageDate = m.MarriageDate,
            CertificateNumber = m.CertificateNumber,
            DivisionNumber = m.DivisionNumber,
            Remarks = m.Remarks
        };

        public async Task<List<EmployeeFamilyMemberDto>> GetFamilyMembers(int idEmployee)
        {
            var rows = await _db.EmployeeFamilyMembers.AsNoTracking().Where(m => m.IdEmployee == idEmployee).ToListAsync();
            return rows
                .OrderBy(m => Order.TryGetValue(m.Relationship, out var o) ? o : 99)
                .ThenBy(m => m.DateOfBirth ?? DateTime.MaxValue)
                .ThenBy(m => m.MemberName)
                .Select(ToDto).ToList();
        }

        private static string? Clean(string? v, int max)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            v = v.Trim();
            if (v.Length > max) throw new InvalidOperationException($"A value is longer than {max} characters.");
            return v;
        }

        public async Task<int> AddOrUpdateFamilyMember(EmployeeFamilyMemberDto dto, int userId)
        {
            if (dto.IdEmployee <= 0 || !await _db.Employees.AnyAsync(e => e.IdEmployee == dto.IdEmployee))
                throw new InvalidOperationException("Employee not found.");
            var relationship = (dto.Relationship ?? "").Trim().ToUpperInvariant();
            if (!FamilyRelationships.IsValid(relationship)) throw new InvalidOperationException("Select a valid relationship.");
            var gender = string.IsNullOrWhiteSpace(dto.Gender) ? FamilyRelationships.DefaultGender(relationship) : dto.Gender.Trim().ToUpperInvariant();
            if (gender != null && !new[] { "MALE", "FEMALE", "OTHER" }.Contains(gender)) throw new InvalidOperationException("Gender must be Male, Female or Other.");
            if (!string.IsNullOrWhiteSpace(dto.ContactNumber) && !System.Text.RegularExpressions.Regex.IsMatch(dto.ContactNumber.Trim(), @"^[0-9+\- ]{6,20}$"))
                throw new InvalidOperationException("Contact number must be 6 to 20 digits.");

            var family = await _db.EmployeeFamilyMembers.Where(m => m.IdEmployee == dto.IdEmployee).ToListAsync();
            EmployeeFamilyMembers member;
            object? before = null;
            if (dto.IdEmployeeFamilyMember > 0)
            {
                member = family.FirstOrDefault(m => m.IdEmployeeFamilyMember == dto.IdEmployeeFamilyMember)
                         ?? throw new KeyNotFoundException("Family member not found.");
                before = ToDto(member);
                member.ModifiedBy = userId;
                member.ModifiedDateTime = DateTime.Now;
            }
            else
            {
                member = new EmployeeFamilyMembers { IdEmployee = dto.IdEmployee, CreatedBy = userId, CreatedDateTime = DateTime.Now };
                family.Add(member);
                await _db.EmployeeFamilyMembers.AddAsync(member);
            }

            member.Relationship = relationship;
            member.MemberName = (dto.MemberName ?? "").Trim();
            member.Gender = gender;
            member.DateOfBirth = dto.DateOfBirth?.Date;
            member.ContactNumber = Clean(dto.ContactNumber, 20);
            member.Occupation = Clean(dto.Occupation, 100);
            member.IsDependent = dto.IsDependent;
            member.IsNominee = dto.IsNominee;
            member.NomineePercentage = dto.IsNominee ? dto.NomineePercentage : null;
            member.MarriageDate = dto.MarriageDate?.Date;
            member.CertificateNumber = Clean(dto.CertificateNumber, 20);
            member.DivisionNumber = Clean(dto.DivisionNumber, 20);
            member.Remarks = Clean(dto.Remarks, 250);

            FamilyRelationships.Validate(family);
            await _db.SaveChangesAsync();
            await UpdateChildrenCount(dto.IdEmployee);

            await _audit.LogAuditAsync(before == null ? "Create" : "Update", "EmployeeFamilyMember", member.IdEmployeeFamilyMember,
                before == null ? new { after = ToDto(member) } : (object)new { before, after = ToDto(member) });
            return member.IdEmployeeFamilyMember;
        }

        public async Task DeleteFamilyMember(int idEmployeeFamilyMember, int userId)
        {
            var member = await _db.EmployeeFamilyMembers.FirstOrDefaultAsync(m => m.IdEmployeeFamilyMember == idEmployeeFamilyMember)
                         ?? throw new KeyNotFoundException("Family member not found.");
            var snapshot = ToDto(member);
            _db.EmployeeFamilyMembers.Remove(member);
            await _db.SaveChangesAsync();
            await UpdateChildrenCount(member.IdEmployee);
            await _audit.LogAuditAsync("Delete", "EmployeeFamilyMember", idEmployeeFamilyMember, new { before = snapshot, deletedBy = userId });
        }

        // Employees.ChildrenCount is used elsewhere (e.g. allowances): number of sons and daughters
        public async Task UpdateChildrenCount(int idEmployee)
        {
            var count = await _db.EmployeeFamilyMembers.CountAsync(m => m.IdEmployee == idEmployee && FamilyRelationships.ChildCodes.Contains(m.Relationship));
            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.IdEmployee == idEmployee);
            if (emp != null && emp.ChildrenCount != count)
            {
                emp.ChildrenCount = count;
                await _db.SaveChangesAsync();
            }
        }
    }
}
