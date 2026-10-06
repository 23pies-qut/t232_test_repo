using System.Diagnostics;

namespace MockModule.Server.Models
{
    /// <summary>
    /// One step of an upload (data for the storage animation).
    /// Node = browser | api | proc | table; Lines = executed statements; Rows = table rows read or written; Cipher = whether the data is encrypted at this point.
    /// </summary>
    public record Step(string Node, string Title, string Detail, double Ms,
        string? Proc = null, string[]? Lines = null, TableRow[]? Rows = null, bool Cipher = false, bool Error = false);

    public record TableRow(string Table, string Op, Dictionary<string, string> Cells);

    /// <summary>Records each step in order with its measured duration (ms since the previous step); notifies onChange after every step (used for upload progress).</summary>
    public class StepTrace
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<Step> _steps = [];
        private readonly Action<IReadOnlyList<Step>>? _onChange;
        private double _last;

        public StepTrace(Action<IReadOnlyList<Step>>? onChange = null) => _onChange = onChange;

        public IReadOnlyList<Step> Steps => _steps;

        public void Add(string node, string title, string detail, string? proc = null, string[]? lines = null,
            TableRow[]? rows = null, bool cipher = false, bool error = false)
        {
            var now = _clock.Elapsed.TotalMilliseconds;
            _steps.Add(new Step(node, title, detail, Math.Round(now - _last, 2), proc, lines, rows, cipher, error));
            _last = now;
            _onChange?.Invoke(_steps.ToArray());
        }

        public void Fail(string node, string title, string detail) => Add(node, title, detail, error: true);

        public static TableRow Row(string table, string op, params (string Column, string Value)[] cells) =>
            new(table, op, cells.ToDictionary(c => c.Column, c => c.Value));

        /// <summary>Shows only the first 8 characters of a long id.</summary>
        public static string Short(string id) => id.Length > 8 ? id[..8] + "…" : id;

        /// <summary>Byte preview: the first 8 bytes as hex.</summary>
        public static string Hex(byte[] bytes) =>
            Convert.ToHexString(bytes.AsSpan(0, Math.Min(8, bytes.Length))) + (bytes.Length > 8 ? "…" : "");
    }
}
