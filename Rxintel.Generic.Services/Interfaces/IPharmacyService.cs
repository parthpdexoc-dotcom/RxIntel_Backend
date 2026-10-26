using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{   
    public interface IPharmacyService : IGenericService<PharmacyDto, Pharmacy>
    {
        Task<PagedList<PharmacyDto>> GetAll(PageParam pageParam);
        Task<TokenDto> Login(string email, string password);
        Task<SuccessResult<PharmacyDto>> RegisterPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null);
        Task<SuccessResult<PharmacyDto>> UpsertPharmacy(
            PharmacyDto pharmacyDto,
            IFormFile? deaDocument = null,
            IFormFile? stateBoardLicenseDocument = null);
        Task<string?> GetDeaDocumentPhysicalPathAsync(Guid pharmacyId);
        Task<string?> GetStateBoardLicenseDocumentPhysicalPathAsync(Guid pharmacyId);
    }
}
