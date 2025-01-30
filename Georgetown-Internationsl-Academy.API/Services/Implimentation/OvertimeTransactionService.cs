using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class OvertimeTransactionService:IOvertimeTransactionService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<OvertimeTransactionService> _logger;

        public OvertimeTransactionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<OvertimeTransactionService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<OvertimeTransactionDto>> GetOvertimeTransactionList()
        {
            try
            {
                var transactions = await _dbContext.OvertimeTransactions.ToListAsync();
            return _mapper.Map<IEnumerable<OvertimeTransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching overtime transactions.");
                throw;
            }
        }

        public async Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id)
        {
            try
            {
                var transaction = await _dbContext.OvertimeTransactions.FirstOrDefaultAsync(x => x.IdOvertimeTransaction == id);
            return transaction == null ? null : _mapper.Map<OvertimeTransactionDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching overtime transaction with ID: {Id}.", id);
                throw;
            }
        }

        public async Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transactionDto)
        {
            try
            {
                var transactionEntity = _mapper.Map<OvertimeTransactionEntity>(transactionDto);
            transactionEntity.CreatedBy = 1;
            transactionEntity.CreatedOn = DateTime.Now;
            await _dbContext.OvertimeTransactions.AddAsync(transactionEntity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<OvertimeTransactionDto>(transactionEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding overtime transaction: {@Dto}.", transactionDto);
                return null;
            }
        }

        public async Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transactionDto)
        {
            try
            {
                var transaction = await _dbContext.OvertimeTransactions.FirstOrDefaultAsync(x => x.IdOvertimeTransaction == transactionDto.IdOvertimeTransaction);
            if (transaction == null) return null;

            transaction.StartTime = transactionDto.StartTime;
            transaction.EndTime = transactionDto.EndTime;
            transaction.StartDate = transactionDto.StartDate;
            transaction.EndDate = transactionDto.EndDate;
            transaction.DurationInHours = transactionDto.DurationInHours;
            transaction.ReasonForOverTime = transactionDto.ReasonForOvertime;
            transaction.ManagerApprovedDate = transactionDto.ManagerApprovedDate;
            transaction.AttachmentDescription = transactionDto.AttachmentDescription;
            transaction.HRApprovedDate = transactionDto.HRApprovedDate;
            transaction.IdManagerApprovedBy = transactionDto.IdManagerApprovedBy;
            transaction.IdHRApprovedBy = transactionDto.IdHRApprovedBy;

            _dbContext.OvertimeTransactions.Update(transaction);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<OvertimeTransactionDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overtime transaction with ID: {Id}.", transactionDto.IdOvertimeTransaction);
                return null;
            }
        }
    }
}
