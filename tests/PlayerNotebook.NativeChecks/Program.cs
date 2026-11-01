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
        stack.SetIllumination(NotebookCodec.IlluminationKey, NotebookCodec.Write(document));
        var command = new Encaustery<ElementStackCreationCommand>().Encaust(stack);
        var json = JsonConvert.SerializeObject(command);
        var restored = JsonConvert.DeserializeObject<ElementStackCreationCommand>(json);
        var result = NotebookCodec.Read(restored.Illuminations[NotebookCodec.IlluminationKey], restored.Id);
        if (result.Id != document.Id || result.Title != document.Title || result.Entries[0].Body != document.Entries[0].Body)
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
            }
            Console.WriteLine("PASS real engine requirements: 9 skill levels, exclusive tiers, paper required, optional inputs excluded; compiled priority patch is scoped");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("Not an in-game save/load or UI test.");
    }
}
