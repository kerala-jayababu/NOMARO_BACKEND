using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Implementation;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class DesignationServices : IDesignationServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<DesignationServices> _logger;
        private readonly IAuditService _auditService;

        public DesignationServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<DesignationServices> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        #region Designations

        public async Task<DesignationDto?> GetDesignationByID(int id)
        {
            try
            {
                var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.IdDesignation == id);
                if (designation == null)
                {
                    _logger.LogWarning("Designation with ID: {Id} not found.", id);
                    return null;
                }
                return _mapper.Map<DesignationDto>(designation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Designation with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DesignationDto>> GetDesignationList()
        {
            try
            {
                var designations = await _dbContext.Designations.OrderBy(b => b.DesignationCode).ToListAsync();
                var designationDtos = _mapper.Map<IEnumerable<DesignationDto>>(designations);

                return designationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of Designations.");
                throw;
            }
        }

        public async Task<DesignationDto?> AddDesignation(DesignationDto designationDto)
        {
            try
            {
                var designationEntity = _mapper.Map<DesignationEntity>(designationDto);
                var addedEntity = await _dbContext.Designations.AddAsync(designationEntity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "Designation",
                    entityId: addedEntity.Entity.IdDesignation,
                    actionDetails: new { after = addedEntity.Entity });

                return _mapper.Map<DesignationDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Designation: {@DesignationDto}", designationDto);
                return null;
            }
        }

        public async Task<DesignationDto?> UpdateDesignation(DesignationDto designationDto)
        {
            try
            {
                var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.IdDesignation == designationDto.IdDesignation);

                if (designation != null)
                {
                    var beforeUpdate = _mapper.Map<DesignationDto>(designation);

                    designation.DesignationCode = designationDto.DesignationCode;
                    designation.DesignationName = designationDto.DesignationName;
                    designation.IsOvertimeAllowanceAllowed = designationDto.IsOvertimeAllowanceAllowed;

                    var updatedEntity = _dbContext.Designations.Update(designation);
                    await _dbContext.SaveChangesAsync();

                    await _auditService.LogAuditAsync(
                        actionType: "Update",
                        entityName: "Designation",
                        entityId: updatedEntity.Entity.IdDesignation,
                        actionDetails: new { before = beforeUpdate, after = _mapper.Map<DesignationDto>(updatedEntity.Entity) });

                    return _mapper.Map<DesignationDto>(updatedEntity.Entity);
                }

                _logger.LogWarning("Designation with ID: {Id} not found for update.", designationDto.IdDesignation);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Designation with ID: {Id}", designationDto.IdDesignation);
                return null;
            }
        }

        #endregion


    }

}

