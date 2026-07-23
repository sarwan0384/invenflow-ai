namespace InvenFlow.Core.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Code { get; set; } // <--- ADD THIS LINE
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}