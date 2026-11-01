using System;
using PlayerNotebooks;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine("PASS " + name);
}
void Reject(string json, string name)
{
    bool rejected = false;
    try { NotebookCodec.Read(json, "card-a"); } catch { rejected = true; }
    Check(rejected, name);
}
var a = NotebookCodec.Read(null, "card-a");
var b = NotebookCodec.Read(null, "card-b");
Check(a.Id != b.Id && a.Entries.Count == 0, "Fresh identities; opening creates no entries");
Check(a.DisplayName == "Untitled Notebook", "Empty title fallback");
a.Title = " \t\u2003";
Check(a.DisplayName == "Untitled Notebook", "Unicode whitespace fallback");
var whitespace = NotebookCodec.Read(NotebookCodec.Write(a), "card-a");
Check(whitespace.Title == a.Title, "Whitespace stored verbatim");
a.Title = "Inks — 墨 📚";
b.Title = a.Title;
a.Entries.Add(new NotebookEntry { Heading = "", Body = "α\nβ\r\n‘quoted’ 🖋️\t終" });
a.Entries.Add(new NotebookEntry { Heading = "Second", Body = "" });
var body = a.Entries[0].Body;
a.MoveEntry(0, 1);
var raw = NotebookCodec.Write(a);
var loaded = NotebookCodec.Read(raw, "card-a");
Check(loaded.Id == a.Id && loaded.Entries[1].Id == a.Entries[1].Id, "Identity survives serialization");
Check(loaded.DisplayName == a.Title && b.DisplayName == a.Title && b.Entries.Count == 0, "Duplicate titles remain independent");
Check(loaded.Entries[1].Body == body && loaded.Entries[0].Body == "", "Order, Unicode, line breaks, and empty fields survive");
var copy = NotebookCodec.Read(raw, "card-copy");
Check(copy.Id != a.Id && copy.Entries[1].Id != a.Entries[1].Id && copy.Entries[1].Body == body, "Copied stack gets independent identities and contents");
copy.Entries[1].Body = "changed";
Check(a.Entries[1].Body == body, "Copy editing is independent");
loaded.Entries.RemoveAt(0);
loaded.Title = "";
Check(NotebookCodec.Read(NotebookCodec.Write(loaded), "card-a").Entries.Count == 1 && loaded.DisplayName == "Untitled Notebook", "Delete entry and clear title");
Reject(raw.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":2"), "Future schema refused");
Reject("{}", "Missing schema refused");
Reject("{broken", "Corrupt JSON refused");
Reject(raw.Replace("\"Entries\":[", "\"Entries\":[null,"), "Invalid entry refused");
a.Entries[0].Body = new string('字', 150000);
Check(NotebookCodec.Read(NotebookCodec.Write(a), "card-a").Entries[0].Body.Length == 150000, "Long text survives");
Console.WriteLine($"{checks} checks passed.");
