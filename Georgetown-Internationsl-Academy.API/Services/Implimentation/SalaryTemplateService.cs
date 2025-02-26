using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class SalaryTemplateService : ISalaryTemplateService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<SalaryTemplateService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApprovalWorkflowService _approvalWorkflowService;


    public SalaryTemplateService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryTemplateService> logger, IConfiguration configuration, IApprovalWorkflowService approvalWorkflowService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _approvalWorkflowService = approvalWorkflowService;
    }

    public async Task<IEnumerable<SalaryTemplateDto>> GetAllSalaryTemplates(string? searchText = null,string? dropdownFilter = null)
    {
        try
        {
            var query = _dbContext.SalaryTemplates.Where(x=>x.ActiveStatus==true).AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.SalaryTemplateName.Contains(searchText) || x.Description.Contains(searchText));
            }

            

            // Apply dropdown filter if provided (e.g., ApprovalStatus)
            if (!string.IsNullOrEmpty(dropdownFilter))
            {
                if(dropdownFilter == _configuration["DropdDownStatus:ApproveStatus"])
                {
                    var interminapprovedSatus = _configuration["DropdDownStatus:InterminApproved"];
                    query = query.Where(x => x.ApprovalStatus == dropdownFilter ||x.ApprovalStatus == interminapprovedSatus);
                }
                else
                {
                    query = query.Where(x => x.ApprovalStatus == dropdownFilter);

                }

            }

            var templates = await query.ToListAsync();
            return _mapper.Map<IEnumerable<SalaryTemplateDto>>(templates);
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
            // Fetch the SalaryTemplate
            var salaryTemplate = await _dbContext.SalaryTemplates
                .FirstOrDefaultAsync(t => t.IdSalaryTemplate == idSalaryTemplate);
            if (salaryTemplate == null)
            {
                return null; // Return null if the SalaryTemplate does not exist
            }

            // Fetch the associated SalaryTemplateDetails with additional fields
            var details = await (from std in _dbContext.SalaryTemplateDetails
                                 join sh in _dbContext.SalaryHeads
                                 on std.IdSalaryHead equals sh.IdSalaryHead into shGroup
                                 from sh in shGroup.DefaultIfEmpty() // LEFT JOIN
                                 where std.IdSalaryTemplate == idSalaryTemplate
                                 select new SalaryTemplateDetailDto
                                 {
                                     IdSalaryTemplateDetail = std.IdSalaryTemplateDetail,
                                     IdSalaryTemplate = std.IdSalaryTemplate,
                                     IdSalaryHead = std.IdSalaryHead,
                                     SalaryHeadName = sh != null ? sh.SalaryHeadName : string.Empty,
                                     HeadType = sh != null ? sh.HeadType : string.Empty,
                                     IsTaxable = sh != null && sh.IsTaxable, // Null for boolean
                                     OrderNumber = sh != null ? sh.OrderNumber : null, // Nullable int
                                     CalculationMethod = std.CalculationMethod,
                                     FixedAmount = std.FixedAmount,
                                     PercentageOfIdSalaryHead = std.PercentageOfIdSalaryHead,
                                     PercentageValue = std.PercentageValue,
                                     CustomFormula = std.CustomFormula,
                                     FinalSalaryAmount = std.FinalSalaryAmount,
                                     Remarks = std.Remarks
                                 }).ToListAsync();

            // Map the SalaryTemplate to DTO
            var result = _mapper.Map<SalaryTemplateDto>(salaryTemplate);

            // Attach the details to the DTO
            result.SalaryTemplateDetails = details;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching SalaryTemplate with ID {idSalaryTemplate}.");
            throw new Exception("An error occurred while fetching the salary template. Please try again later.");
        }
    }


    public async Task<SalaryTemplateDto?> AddSalaryTemplate(SalaryTemplateDto dto, int IdEmployee)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Map and add the SalaryTemplate
            var templateEntity = _mapper.Map<SalaryTemplate>(dto);
            templateEntity.CreatedBy = IdEmployee;
            templateEntity.CreatedOn = DateTime.UtcNow;
            templateEntity.ApprovalStatus = "SUBMITTED";

            await _dbContext.SalaryTemplates.AddAsync(templateEntity);
            await _dbContext.SaveChangesAsync();

            // Add associated SalaryTemplateDetails
            if (dto.SalaryTemplateDetails != null && dto.SalaryTemplateDetails.Any())
            {
                foreach (var detailDto in dto.SalaryTemplateDetails)
                {
                    var detailEntity = _mapper.Map<SalaryTemplateDetails>(detailDto);
                    detailEntity.IdSalaryTemplate = templateEntity.IdSalaryTemplate;

                    await _dbContext.SalaryTemplateDetails.AddAsync(detailEntity);
                }

                await _dbContext.SaveChangesAsync();
            }
            var entityCode = _configuration["WorkflowEntityCodes:SalaryTemplate"];
            // Step: Call the approval workflow service
            var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(templateEntity.IdSalaryTemplate, entityCode, IdEmployee, "SUBMITTED",null);

            if (approvalResult != "Approval workflow initiated.")
            {
                throw new Exception(approvalResult);
            }

            await transaction.CommitAsync();
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
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Update the SalaryTemplate
            var templateEntity = await _dbContext.SalaryTemplates.FirstOrDefaultAsync(t => t.IdSalaryTemplate == dto.IdSalaryTemplate);
            if (templateEntity == null)
            {
                _logger.LogWarning("SalaryTemplate with ID {Id} not found for update.", dto.IdSalaryTemplate);
                return null;
            }

            // Manual mapping for update
            templateEntity.SalaryTemplateName = dto.SalaryTemplateName;
            templateEntity.Description = dto.Description;
            templateEntity.TotalDeductions = dto.TotalDeductions;
            templateEntity.TotalEarnings = dto.TotalEarnings;
            templateEntity.NetSalary = dto.NetSalary;
            templateEntity.ModifiedBy = IdEmployee;
            templateEntity.ModifiedOn = DateTime.UtcNow;
            templateEntity.ApprovalStatus = dto.ApprovalStatus;
            templateEntity.ActiveStatus = dto.ActiveStatus;

            _dbContext.SalaryTemplates.Update(templateEntity);
            await _dbContext.SaveChangesAsync();

            // Handle SalaryTemplateDetails
            if (dto.SalaryTemplateDetails != null)
            {
                var existingDetails = await _dbContext.SalaryTemplateDetails
                    .Where(d => d.IdSalaryTemplate == dto.IdSalaryTemplate)
                    .ToListAsync();

                // Delete details that are not in the updated DTO
                var detailsToDelete = existingDetails
                    .Where(ed => !dto.SalaryTemplateDetails.Any(d => d.IdSalaryTemplateDetail == ed.IdSalaryTemplateDetail))
                    .ToList();
                _dbContext.SalaryTemplateDetails.RemoveRange(detailsToDelete);

                // Update existing details and add new ones
                foreach (var detailDto in dto.SalaryTemplateDetails)
                {
                    var existingDetail = existingDetails.FirstOrDefault(ed => ed.IdSalaryTemplateDetail == detailDto.IdSalaryTemplateDetail);
                    if (existingDetail != null)
                    {
                        // Update existing detail
                        existingDetail.IdSalaryHead = detailDto.IdSalaryHead;
                        existingDetail.CalculationMethod = detailDto.CalculationMethod;
                        existingDetail.FixedAmount = detailDto.FixedAmount;
                        existingDetail.PercentageOfIdSalaryHead = detailDto.PercentageOfIdSalaryHead;
                        existingDetail.PercentageValue = detailDto.PercentageValue;
                        existingDetail.CustomFormula = detailDto.CustomFormula;
                        existingDetail.FinalSalaryAmount = detailDto.FinalSalaryAmount;
                        existingDetail.Remarks = detailDto.Remarks;

                        _dbContext.SalaryTemplateDetails.Update(existingDetail);
                    }
                    else
                    {
                        // Add new detail
                        // Check if the entity is already being tracked and detach it
                        var trackedEntity = _dbContext.ChangeTracker.Entries<SalaryTemplateDetails>()
                            .FirstOrDefault(e => e.Entity.IdSalaryTemplateDetail == detailDto.IdSalaryTemplateDetail);

                        if (trackedEntity != null)
                        {
                            _dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
                        }

                        // Map the new detail entity
                        var newDetailEntity = _mapper.Map<SalaryTemplateDetails>(detailDto);
                        newDetailEntity.IdSalaryTemplate = templateEntity.IdSalaryTemplate;
                        newDetailEntity.IdSalaryTemplateDetail = null; 

                        // Add new details
                        await _dbContext.SalaryTemplateDetails.AddAsync(newDetailEntity);




                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            var entityCode = _configuration["WorkflowEntityCodes:SalaryTemplate"];
            // Step: Call the approval workflow service
            var approvalResult = await _approvalWorkflowService.InitiateApprovalWorkflow(templateEntity.IdSalaryTemplate, entityCode, IdEmployee, "SUBMITTED", null);

            await transaction.CommitAsync();
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
