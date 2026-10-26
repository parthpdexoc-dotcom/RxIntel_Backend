using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IPharmacyUserService : IGenericService<PharmacyUserDto, PharmacyUser>
    {
        Task<PagedList<PharmacyUserDto>> GetAll(PageParam pageParam, Guid? pharmacyId = null);

        Task<SuccessResult<PharmacyUserDto>> RegisterPharmacyUser(PharmacyUserDto pharmacyUserDto);

        Task<SuccessResult<string>> UploadProfileImageAsync(IFormFile? file, Guid? userId = null);

        string? GetProfileImagePhysicalPath(string fileName);
    }
}
