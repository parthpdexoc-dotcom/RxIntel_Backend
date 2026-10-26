using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.Generic.Services.Interfaces
{
    public interface IMedicineService : IGenericService<MedicineDto, Medicine>
    {
        Task<PagedList<MedicineDto>> GetAll(PageParam pageParam);
        Task<PagedList<MedicineDto>> MedicineDropdownById(Guid? excludeMedicineId, PageParam pageParam);
        Task<Helpers.MedicineMasterLookupResult?> LookupClinicalAsync(string drugName, string? dosageForm = null, string? strength = null, string? packageSize = null);
    }
}
