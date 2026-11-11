using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IScreenAttributeConfigService
    {
        Task<IReadOnlyList<ScreenAttributeConfigDto>> GetByScreenCodeAsync(
            string screenCode,
            CancellationToken cancellationToken);
    }
}
