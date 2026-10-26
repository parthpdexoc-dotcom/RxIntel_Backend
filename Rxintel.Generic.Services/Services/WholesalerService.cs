using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;

namespace Rxintel.Generic.Services.Services
{
    public class WholesalerService : GenericService<WholesalerDto, Wholesaler>, IWholesalerService
    {
        private readonly IWholesalerRepository _wholesalerRepository;
        private readonly IPharmacyWholesalerRepository _pharmacyWholesalerRepository;
        private readonly ITokenParser _tokenParser;
        private readonly IBackgroundTaskRunner _backgroundTaskRunner;

        public WholesalerService(
            IWholesalerRepository wholesalerRepository,
            IPharmacyWholesalerRepository pharmacyWholesalerRepository,
            ITokenParser tokenParser,
            IBackgroundTaskRunner backgroundTaskRunner,
            IMapper mapper,
            ILogger<WholesalerService> logger)
            : base(wholesalerRepository, mapper, "Id")
        {
            _wholesalerRepository = wholesalerRepository;
            _pharmacyWholesalerRepository = pharmacyWholesalerRepository;
            _tokenParser = tokenParser;
            _backgroundTaskRunner = backgroundTaskRunner;
        }

        public async Task<PagedList<WholesalerDto>> GetAll(PageParam pageParam)
        {
            var data = (await GetAllAsync()).ToList();

            data = data.FilterBySearch(
                pageParam?.SearchText,
                w => w.WholesalerName,
                w => w.Address,
                w => w.Phone,
                w => w.Email);

            return ToPagedList(data, pageParam);
        }

        public override async Task<SuccessResult<WholesalerDto>> UpsertAsync(WholesalerDto dto)
        {
            var isNew = dto != null && dto.Id == Guid.Empty;
            var notifyPharmacyAdd = isNew && _tokenParser.IsEmployee();
            var pharmacyId = notifyPharmacyAdd ? _tokenParser.GetPharmacyId() : null;
            var addedByName = notifyPharmacyAdd ? _tokenParser.GetName() : null;
            var addedByEmail = notifyPharmacyAdd ? _tokenParser.GetEmail() : null;

            var result = await base.UpsertAsync(dto);

            if (notifyPharmacyAdd
                && pharmacyId.HasValue
                && pharmacyId.Value != Guid.Empty
                && result.IsSuccessStatusCode
                && result.Data != null)
            {
                var wholesaler = result.Data;
                var pharmacyIdValue = pharmacyId.Value;
                _backgroundTaskRunner.Enqueue(async sp =>
                {
                    var notificationService = sp.GetRequiredService<IWholesalerNotificationService>();
                    await notificationService.NotifyAdminsWholesalerAddedByPharmacyAsync(
                        pharmacyIdValue,
                        wholesaler,
                        addedByName,
                        addedByEmail);
                });
            }

            return result;
        }

        public override async Task<SuccessResult<WholesalerDto>> DeleteAsync(Guid id)
        {
            var linked = await _pharmacyWholesalerRepository.GetAllAsync(pw => pw.WholesalerId == id);
            if (linked.Any())
            {
                throw new WebApiException(HttpStatusCode.Conflict,
                    "Cannot delete. This wholesaler is linked to one or more pharmacies.");
            }

            return await base.DeleteAsync(id);
        }
    }
}
