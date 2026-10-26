using AutoMapper;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Linq;

namespace Rxintel.Generic.Services.Services
{
    public class PharmacyWholesalerService : GenericService<PharmacyWholesalerDto, PharmacyWholesaler>, IPharmacyWholesalerService
    {
        private readonly IPharmacyWholesalerRepository _pharmacyWholesalerRepository;

        public PharmacyWholesalerService(IPharmacyWholesalerRepository repository, IMapper mapper, ILogger<PharmacyWholesalerService> logger): base(repository, mapper, "Id")
        {
            _pharmacyWholesalerRepository = repository;
        }

        public async Task<PagedList<PharmacyWholesalerDto>> GetAll(PageParam pageParam, Guid? pharmacyId = null)
        {
            var filter = pharmacyId.HasValue
                ? (System.Linq.Expressions.Expression<Func<PharmacyWholesaler, bool>>)(x => x.PharmacyId == pharmacyId.Value)
                : null;

            var hasSearch = !string.IsNullOrWhiteSpace(pageParam?.SearchText);

            if (!hasSearch && pageParam != null && pageParam.IsPaginationRequested)
            {
                var totalRecords = await _pharmacyWholesalerRepository.GetFilteredCountAsync(filter);
                var pageData = (await base.GetAllAsync(filter, null, pageParam)).ToList();
                return new PagedList<PharmacyWholesalerDto>(pageData, pageParam, totalRecords);
            }

            var data = await base.GetAllAsync(filter, null, null);
            var total = _pharmacyWholesalerRepository.GetAllQueryable(filter, null).Count();
            return new PagedList<PharmacyWholesalerDto>(data.ToList(), pageParam ?? new PageParam(), total);
        }
    }
}
