using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlayerNotebooks
{
    public sealed class NotebookEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Heading { get; set; } = "";
        public string Body { get; set; } = "";
    }

    // Array position is the authoritative entry order; no conflicting numeric order field.
    public sealed class NotebookDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string OwnerStackId { get; set; }
        public string Title { get; set; } = "";
        public string Color { get; set; } = "brown";
        public string Design { get; set; } = "plain";
        public List<NotebookEntry> Entries { get; set; } = new List<NotebookEntry>();
        [JsonIgnore] public string DisplayName => string.IsNullOrWhiteSpace(Title) ? "Untitled Notebook" : Title;

        public void MoveEntry(int from, int to)
        {
            if (from < 0 || from >= Entries.Count || to < 0 || to >= Entries.Count)
                throw new ArgumentOutOfRangeException();
            var entry = Entries[from];
            Entries.RemoveAt(from);
            Entries.Insert(to, entry);
        }
    }

    public static class NotebookCodec
    {
        public const string IlluminationKey = "playernotebook.document";
        public static NotebookDocument Read(string json, string ownerStackId)
        {
            if (string.IsNullOrWhiteSpace(ownerStackId)) throw new ArgumentException("Missing card identity");
            if (string.IsNullOrEmpty(json)) return new NotebookDocument { OwnerStackId = ownerStackId };
            var root = JObject.Parse(json);
            // Refuse future or missing versions without overwriting the original payload.
            if ((int?)root["SchemaVersion"] != 1) throw new NotSupportedException("Unsupported notebook schema");
            var document = root.ToObject<NotebookDocument>();
            if (document == null || string.IsNullOrEmpty(document.Id) || string.IsNullOrEmpty(document.OwnerStackId)
                || document.Title == null || document.Color == null || document.Design == null || document.Entries == null
                || document.Entries.Any(e => e == null || string.IsNullOrEmpty(e.Id) || e.Heading == null || e.Body == null)
                || document.Entries.Select(e => e.Id).Distinct().Count() != document.Entries.Count)
                throw new FormatException("Invalid notebook data; original preserved");
            // Native stack splitting copies illuminations. A different owner means a copy,
            // never shared mutable storage. Load with the same owner preserves all identities.
            if (document.OwnerStackId != ownerStackId)
            {
                document.Id = Guid.NewGuid().ToString("N");
                document.OwnerStackId = ownerStackId;
                foreach (var entry in document.Entries) entry.Id = Guid.NewGuid().ToString("N");
            }
            return document;
        }

        public static string Write(NotebookDocument document) => JsonConvert.SerializeObject(document, Formatting.None);
    }
}
