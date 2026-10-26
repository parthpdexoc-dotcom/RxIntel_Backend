using AutoMapper;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Implementations.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;
namespace Rxintel.Generic.Services.Services
{
    public class LoginService : GenericService<AdminDto, Admin>, ILoginService
    {
        private readonly ILoginRepository _repository;
        private readonly IPharmacyUserRepository _pharmacyUserRepository;
        private readonly ITokenParser _tokenParser;
        private readonly IMapper _mapper;
        private readonly Configurations _configurations;

        public LoginService(
            ILoginRepository repository,
            IPharmacyUserRepository pharmacyUserRepository,
            ITokenParser tokenParser,
            IMapper mapper,
            Configurations configurations)
            : base(repository, mapper, "Id")
        {
            _repository = repository;
            _pharmacyUserRepository = pharmacyUserRepository ?? throw new ArgumentNullException(nameof(pharmacyUserRepository));
            _tokenParser = tokenParser ?? throw new ArgumentNullException(nameof(tokenParser));
            _mapper = mapper;
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        }
        public async Task<SuccessResult<AdminDto>> RegisterWithMessage(AdminDto adminDto)
        {           
            if (adminDto.Password != adminDto.ConfirmPassword)
            {
                return new SuccessResult<AdminDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.BadRequest,
                    Message = CustomMessage.PasswordMismatch
                };
            }
            var users = await _repository.GetAllAsync();
            var existingUser = users.FirstOrDefault(x => x.Email.ToLower() == adminDto.Email.ToLower());

            if (existingUser != null)
            {
                return new SuccessResult<AdminDto>
                {
                    Data = null,
                    Status = (int)HttpStatusCode.Conflict,
                    Message = CustomMessage.UserAlreadyExists
                };
            }
            var result = await UpsertAsync(adminDto);
            

            return new SuccessResult<AdminDto>
            {
                Data = result.Data,
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.Registersuccess
            };
        }

        public async Task<TokenDto> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new WebApiException(HttpStatusCode.BadRequest, CustomMessage.GetInvalidMessage("User"));

            var users = await _repository.GetAllAsync();
            var login = users.FirstOrDefault(x =>
                x.Email != null &&
                x.Email.ToLower() == email.ToLower() &&
                x.Password == password);

            if (login == null)
                throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.InvalidUser);

            var token = TokenHelper.GenerateAdminToken(
                _configurations,
                login.Id,
                login.Email ?? string.Empty,
                login.Name,
                login.Email,
                login.Role);

            return ToTokenDto(token);
        }

        public async Task<TokenDto> RefreshAsync()
        {
            var userId = _tokenParser.GetUserId();
            if (userId == null || userId == Guid.Empty)
                throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.SessionRefreshFailed);

            if (_tokenParser.IsAdmin())
            {
                var admin = await _repository.GetByIdAsync(userId.Value, "Id");
                if (admin == null)
                    throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.SessionRefreshFailed);

                var token = TokenHelper.GenerateAdminToken(
                    _configurations,
                    admin.Id,
                    admin.Email ?? string.Empty,
                    admin.Name,
                    admin.Email,
                    admin.Role);

                return ToTokenDto(token);
            }

            if (_tokenParser.IsEmployee())
            {
                var user = await _pharmacyUserRepository.GetByIdAsync(userId.Value, "Id");
                if (user == null)
                    throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.SessionRefreshFailed);

                if (!user.IsActive)
                    throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.EmployeeAccountInactive);

                var token = TokenHelper.GenerateEmployeeToken(_configurations, user);
                return ToTokenDto(token);
            }

            throw new WebApiException(HttpStatusCode.Unauthorized, CustomMessage.SessionRefreshFailed);
        }

        private TokenDto ToTokenDto(string token)
        {
            return new TokenDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenHelper.GetTokenExpirationInMinutes(_configurations)),
                TokenType = "Bearer"
            };
        }
    }
}