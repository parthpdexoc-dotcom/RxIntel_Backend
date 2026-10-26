using System.Threading.Tasks;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IErxSigMappingService
    {
        Task<PagedList<ErxSigMappingDto>> GetAllMappingsAsync(PageParam pageParam, string? searchTerm = null, string? category = null);
        Task<ErxSigMappingDto?> GetByIdAsync(int id);
        Task<SuccessResult<ErxSigMappingDto>> CreateMappingAsync(CreateErxSigMappingDto dto);
        Task<SuccessResult<ErxSigMappingDto>> UpdateMappingAsync(int id, UpdateErxSigMappingDto dto);
        Task<SuccessResult<string>> DeleteMappingAsync(int id);
        Task<SuccessResult<string>> ToggleActiveAsync(int id);
        Task<TestSigComparisonResponseDto> TestComparisonAsync(TestSigComparisonRequestDto dto);
        Task EnsureCacheLoadedAsync();
        Task<bool> AreSigsEquivalentAsync(string? sig1, string? sig2);
        Task InvalidateCacheAsync();
    }
}
