using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface ITermsService
    {
        Task<TermsAndConditionsDto?> GetCurrentAsync();

        Task<SuccessResult<TermsAndConditionsDto>> UpsertCurrentAsync(TermsAndConditionsUpdateDto dto);
    }
}
