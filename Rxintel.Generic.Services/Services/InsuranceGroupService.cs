using AutoMapper;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Net;
namespace Rxintel.Generic.Services.Services
{
    public class InsuranceGroupService : GenericService<InsuranceGroupDto, InsuranceGroup>, IInsuranceGroupService
    {
        private readonly IInsuranceGroupRepository _insuranceGroupRepository;
        private readonly IMapper _mapper;

        public InsuranceGroupService(IInsuranceGroupRepository repository, IMapper mapper, ILogger<InsuranceGroupService> logger) : base(repository, mapper, "Id")
        {
            _insuranceGroupRepository = repository;
            _mapper = mapper;
            _ = logger;
        }
        public async Task<SuccessResult<List<InsuranceGroupDto>>> SaveParentChildMappings(List<InsuranceGroupDto> insuranceGroupDtos)
        {
            var binNumber = insuranceGroupDtos?
                .Select(x => NormalizeBin(x.BINNumber))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (insuranceGroupDtos == null || insuranceGroupDtos.Count == 0 || string.IsNullOrWhiteSpace(binNumber))
            {
                return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupBinNumberRequiredMessage);
            }

            if (!PharmacyValidationHelper.IsValidBinNumber(binNumber))
            {
                return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupBinNumberInvalidMessage);
            }

            var parentItems = insuranceGroupDtos
                .Where(x => string.Equals(NormalizeBin(x.BINNumber), binNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var childMappings = parentItems
                .Where(x => x.ChildInsuranceId != Guid.Empty)
                .ToList();

            var validationError = await ValidateSaveMappings(binNumber, parentItems, childMappings);
            if (validationError != null)
            {
                return validationError;
            }

            await _insuranceGroupRepository.DeleteByPropertyAsync(nameof(InsuranceGroup.BINNumber), binNumber);

            if (childMappings.Count == 0)
            {
                return BuildMappingSavedSuccess(new List<InsuranceGroup>());
            }

            var mappings = childMappings.Select(dto => new InsuranceGroup
            {
                Id = Guid.NewGuid(),
                BINNumber = binNumber,
                ChildInsuranceId = dto.ChildInsuranceId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            }).ToList();

            await _insuranceGroupRepository.AddRangeAsync(mappings);

            var savedMappings = (await _insuranceGroupRepository.GetAllAsync(x => x.BINNumber == binNumber)).ToList();

            return BuildMappingSavedSuccess(savedMappings);
        }
        public async Task<List<InsuranceGroupDto>> GetMappingsByBinNumber(string binNumber)
        {
            var normalizedBin = NormalizeBin(binNumber);
            if (string.IsNullOrWhiteSpace(normalizedBin))
            {
                throw new ArgumentException("BIN Number is required");
            }
            var mappings = await _insuranceGroupRepository.GetAllAsync(x => x.BINNumber == normalizedBin);
            return _mapper.Map<List<InsuranceGroupDto>>(mappings);
        }

        private async Task<SuccessResult<List<InsuranceGroupDto>>?> ValidateSaveMappings(
            string binNumber,
            List<InsuranceGroupDto> parentItems,
            List<InsuranceGroupDto> childMappings)
        {
            if (childMappings.Count > 0 && parentItems.Any(x => x.ChildInsuranceId == Guid.Empty))
            {
                return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupChildInsuranceRequiredMessage);
            }

            if (childMappings.Count == 0)
            {
                return null;
            }

            foreach (var dto in childMappings)
            {
                if (!dto.StartDate.HasValue)
                {
                    return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupDatesRequiredMessage);
                }

                if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Value.Date)
                {
                    return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupEndDateBeforeStartMessage);
                }
            }

            var childIds = childMappings.Select(x => x.ChildInsuranceId).Distinct().ToList();
            var otherBinMappings = (await _insuranceGroupRepository.GetAllAsync(
                    x => childIds.Contains(x.ChildInsuranceId)))
                .Where(x => !string.Equals(NormalizeBin(x.BINNumber), binNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var childGroup in childMappings.GroupBy(x => x.ChildInsuranceId))
            {
                var ranges = childGroup
                    .Select(x => (Start: x.StartDate!.Value, End: x.EndDate))
                    .Concat(otherBinMappings
                        .Where(m => m.ChildInsuranceId == childGroup.Key && m.StartDate.HasValue)
                        .Select(m => (Start: m.StartDate!.Value, End: m.EndDate)))
                    .ToList();

                for (var i = 0; i < ranges.Count; i++)
                {
                    for (var j = i + 1; j < ranges.Count; j++)
                    {
                        if (DateRangesOverlap(ranges[i].Start, ranges[i].End, ranges[j].Start, ranges[j].End))
                        {
                            return SuccessResult<List<InsuranceGroupDto>>.BadRequest(CustomMessage.InsuranceGroupDateOverlapMessage);
                        }
                    }
                }
            }

            return null;
        }

        private static bool DateRangesOverlap(DateTime start1, DateTime? end1, DateTime start2, DateTime? end2)
        {
            var effectiveEnd1 = end1?.Date ?? DateTime.MaxValue.Date;
            var effectiveEnd2 = end2?.Date ?? DateTime.MaxValue.Date;
            return start1.Date <= effectiveEnd2 && start2.Date <= effectiveEnd1;
        }

        private static string? NormalizeBin(string? binNumber) =>
            string.IsNullOrWhiteSpace(binNumber) ? null : binNumber.Trim();

        private SuccessResult<List<InsuranceGroupDto>> BuildMappingSavedSuccess(List<InsuranceGroup> mappings)
        {
            return new SuccessResult<List<InsuranceGroupDto>>
            {
                Status = (int)HttpStatusCode.OK,
                Message = CustomMessage.GetMappingSavedMessage(nameof(InsuranceGroup)),
                Data = _mapper.Map<List<InsuranceGroupDto>>(mappings)
            };
        }
    }
}
