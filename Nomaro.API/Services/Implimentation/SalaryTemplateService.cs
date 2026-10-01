using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class SalaryTemplateService : ISalaryTemplateService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<SalaryTemplateService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IAuditService _auditService;

    private static readonly string[] ApprovedStatuses = { "APPROVED", "FINAL APPROVED" };

    public SalaryTemplateService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryTemplateService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService, IAuditService auditService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _approvalWorkflowService = approvalWorkflowService;
        _auditService = auditService;
    }

    public async Task<IEnumerable<SalaryTemplateDto>> GetAllSalaryTemplates(string? searchText = null, string? dropdownFilter = null, bool includeInactive = false)
    {
        try
        {
            var query = _dbContext.SalaryTemplates.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(x => x.ActiveStatus == true);
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.SalaryTemplateName.Contains(searchText) || x.Description.Contains(searchText));
            }

            // Apply dropdown filter if provided (e.g., ApprovalStatus)
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                if (dropdownFilter == _configuration["DropdDownStatus:ApproveStatus"])
                {
                    var interminapprovedSatus = _configuration["DropdDownStatus:InterminApproved"];
                    query = query.Where(x => x.ApprovalStatus == dropdownFilter || x.ApprovalStatus == interminapprovedSatus);
                }
                else
                {
                    query = query.Where(x => x.ApprovalStatus == dropdownFilter);
                }
            }

            var templates = await query.ToListAsync();
            var res = _mapper.Map<List<SalaryTemplateDto>>(templates);

            // Load the details of all templates in one query
            var templateIds = res.Where(t => t.IdSalaryTemplate.HasValue).Select(t => t.IdSalaryTemplate!.Value).ToList();
            var details = await GetTemplateDetails(templateIds);
            foreach (var template in res)
            {
                template.SalaryTemplateDetails = details.Where(d => d.IdSalaryTemplate == template.IdSalaryTemplate).ToList();
            }
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching salary templates.");
            throw;
        }
    }

    public async Task<SalaryTemplateDto?> GetSalaryTemplateById(int idSalaryTemplate)
    {
        try
        {
            var salaryTemplate = await (from st in _dbContext.SalaryTemplates
                                        join emp in _dbContext.Employees
                                        on st.CreatedBy equals emp.IdEmployee into empGroup
                                        from emp in empGroup.DefaultIfEmpty() // LEFT JOIN
                                        where st.IdSalaryTemplate == idSalaryTemplate
                                        select new SalaryTemplateDto
                                        {
                                            IdSalaryTemplate = st.IdSalaryTemplate,
                                            SalaryTemplateName = st.SalaryTemplateName,
                                            Description = st.Description,
                                            CreatedOn = st.CreatedOn,
                                            ModifiedBy = st.ModifiedBy,
                                            ModifiedOn = st.ModifiedOn,
                                            ApprovalStatus = st.ApprovalStatus,
                                            ActiveStatus = st.ActiveStatus,
                                            TotalDeductions = st.TotalDeductions,
                                            TotalEarnings = st.TotalEarnings,
                                            NetSalary = st.NetSalary,
                                            GrossMonthly = st.GrossMonthly,
                                            TotalEmployerContribution = st.TotalEmployerContribution,
                                            CTCMonthly = st.CTCMonthly,
                                            CTCAnnual = st.CTCAnnual,
                                            IsCTCBased = st.IsCTCBased,
                                            ApprovedBy = st.ApprovedBy,
                                            ApprovedOn = st.ApprovedOn,
                                            CreatedBy = st.CreatedBy,
                                            CreatedByValue = emp != null
                                                ? (emp.FirstName + " " + (string.IsNullOrEmpty(emp.MiddleName) ? "" : emp.MiddleName + " ") + emp.LastName).Trim()
                                                : string.Empty
                                        }).AsNoTracking().FirstOrDefaultAsync();
            if (salaryTemplate == null)
            {
                return null; // Return null if the SalaryTemplate does not exist
            }

            salaryTemplate.SalaryTemplateDetails = await GetTemplateDetails(new List<int> { idSalaryTemplate });
            return salaryTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching SalaryTemplate with ID {idSalaryTemplate}.");
            throw new Exception("An error occurred while fetching the salary template. Please try again later.");
        }
    }

    /// <summary>
    /// Template rows with the head information (method, base head, formula) taken from SalaryHeads,
    /// sorted by Type and then Calculation Sequence.
    /// </summary>
    private async Task<List<SalaryTemplateDetailDto>> GetTemplateDetails(List<int> templateIds)
    {
        var rows = await (from std in _dbContext.SalaryTemplateDetails
                          join sh in _dbContext.SalaryHeads
                          on std.IdSalaryHead equals sh.IdSalaryHead into shGroup
                          from sh in shGroup.DefaultIfEmpty()
                          join ph in _dbContext.SalaryHeads
                          on sh.IdPercentageSalaryHead equals ph.IdSalaryHead into phGroup
                          from ph in phGroup.DefaultIfEmpty()
                          where std.IdSalaryTemplate.HasValue && templateIds.Contains(std.IdSalaryTemplate.Value)
                          select new { std, sh, BaseHeadName = ph != null ? ph.SalaryHeadName : null })
                          .AsNoTracking()
                          .ToListAsync();

        return rows
            .Select(x => new SalaryTemplateDetailDto
            {
                IdSalaryTemplateDetail = x.std.IdSalaryTemplateDetail,
                IdSalaryTemplate = x.std.IdSalaryTemplate ?? 0,
                IdSalaryHead = x.std.IdSalaryHead ?? 0,
                FixedAmount = x.std.FixedAmount,
                PercentageValue = x.std.PercentageValue,
                FinalSalaryAmount = x.std.FinalSalaryAmount ?? 0,
                Remarks = x.std.Remarks,
                SalaryHeadName = x.sh?.SalaryHeadName ?? string.Empty,
                SalaryHeadCode = x.sh?.SalaryHeadCode ?? string.Empty,
                HeadType = x.sh?.HeadType ?? string.Empty,
                IsTaxable = x.sh != null && x.sh.IsTaxable,
                OrderNumber = x.sh?.OrderNumber,
                CalcSequence = x.sh?.CalcSequence,
                CalculationMethod = x.sh?.CalculationMethod,
                PercentageOfIdSalaryHead = x.sh?.IdPercentageSalaryHead,
                PercentageOfIdSalaryHeadValue = x.BaseHeadName ?? string.Empty,
                CustomFormula = x.sh?.CustomFormula,
                PaidInNote = x.sh != null && !SalaryStructureCalculator.IsMonthlyHead(x.sh) ? SalaryStructureCalculator.BuildPaidInNote(x.sh) : null
            })
            .OrderBy(d => SalaryStructureCalculator.HeadTypeDisplayOrder(d.HeadType))
            .ThenBy(d => d.CalcSequence ?? int.MaxValue)
            .ToList();
    }

    public async Task<SalaryStructureResultDto> CalculateSalaryStructure(IEnumerable<SalaryStructureRowDto> rows)
    {
        var heads = await _dbContext.SalaryHeads.AsNoTracking().ToDictionaryAsync(h => h.IdSalaryHead);
        return SalaryStructureCalculator.Calculate(rows, heads);
    }

    /// <summary>Checks the template name and rows, and calculates the amounts. Throws InvalidOperationException with the user message.</summary>
    private async Task<SalaryStructureResultDto> ValidateAndCalculate(SalaryTemplateDto dto, int? idSalaryTemplate)
    {
        var name = (dto.SalaryTemplateName ?? string.Empty).Trim();
        var nameExists = await _dbContext.SalaryTemplates.AsNoTracking()
            .AnyAsync(t => t.SalaryTemplateName.Trim() == name && t.IdSalaryTemplate != (idSalaryTemplate ?? 0));
        if (string.IsNullOrEmpty(name) || nameExists)
        {
            throw new InvalidOperationException("A template with this name already exists.");
        }

        var rows = (dto.SalaryTemplateDetails ?? new List<SalaryTemplateDetailDto>())
            .Select(d => new SalaryStructureRowDto { IdSalaryHead = d.IdSalaryHead, FixedAmount = d.FixedAmount, PercentageValue = d.PercentageValue });

        var result = await CalculateSalaryStructure(rows);
        if (result.Errors.Any())
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors));
        }
        return result;
    }

    private static void ApplyTotals(SalaryTemplate entity, SalaryStructureResultDto result)
    {
        entity.TotalEarnings = result.TotalEarnings;
        entity.TotalDeductions = result.TotalDeductions;
        entity.NetSalary = result.NetSalary;
        entity.GrossMonthly = result.GrossMonthly;
        entity.TotalEmployerContribution = result.TotalEmployerContribution;
        entity.CTCMonthly = result.CTCMonthly;
        entity.CTCAnnual = result.CTCAnnual;
    }

    public async Task<SalaryTemplateDto?> AddSalaryTemplate(SalaryTemplateDto dto, int IdEmployee)
    {
        var result = await ValidateAndCalculate(dto, null);

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var templateEntity = new SalaryTemplate
            {
                SalaryTemplateName = dto.SalaryTemplateName.Trim(),
                Description = dto.Description,
                CreatedBy = IdEmployee,
                CreatedOn = DateTime.Now,
                ActiveStatus = true,
                ApprovalStatus = "SUBMITTED"
            };
            ApplyTotals(templateEntity, result);

            await _dbContext.SalaryTemplates.AddAsync(templateEntity);
            await _dbContext.SaveChangesAsync();

            foreach (var row in result.Rows)
            {
                await _dbContext.SalaryTemplateDetails.AddAsync(new SalaryTemplateDetails
                {
                    IdSalaryTemplate = templateEntity.IdSalaryTemplate,
                    IdSalaryHead = row.IdSalaryHead,
                    FixedAmount = row.FixedAmount,
                    PercentageValue = row.PercentageValue,
                    FinalSalaryAmount = row.CalculatedValue,
                    Remarks = dto.SalaryTemplateDetails?.FirstOrDefault(d => d.IdSalaryHead == row.IdSalaryHead)?.Remarks
                });
            }
            await _dbContext.SaveChangesAsync();

            var entityCode = _configuration["WorkflowEntityCodes:SalaryTemplate"];
            // Step: Call the approval workflow service
            var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(templateEntity.IdSalaryTemplate, entityCode, IdEmployee, "SUBMITTED", null, null);

            if (approvalResult != "Approval workflow initiated.")
            {
                throw new Exception(approvalResult);
            }

            await transaction.CommitAsync();
            await _auditService.LogAuditAsync("Create", "SalaryTemplate", templateEntity.IdSalaryTemplate, dto);
            return _mapper.Map<SalaryTemplateDto>(templateEntity);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding SalaryTemplate and Details.");
            throw new Exception("An error occurred while adding the salary template. Please try again.");
        }
    }

    public async Task<SalaryTemplateDto?> UpdateSalaryTemplate(SalaryTemplateDto dto, int IdEmployee)
    {
        var templateEntity = await _dbContext.SalaryTemplates.FirstOrDefaultAsync(t => t.IdSalaryTemplate == dto.IdSalaryTemplate);
        if (templateEntity == null)
        {
            _logger.LogWarning("SalaryTemplate with ID {Id} not found for update.", dto.IdSalaryTemplate);
            return null;
        }

        var result = await ValidateAndCalculate(dto, templateEntity.IdSalaryTemplate);

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var beforeUpdate = _mapper.Map<SalaryTemplateDto>(templateEntity);

            templateEntity.SalaryTemplateName = dto.SalaryTemplateName.Trim();
            templateEntity.Description = dto.Description;
            templateEntity.ModifiedBy = IdEmployee;
            templateEntity.ModifiedOn = DateTime.Now;
            templateEntity.ActiveStatus = dto.ActiveStatus;
            // Any change is submitted for approval again; the approval status is never taken from the request
            templateEntity.ApprovalStatus = "SUBMITTED";
            templateEntity.ApprovedBy = null;
            templateEntity.ApprovedOn = null;
            ApplyTotals(templateEntity, result);

            // One row per head: update rows of heads still in the grid, add new heads, remove the rest
            var existingDetails = await _dbContext.SalaryTemplateDetails
                .Where(d => d.IdSalaryTemplate == templateEntity.IdSalaryTemplate)
                .ToListAsync();

            _dbContext.SalaryTemplateDetails.RemoveRange(
                existingDetails.Where(ed => !result.Rows.Any(r => r.IdSalaryHead == ed.IdSalaryHead)));

            foreach (var row in result.Rows)
            {
                var remarks = dto.SalaryTemplateDetails?.FirstOrDefault(d => d.IdSalaryHead == row.IdSalaryHead)?.Remarks;
                var existingDetail = existingDetails.FirstOrDefault(ed => ed.IdSalaryHead == row.IdSalaryHead);
                if (existingDetail != null)
                {
                    existingDetail.FixedAmount = row.FixedAmount;
                    existingDetail.PercentageValue = row.PercentageValue;
                    existingDetail.FinalSalaryAmount = row.CalculatedValue;
                    existingDetail.Remarks = remarks;
                }
                else
                {
                    await _dbContext.SalaryTemplateDetails.AddAsync(new SalaryTemplateDetails
                    {
                        IdSalaryTemplate = templateEntity.IdSalaryTemplate,
                        IdSalaryHead = row.IdSalaryHead,
                        FixedAmount = row.FixedAmount,
                        PercentageValue = row.PercentageValue,
                        FinalSalaryAmount = row.CalculatedValue,
                        Remarks = remarks
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            var entityCode = _configuration["WorkflowEntityCodes:SalaryTemplate"];
            // Step: Call the approval workflow service
            var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(templateEntity.IdSalaryTemplate, entityCode, IdEmployee, "SUBMITTED", null, null);

            await transaction.CommitAsync();
            await _auditService.LogAuditAsync("Update", "SalaryTemplate", templateEntity.IdSalaryTemplate, new { before = beforeUpdate, after = dto });
            return _mapper.Map<SalaryTemplateDto>(templateEntity);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating SalaryTemplate and Details.");
            throw new Exception("An error occurred while updating the salary template. Please try again.");
        }
    }
}
