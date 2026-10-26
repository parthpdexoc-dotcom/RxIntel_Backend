using AutoMapper;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.AI.Mappings
{
    public class ErxAuditMappingProfile : Profile
    {
        public ErxAuditMappingProfile()
        {
            CreateMap<ErxAuditBatch, ErxAuditBatchDto>()
                .ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => src.Pharmacy != null ? (src.Pharmacy.BusinessTradeName ?? src.Pharmacy.BusinessLegalName) : null))
                .ForMember(dest => dest.PharmacyNpi, opt => opt.MapFrom(src => src.Pharmacy != null ? src.Pharmacy.NPI : null));

            CreateMap<ErxAuditBatchDto, ErxAuditBatch>()
                .ForMember(dest => dest.Pharmacy, opt => opt.Ignore())
                .ForMember(dest => dest.Records, opt => opt.Ignore());

            CreateMap<ErxAuditRecord, ErxAuditRecordDto>()
                .ForMember(dest => dest.PharmacyId, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.PharmacyId : null))
                .ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => src.Batch != null && src.Batch.Pharmacy != null ? (src.Batch.Pharmacy.BusinessTradeName ?? src.Batch.Pharmacy.BusinessLegalName) : null))
                .ForMember(dest => dest.DiscrepancyCount, opt => opt.MapFrom(src => src.Discrepancies.Count(d => d.Status == "Open" || string.IsNullOrEmpty(d.Status))))
                .ForMember(dest => dest.Discrepancies, opt => opt.MapFrom(src => src.Discrepancies));

            CreateMap<ErxAuditRecordDto, ErxAuditRecord>()
                .ForMember(dest => dest.Batch, opt => opt.Ignore())
                .ForMember(dest => dest.Discrepancies, opt => opt.Ignore())
                .ForMember(dest => dest.AlertLogs, opt => opt.Ignore());

            CreateMap<ErxColumnMatchRule, ErxColumnMatchRuleDto>()
                .ForMember(dest => dest.InsuranceCompanyName, opt => opt.MapFrom(src =>
                    !string.IsNullOrWhiteSpace(src.InsuranceCompanyNames)
                        ? src.InsuranceCompanyNames
                        : (src.InsuranceCompany != null ? src.InsuranceCompany.Name : null)))
                .ForMember(dest => dest.InsuranceCompanyIds, opt => opt.MapFrom(src =>
                    ParseInsuranceCompanyIds(src.InsuranceCompanyIds, src.InsuranceCompanyId)))
                .ForMember(dest => dest.InsuranceCompanyNames, opt => opt.MapFrom(src =>
                    ParseInsuranceCompanyNames(src.InsuranceCompanyNames, src.InsuranceCompany)));
            CreateMap<ErxColumnMatchRuleDto, ErxColumnMatchRule>()
                .ForMember(dest => dest.InsuranceCompany, opt => opt.Ignore())
                .ForMember(dest => dest.Discrepancies, opt => opt.Ignore())
                .ForMember(dest => dest.InsuranceCompanyIds, opt => opt.MapFrom(src =>
                    src.InsuranceCompanyIds != null && src.InsuranceCompanyIds.Any()
                        ? string.Join(",", src.InsuranceCompanyIds)
                        : null))
                .ForMember(dest => dest.InsuranceCompanyNames, opt => opt.MapFrom(src =>
                    src.InsuranceCompanyNames != null && src.InsuranceCompanyNames.Any()
                        ? string.Join(", ", src.InsuranceCompanyNames)
                        : null));

            CreateMap<ErxAuditDiscrepancy, ErxAuditDiscrepancyDto>()
                .ForMember(dest => dest.BatchId, opt => opt.MapFrom(src => src.Record != null ? src.Record.BatchId : Guid.Empty))
                .ForMember(dest => dest.RxNumber, opt => opt.MapFrom(src => src.Record != null ? src.Record.RxNumber : string.Empty))
                .ForMember(dest => dest.FieldGroupName, opt => opt.MapFrom(src => src.Rule != null ? src.Rule.FieldGroupName : string.Empty))
                .ForMember(dest => dest.MmColumnName, opt => opt.MapFrom(src => src.Rule != null ? src.Rule.MmColumnName : string.Empty))
                .ForMember(dest => dest.ErxColumnName, opt => opt.MapFrom(src => src.Rule != null ? src.Rule.ErxColumnName : string.Empty));
            CreateMap<ErxAuditDiscrepancyDto, ErxAuditDiscrepancy>()
                .ForMember(dest => dest.Record, opt => opt.Ignore())
                .ForMember(dest => dest.Rule, opt => opt.Ignore());

            CreateMap<InsuranceCompany, InsuranceCompanyDto>().ReverseMap();
            CreateMap<ErxSigMapping, ErxSigMappingDto>().ReverseMap();
        }

        private static List<int> ParseInsuranceCompanyIds(string? idsString, int? fallbackId)
        {
            if (!string.IsNullOrWhiteSpace(idsString))
            {
                var result = new List<int>();
                var parts = idsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out var id) && id > 0)
                    {
                        result.Add(id);
                    }
                }
                return result;
            }

            return fallbackId.HasValue ? new List<int> { fallbackId.Value } : new List<int>();
        }

        private static List<string> ParseInsuranceCompanyNames(string? namesString, InsuranceCompany? fallbackCompany)
        {
            if (!string.IsNullOrWhiteSpace(namesString))
            {
                return namesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            return fallbackCompany != null && !string.IsNullOrEmpty(fallbackCompany.Name)
                ? new List<string> { fallbackCompany.Name }
                : new List<string>();
        }
    }
}
