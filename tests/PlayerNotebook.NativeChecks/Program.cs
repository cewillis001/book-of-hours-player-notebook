using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using PlayerNotebooks;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Logic;
using SecretHistories.UI;
using Newtonsoft.Json;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SecretHistories.Core;

internal static class Program
{
    static void Main(string[] args)
    {
        var managed = args.Length > 0 ? args[0] : @"D:\SteamLibrary\steamapps\common\Book of Hours\bh_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            var path = Path.Combine(managed, new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        Run();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        // Exercise the installed engine's actual [Encaust] property extraction.
        // Does not launch Unity, read/write player saves, or execute creation commands.
        var element = new Element();
        element.SetId("playernotebook.brown.plain");
        element.Label = "Untitled Notebook";
        var stack = new ElementStack("native-test-owner", element, 1, Timeshadow.CreateTimelessShadow(), null);
        var document = NotebookCodec.Read(null, stack.Id);
        document.Title = "Native — 記録";
        document.Entries.Add(new NotebookEntry { Body = "One\nTwo\r\n📚" });
        document.Entries.Add(new NotebookEntry { Heading = "Last used", Body = "Independent entry" });
        document.LastSelectedEntryId = document.Entries[1].Id;
        stack.SetIllumination(NotebookCodec.IlluminationKey, NotebookCodec.Write(document));
        var command = new Encaustery<ElementStackCreationCommand>().Encaust(stack);
        var json = JsonConvert.SerializeObject(command);
        var restored = JsonConvert.DeserializeObject<ElementStackCreationCommand>(json);
        var result = NotebookCodec.Read(restored.Illuminations[NotebookCodec.IlluminationKey], restored.Id);
        if (result.Id != document.Id || result.Title != document.Title || result.Entries[0].Body != document.Entries[0].Body
            || result.LastSelectedEntryId != document.LastSelectedEntryId || result.OpeningEntryIndex != 1)
            throw new Exception("Native serialization round trip failed");
        Console.WriteLine("PASS installed-engine Encaustery -> creation command -> JSON -> notebook round trip");
        // Run the compiled production patches against real game objects, without Unity UI.
        var pluginPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "../../../../../src/PlayerNotebook/bin/Release/net472/PlayerNotebook.dll"));
        var plugin = Assembly.LoadFrom(pluginPath);
        var harmony = new Harmony("playernotebook.offline-native-checks");
        try
        {
            harmony.CreateClassProcessor(plugin.GetType("NotebookLabelPatch", true)).Patch();
            harmony.CreateClassProcessor(plugin.GetType("NotebookMergePatch", true)).Patch();
            harmony.CreateClassProcessor(plugin.GetType("NotebookRecipePriorityPatch", true)).Patch();
            if (stack.Label != document.Title) throw new Exception("Compiled label patch failed");
            document.Title = " \t\u2003";
            stack.SetIllumination(NotebookCodec.IlluminationKey, NotebookCodec.Write(document));
            if (stack.Label != "Untitled Notebook") throw new Exception("Compiled fallback patch failed");
            var other = new ElementStack("native-test-other", element, 1, Timeshadow.CreateTimelessShadow(), null);
            if (stack.CanMergeWith(other) || other.CanMergeWith(stack)) throw new Exception("Compiled merge guard failed");
            Console.WriteLine("PASS compiled Harmony patches: dynamic label, whitespace fallback, merge blocked both directions");
            var repo = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../../"));
            var recipes = JObject.Parse(File.ReadAllText(Path.Combine(repo, "mod/PlayerNotebook/content/recipes/notebooks.json")))["recipes"];
            var definitions = recipes.Select(r => new {
                Id = (string)r["id"], Reqs = r["reqs"].ToObject<Dictionary<string, string>>()
            }).ToList();
            for (int level = 1; level <= 9; level++)
            {
                var aspects = new AspectsDictionary(new Dictionary<string, int> {
                    {"ability", 1}, {"s.stitching.binding", 1}, {"skill", level}, {"quire.paper", 1}
                });
                var matches = definitions.Where(d => aspects.SatisfiesRequirements(d.Reqs)).ToList();
                var wanted = level < 4 ? "playernotebook.craft.brown.plain" : "playernotebook.craft.brown.decorated";
                if (matches.Count != 1 || matches[0].Id != wanted) throw new Exception("Ambiguous/wrong tier " + level);
                foreach (var forbidden in new[] {"ink", "tool", "memory", "readable", "perinculate"})
                {
                    aspects[forbidden] = 1;
                    if (definitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Forbidden input accepted: " + forbidden);
                    aspects.Remove(forbidden);
                }
                aspects.Remove("quire.paper");
                if (definitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Paper missing but notebook matched");
            }
            foreach (var definition in definitions)
            {
                var recipe = new Recipe { Craftable = true };
                recipe.SetId(definition.Id);
                if (recipe.Priority != 2) throw new Exception("Notebook priority patch failed");
                recipe.SetId("craft.stitching.binding_awakened.feather_sky");
                if (recipe.Priority != 1) throw new Exception("Vanilla priority changed");
                recipe.SetId(null);
                if (recipe.Priority != 1) throw new Exception("Null recipe identity not handled safely");
            }
            Console.WriteLine("PASS real engine requirements: 9 skill levels, exclusive tiers, paper required, optional inputs excluded; compiled priority patch is scoped");
            var rebindRecipes = JObject.Parse(File.ReadAllText(Path.Combine(repo, "mod/PlayerNotebook/content/recipes/rebinding.json")))["recipes"];
            var rebindDefinitions = rebindRecipes.Select(r => new {
                Id = (string)r["id"], Action = (string)r["actionid"], Reqs = r["reqs"].ToObject<Dictionary<string, string>>()
            }).ToList();
            if (rebindDefinitions.Any(d => d.Action != (d.Id.Contains(".color.") ? "library.prospect.loggia" : "library.desk.*")))
                throw new Exception("Rebinding workstation scope changed");
            var installedPigments = new HashSet<string>();
            var elementsDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(ElementStack).Assembly.Location), "../StreamingAssets/bhcontent/core/elements"));
            foreach (var file in Directory.GetFiles(elementsDir, "*.json"))
            foreach (var item in JObject.Parse(File.ReadAllText(file))["elements"] ?? new JArray())
            {
                var record = (JObject)item;
                var aspects = record.GetValue("aspects", StringComparison.OrdinalIgnoreCase) as JObject;
                if ((int?)aspects?["pigment"] > 0)
                    installedPigments.Add((string)record.GetValue("id", StringComparison.OrdinalIgnoreCase));
            }
            if (!installedPigments.SetEquals(NotebookBinding.Palette.Where(p => p.Pigment != null).Select(p => p.Pigment)))
                throw new Exception("Binding palette does not cover exactly the installed pigment set");
            Console.WriteLine("PASS all " + installedPigments.Count + " installed pigments have a binding mapping");
            for (int level = 1; level <= 9; level++)
            foreach (var binding in NotebookBinding.Palette)
            foreach (var color in new[] { binding.Color })
            foreach (var design in new[] { "plain", "decorated" })
            {
                var aspects = new AspectsDictionary(new Dictionary<string, int> {
                    {"ability", 1}, {"s.inks.containment", 1}, {"skill", level}, {"notebook.bindable", 1},
                    {"notebook." + design, 1}, {"playernotebook.green." + design, 1}
                });
                if (color != "brown") { aspects["pigment"] = 1; aspects[binding.Pigment] = 1; }
                var matches = rebindDefinitions.Where(d => aspects.SatisfiesRequirements(d.Reqs)).ToList();
                var wanted = "playernotebook.rebind.color." + color + "." + design;
                if (matches.Count != 1 || matches[0].Id != wanted) throw new Exception("Ambiguous rebinding: " + wanted);
                aspects.Remove("s.inks.containment"); aspects["s.stitching.binding"] = 1;
                if (rebindDefinitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Recoloring accepted binding skill");
                aspects.Remove("s.stitching.binding"); aspects["s.inks.containment"] = 1;
                foreach (var forbidden in new[] { "ink", "tool", "memory", "readable", "quire.paper" })
                {
                    aspects[forbidden] = 1;
                    if (rebindDefinitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Rebinding accepted " + forbidden);
                    aspects.Remove(forbidden);
                }
                aspects.Remove("notebook.bindable");
                if (rebindDefinitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Rebinding without notebook");
                var recipe = new Recipe { Craftable = true };
                recipe.SetId(wanted);
                if (recipe.Priority != 2) throw new Exception("Rebinding priority not applied");
            }
            var unknownPigment = new AspectsDictionary(new Dictionary<string, int> {
                {"ability",1}, {"s.inks.containment",1}, {"skill",4}, {"notebook.bindable",1}, {"notebook.plain",1}, {"pigment",1}, {"notebook.test.unknownpigment",1}
            });
            if (rebindDefinitions.Any(d => unknownPigment.SatisfiesRequirements(d.Reqs))) throw new Exception("Unknown paint accepted");
            for (int level = 1; level <= 9; level++)
            foreach (var binding in NotebookBinding.Palette)
            foreach (var color in new[] { binding.Color })
            foreach (var design in new[] { "plain", "decorated" })
            {
                var aspects = new AspectsDictionary(new Dictionary<string, int> {
                    {"ability",1}, {"s.stitching.binding",1}, {"skill",level}, {"notebook.bindable",1},
                    {"notebook." + design,1}, {"playernotebook." + color + "." + design,1}, {"book.press",1}, {"tool",1}
                });
                var matches = rebindDefinitions.Where(d => aspects.SatisfiesRequirements(d.Reqs)).ToList();
                var wanted = "playernotebook.rebind.design." + color + "." + (design == "plain" ? "decorated" : "plain");
                if (level < 4 ? matches.Count != 0 : matches.Count != 1 || matches[0].Id != wanted)
                    throw new Exception("Wrong decoration gate/outcome: " + wanted + " level " + level);
                aspects["pigment"] = 1;
                if (rebindDefinitions.Any(d => aspects.SatisfiesRequirements(d.Reqs))) throw new Exception("Decoration accepted paint");
            }
            Console.WriteLine("PASS real engine rebinding requirements: " + (NotebookBinding.Palette.Count * 18) + " recolor + " + (NotebookBinding.Palette.Count * 18) + " design cases, preserved design at all levels, design gate at 4, exclusive outcomes and exclusions");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("Not an in-game save/load or UI test.");
    }
}
