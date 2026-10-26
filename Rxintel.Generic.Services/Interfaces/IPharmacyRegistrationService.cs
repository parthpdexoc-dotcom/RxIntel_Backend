using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IPharmacyRegistrationService
    {
        Task<SuccessResult<PharmacyDto>> RegisterPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null);
    }
}
