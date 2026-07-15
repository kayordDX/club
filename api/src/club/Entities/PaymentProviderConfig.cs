namespace Club.Entities;

public class PaymentProviderConfig : AuditableEntity
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = default!;
    public required string ProviderKey { get; set; }
    public byte[] Iv { get; set; } = [];
    public required string EncryptedSettings { get; set; }
    public bool Enabled { get; set; } = true;
}
