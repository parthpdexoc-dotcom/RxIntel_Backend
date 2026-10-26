using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IAdminService : IGenericService<AdminDto, Admin>
    {
        Task<SuccessResult<string>> UploadProfileImageAsync(IFormFile? file, Guid? userId = null);

        string? GetProfileImagePhysicalPath(string fileName);
    }
}
