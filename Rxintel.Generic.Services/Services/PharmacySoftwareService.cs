using AutoMapper;
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
    public class PharmacySoftwareService : GenericService<PharmacySoftwareDto, PharmacyBillingSoftware>, IPharmacySoftwareService
    {
        private readonly IPharmacySoftwareRepository _pharmacySoftwareRepository;
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IMapper _mapper;

        public PharmacySoftwareService(
            IPharmacySoftwareRepository pharmacySoftwareRepository,
            IPharmacyRepository pharmacyRepository,
            IMapper mapper,
            ILogger<PharmacySoftwareService> logger)
            : base(pharmacySoftwareRepository, mapper, "Id")
        {
            _pharmacySoftwareRepository = pharmacySoftwareRepository;
            _pharmacyRepository = pharmacyRepository;
            _mapper = mapper;
        }

        public async Task<PagedList<PharmacySoftwareDto>> GetAll(PageParam pageParam)
        {
            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _pharmacySoftwareRepository.GetFilteredCountAsync();
                var pageEntities = (await _pharmacySoftwareRepository.GetAllAsync(null, null, pageParam)).ToList();
                var pageData = _mapper.Map<List<PharmacySoftwareDto>>(pageEntities);
                return new PagedList<PharmacySoftwareDto>(pageData, pageParam, totalRecords);
            }

            var data = (await GetAllAsync()).ToList();

            data = data.FilterBySearch(
                pageParam?.SearchText,
                x => x.SoftwareName);

            return ToPagedList(data, pageParam);
        }

        public override async Task<SuccessResult<PharmacySoftwareDto>> DeleteAsync(Guid id)
        {
            var inUse = await _pharmacyRepository.GetAllAsync(p => p.PharmacySoftwareId == id);
            if (inUse.Any())
            {
                throw new WebApiException(HttpStatusCode.Conflict,
                    "Cannot delete. This software is in use by one or more pharmacies.");
            }

            return await base.DeleteAsync(id);
        }
    }
}
