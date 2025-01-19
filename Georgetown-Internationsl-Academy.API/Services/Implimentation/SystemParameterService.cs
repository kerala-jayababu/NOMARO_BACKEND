using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class SystemParameterService: ISystemParameterService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SalaryHeadServices> _logger;

        public SystemParameterService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SalaryHeadServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<List<SystemParameterDto>> GetAllSystemParameters()
        {
            try
            {
                var parameters = await _dbContext.SystemParameters.ToListAsync();

                if (parameters == null || !parameters.Any())
                {
                    return new List<SystemParameterDto>(); // Return an empty list if no data is found
                }

                return _mapper.Map<List<SystemParameterDto>>(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system parameters.");
                throw;
            }
        }


        public async Task<SystemParameterDto> GetSystemParameterById(int id)
        {
            try
            {
                var parameter = await _dbContext.SystemParameters
                    .FirstOrDefaultAsync(param => param.IdSystemParameter == id);

                if (parameter == null)
                {
                    _logger.LogWarning("No system parameter found for ID: {Id}", id);
                    return null;
                }

                // Map to DTO
                return _mapper.Map<SystemParameterDto>(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system parameter for ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateSystemParameter(SystemParameterDto dto)
        {
            try
            {
                var existingParameter = await _dbContext.SystemParameters
                    .FirstOrDefaultAsync(param => param.IdSystemParameter == dto.IdSystemParameter);

                if (existingParameter == null)
                {
                    _logger.LogWarning("No system parameter found for update with ID: {Id}", dto.IdSystemParameter);
                    return false;
                }

                // Update fields
                existingParameter.ParameterName = dto.ParameterName;
                existingParameter.ParameterDescription = dto.ParameterDescription;
                existingParameter.ParameterValue = dto.ParameterValue;
                existingParameter.ParameterBinaryValue = dto.ParameterBinaryValue;
                existingParameter.DataType = dto.DataType;
                existingParameter.ValidValues = dto.ValidValues;

                _dbContext.SystemParameters.Update(existingParameter);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system parameter with ID: {Id}", dto.IdSystemParameter);
                throw;
            }
        }



    }
}
