using System.Collections.Concurrent;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// Tracks which steps in-progress uploads have completed (kept in memory, removed when the upload ends).
    /// The frontend polls it every second during an upload and shows the stalled step when nothing changes. Key = member id + upload id, so users can only query their own uploads.
    /// </summary>
    public class UploadProgressService
    {
        private readonly ConcurrentDictionary<string, IReadOnlyList<Step>> _uploads = new();

        public void Update(string memberId, string uploadId, IReadOnlyList<Step> steps) => _uploads[Key(memberId, uploadId)] = steps;

        public IReadOnlyList<Step> Get(string memberId, string uploadId) =>
            _uploads.TryGetValue(Key(memberId, uploadId), out var steps) ? steps : [];

        public void Remove(string memberId, string uploadId) => _uploads.TryRemove(Key(memberId, uploadId), out _);

        private static string Key(string memberId, string uploadId) => $"{memberId}|{uploadId}";
    }
}
