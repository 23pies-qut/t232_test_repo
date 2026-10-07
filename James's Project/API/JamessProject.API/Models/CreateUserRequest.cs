namespace JamessProject.API.Models;

public class CreateUserRequest
{
    public Guid TenantId { get; set; }
    public int MemberId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string UserPhone { get; set; } = "";
    public string UserPasswordHash { get; set; } = "";
}