using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Rxintel.Generic.Services.Services
{
    public class MedicineService : GenericService<MedicineDto, Medicine>, IMedicineService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly INDCGroupRepository _ndcGroupRepository;
        private readonly IMapper _mapper;
        private readonly RxintelDbContext? _dbContext;

        public MedicineService(
            IMedicineRepository medicineMasterRepository,
            INDCGroupRepository ndcGroupRepository,
            IMapper mapper,
            ILogger<MedicineService> logger,
            RxintelDbContext? dbContext = null) : base(medicineMasterRepository, mapper, "Id")
        {
            _medicineRepository = medicineMasterRepository;
            _ndcGroupRepository = ndcGroupRepository;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public Task<PagedList<MedicineDto>> GetAll(PageParam pageParam)
            => GetMedicinesAsync(excludeMedicineId: null, pageParam);

        public Task<PagedList<MedicineDto>> MedicineDropdownById(Guid? excludeMedicineId, PageParam pageParam)
            => GetMedicinesAsync(excludeMedicineId, pageParam);

        public Task<Helpers.MedicineMasterLookupResult?> LookupClinicalAsync(string drugName, string? dosageForm = null, string? strength = null, string? packageSize = null)
        {
            var result = Helpers.PharmacyDrugNormalizer.LookupMedicineMaster(drugName, dosageForm, strength, packageSize, _dbContext);
            return Task.FromResult(result);
        }

        public override async Task<SuccessResult<MedicineDto>> DeleteAsync(Guid id)
        {
            if ((await _ndcGroupRepository.GetAllAsync(x => x.ParentMedicineId == id)).Any())
                return SuccessResult<MedicineDto>.BadRequest(CustomMessage.MedicineDeleteBlockedByNdcGroupLink);

            if ((await _ndcGroupRepository.GetAllAsync(x => x.ChildMedicineId == id)).Any())
                return SuccessResult<MedicineDto>.BadRequest(CustomMessage.MedicineDeleteBlockedByNdcGroupChild);

            return await base.DeleteAsync(id);
        }

        /// <summary>
        /// Shared Medicine list/dropdown query: SQL filter + common ApplyPagination.
        /// </summary>
        private async Task<PagedList<MedicineDto>> GetMedicinesAsync(Guid? excludeMedicineId, PageParam? pageParam)
        {
            pageParam ??= new PageParam();

            IQueryable<Medicine> query = _medicineRepository.GetAllQueryable().AsNoTracking();

            if (excludeMedicineId.HasValue)
                query = query.Where(m => m.Id != excludeMedicineId.Value);

            if (!string.IsNullOrWhiteSpace(pageParam.SearchText))
            {
                foreach (var token in Regex.Split(pageParam.SearchText.Trim(), @"[\s,;]+").Where(t => t.Length > 0))
                {
                    var t = token;
                    query = query.Where(m =>
                        m.Brand.Contains(t) ||
                        (m.Generic != null && m.Generic.Contains(t)) ||
                        m.NDC.Contains(t) ||
                        (m.NormalizedNdc != null && m.NormalizedNdc.Contains(t)) ||
                        (m.Strength != null && m.Strength.Contains(t)) ||
                        (m.StrengthUnit != null && m.StrengthUnit.Contains(t)) ||
                        m.PackageSize.Contains(t) ||
                        (m.DosageForm != null && m.DosageForm.Contains(t)));
                }
            }

            if (pageParam.IsPaginationRequested)
            {
                var paged = ApplyPagination(query, pageParam);
                return new PagedList<MedicineDto>(_mapper.Map<List<MedicineDto>>(paged.Data), pageParam, paged.TotalRecords);
            }

            var entities = await query.ToListAsync();
            return new PagedList<MedicineDto>(_mapper.Map<List<MedicineDto>>(entities), pageParam, entities.Count);
        }
    }
}
