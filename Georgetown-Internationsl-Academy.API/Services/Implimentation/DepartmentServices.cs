using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class DepartmentServices : IDepartmentServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentServices> _logger;

        public DepartmentServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<DepartmentServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<DepartmentDto>> GetDepartmentList()
        {
            try
            {
                var departments = await _dbContext.Departments.ToListAsync();
                return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching the list of departments.");
                throw;
            }
        }

        public async Task<DepartmentDto?> GetDepartmentByID(int id)
        {
            try
            {
                var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.IdDepartment == id);
                return department == null ? null : _mapper.Map<DepartmentDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching department with ID: {Id}", id);
                throw;
            }
        }

        public async Task<DepartmentDto?> AddDepartment(DepartmentDto dto)
        {
            try
            {
                var departmentEntity = _mapper.Map<DepartmentEntity>(dto);
                var addedEntity = await _dbContext.Departments.AddAsync(departmentEntity);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<DepartmentDto>(addedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding department: {@DepartmentDto}", dto);
                return null;
            }
        }

        public async Task<DepartmentDto?> UpdateDepartment(DepartmentDto dto)
        {
            try
            {
                var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.IdDepartment == dto.IdDepartment);
                if (department == null) return null;

                department.DepartmentCode = dto.DepartmentCode;
                department.DepartmentName = dto.DepartmentName;

                var updatedEntity = _dbContext.Departments.Update(department);
                await _dbContext.SaveChangesAsync();

                return _mapper.Map<DepartmentDto>(updatedEntity.Entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department with ID: {Id}", dto.IdDepartment);
                return null;
            }
        }
    }

}
