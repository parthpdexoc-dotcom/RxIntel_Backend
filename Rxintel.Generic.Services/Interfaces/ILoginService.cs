using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface ILoginService : IGenericService<AdminDto, Admin>
    {
        Task<SuccessResult<AdminDto>> RegisterWithMessage(AdminDto adminDto);

        Task<TokenDto> LoginAsync(string email, string password);

        Task<TokenDto> RefreshAsync();
    }
}


