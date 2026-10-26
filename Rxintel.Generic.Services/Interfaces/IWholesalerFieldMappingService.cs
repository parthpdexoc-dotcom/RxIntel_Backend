using Microsoft.AspNetCore.Http;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface IWholesalerFieldMappingService : IGenericService<WholesalerFieldMappingDto, WholesalerFieldMapping>
    {
        Task<PagedList<WholesalerFieldMappingDto>> GetAll(PageParam pageParam, Guid? wholesalerId = null);

        Task<IEnumerable<WholesalerFieldMappingDto>> GetByWholesalerId(Guid? wholesalerId);

        Task<(WholesalerFieldMappingCsvPreviewDto? Preview, string? Error)> GetCsvPreviewFromUpload(
            IFormFile? file,
            Guid wholesalerId);

        Task<SuccessResult<IEnumerable<WholesalerFieldMappingDto>>> BulkUpsertByWholesaler(
            IEnumerable<WholesalerFieldMappingDto> wholesalerFieldMappingDtos);
    }
}
