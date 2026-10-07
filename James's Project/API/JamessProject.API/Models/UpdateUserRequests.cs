namespace JamessProject.API.Models;

public class UpdateUserRequests
{
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string UserPhone { get; set; } = "";
  
}
