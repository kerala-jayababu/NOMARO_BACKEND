using Georgetown_Internationsl_Academy.API.DTO.Shift;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Shift
{
    public interface IShiftService
    {
        Task<IEnumerable<ShiftDto>> GetShiftList();
        Task<ShiftDto?> GetShiftById(int id);
        Task<ShiftDto?> AddShift(ShiftDto shift);
        Task<ShiftDto?> UpdateShift(ShiftDto shift);
    }
}
