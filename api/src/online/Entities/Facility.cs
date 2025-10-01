namespace Online.Entities;

public class Facility
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SiteId { get; set; }
    public required Site Site { get; set; }
}