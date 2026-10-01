using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
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

        public async Task<IEnumerable<AssetDto>> GetAssets(string? searchText)
        {
            try
            {
                var query =
                    from a in _dbContext.Assets
                    join at in _dbContext.AssetTypes
                        on a.IdAssetType equals at.IdAssetType
                    select new { a, at };

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(x =>
                        (x.a.AssetSerialNumber != null &&
                         x.a.AssetSerialNumber.Contains(searchText)) ||

                        (x.a.AssetDetails != null &&
                         x.a.AssetDetails.Contains(searchText)) ||

                        (x.at.AssetTypeName != null &&
                         x.at.AssetTypeName.Contains(searchText))
                    );
                }

                var assets = await query
                    .OrderBy(x => x.a.AssetSerialNumber)
                    .Select(x => new AssetDto
                    {
                        IdAsset = x.a.IdAsset,
                        AssetSerialNumber = x.a.AssetSerialNumber,
                        AssetDetails = x.a.AssetDetails,
                        IdAssetType = x.a.IdAssetType,
                        AssetTypeName = x.at.AssetTypeName,
                        AssetWorkingStatus = x.a.AssetWorkingStatus,
                        AverageCost = x.a.AverageCost,
                        IsAllocated = x.a.IsAllocated
                    })
                    .AsNoTracking()
                    .ToListAsync();

                // Current allocation: the latest assignment of each asset
                var assetIds = assets.Select(a => a.IdAsset).ToList();
                var allocations = await (
                    from aa in _dbContext.AssetAssignments.AsNoTracking()
                    where assetIds.Contains(aa.IdAsset)
                    join o in _dbContext.Offices.AsNoTracking() on aa.IdOffice equals (int?)o.IdOffice into officeGroup
                    from o in officeGroup.DefaultIfEmpty()
                    join e in _dbContext.Employees.AsNoTracking() on aa.IdEmployee equals e.IdEmployee into employeeGroup
                    from e in employeeGroup.DefaultIfEmpty()
                    select new
                    {
                        aa.IdAsset,
                        aa.IdAssetAssignment,
                        aa.IdOffice,
                        OfficeName = o != null ? o.OfficeName : null,
                        aa.IdEmployee,
                        EmployeeCode = e != null ? e.EmployeeCode : null,
                        FirstName = e != null ? e.FirstName : null,
                        MiddleName = e != null ? e.MiddleName : null,
                        LastName = e != null ? e.LastName : null
                    }).ToListAsync();

                foreach (var asset in assets)
                {
                    var current = allocations
                        .Where(x => x.IdAsset == asset.IdAsset)
                        .OrderByDescending(x => x.IdAssetAssignment)
                        .FirstOrDefault();
                    if (current == null) continue;

                    asset.IdOffice = current.IdOffice;
                    asset.OfficeName = current.OfficeName;
                    asset.IdEmployee = current.IdEmployee;
                    asset.EmployeeCode = current.EmployeeCode;
                    asset.EmployeeName = current.IdEmployee.HasValue
                        ? string.Join(" ", new[] { current.FirstName, current.MiddleName, current.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)))
                        : null;
                }

                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Assets.");
                throw;
            }
        }


        public async Task<bool> AddOrUpdateAssets(List<AssetDto> assetDtos, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var existingAssets = await _dbContext.Assets.ToListAsync();

                foreach (var dto in assetDtos)
                {
                    var existing = existingAssets
                        .FirstOrDefault(a => a.IdAsset == dto.IdAsset);

                    await ValidateAllocation(dto);

                    if (existing != null)
                    {
                        // Update
                        existing.IdAssetType = dto.IdAssetType;
                        existing.AssetSerialNumber = dto.AssetSerialNumber;
                        existing.AssetDetails = dto.AssetDetails;
                        existing.AverageCost = dto.AverageCost;
                        existing.AssetWorkingStatus = dto.AssetWorkingStatus;
                    }
                    else
                    {
                        // Insert
                        existing = new Assets
                        {
                            IdAssetType = dto.IdAssetType,
                            AssetSerialNumber = dto.AssetSerialNumber,
                            AssetDetails = dto.AssetDetails,
                            AverageCost = dto.AverageCost,
                            AssetWorkingStatus = dto.AssetWorkingStatus
                        };

                        await _dbContext.Assets.AddAsync(existing);
                        await _dbContext.SaveChangesAsync(); // gets IdAsset for the assignment
                    }

                    await SaveAllocation(existing, dto.IdOffice, dto.IdEmployee, idLoggedInEmployee);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating Assets.");
                throw new Exception("Error processing assets.");
            }
        }

        /// <summary>Office must exist and be active; the employee must exist and, when an office is chosen, be posted there now.</summary>
        private async Task ValidateAllocation(AssetDto dto)
        {
            if (dto.IdOffice.HasValue)
            {
                var officeIsActive = await _dbContext.Offices.AsNoTracking()
                    .AnyAsync(o => o.IdOffice == dto.IdOffice.Value && o.IsActive);
                if (!officeIsActive)
                    throw new InvalidOperationException("The selected office does not exist or is inactive.");
            }

            if (dto.IdEmployee.HasValue)
            {
                var employeeExists = await _dbContext.Employees.AsNoTracking()
                    .AnyAsync(e => e.IdEmployee == dto.IdEmployee.Value);
                if (!employeeExists)
                    throw new InvalidOperationException("The selected employee does not exist.");

                if (dto.IdOffice.HasValue)
                {
                    var isPostedInOffice = await _dbContext.EmployeeOfficePostings.AsNoTracking()
                        .AnyAsync(p => p.IdEmployee == dto.IdEmployee.Value && p.IdOffice == dto.IdOffice.Value && p.IsCurrentPosting);
                    if (!isPostedInOffice)
                        throw new InvalidOperationException("The selected employee is not currently posted in the selected office.");
                }
            }
        }

        /// <summary>
        /// Keeps one current allocation row per asset in AssetAssignments (same as Assign / Unassign on the employee screen):
        /// office and / or employee set -> the row is added or updated; both empty -> the row is removed.
        /// IsAllocated is true while the asset is allocated to an office or an employee.
        /// </summary>
        private async Task SaveAllocation(Assets asset, int? idOffice, int? idEmployee, int idLoggedInEmployee)
        {
            var current = await _dbContext.AssetAssignments
                .Where(x => x.IdAsset == asset.IdAsset)
                .OrderByDescending(x => x.IdAssetAssignment)
                .FirstOrDefaultAsync();

            if (!idOffice.HasValue && !idEmployee.HasValue)
            {
                if (current != null)
                {
                    _dbContext.AssetAssignments.Remove(current);
                }
                asset.IsAllocated = false;
                return;
            }

            if (current == null)
            {
                await _dbContext.AssetAssignments.AddAsync(new AssetAssignments
                {
                    IdAsset = asset.IdAsset,
                    IdOffice = idOffice,
                    IdEmployee = idEmployee,
                    AssignedDate = DateTime.Today,
                    AssignedBy = idLoggedInEmployee,
                    AssignedDateTime = DateTime.Now
                });
            }
            else if (current.IdOffice != idOffice || current.IdEmployee != idEmployee)
            {
                // A new holder starts from today
                if (current.IdEmployee != idEmployee)
                {
                    current.AssignedDate = DateTime.Today;
                    current.AssignedTillDate = null;
                }
                current.IdOffice = idOffice;
                current.IdEmployee = idEmployee;
                current.AssignedBy = idLoggedInEmployee;
                current.AssignedDateTime = DateTime.Now;
            }

            asset.IsAllocated = true;
        }

        #endregion
    }
}

