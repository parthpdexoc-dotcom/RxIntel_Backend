using Rxintel.Generic.Entities.Interface;
namespace Rxintel.Generic.Entities.Entities
{
    public abstract class BaseEntity : IBaseEntity
    {
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}