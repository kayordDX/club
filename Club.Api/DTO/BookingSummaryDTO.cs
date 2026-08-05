namespace Club.DTO;

public class BookingSummaryDTO
{
    public int Id { get; set; }
    public int BookingStatusId { get; set; }
    public required string BookingStatusName { get; set; }
    public required DateTime BookingStatusDate { get; set; }
    public string? FacilityName { get; set; }
    public DateTime? SlotStartDatetime { get; set; }
    public DateTime? SlotEndDatetime { get; set; }
    public int PlayerCount { get; set; }
    public decimal AmountOutstanding { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsPaid { get; set; }
    public DateTime ExpiresAt { get; set; }
}
