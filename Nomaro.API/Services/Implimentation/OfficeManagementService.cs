using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Models.Shift;
using Nomaro.API.Models.Time___Attendance.Shift;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    public class OfficeManagementService : IOfficeManagementService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<OfficeManagementService> _logger;
        private readonly IAuditService _auditService;

        public OfficeManagementService(
            ApplicationDBContext dbContext,
            IMapper mapper,
            ILogger<OfficeManagementService> logger,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }

        #region Office Types

        public async Task<IEnumerable<OfficeTypeDto>> GetOfficeTypes(bool? isActive)
        {
            try
            {
                var query = _dbContext.OfficeTypes.AsNoTracking();

                if (isActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == isActive.Value);
                }

                var officeTypes = await query
                    .OrderBy(x => x.HierarchyLevel)
                    .ThenBy(x => x.OfficeTypeName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<OfficeTypeDto>>(officeTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Office Types.");
                throw;
            }
        }

        public async Task<bool> IsOfficeTypeCodeExists(string officeTypeCode, int idOfficeType)
        {
            return await _dbContext.OfficeTypes
                .AnyAsync(x => x.OfficeTypeCode == officeTypeCode.Trim() && x.IdOfficeType != idOfficeType);
        }

        public async Task<bool> AddOrUpdateOfficeTypes(OfficeTypeDto dto, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var entity = await _dbContext.OfficeTypes
                    .FirstOrDefaultAsync(x => x.IdOfficeType == dto.IdOfficeType);

                OfficeTypeDto? beforeData = null;
                var isUpdate = entity != null;

                if (entity != null)
                {
                    beforeData = _mapper.Map<OfficeTypeDto>(entity);
                    entity.OfficeTypeCode = dto.OfficeTypeCode.Trim();
                    entity.OfficeTypeName = dto.OfficeTypeName.Trim();
                    entity.HierarchyLevel = dto.HierarchyLevel;
                    entity.IsActive = dto.IsActive;
                    entity.IdModifiedBy = idLoggedInEmployee;
                    entity.ModifiedDateTime = DateTime.Now;
                }
                else
                {
                    entity = new OfficeTypes
                    {
                        OfficeTypeCode = dto.OfficeTypeCode.Trim(),
                        OfficeTypeName = dto.OfficeTypeName.Trim(),
                        HierarchyLevel = dto.HierarchyLevel,
                        IsActive = dto.IsActive,
                        IdCreatedBy = idLoggedInEmployee,
                        CreatedDateTime = DateTime.Now
                    };
                    await _dbContext.OfficeTypes.AddAsync(entity);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isUpdate)
                {
                    await _auditService.LogAuditAsync(
                        actionType: "Update",
                        entityName: "OfficeType",
                        entityId: entity.IdOfficeType,
                        actionDetails: new { before = beforeData, after = _mapper.Map<OfficeTypeDto>(entity) });
                }
                else
                {
                    await _auditService.LogAuditAsync(
                        actionType: "Create",
                        entityName: "OfficeType",
                        entityId: entity.IdOfficeType,
                        actionDetails: new { after = _mapper.Map<OfficeTypeDto>(entity) });
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating Office Type.");
                throw;
            }
        }

        #endregion

        #region Offices

        public async Task<IEnumerable<OfficeDto>> GetOffices(string? searchText, int? idOfficeType, int? idParentOffice, bool? isActive)
        {
            try
            {
                var query =
                    from o in _dbContext.Offices
                    join ot in _dbContext.OfficeTypes
                        on o.IdOfficeType equals ot.IdOfficeType
                    join po in _dbContext.Offices
                        on o.IdParentOffice equals po.IdOffice into parentOffices
                    from po in parentOffices.DefaultIfEmpty()
                    join e in _dbContext.Employees
                        on o.IdOfficeHead equals e.IdEmployee into officeHeads
                    from e in officeHeads.DefaultIfEmpty()
                    select new { o, ot, po, e };

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(x =>
                        x.o.OfficeCode.Contains(searchText) ||
                        x.o.OfficeName.Contains(searchText) ||
                        (x.o.City != null && x.o.City.Contains(searchText)) ||
                        (x.o.State != null && x.o.State.Contains(searchText))
                    );
                }

                if (idOfficeType.HasValue)
                {
                    query = query.Where(x => x.o.IdOfficeType == idOfficeType.Value);
                }

                if (idParentOffice.HasValue)
                {
                    query = query.Where(x => x.o.IdParentOffice == idParentOffice.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(x => x.o.IsActive == isActive.Value);
                }

                var offices = await query
                    .OrderBy(x => x.ot.HierarchyLevel)
                    .ThenBy(x => x.o.OfficeName)
                    .Select(x => new OfficeDto
                    {
                        IdOffice = x.o.IdOffice,
                        OfficeCode = x.o.OfficeCode,
                        OfficeName = x.o.OfficeName,
                        IdOfficeType = x.o.IdOfficeType,
                        IdParentOffice = x.o.IdParentOffice,
                        AddressLine1 = x.o.AddressLine1,
                        AddressLine2 = x.o.AddressLine2,
                        City = x.o.City,
                        District = x.o.District,
                        State = x.o.State,
                        Country = x.o.Country,
                        PinCode = x.o.PinCode,
                        PhoneNumber = x.o.PhoneNumber,
                        EmailId = x.o.EmailId,
                        GSTIN = x.o.GSTIN,
                        IdOfficeHead = x.o.IdOfficeHead,
                        IdShiftSchedule = x.o.IdShiftSchedule,
                        OpenedDate = x.o.OpenedDate,
                        ClosedDate = x.o.ClosedDate,
                        IsActive = x.o.IsActive,
                        OfficeTypeCode = x.ot.OfficeTypeCode,
                        OfficeTypeName = x.ot.OfficeTypeName,
                        HierarchyLevel = x.ot.HierarchyLevel,
                        ParentOfficeCode = x.po != null ? x.po.OfficeCode : null,
                        ParentOfficeName = x.po != null ? x.po.OfficeName : null,
                        OfficeHeadName = x.e != null
                            ? x.e.FirstName + (x.e.MiddleName != null && x.e.MiddleName != "" ? " " + x.e.MiddleName : "") + (x.e.LastName != null && x.e.LastName != "" ? " " + x.e.LastName : "")
                            : null
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return offices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Offices.");
                throw;
            }
        }

        public async Task<OfficeDto?> GetOfficeById(int idOffice)
        {
            try
            {
                var office = await (
                    from o in _dbContext.Offices
                    join ot in _dbContext.OfficeTypes
                        on o.IdOfficeType equals ot.IdOfficeType
                    join po in _dbContext.Offices
                        on o.IdParentOffice equals po.IdOffice into parentOffices
                    from po in parentOffices.DefaultIfEmpty()
                    join e in _dbContext.Employees
                        on o.IdOfficeHead equals e.IdEmployee into officeHeads
                    from e in officeHeads.DefaultIfEmpty()
                    where o.IdOffice == idOffice
                    select new OfficeDto
                    {
                        IdOffice = o.IdOffice,
                        OfficeCode = o.OfficeCode,
                        OfficeName = o.OfficeName,
                        IdOfficeType = o.IdOfficeType,
                        IdParentOffice = o.IdParentOffice,
                        AddressLine1 = o.AddressLine1,
                        AddressLine2 = o.AddressLine2,
                        City = o.City,
                        District = o.District,
                        State = o.State,
                        Country = o.Country,
                        PinCode = o.PinCode,
                        PhoneNumber = o.PhoneNumber,
                        EmailId = o.EmailId,
                        GSTIN = o.GSTIN,
                        IdOfficeHead = o.IdOfficeHead,
                        IdShiftSchedule = o.IdShiftSchedule,
                        OpenedDate = o.OpenedDate,
                        ClosedDate = o.ClosedDate,
                        IsActive = o.IsActive,
                        OfficeTypeCode = ot.OfficeTypeCode,
                        OfficeTypeName = ot.OfficeTypeName,
                        HierarchyLevel = ot.HierarchyLevel,
                        ParentOfficeCode = po != null ? po.OfficeCode : null,
                        ParentOfficeName = po != null ? po.OfficeName : null,
                        OfficeHeadName = e != null
                            ? e.FirstName + (e.MiddleName != null && e.MiddleName != "" ? " " + e.MiddleName : "") + (e.LastName != null && e.LastName != "" ? " " + e.LastName : "")
                            : null
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (office != null)
                {
                    var shiftScheduleQuery =
                        from schedule in _dbContext.ShiftSchedules.AsNoTracking()
                        join shift in _dbContext.ShiftDefinitions.AsNoTracking()
                            on schedule.IdShift equals shift.IdShift
                        select new { Schedule = schedule, Shift = shift };

                    if (office.IdShiftSchedule.HasValue)
                    {
                        shiftScheduleQuery = shiftScheduleQuery
                            .Where(x => x.Schedule.IdShiftSchedule == office.IdShiftSchedule.Value);
                    }
                    else
                    {
                        shiftScheduleQuery = shiftScheduleQuery
                            .Where(x => x.Shift.IdOffice == office.IdOffice && x.Shift.IsRegularShiftJustTimeChange);
                    }

                    office.ShiftSchedule = await shiftScheduleQuery
                        .OrderBy(x => x.Schedule.IdShiftSchedule)
                        .Select(x => new OfficeShiftScheduleDto
                        {
                            ShiftName = x.Shift.ShiftName,
                            IsRegularShiftJustTimeChange = x.Shift.IsRegularShiftJustTimeChange,
                            StartTime = x.Schedule.StartTime,
                            EndTime = x.Schedule.EndTime,
                            WorkDays = x.Schedule.WorkDays
                        })
                        .FirstOrDefaultAsync();
                }

                return office;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Office. IdOffice: {IdOffice}", idOffice);
                throw;
            }
        }

        public async Task<bool> IsOfficeCodeExists(string officeCode, int idOffice)
        {
            return await _dbContext.Offices
                .AnyAsync(x => x.OfficeCode == officeCode.Trim() && x.IdOffice != idOffice);
        }

        public async Task<int> AddOffice(OfficeDto dto, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await ValidateOfficeReferences(dto);

                var entity = new Offices
                {
                    OfficeCode = dto.OfficeCode.Trim(),
                    OfficeName = dto.OfficeName.Trim(),
                    IdOfficeType = dto.IdOfficeType,
                    IdParentOffice = dto.IdParentOffice,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    District = dto.District,
                    State = dto.State,
                    Country = dto.Country,
                    PinCode = dto.PinCode,
                    PhoneNumber = dto.PhoneNumber,
                    EmailId = dto.EmailId,
                    GSTIN = dto.GSTIN,
                    IdOfficeHead = dto.IdOfficeHead,
                    IdShiftSchedule = dto.IdShiftSchedule,
                    OpenedDate = dto.OpenedDate,
                    ClosedDate = dto.ClosedDate,
                    IsActive = dto.IsActive,
                    IdCreatedBy = idLoggedInEmployee,
                    CreatedDateTime = DateTime.Now
                };

                await _dbContext.Offices.AddAsync(entity);
                await _dbContext.SaveChangesAsync();

                if (dto.ShiftSchedule != null)
                {
                    var shiftSetup = dto.ShiftSchedule;
                    if (string.IsNullOrWhiteSpace(shiftSetup.ShiftName)
                        || shiftSetup.StartTime == shiftSetup.EndTime
                        || string.IsNullOrWhiteSpace(shiftSetup.WorkDays))
                    {
                        throw new InvalidOperationException("Shift name, start/end times, and working days are required.");
                    }

                    var workDays = shiftSetup.WorkDays
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(day => day.ToUpperInvariant())
                        .Distinct()
                        .ToList();
                    var validWorkDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY"
                    };
                    if (workDays.Count == 0 || workDays.Any(day => !validWorkDays.Contains(day)))
                    {
                        throw new InvalidOperationException("Working days contain an invalid day.");
                    }

                    var shiftDefinition = new ShiftDefinitionEntity
                    {
                        ShiftName = shiftSetup.ShiftName.Trim(),
                        IsRegularShiftJustTimeChange = shiftSetup.IsRegularShiftJustTimeChange,
                        IdOffice = entity.IdOffice
                    };
                    await _dbContext.ShiftDefinitions.AddAsync(shiftDefinition);
                    await _dbContext.SaveChangesAsync();

                    var duration = shiftSetup.EndTime - shiftSetup.StartTime;
                    if (duration < TimeSpan.Zero)
                    {
                        duration += TimeSpan.FromDays(1);
                    }

                    var shiftSchedule = new ShiftSchedule
                    {
                        IdShift = shiftDefinition.IdShift,
                        StartTime = shiftSetup.StartTime,
                        EndTime = shiftSetup.EndTime,
                        TotalDurationMinutes = (int)duration.TotalMinutes,
                        TotalDurationHours = Math.Round((decimal)duration.TotalMinutes / 60m, 2),
                        WorkDays = string.Join(",", workDays)
                    };
                    await _dbContext.ShiftSchedules.AddAsync(shiftSchedule);
                    await _dbContext.SaveChangesAsync();

                    entity.IdShiftSchedule = shiftSchedule.IdShiftSchedule;
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Create",
                    entityName: "Office",
                    entityId: entity.IdOffice,
                    actionDetails: new { after = _mapper.Map<OfficeDto>(entity) });

                return entity.IdOffice;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding Office.");
                throw;
            }
        }

        public async Task<bool> UpdateOffice(OfficeDto dto, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var entity = await _dbContext.Offices
                    .FirstOrDefaultAsync(x => x.IdOffice == dto.IdOffice);

                if (entity == null)
                    return false;

                await ValidateOfficeReferences(dto);

                var beforeData = _mapper.Map<OfficeDto>(entity);

                entity.OfficeCode = dto.OfficeCode.Trim();
                entity.OfficeName = dto.OfficeName.Trim();
                entity.IdOfficeType = dto.IdOfficeType;
                entity.IdParentOffice = dto.IdParentOffice;
                entity.AddressLine1 = dto.AddressLine1;
                entity.AddressLine2 = dto.AddressLine2;
                entity.City = dto.City;
                entity.District = dto.District;
                entity.State = dto.State;
                entity.Country = dto.Country;
                entity.PinCode = dto.PinCode;
                entity.PhoneNumber = dto.PhoneNumber;
                entity.EmailId = dto.EmailId;
                entity.GSTIN = dto.GSTIN;
                entity.IdOfficeHead = dto.IdOfficeHead;
                entity.IdShiftSchedule = dto.IdShiftSchedule;
                entity.OpenedDate = dto.OpenedDate;
                entity.ClosedDate = dto.ClosedDate;
                entity.IsActive = dto.IsActive;
                entity.IdModifiedBy = idLoggedInEmployee;
                entity.ModifiedDateTime = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "Office",
                    entityId: entity.IdOffice,
                    actionDetails: new { before = beforeData, after = _mapper.Map<OfficeDto>(entity) });

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating Office. IdOffice: {IdOffice}", dto.IdOffice);
                throw;
            }
        }

        public async Task<bool> UpdateOfficeStatus(int idOffice, bool isActive, int idLoggedInEmployee)
        {
            try
            {
                var entity = await _dbContext.Offices
                    .FirstOrDefaultAsync(x => x.IdOffice == idOffice);

                if (entity == null)
                    return false;

                var beforeStatus = entity.IsActive;

                entity.IsActive = isActive;
                entity.IdModifiedBy = idLoggedInEmployee;
                entity.ModifiedDateTime = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                await _auditService.LogAuditAsync(
                    actionType: "Update",
                    entityName: "Office",
                    entityId: entity.IdOffice,
                    actionDetails: new { before = new { IsActive = beforeStatus }, after = new { IsActive = isActive } });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Office status. IdOffice: {IdOffice}", idOffice);
                throw;
            }
        }

        private async Task ValidateOfficeReferences(OfficeDto dto)
        {
            var isOfficeTypeValid = await _dbContext.OfficeTypes
                .AnyAsync(x => x.IdOfficeType == dto.IdOfficeType);

            if (!isOfficeTypeValid)
                throw new Exception("Invalid office type.");

            if (dto.IdParentOffice.HasValue)
            {
                // Load only Id and parent Id to check existence and avoid circular hierarchy
                var officeParents = await _dbContext.Offices
                    .AsNoTracking()
                    .Select(x => new { x.IdOffice, x.IdParentOffice })
                    .ToDictionaryAsync(x => x.IdOffice, x => x.IdParentOffice);

                if (!officeParents.ContainsKey(dto.IdParentOffice.Value))
                    throw new Exception("Invalid parent office.");

                if (dto.IdOffice > 0)
                {
                    int? currentId = dto.IdParentOffice;
                    while (currentId.HasValue)
                    {
                        if (currentId.Value == dto.IdOffice)
                            throw new Exception("Invalid parent office. An office cannot be placed under itself or one of its sub offices.");

                        currentId = officeParents.TryGetValue(currentId.Value, out var parentId) ? parentId : null;
                    }
                }
            }

            if (dto.IdOfficeHead.HasValue)
            {
                var isEmployeeValid = await _dbContext.Employees
                    .AnyAsync(x => x.IdEmployee == dto.IdOfficeHead.Value);

                if (!isEmployeeValid)
                    throw new Exception("Invalid office head employee.");
            }

            if (dto.IdShiftSchedule.HasValue)
            {
                var isShiftScheduleValid = await _dbContext.ShiftSchedules
                    .AnyAsync(x => x.IdShiftSchedule == dto.IdShiftSchedule.Value);

                if (!isShiftScheduleValid)
                    throw new Exception("Invalid shift schedule.");
            }
        }

        #endregion

        #region Employee Office Postings

        public async Task<IEnumerable<OfficeEmployeeDto>> GetOfficeEmployees(int idOffice, bool isCurrentOnly)
        {
            try
            {
                var query =
                    from p in _dbContext.EmployeeOfficePostings
                    join e in _dbContext.Employees
                        on p.IdEmployee equals e.IdEmployee
                    join d in _dbContext.Departments
                        on e.IdDepartment equals d.IdDepartment into departments
                    from d in departments.DefaultIfEmpty()
                    join g in _dbContext.Designations
                        on e.IdDesignation equals g.IdDesignation into designations
                    from g in designations.DefaultIfEmpty()
                    where p.IdOffice == idOffice
                    select new { p, e, d, g };

                if (isCurrentOnly)
                {
                    query = query.Where(x => x.p.IsCurrentPosting);
                }

                var employees = await query
                    .OrderBy(x => x.e.FirstName)
                    .ThenByDescending(x => x.p.PostingFromDate)
                    .Select(x => new OfficeEmployeeDto
                    {
                        IdEmployeeOfficePosting = x.p.IdEmployeeOfficePosting,
                        IdEmployee = x.p.IdEmployee,
                        EmployeeCode = x.e.EmployeeCode,
                        EmployeeName = x.e.FirstName + (x.e.MiddleName != null && x.e.MiddleName != "" ? " " + x.e.MiddleName : "") + (x.e.LastName != null && x.e.LastName != "" ? " " + x.e.LastName : ""),
                        DepartmentName = x.d != null ? x.d.DepartmentName : null,
                        DesignationName = x.g != null ? x.g.DesignationName : null,
                        EmailID = x.e.EmailID,
                        PhoneNumber1 = x.e.PhoneNumber1,
                        CurrentStatus = x.e.CurrentStatus,
                        PostingFromDate = x.p.PostingFromDate,
                        PostingToDate = x.p.PostingToDate,
                        PostingType = x.p.PostingType,
                        IsCurrentPosting = x.p.IsCurrentPosting
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Office Employees. IdOffice: {IdOffice}", idOffice);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeOfficePostingDto>> GetEmployeeOfficePostings(int idEmployee)
        {
            try
            {
                var postings = await (
                    from p in _dbContext.EmployeeOfficePostings
                    join o in _dbContext.Offices
                        on p.IdOffice equals o.IdOffice
                    join e in _dbContext.Employees
                        on p.IdEmployee equals e.IdEmployee
                    where p.IdEmployee == idEmployee
                    orderby p.PostingFromDate descending
                    select new EmployeeOfficePostingDto
                    {
                        IdEmployeeOfficePosting = p.IdEmployeeOfficePosting,
                        IdEmployee = p.IdEmployee,
                        IdOffice = p.IdOffice,
                        PostingFromDate = p.PostingFromDate,
                        PostingToDate = p.PostingToDate,
                        PostingType = p.PostingType,
                        TransferOrderNumber = p.TransferOrderNumber,
                        IsCurrentPosting = p.IsCurrentPosting,
                        PostingRemarks = p.PostingRemarks,
                        ApprovalStatus = p.ApprovalStatus,
                        IdApprovedBy = p.IdApprovedBy,
                        ApprovedDateTime = p.ApprovedDateTime,
                        EmployeeCode = e.EmployeeCode,
                        EmployeeName = e.FirstName + (e.MiddleName != null && e.MiddleName != "" ? " " + e.MiddleName : "") + (e.LastName != null && e.LastName != "" ? " " + e.LastName : ""),
                        OfficeCode = o.OfficeCode,
                        OfficeName = o.OfficeName
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return postings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Employee Office Postings. IdEmployee: {IdEmployee}", idEmployee);
                throw;
            }
        }

        public async Task<bool> IsEmployeeOfficePostingExists(EmployeeOfficePostingDto dto)
        {
            return await _dbContext.EmployeeOfficePostings
                .AnyAsync(x => x.IdEmployee == dto.IdEmployee &&
                               x.IdOffice == dto.IdOffice &&
                               x.PostingFromDate.Date == dto.PostingFromDate.Date &&
                               x.IdEmployeeOfficePosting != dto.IdEmployeeOfficePosting);
        }

        public async Task<int> AddOrUpdateEmployeeOfficePosting(EmployeeOfficePostingDto dto, int idLoggedInEmployee)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var isEmployeeValid = await _dbContext.Employees
                    .AnyAsync(x => x.IdEmployee == dto.IdEmployee);

                if (!isEmployeeValid)
                    throw new Exception("Invalid employee.");

                var office = await _dbContext.Offices
                    .AsNoTracking()
                    .Where(x => x.IdOffice == dto.IdOffice)
                    .Select(x => new { x.IdOffice, x.IsActive })
                    .FirstOrDefaultAsync();

                if (office == null)
                    throw new Exception("Invalid office.");

                // "PostingToDate = NULL" is treated as the current posting
                var isCurrentPosting = !dto.PostingToDate.HasValue || dto.PostingToDate.Value.Date >= DateTime.Now.Date;

                var entity = await _dbContext.EmployeeOfficePostings
                    .FirstOrDefaultAsync(x => x.IdEmployeeOfficePosting == dto.IdEmployeeOfficePosting);

                EmployeeOfficePostingDto? beforeData = null;
                var isUpdate = entity != null;

                if (entity != null)
                {
                    beforeData = _mapper.Map<EmployeeOfficePostingDto>(entity);

                    // Allow closing an existing posting even if the office is inactive, but not moving it to one
                    if (!office.IsActive && entity.IdOffice != dto.IdOffice)
                        throw new Exception("Selected office is inactive.");

                    entity.IdEmployee = dto.IdEmployee;
                    entity.IdOffice = dto.IdOffice;
                    entity.PostingFromDate = dto.PostingFromDate.Date;
                    entity.PostingToDate = dto.PostingToDate?.Date;
                    entity.PostingType = dto.PostingType;
                    entity.TransferOrderNumber = dto.TransferOrderNumber;
                    entity.IsCurrentPosting = isCurrentPosting;
                    entity.PostingRemarks = dto.PostingRemarks;
                    entity.IdModifiedBy = idLoggedInEmployee;
                    entity.ModifiedDateTime = DateTime.Now;
                }
                else
                {
                    if (!office.IsActive)
                        throw new Exception("Selected office is inactive.");

                    entity = new EmployeeOfficePostings
                    {
                        IdEmployee = dto.IdEmployee,
                        IdOffice = dto.IdOffice,
                        PostingFromDate = dto.PostingFromDate.Date,
                        PostingToDate = dto.PostingToDate?.Date,
                        PostingType = dto.PostingType,
                        TransferOrderNumber = dto.TransferOrderNumber,
                        IsCurrentPosting = isCurrentPosting,
                        PostingRemarks = dto.PostingRemarks,
                        IdCreatedBy = idLoggedInEmployee,
                        CreatedDateTime = DateTime.Now
                    };
                    await _dbContext.EmployeeOfficePostings.AddAsync(entity);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isUpdate)
                {
                    await _auditService.LogAuditAsync(
                        actionType: "Update",
                        entityName: "EmployeeOfficePosting",
                        entityId: entity.IdEmployeeOfficePosting,
                        actionDetails: new { before = beforeData, after = _mapper.Map<EmployeeOfficePostingDto>(entity) });
                }
                else
                {
                    await _auditService.LogAuditAsync(
                        actionType: "Create",
                        entityName: "EmployeeOfficePosting",
                        entityId: entity.IdEmployeeOfficePosting,
                        actionDetails: new { after = _mapper.Map<EmployeeOfficePostingDto>(entity) });
                }

                return entity.IdEmployeeOfficePosting;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding or updating Employee Office Posting.");
                throw;
            }
        }

        #endregion
    }
}
