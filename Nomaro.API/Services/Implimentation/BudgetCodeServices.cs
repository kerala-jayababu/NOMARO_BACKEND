using AutoMapper;
using Dapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Nomaro.API.Services.Implementation
{
    public class BudgetCodeServices : IBudgetCodeServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;        
        private readonly ILogger<BudgetCodeServices> _logger;
        private readonly IAuditService _auditService;

        public BudgetCodeServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BudgetCodeServices> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        #region BudgetCodes

        public async Task<BudgetCodeDto?> GetBudgetCodeByID(int id)
        {
            try
            {
               
                var budgetCode = await _dbContext.BudgetCodes.FirstOrDefaultAsync(x => x.IdBudgetCode == id);
                if (budgetCode == null)
                {
                    _logger.LogWarning("BudgetCode with ID: {Id} not found.", id);
                    return null;
                }
                return _mapper.Map<BudgetCodeDto>(budgetCode); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BudgetCode with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<BudgetCodeDto>> GetBudgetList()
        {
            try
            {
                var budgetCodes = await _dbContext.BudgetCodes
         .OrderBy(b => b.BudgetCode)  // Sorting by BudgetCode in ascending order
         .ToListAsync();
                var budgetCodeDtos = _mapper.Map<IEnumerable<BudgetCodeDto>>(budgetCodes);

                return budgetCodeDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of BudgetCodes.");
                throw;
            }
        }

        //public async Task<IEnumerable<BudgetCodeDto>> GetBudgetList1()
        //{
        //    try
        //    {
        //        _logger.LogInformation("Fetching BudgetCode list using Dapper.");

        //        // Define the raw SQL query
        //        var query = "SELECT IdBudgetCode, BudgetCode, BudgetCodeName FROM BudgetCodes";

        //        // Get the database connection
        //        using (var connection = _dbContext.Database.GetDbConnection())
        //        {
        //            // Open the connection if it's not already open
        //            if (connection.State == ConnectionState.Closed)
        //                await connection.OpenAsync();

        //            // Execute the query and map the result to BudgetCodeDto
        //            var budgetCodeDtos = await connection.QueryAsync<BudgetCodeDto>(query);

        //            return budgetCodeDtos;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching the list of BudgetCodes using Dapper.");
        //        throw;
        //    }
        //}

        public async Task<BudgetCodeDto?> AddBudgetCode(BudgetCodeDto budgetCodeDto)
        {
            try
            {

                var budgetCodeEntity = _mapper.Map<BudgetCodeEntity>(budgetCodeDto);
                var addedEntity = await _dbContext.BudgetCodes.AddAsync(budgetCodeEntity);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "BudgetCode",
                    entityId: addedEntity.Entity.IdBudgetCode,
                    actionDetails: new { after = addedEntity.Entity });

                return _mapper.Map<BudgetCodeDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding BudgetCode: {@BudgetCodeDto}", budgetCodeDto);
                return null;
            }
        }   
       
        public async Task<BudgetCodeDto?> UpdateBudgetCode(BudgetCodeDto budgetCodeDto)
        {
            try
            {
                var budgetCode = await _dbContext.BudgetCodes.FirstOrDefaultAsync(x => x.IdBudgetCode == budgetCodeDto.IdBudgetCode);

                if (budgetCode != null)
                {

                    var beforeUpdate = _mapper.Map<BudgetCodeDto>(budgetCode);

                    budgetCode.BudgetCode = budgetCodeDto.BudgetCode;
                    budgetCode.BudgetCodeName = budgetCodeDto.BudgetCodeName;

                    var updatedEntity = _dbContext.BudgetCodes.Update(budgetCode);
                    await _dbContext.SaveChangesAsync();

                    await _auditService.LogAuditAsync(
                        actionType: "Update",
                        entityName: "BudgetCode",
                        entityId: updatedEntity.Entity.IdBudgetCode,
                        actionDetails: new { before = beforeUpdate, after = _mapper.Map<BudgetCodeDto>(updatedEntity.Entity) });

                    return _mapper.Map<BudgetCodeDto>(updatedEntity.Entity);
                }

                _logger.LogWarning("BudgetCode with ID: {Id} not found for update.", budgetCodeDto.IdBudgetCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating BudgetCode with ID: {Id}", budgetCodeDto.IdBudgetCode);
                return null;
            }
        }

        #endregion 


    }
}

