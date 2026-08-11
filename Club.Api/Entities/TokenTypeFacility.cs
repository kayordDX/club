namespace Club.Entities;

public class TokenTypeFacility
{
    public int TokenTypeId { get; set; }
    public required TokenType TokenType { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}
