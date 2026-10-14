namespace MockModule.Server.Models
{
    public record FileScope(string TenantId, string MemberID, HashSet<string> Permissions)
    {
        public bool Has(string permission) => Permissions.Contains(permission);
    }

    public class FileDownload
    {
        public byte[] Ciphertext { get; set; } = [];
        public byte[] Nonce { get; set; } = [];
        public byte[] AuthTag { get; set; } = [];
        public byte[] WrappedDek { get; set; } = [];
        public string PlaintextChecksum { get; set; } = "";
        public string FileName { get; set; } = "";

    }
}
