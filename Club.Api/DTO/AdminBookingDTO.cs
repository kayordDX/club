namespace Club.DTO;

public class AdminBookingDTO
{
    public int Id { get; set; }
    public int BookingStatusId { get; set; }
    public required string BookingStatusName { get; set; }
    public required DateTime BookingStatusDate { get; set; }
    public DateTime? SlotStartDatetime { get; set; }
    public Guid? UserId { get; set; }
    public string? CustomerName { get; set; }
    public int PlayerCount { get; set; }
    public int ExtraCount { get; set; }
    public bool IsPaid { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
