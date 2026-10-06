using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace MockModule.Server.Services
{
    /// <summary>
    /// In Development, generates two keys on first run and stores them in .NET user-secrets (outside the project folder):
    ///   SigningKeyBase64 - signs JWTs; MasterKeyBase64 - wraps each file's data key.
    /// Existing keys are never overwritten (a new master key would make existing files undecryptable).
    /// </summary>
    public static class DevelopmentKeys
    {
        // Must match the UserSecretsId in MockModule.Server.csproj
        private const string UserSecretsId = "f6a1c9b8-8e1d-4c57-9a0b-fd8e2b7c0a18";

        public static void EnsureCreated(WebApplicationBuilder builder)
        {
            string[] names = [TokenService.SigningKeySetting, EncryptionService.MasterKeySetting];
            var missing = names.Where(n => string.IsNullOrWhiteSpace(builder.Configuration[n])).ToList();
            if (missing.Count == 0) return;
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException($"Missing keys: {string.Join(", ", missing)}. Provide them through environment variables or a key vault in production.");

            var path = PathHelper.GetSecretsPathFromSecretsId(UserSecretsId);
            var secrets = (File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject : null) ?? new JsonObject();
            var added = new Dictionary<string, string?>();
            foreach (var name in missing)
            {
                var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                secrets[name] = key;
                added[name] = key;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, secrets.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            builder.Configuration.AddInMemoryCollection(added);
            Console.WriteLine($"[MockModule] Generated development keys {string.Join(", ", missing)} -> {path}");
        }
    }
}
