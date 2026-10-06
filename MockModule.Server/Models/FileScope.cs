namespace MockModule.Server.Models
{
    public record FileScope(string TenantId, string MemberID, HashSet<string> Permissions)
    {
        public bool Has(string permission) => Permissions.Contains(permission);
    }
}
