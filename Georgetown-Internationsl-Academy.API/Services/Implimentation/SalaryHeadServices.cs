using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class SalaryHeadServices : ISalaryHeadServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryHeadServices> _logger;

        public SalaryHeadServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryHeadServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SalaryHeadDto>> GetSalaryHeadList()
        {
            try
            {
                var salaryHeads = await _dbContext.SalaryHeads
             .OrderBy(b => b.HeadType == "EARNING" ? 0 : 1) // "EARNING" first, then "DEDUCTION"
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
            try
            {
                var validCalculationMethods = new[] { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT" };
                if (string.IsNullOrWhiteSpace(dto.CalculationMethod) ||
                    !validCalculationMethods.Contains(dto.CalculationMethod.ToUpper()))
                {
                    throw new ArgumentException($"Invalid CalculationMethod. Allowed values are: {string.Join(", ", validCalculationMethods)}.", nameof(dto.CalculationMethod));
                }
                var salaryHeadEntity = _mapper.Map<SalaryHeads>(dto);              
                salaryHeadEntity.CreatedOn = DateTime.Now;
                salaryHeadEntity.HeadType = dto.HeadType.ToUpper().Trim();
                salaryHeadEntity.CreatedBy = IdEmployee;
                var addedEntity = await _dbContext.SalaryHeads.AddAsync(salaryHeadEntity);
                await _dbContext.SaveChangesAsync();

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
            try
            {
                var salaryHead = await _dbContext.SalaryHeads.FirstOrDefaultAsync(s => s.IdSalaryHead == dto.IdSalaryHead);
                if (salaryHead == null) return null;

                salaryHead.SalaryHeadCode = dto.SalaryHeadCode;
                salaryHead.SalaryHeadName = dto.SalaryHeadName;
                salaryHead.HeadType = dto.HeadType;
                salaryHead.IsTaxable = dto.IsTaxable;
                salaryHead.IsActive = dto.IsActive;
                salaryHead.CalculationMethod = dto.CalculationMethod;
                salaryHead.IdPercentageSalaryHead = dto.IdPercentageSalaryHead;
                salaryHead.PercentageValue = dto.PercentageValue;
                salaryHead.TaxExcemptionThresholdType = dto.TaxExcemptionThresholdType;
                salaryHead.TaxExcemptionThresholdValue = dto.TaxExcemptionThresholdValue;
                salaryHead.FixedValue = dto.FixedValue;
                salaryHead.CustomFormula = dto.CustomFormula;
                salaryHead.ModifiedBy = IdEmployee;
                salaryHead.ModifiedOn = DateTime.Now;
                salaryHead.OrderNumber = dto.OrderNumber;

                var updatedEntity = _dbContext.SalaryHeads.Update(salaryHead);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<SalaryHeadDto>(updatedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salary head with ID: {Id}", dto.IdSalaryHead);
                return null;
            }
        }
    }

}
