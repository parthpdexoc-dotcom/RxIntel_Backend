using AutoMapper;
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
    public class InsuranceService : GenericService<InsuranceDto, Insurance>, IInsuranceService
    {
        private readonly IInsuranceRepository _insuranceRepository;
        private readonly IInsuranceGroupRepository _insuranceGroupRepository;
        private readonly IMapper _mapper;

        public InsuranceService(IInsuranceRepository insuranceRepository, IInsuranceGroupRepository insuranceGroupRepository, IMapper mapper, ILogger<InsuranceService> logger) : base(insuranceRepository, mapper, "Id")
        {
            _insuranceRepository = insuranceRepository;
            _insuranceGroupRepository = insuranceGroupRepository;
            _mapper = mapper;
        }
        public async Task<PagedList<InsuranceDto>> GetAll(PageParam pageParam, bool uniqueInsuranceNames = false)
        {
            var data = (await GetAllAsync()).ToList();

            data = data.FilterBySearch(
                pageParam?.SearchText,
                i => i.InsuranceName,
                i => i.BINNumber,
                i => i.Phone);

            data = data
                .OrderBy(i => i.InsuranceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.BINNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueInsuranceNames)
            {
                data = DistinctByInsuranceName(data, mergeInsuranceGroups: false);
            }

            return ToPagedList(data, pageParam);
        }

        public override async Task<SuccessResult<InsuranceDto>> DeleteAsync(Guid id)
        {
            var insurance = await _insuranceRepository.GetByIdAsync(id, "Id");
            if (insurance != null && !string.IsNullOrWhiteSpace(insurance.BINNumber))
            {
                var binNumber = insurance.BINNumber.Trim();
                if ((await _insuranceGroupRepository.GetAllAsync(x => x.BINNumber == binNumber)).Any())
                    return SuccessResult<InsuranceDto>.BadRequest(CustomMessage.InsuranceDeleteBlockedByInsuranceGroupLink);
            }

            if ((await _insuranceGroupRepository.GetAllAsync(x => x.ChildInsuranceId == id)).Any())
                return SuccessResult<InsuranceDto>.BadRequest(CustomMessage.InsuranceDeleteBlockedByInsuranceGroupChild);

            return await base.DeleteAsync(id);
        }

        public async Task<List<InsuranceDto>> InsuranceDropdownById(Guid? Id = null)
        {
            var data = await GetAllAsync();
            var allMappings = (await _insuranceGroupRepository.GetAllAsync()).ToList();

            var excludedIds = Id.HasValue && Id.Value != Guid.Empty
                ? GetExcludedInsuranceIdsForLinkDropdown(Id.Value, allMappings, data)
                : null;

            var filteredData = data
                .Where(x => excludedIds == null || !excludedIds.Contains(x.Id))
                .ToList();

            foreach (var item in filteredData)
            {
                var occupiedRanges = allMappings
                    .Where(m => m.ChildInsuranceId == item.Id)
                    .ToList();

                item.InsuranceGroups = _mapper.Map<List<InsuranceGroupDto>>(occupiedRanges);
            }

            filteredData = filteredData
                .OrderBy(i => i.InsuranceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.BINNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return DistinctByInsuranceName(filteredData, mergeInsuranceGroups: true);
        }

        private static List<InsuranceDto> DistinctByInsuranceName(
            IEnumerable<InsuranceDto> items,
            bool mergeInsuranceGroups)
        {
            return items
                .GroupBy(i => i.InsuranceName?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var ordered = group
                        .OrderBy(i => i.InsuranceName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(i => i.BINNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var primary = ordered[0];
                    if (mergeInsuranceGroups && ordered.Count > 1)
                    {
                        primary.InsuranceGroups = ordered
                            .SelectMany(i => i.InsuranceGroups ?? [])
                            .ToList();
                    }

                    return primary;
                })
                .OrderBy(i => i.InsuranceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.BINNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static HashSet<Guid> GetExcludedInsuranceIdsForLinkDropdown(
            Guid insuranceId,
            List<InsuranceGroup> mappings,
            IEnumerable<InsuranceDto> insurances)
        {
            var excluded = new HashSet<Guid> { insuranceId };
            var insuranceList = insurances.ToList();

            foreach (var binNumber in mappings
                         .Where(m => m.ChildInsuranceId == insuranceId)
                         .Select(m => m.BINNumber?.Trim())
                         .Where(bin => !string.IsNullOrWhiteSpace(bin)))
            {
                foreach (var insurance in insuranceList.Where(i =>
                             !string.IsNullOrWhiteSpace(i.BINNumber) &&
                             string.Equals(i.BINNumber.Trim(), binNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    excluded.Add(insurance.Id);
                }
            }

            return excluded;
        }
    }
}
