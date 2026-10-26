using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IDataAnalyticsService
    {
        Task<DataAnalyticsResponseDto> GetAnalyticsAsync(DataAnalyticsQueryParam param);
        Task<byte[]> ExportAnalyticsAsync(DataAnalyticsQueryParam param);
    }
}