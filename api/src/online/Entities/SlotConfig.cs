namespace Online.Entities;

public class SlotConfig : AuditableEntity
{
    public int Id { get; set; }
    public int SlotConfigTypeId { get; set; }
    public required SlotConfigType SlotConfigType { get; set; }
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
    public int WeekdayId { get; set; }
    public int GroupCount { get; set; }
    public int Interval { get; set; }
}
