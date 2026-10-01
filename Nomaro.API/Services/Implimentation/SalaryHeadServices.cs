using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Implementation;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class SalaryHeadServices : ISalaryHeadServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryHeadServices> _logger;
        private readonly IAuditService _auditService;

        public SalaryHeadServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryHeadServices> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<IEnumerable<SalaryHeadDto>> GetSalaryHeadList()
        {
            try
            {
                var salaryHeads = await _dbContext.SalaryHeads
             .OrderBy(b => b.HeadType == "EARNINGS" ? 0
                         : b.HeadType == "REIMBURSEMENT" ? 1
                         : b.HeadType == "DEDUCTION" ? 2
                         : b.HeadType == "EMPLOYER_CONTRIBUTION" ? 3 : 4) // EARNINGS, REIMBURSEMENT, DEDUCTION, EMPLOYER_CONTRIBUTION
             .ThenBy(b => b.OrderNumber) // Sorting by OrderNumber inside each type
             .ToListAsync();
                return _mapper.Map<IEnumerable<SalaryHeadDto>>(salaryHeads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of salary heads.");
                throw;
            }
        }

        public async Task<SalaryHeadDto?> GetSalaryHeadByID(int id)
        {
            try
            {
                var salaryHead = await _dbContext.SalaryHeads.FirstOrDefaultAsync(s => s.IdSalaryHead == id);
                return salaryHead == null ? null : _mapper.Map<SalaryHeadDto>(salaryHead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching salary head with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SalaryHeadDto?> AddSalaryHead(SalaryHeadDto dto,int IdEmployee)
        {
            await ValidateSalaryHeadRules(dto, null);
            try
            {
                var validCalculationMethods = new[] { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT", "MANUAL", "STATUTORY" };
                if (string.IsNullOrWhiteSpace(dto.CalculationMethod) ||
                    !validCalculationMethods.Contains(dto.CalculationMethod.ToUpper()))
                {
                    throw new ArgumentException($"Invalid CalculationMethod. Allowed values are: {string.Join(", ", validCalculationMethods)}.", nameof(dto.CalculationMethod));
                }
                if (!string.IsNullOrEmpty(dto.DisbursingMonths))
                {
                    // Check if the value is a valid comma-separated list of months
                    var validMonths = new[] {
                "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER",
                "OCTOBER", "NOVEMBER", "DECEMBER"
            };

                    // Ensure months are in uppercase and separated by commas
                    var monthsList = dto.DisbursingMonths.Split(',')
                                                          .Select(m => m.Trim().ToUpper())
                                                          .Distinct()
                                                          .ToList();

                    // Check if the provided months are valid
                    var invalidMonths = monthsList.Where(m => !validMonths.Contains(m)).ToList();
                    if (invalidMonths.Any())
                    {
                        throw new ArgumentException($"Invalid month(s) in DisbursingMonths: {string.Join(", ", invalidMonths)}.", nameof(dto.DisbursingMonths));
                    }

                    // If the user provides all 12 months, we can store null or handle it as required
                    if (monthsList.Count == 12)
                    {
                        dto.DisbursingMonths = null;  // If all months are selected, we set it to null
                    }
                    else
                    {
                       
                        var ordered = monthsList
                         .OrderBy(m => Array.IndexOf(validMonths, m))
                         .ToArray();

                        dto.DisbursingMonths = string.Join(",", ordered);
                    }
                }
                var salaryHeadEntity = _mapper.Map<SalaryHeads>(dto);              
                salaryHeadEntity.CreatedOn = DateTime.Now;
                salaryHeadEntity.HeadType = dto.HeadType.ToUpper().Trim();
                salaryHeadEntity.CreatedBy = IdEmployee;
                var addedEntity = await _dbContext.SalaryHeads.AddAsync(salaryHeadEntity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync("Create", "SalaryHead", addedEntity.Entity.IdSalaryHead, dto);
                return _mapper.Map<SalaryHeadDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding salary head: {@SalaryHeadDto}", dto);
                return null;
            }
        }

        public async Task<SalaryHeadDto?> UpdateSalaryHead(SalaryHeadDto dto,int IdEmployee)
        {
            var existingHead = await _dbContext.SalaryHeads.AsNoTracking().FirstOrDefaultAsync(s => s.IdSalaryHead == dto.IdSalaryHead);
            if (existingHead == null) return null;
            await ValidateSalaryHeadRules(dto, existingHead);
            try
            {
                var salaryHead = await _dbContext.SalaryHeads.FirstOrDefaultAsync(s => s.IdSalaryHead == dto.IdSalaryHead);
                if (salaryHead == null) return null;

                var beforeUpdate = _mapper.Map<SalaryHeadDto>(salaryHead);

                salaryHead.SalaryHeadCode = dto.SalaryHeadCode;
                salaryHead.SalaryHeadName = dto.SalaryHeadName;
                salaryHead.HeadType = dto.HeadType.ToUpper().Trim();
                salaryHead.IsTaxable = dto.IsTaxable;
                salaryHead.IsActive = dto.IsActive;
                salaryHead.CalculationMethod = dto.CalculationMethod;
                salaryHead.IdPercentageSalaryHead = dto.IdPercentageSalaryHead;
                salaryHead.PercentageValue = dto.PercentageValue;
                salaryHead.FixedValue = dto.FixedValue;
                salaryHead.CustomFormula = dto.CustomFormula;
                salaryHead.ModifiedBy = IdEmployee;
                salaryHead.ModifiedOn = DateTime.Now;
                salaryHead.OrderNumber = dto.OrderNumber;
                salaryHead.NonTaxableThreshold = dto.NonTaxableThreshold;
                salaryHead.StatutoryType = dto.StatutoryType;
                salaryHead.IsPartOfGross = dto.IsPartOfGross;
                salaryHead.IsPartOfCTC = dto.IsPartOfCTC;
                salaryHead.IsPartOfPFWage = dto.IsPartOfPFWage;
                salaryHead.IsPartOfESIWage = dto.IsPartOfESIWage;
                salaryHead.IsPartOfGratuityWage = dto.IsPartOfGratuityWage;
                salaryHead.IsPartOfPTWage = dto.IsPartOfPTWage;
                salaryHead.IsProratedOnPaidDays = dto.IsProratedOnPaidDays;
                salaryHead.IsArrearHead = dto.IsArrearHead;
                salaryHead.IdBaseSalaryHead = dto.IdBaseSalaryHead;
                salaryHead.MinAmount = dto.MinAmount;
                salaryHead.MaxAmount = dto.MaxAmount;
                salaryHead.WageCeiling = dto.WageCeiling;
                salaryHead.PayFrequency = dto.PayFrequency;
                salaryHead.CalcSequence = dto.CalcSequence;
                salaryHead.RoundingRule = dto.RoundingRule;
                salaryHead.ShowOnPayslip = dto.ShowOnPayslip;
                if (!string.IsNullOrEmpty(dto.DisbursingMonths))
                {
                    // Validate the provided DisbursingMonths (uppercase, valid months, comma-separated)
                    var validMonths = new[] {
                "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER",
                "OCTOBER", "NOVEMBER", "DECEMBER"
            };

                    // Ensure months are in uppercase and separated by commas
                    var monthsList = dto.DisbursingMonths.Split(',')
                                                          .Select(m => m.Trim().ToUpper())
                                                          .Distinct()
                                                          .ToList();

                    // Check if the provided months are valid
                    var invalidMonths = monthsList.Where(m => !validMonths.Contains(m)).ToList();
                    if (invalidMonths.Any())
                    {
                        throw new ArgumentException($"Invalid month(s) in DisbursingMonths: {string.Join(", ", invalidMonths)}.", nameof(dto.DisbursingMonths));
                    }

                    // If the user provides all 12 months, we can store null
                    if (monthsList.Count == 12)
                    {
                        salaryHead.DisbursingMonths = null;  // Set to null if all 12 months are provided
                    }
                    else
                    {

                        var ordered = monthsList
                       .OrderBy(m => Array.IndexOf(validMonths, m))
                       .ToArray();
                        salaryHead.DisbursingMonths = string.Join(",", ordered);
                        
                    }
                }
                else
                {
                    salaryHead.DisbursingMonths = null; // "All months"
                }
                var updatedEntity = _dbContext.SalaryHeads.Update(salaryHead);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync("Update", "SalaryHead", updatedEntity.Entity.IdSalaryHead, new { before = beforeUpdate, after = dto });
                return _mapper.Map<SalaryHeadDto>(updatedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salary head with ID: {Id}", dto.IdSalaryHead);
                return null;
            }
        }

        /// <summary>
        /// Save checks from the Salary Heads screen. Throws InvalidOperationException with the user message.
        /// Also normalises HeadType / StatutoryType for statutory heads.
        /// </summary>
        private async Task ValidateSalaryHeadRules(SalaryHeadDto dto, SalaryHeads? existingHead)
        {
            var idSalaryHead = existingHead?.IdSalaryHead ?? 0;
            var code = (dto.SalaryHeadCode ?? string.Empty).Trim().ToUpper();
            var method = (dto.CalculationMethod ?? string.Empty).Trim().ToUpper();
            dto.SalaryHeadCode = code;
            dto.CalculationMethod = method;

            var allHeads = await _dbContext.SalaryHeads.AsNoTracking().ToListAsync();
            var others = allHeads.Where(h => h.IdSalaryHead != idSalaryHead).ToList();

            // Code: unique, and locked once the head has been used
            if (others.Any(h => string.Equals(h.SalaryHeadCode, code, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("A salary head with this code already exists.");
            }
            if (existingHead != null && !string.Equals(existingHead.SalaryHeadCode, code, StringComparison.OrdinalIgnoreCase))
            {
                var usage = await GetSalaryHeadUsage(existingHead.IdSalaryHead);
                if (usage.Any())
                {
                    throw new InvalidOperationException("The salary head code cannot be changed because the head is already used: " + string.Join("; ", usage));
                }
            }

            if (method == SalaryHeadConstants.Percentage)
            {
                var baseHead = others.FirstOrDefault(h => h.IdSalaryHead == dto.IdPercentageSalaryHead);
                if (baseHead == null)
                {
                    throw new InvalidOperationException("Percentage Of head does not exist.");
                }
                if (!(baseHead.CalcSequence < dto.CalcSequence))
                {
                    throw new InvalidOperationException($"\"{baseHead.SalaryHeadName}\" must have a lower Calculation Sequence than this head.");
                }
            }

            if (method == SalaryHeadConstants.Formula)
            {
                foreach (var formulaCode in SalaryHeadConstants.GetFormulaCodes(dto.CustomFormula))
                {
                    var referenced = others.FirstOrDefault(h => string.Equals(h.SalaryHeadCode, formulaCode, StringComparison.OrdinalIgnoreCase));
                    if (referenced == null)
                        throw new InvalidOperationException($"[{formulaCode}] in the formula is not a salary head code.");
                    if (referenced.CalculationMethod == SalaryHeadConstants.Statutory)
                        throw new InvalidOperationException($"[{formulaCode}] is a statutory head and cannot be used in a formula.");
                    if (!(referenced.CalcSequence < dto.CalcSequence))
                        throw new InvalidOperationException($"[{formulaCode}] must have a lower Calculation Sequence than this head.");
                }
            }

            if (method == SalaryHeadConstants.Statutory)
            {
                var statutoryType = (dto.StatutoryType ?? string.Empty).Trim().ToUpper();
                dto.StatutoryType = statutoryType;
                if (dto.IsActive && others.Any(h => h.IsActive && h.CalculationMethod == SalaryHeadConstants.Statutory && h.StatutoryType == statutoryType))
                {
                    throw new InvalidOperationException($"Another active salary head already has Statutory Type {statutoryType}.");
                }
                // Employee items are deductions, employer items are employer contributions
                dto.HeadType = SalaryHeadConstants.EmployerStatutoryTypes.Contains(statutoryType)
                    ? SalaryHeadConstants.EmployerContribution
                    : SalaryHeadConstants.Deduction;
            }
            else
            {
                dto.StatutoryType = null;
            }
        }

        /// <summary>
        /// Where a salary head is used: salary templates, employee salary structures and other heads' formulas / percentages.
        /// Used to warn before deactivating a head and to lock its code.
        /// </summary>
        public async Task<List<string>> GetSalaryHeadUsage(int idSalaryHead)
        {
            var usage = new List<string>();
            var head = await _dbContext.SalaryHeads.AsNoTracking().FirstOrDefaultAsync(h => h.IdSalaryHead == idSalaryHead);
            if (head == null) return usage;

            var templateCount = await _dbContext.SalaryTemplateDetails.AsNoTracking()
                .Where(d => d.IdSalaryHead == idSalaryHead).Select(d => d.IdSalaryTemplate).Distinct().CountAsync();
            if (templateCount > 0) usage.Add($"{templateCount} salary template(s)");

            var structureCount = await _dbContext.EmployeeSalaryConfigDetails.AsNoTracking()
                .Where(d => d.IdSalaryHead == idSalaryHead).Select(d => d.IdEmployeeSalaryConfig).Distinct().CountAsync();
            if (structureCount > 0) usage.Add($"{structureCount} employee salary structure(s)");

            var token = "[" + head.SalaryHeadCode + "]";
            var dependentHeads = await _dbContext.SalaryHeads.AsNoTracking()
                .Where(h => h.IdSalaryHead != idSalaryHead
                    && ((h.CustomFormula != null && h.CustomFormula.Contains(token))
                        || h.IdPercentageSalaryHead == idSalaryHead
                        || h.IdBaseSalaryHead == idSalaryHead))
                .Select(h => h.SalaryHeadName)
                .ToListAsync();
            if (dependentHeads.Any()) usage.Add("used by " + string.Join(", ", dependentHeads));

            return usage;
        }
    }

}

