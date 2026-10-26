namespace Rxintel.Generic.Entities.Interface
{
    public interface IBaseEntity
    {
       public DateTime CreatedDate { get; set; }
       public DateTime? ModifiedDate { get; set; }
    }
}
