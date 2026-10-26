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
    public class BillingFieldMappingService : GenericService<BillingFieldMappingDto, BillingFieldMapping>, IBillingFieldMappingService
    {
        private readonly IBillingFieldMappingRepository _billingFieldMappingRepository;
        private readonly IBillingService _billingService;
        private readonly IBillingFieldMappingNotificationService _billingFieldMappingNotificationService;

        public BillingFieldMappingService(
            IBillingFieldMappingRepository billingFieldMappingRepository,
            IBillingService billingService,
            IBillingFieldMappingNotificationService billingFieldMappingNotificationService,
            IMapper mapper,
            ILogger<BillingFieldMappingService> logger)
            : base(billingFieldMappingRepository, mapper, "Id")
        {
            _billingFieldMappingRepository = billingFieldMappingRepository;
            _billingService = billingService;
            _billingFieldMappingNotificationService = billingFieldMappingNotificationService;
        }

        public async Task<PagedList<BillingFieldMappingDto>> GetAll(PageParam pageParam, Guid? pharmacySoftwareId = null)
        {
            var data = (await GetAllAsync(x => x.PharmacySoftwareId == pharmacySoftwareId)).ToList();
            data = data.FilterBySearch(
                pageParam?.SearchText,
                p => p.SourceFieldName,
                p => p.DestinationFieldName);
            return ToPagedList(data, pageParam);
        }

        public async Task<IEnumerable<BillingFieldMappingDto>> GetById(Guid? pharmacySoftwareId) =>
            await GetAllAsync(x => x.PharmacySoftwareId == pharmacySoftwareId);

        public Task<(BillingFieldMappingCsvPreviewDto? Preview, string? Error)> GetCsvPreviewFromUpload(
            IFormFile? file,
            Guid pharmacySoftwareId) =>
            FieldMappingHelper.GetPreviewFromUploadAsync(
                file,
                (stream, fileName) => GetCsvPreviewAsync(stream, pharmacySoftwareId, fileName),
                preview => preview.Message = CustomMessage.GetCsvUploadSuccessMessage(typeof(BillingFieldMapping).Name));

        public async Task<BillingFieldMappingCsvPreviewDto> GetCsvPreviewAsync(Stream csvStream, Guid pharmacySoftwareId, string? fileName = null)
        {
            var csvColumns = await FieldMappingHelper.ReadColumnsAsync(csvStream, fileName);
            if (csvColumns.Count == 0)
                return new BillingFieldMappingCsvPreviewDto();

            var billingColumns = (await _billingService.GetBillingColumns()).ToList();
            var rows = FieldMappingHelper.BuildRows(
                billingColumns,
                csvColumns,
                (csv, billing) => new BillingFieldMappingDto
                {
                    Id = Guid.Empty,
                    PharmacySoftwareId = pharmacySoftwareId,
                    SourceFieldName = csv ?? string.Empty,
                    DestinationFieldName = billing,
                });

            return new BillingFieldMappingCsvPreviewDto
            {
                DestinationColumns = csvColumns,
                Rows = rows,
            };
        }

        public Task DeleteByPharmacySoftwareId(Guid pharmacySoftwareId) =>
            _billingFieldMappingRepository.DeleteByPropertyAsync(
                nameof(BillingFieldMapping.PharmacySoftwareId),
                pharmacySoftwareId);

        public async Task<SuccessResult<IEnumerable<BillingFieldMappingDto>>> SaveMappingsForPharmacySoftware(
            IEnumerable<BillingFieldMappingDto> billingFieldMappingDtos)
        {
            var list = billingFieldMappingDtos?.ToList() ?? [];
            await FieldMappingHelper.DeleteExistingBeforeBulkUpsertAsync(
                list,
                d => d.PharmacySoftwareId,
                DeleteByPharmacySoftwareId);
            var result = await BulkUpsert(list);

            if (result.Data != null
                && result.Data.Any()
                && list.Count > 0
                && list[0].PharmacySoftwareId != Guid.Empty)
            {
                await _billingFieldMappingNotificationService.NotifyPharmacyUsersMappingConfiguredAsync(
                    list[0].PharmacySoftwareId);
            }

            return result;
        }
    }
}
