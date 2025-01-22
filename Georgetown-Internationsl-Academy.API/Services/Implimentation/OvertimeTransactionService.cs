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
            var transactions = await _dbContext.OvertimeTransactions.ToListAsync();
            return _mapper.Map<IEnumerable<OvertimeTransactionDto>>(transactions);
        }

        public async Task<OvertimeTransactionDto?> GetOvertimeTransactionById(int id)
        {
            var transaction = await _dbContext.OvertimeTransactions.FirstOrDefaultAsync(x => x.IdOvertimeTransaction == id);
            return transaction == null ? null : _mapper.Map<OvertimeTransactionDto>(transaction);
        }

        public async Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transactionDto)
        {
            var transactionEntity = _mapper.Map<OvertimeTransactionEntity>(transactionDto);
            await _dbContext.OvertimeTransactions.AddAsync(transactionEntity);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<OvertimeTransactionDto>(transactionEntity);
        }

        public async Task<OvertimeTransactionDto?> UpdateOvertimeTransaction(OvertimeTransactionDto transactionDto)
        {
            var transaction = await _dbContext.OvertimeTransactions.FirstOrDefaultAsync(x => x.IdOvertimeTransaction == transactionDto.IdOvertimeTransaction);
            if (transaction == null) return null;

            transaction.StartTime = transactionDto.StartTime;
            transaction.DurationInHours = transactionDto.DurationInHours;
            transaction.ReasonForOverTime = transactionDto.ReasonForOverTime;
            transaction.ManagerApprovedDate = transactionDto.ManagerApprovedDate;
            transaction.HRApprovedDate = transactionDto.HRApprovedDate;

            _dbContext.OvertimeTransactions.Update(transaction);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<OvertimeTransactionDto>(transaction);
        }
    }
}
