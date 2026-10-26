using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
namespace Rxintel.Generic.Services.Services
{
    public class WholesalerFieldMappingService : GenericService<WholesalerFieldMappingDto, WholesalerFieldMapping>, IWholesalerFieldMappingService
    {
        private readonly IWholesalerFieldMappingRepository _wholesalerFieldMappingRepository;
        private readonly IPurchaseService _purchaseService;
        private readonly IWholesalerFieldMappingNotificationService _wholesalerFieldMappingNotificationService;

        public WholesalerFieldMappingService(
            IWholesalerFieldMappingRepository wholesalerFieldMappingRepository,
            IPurchaseService purchaseService,
            IWholesalerFieldMappingNotificationService wholesalerFieldMappingNotificationService,
            IMapper mapper,
            ILogger<WholesalerFieldMappingService> logger) : base(wholesalerFieldMappingRepository, mapper, "Id")
        {
            _wholesalerFieldMappingRepository = wholesalerFieldMappingRepository;
            _purchaseService = purchaseService;
            _wholesalerFieldMappingNotificationService = wholesalerFieldMappingNotificationService;
        }
        public async Task<PagedList<WholesalerFieldMappingDto>> GetAll(PageParam pageParam, Guid? wholesalerId = null)
        {
            var data = (await GetAllAsync(x => x.WholesalerId == wholesalerId)).ToList();
            data = data.FilterBySearch(
                pageParam?.SearchText,
                w => w.SourceColumnName,
                w => w.DestinationColumnName);
            return ToPagedList(data, pageParam);
        }
        public async Task<IEnumerable<WholesalerFieldMappingDto>> GetByWholesalerId(Guid? wholesalerId) =>
            await GetAllAsync(x => x.WholesalerId == wholesalerId);

        public Task<(WholesalerFieldMappingCsvPreviewDto? Preview, string? Error)> GetCsvPreviewFromUpload(
            IFormFile? file,
            Guid wholesalerId) =>
            FieldMappingHelper.GetPreviewFromUploadAsync(file,
                (stream, fileName) => GetCsvPreviewAsync(stream, wholesalerId, fileName),
                preview => preview.Message = CustomMessage.GetCsvUploadSuccessMessage(typeof(WholesalerFieldMapping).Name));
        public async Task<WholesalerFieldMappingCsvPreviewDto> GetCsvPreviewAsync(Stream csvStream, Guid wholesalerId, string? fileName = null)
        {
            var csvColumns = await FieldMappingHelper.ReadColumnsAsync(csvStream, fileName);
            if (csvColumns.Count == 0)
                return new WholesalerFieldMappingCsvPreviewDto();

            var purchaseColumns = (await _purchaseService.GetPurchaseColumns()).ToList();
            var rows = FieldMappingHelper.BuildRows(purchaseColumns,csvColumns,(csv, purchase) => new WholesalerFieldMappingDto
                {
                    Id = Guid.Empty,
                    WholesalerId = wholesalerId,
                    SourceColumnName = csv ?? string.Empty,
                    DestinationColumnName = purchase,
                });
            return new WholesalerFieldMappingCsvPreviewDto
            {
                DestinationColumns = csvColumns,
                Rows = rows,
            };
        }
        public Task DeleteByWholesalerIdAsync(Guid wholesalerId) =>
            _wholesalerFieldMappingRepository.DeleteByPropertyAsync(nameof(WholesalerFieldMapping.WholesalerId),wholesalerId);
        
        public async Task<SuccessResult<IEnumerable<WholesalerFieldMappingDto>>> BulkUpsertByWholesaler(
            IEnumerable<WholesalerFieldMappingDto> wholesalerFieldMappingDtos)
        {
            var list = wholesalerFieldMappingDtos?.ToList() ?? [];
            await FieldMappingHelper.DeleteExistingBeforeBulkUpsertAsync(
                list,
                d => d.WholesalerId,
                DeleteByWholesalerIdAsync);
            var result = await BulkUpsert(list);

            if (result.Data != null
                && result.Data.Any()
                && list.Count > 0
                && list[0].WholesalerId != Guid.Empty)
            {
                await _wholesalerFieldMappingNotificationService.NotifyPharmacyUsersMappingConfiguredAsync(
                    list[0].WholesalerId);
            }

            return result;
        }
    }
}
