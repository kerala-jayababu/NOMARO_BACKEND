using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class AssetServices : IAssetServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<AssetServices> _logger;

        public AssetServices(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<AssetServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region Asset Types

        public async Task<IEnumerable<AssetTypeDto>> GetAssetTypes()
        {
            try
            {
                var assetTypes = await _dbContext.AssetTypes
                    .OrderBy(a => a.AssetTypeName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<AssetTypeDto>>(assetTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Asset Types.");
                throw;
            }
        }

        public async Task<bool> AddOrUpdateAssetTypes(List<AssetTypeDto> assetTypeDtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingAssetTypes = await _dbContext.AssetTypes.ToListAsync();

                foreach (var dto in assetTypeDtos)
                {
                    var existing = existingAssetTypes
                        .FirstOrDefault(a => a.IdAssetType == dto.IdAssetType);

                    if (existing != null)
                    {
                        // Update
                        existing.AssetTypeName = dto.AssetTypeName;
                    }
                    else
                    {
                        // Insert
                        var newAssetType = new AssetTypes
                        {
                            AssetTypeName = dto.AssetTypeName
                        };

                        await _dbContext.AssetTypes.AddAsync(newAssetType);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating Asset Types.");
                throw new Exception("Error processing asset types.");
            }
        }

        #endregion

        #region Assets

        public async Task<IEnumerable<AssetDto>> GetAssets()
        {
            try
            {
                var assets = await _dbContext.Assets
                    .OrderBy(a => a.AssetSerialNumber)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<AssetDto>>(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Assets.");
                throw;
            }
        }

        public async Task<bool> AddOrUpdateAssets(List<AssetDto> assetDtos)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingAssets = await _dbContext.Assets.ToListAsync();

                foreach (var dto in assetDtos)
                {
                    var existing = existingAssets
                        .FirstOrDefault(a => a.IdAsset == dto.IdAsset);

                    if (existing != null)
                    {
                        // Update
                        existing.IdAssetType = dto.IdAssetType;
                        existing.AssetSerialNumber = dto.AssetSerialNumber;
                        existing.AssetDetails = dto.AssetDetails;
                        existing.AverageCost = dto.AverageCost;
                        existing.AssetWorkingStatus = dto.AssetWorkingStatus;
                        existing.IsAllocated = dto.IsAllocated;
                    }
                    else
                    {
                        // Insert
                        var newAsset = new Assets
                        {
                            IdAssetType = dto.IdAssetType,
                            AssetSerialNumber = dto.AssetSerialNumber,
                            AssetDetails = dto.AssetDetails,
                            AverageCost = dto.AverageCost,
                            AssetWorkingStatus = dto.AssetWorkingStatus,
                            IsAllocated = dto.IsAllocated
                        };

                        await _dbContext.Assets.AddAsync(newAsset);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating Assets.");
                throw new Exception("Error processing assets.");
            }
        }

        #endregion
    }
}
