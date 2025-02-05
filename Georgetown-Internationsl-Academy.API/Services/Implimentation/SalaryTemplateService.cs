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


    public SalaryTemplateService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryTemplateService> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
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

    public async Task<SalaryTemplateDto?> GetSalaryTemplateById(int id)
    {
        try
        {
            var template = await _dbContext.SalaryTemplates.FirstOrDefaultAsync(t => t.IdSalaryTemplate == id);
            return template == null ? null : _mapper.Map<SalaryTemplateDto>(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching salary template with ID: {id}");
            throw;
        }
    }

    public async Task<SalaryTemplateDto?> AddSalaryTemplate(SalaryTemplateManageDto salaryTemplate,int IdEmployee)
    {
        try
        {
            var templateEntity = _mapper.Map<SalaryTemplate>(salaryTemplate);
            templateEntity.CreatedBy = IdEmployee;
            templateEntity.CreatedOn = DateTime.Now;
            var addedEntity = await _dbContext.SalaryTemplates.AddAsync(templateEntity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SalaryTemplateDto>(addedEntity.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding salary template.");
            return null;
        }
    }

    public async Task<SalaryTemplateDto?> UpdateSalaryTemplate(SalaryTemplateDto salaryTemplate,int IdEmployee)
    {
        try
        {
            var template = await _dbContext.SalaryTemplates.FirstOrDefaultAsync(t => t.IdSalaryTemplate == salaryTemplate.IdSalaryTemplate);
            if (template == null) return null;

            template.SalaryTemplateName = salaryTemplate.SalaryTemplateName;
            template.Description = salaryTemplate.Description;
            template.ModifiedBy = IdEmployee;
            template.ModifiedOn = DateTime.UtcNow;
            template.ApprovalStatus = salaryTemplate.ApprovalStatus;
            template.ActiveStatus = salaryTemplate.ActiveStatus;

            _dbContext.SalaryTemplates.Update(template);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SalaryTemplateDto>(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating salary template with ID: {salaryTemplate.IdSalaryTemplate}");
            return null;
        }
    }

}
