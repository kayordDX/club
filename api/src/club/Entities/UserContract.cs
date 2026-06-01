namespace Online.Entities;

public class UserContract : AuditableEntity
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }
}
