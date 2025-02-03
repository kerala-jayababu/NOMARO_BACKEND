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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OvertimeTransactionService(ApplicationDBContext dbContext, IMapper mapper, ILogger<OvertimeTransactionService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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

        public async Task<OvertimeTransactionDto?> AddOvertimeTransaction(OvertimeTransactionDto transactionDto,int IdEmployee)
        {
            try
            {
            var transactionEntity = _mapper.Map<OvertimeTransactionEntity>(transactionDto);
                string filePath = null;

            if (transactionDto.File != null)
            {
                string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/OvertimeTransactions");
                if (!Directory.Exists(uploadFolderPath))
                {
                    Directory.CreateDirectory(uploadFolderPath);
                }

                string uniqueFileName = $"{Guid.NewGuid()}_{transactionDto.IdEmployee}_{transactionDto.File.FileName}";
                filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await transactionDto.File.CopyToAsync(stream);
                }
            }

            transactionEntity.CreatedBy = IdEmployee;
            transactionEntity.CreatedOn = DateTime.Now;
            transactionEntity.Attachment = filePath;
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

                string filePath = transaction.Attachment;

                if (transactionDto.File != null)
                {
                    if (Directory.Exists(transaction.Attachment))
                    {
                        Directory.Delete(transaction.Attachment);
                    }

                    string uploadFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads/OvertimeTransactions");
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{transactionDto.IdEmployee}_{transactionDto.File.FileName}";
                    filePath = Path.Combine(uploadFolderPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await transactionDto.File.CopyToAsync(stream);
                    }
                }

            transaction.StartTime = transactionDto.StartTime;
            transaction.EndTime = transactionDto.EndTime;
            transaction.StartDate = transactionDto.StartDate;
            transaction.EndDate = transactionDto.EndDate;
            transaction.DurationInHours = transactionDto.DurationInHours;
            transaction.ReasonForOverTime = transactionDto.ReasonForOvertime;
            transaction.AttachmentDescription = transactionDto.AttachmentDescription;
            transaction.ApprovalStatus = transactionDto.ApprovalStatus;
            transaction.Attachment = filePath;

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
