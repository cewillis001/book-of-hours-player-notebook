#if NOTEBOOK_TESTING
using System;
using System.IO;
using HarmonyLib;
using SecretHistories.Entities;
using UnityEngine;
using System.Linq;
using SecretHistories.UI;
using SecretHistories.Commands;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Persistence;
using System.Collections;
using SecretHistories.Services;
using Newtonsoft.Json;
using SecretHistories.Enums;
using PlayerNotebooks;
using SecretHistories.Constants;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using SecretHistories.Core;

// Compiled ONLY into the development test build, never into the distributable package.
// FEATHER still requires the real, user-enabled GHIRBI permission mod.
internal static class NotebookTestEnvironment
{
    internal static string SaveRoot;
    internal static void Prepare()
    {
        var requested = Environment.GetEnvironmentVariable("PLAYERNOTEBOOK_TEST_ROOT");
        if (string.IsNullOrWhiteSpace(requested) || !Path.IsPathRooted(requested))
            throw new InvalidOperationException("Test build requires an explicit isolated save root");
        SaveRoot = Path.GetFullPath(requested);
        if (!File.Exists(Path.Combine(SaveRoot, ".playernotebook-test")))
            throw new InvalidOperationException("Test root marker is missing; refusing to start test patches");
        Debug.Log("Player Notebook TEST BUILD: game saves redirected to " + SaveRoot);
    }
}

[HarmonyPatch(typeof(MetaInfo), "get_PersistentDataPath")]
internal static class NotebookTestSaveRootPatch
{
    static void Postfix(ref string __result) { __result = NotebookTestEnvironment.SaveRoot; }
}

internal sealed class NotebookTestControls : MonoBehaviour
{
    private string status = "Isolated test build: load the copied playthrough.";
    IEnumerator Start()
    {
        StartCoroutine(AuditCaretAlignment());
        var stage = Watchman.Get<StageHand>();
        while (!stage.SceneIsActive(Watchman.Get<Compendium>().GetSingleEntity<Dictum>().MenuScene))
            yield return null;
        yield return new WaitForSeconds(1);
        Debug.Log("Notebook integration driver: loading isolated playfield");
        GamePersistenceProvider provider = Environment.GetEnvironmentVariable("PLAYERNOTEBOOK_TEST_NEW_GAME") == "1"
            ? (GamePersistenceProvider)new FreshGameProvider(Watchman.Get<Compendium>().GetEntityById<Legacy>("librarian"))
            : new DefaultGamePersistenceProvider();
        stage.LoadGameInPlayfieldWithLoadingScreen(provider, stage.GetForemostScene());
        while (!stage.SceneIsActive(Watchman.Get<Compendium>().GetSingleEntity<Dictum>().PlayfieldScene))
            yield return null;
        yield return new WaitForSecondsRealtime(2);
        if (int.TryParse(Environment.GetEnvironmentVariable("PLAYERNOTEBOOK_TEST_WIDTH"), out var width) &&
            int.TryParse(Environment.GetEnvironmentVariable("PLAYERNOTEBOOK_TEST_HEIGHT"), out var height) && width > 0 && height > 0)
        {
            Screen.SetResolution(width, height, false);
            yield return new WaitForSecondsRealtime(1);
            Debug.Log("Notebook test screen: " + Screen.width + "x" + Screen.height);
        }
    }
    void OnGUI()
    {
        if (NotebookWindow.Instance != null && NotebookWindow.Instance.IsOpen) return;
        GUILayout.BeginArea(new Rect(Screen.width - 520, Mathf.Min(450, Screen.height - 420), 500, 400), GUI.skin.box);
        GUILayout.Label("NOTEBOOK TEST CONTROLS (not included in release)");
        if (GUILayout.Button("Load isolated copied playthrough"))
        {
            var menu = UnityEngine.Object.FindObjectOfType<BHMenuScreenController>();
            if (menu != null) menu.ContinueGame();
        }
        if (GUILayout.Button("Spawn a blank test notebook"))
        {
            try
            {
                var axe = Watchman.Get<HornedAxe>();
                var source = axe.GetExteriorSpheres().SelectMany(s => s.GetTokens())
                    .First(t => t.Payload is ElementStack stack && stack.Element.ManifestationType == "Book");
                var token = new TokenCreationCommand(new ElementStackCreationCommand("playernotebook.brown.plain", 1),
                    new TokenLocation(source)).Execute(Context.Unknown(), source.Sphere);
                NotebookWindow.Instance.Open((ElementStack)token.Payload);
                status = "Spawned " + token.Payload.Id;
                Debug.Log("Notebook test spawn: " + token.Payload.Id + " at " + token.Location.AtSpherePath);
            }
            catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
        }
        if (GUILayout.Button("Open saved notebooks (click again for the next)")) OpenNext();
        if (GUILayout.Button("Save isolated game as NotebookMovement")) Save();
        if (GUILayout.Button("Inspect placement and locate first notebook")) InspectPlacement();
        if (GUILayout.Button("Run isolated desk crafting tests")) TestCrafting();
        if (GUILayout.Button("Reveal Lodge through native unshrouding (new-game test)"))
        {
            try
            {
                var lodge = Watchman.Get<HornedAxe>().GetSpheres().SelectMany(s => s.GetTokens())
                    .Select(t => t.Payload).OfType<SecretHistories.Tokens.Payloads.TerrainFeature>()
                    .Single(t => t.Id == "lodge");
                lodge.Unshroud(true);
                Debug.Log("Notebook test: native Lodge unshrouding completed");
                InspectPlacement();
            }
            catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
        }
        if (GUILayout.Button("Run isolated pause/input checks")) StartCoroutine(CheckModal());
        if (GUILayout.Button("Test native menu transition with notebook open"))
        {
            try
            {
                OpenNext();
                if (!NotebookWindow.Instance.IsOpen) throw new Exception("Notebook did not open");
                Watchman.Get<StageHand>().MenuScreen();
                if (NotebookWindow.Instance.IsOpen) throw new Exception("Notebook survived scene cleanup");
                status = "PASS editor closed synchronously before native scene cleanup; inspect menu next.";
                Debug.Log("Notebook scene cleanup PASS: editor closed synchronously before native teardown");
            }
            catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
        }
        if (GUILayout.Button("Test native shelving and unchanged notebook data")) StartCoroutine(CheckShelving());
        if (GUILayout.Button("Switch fixtures with editor open (long / moved short)")) StartCoroutine(CheckFixtureSwitch());
        if (GUILayout.Button("Audit loaded vanilla recipe predictions")) CheckRecipePredictions();
        if (GUILayout.Button("Test rebinding written notebook through all colors/tiers")) TestRebinding();
        GUILayout.Label(status);
        GUILayout.EndArea();
    }
    private int lastCanvas;
    private IEnumerator AuditCaretAlignment()
    {
        var samples = 0;
        var previousPosition = new Vector2(float.NaN, float.NaN);
        var previousFocus = -1;
        while (samples < 100)
        {
            yield return new WaitForEndOfFrame();
            var window = NotebookWindow.Instance;
            if (window == null || !window.IsOpen) continue;
            var field = window.GetComponentsInChildren<NotebookTextField>().FirstOrDefault(f => f.isFocused);
            if (field == null) continue;
            var caret = field.textViewport.Find("Caret") as RectTransform;
            if (caret == null) continue;
            var position = field.textComponent.rectTransform.anchoredPosition;
            var focus = field.selectionStringFocusPosition;
            if (position == previousPosition && focus == previousFocus) continue;
            previousPosition = position; previousFocus = focus; samples++;
            Debug.Log("Notebook rendered caret alignment: delta=" + (caret.anchoredPosition - position) +
                " text=" + position + " focus=" + focus + " chars=" + field.text.Length);
        }
    }
    private float nextCaretSample;
    private int caretSamples;
    private int lastCaretAnchor = -1, lastCaretEnd = -1;
    void LateUpdate()
    {
        if (NotebookWindow.Instance == null || !NotebookWindow.Instance.IsOpen) return;
        var canvas = NotebookWindow.Instance.GetComponentInChildren<Canvas>();
        if (canvas != null && Time.unscaledTime >= nextCaretSample && caretSamples < 120)
        {
            var focused = canvas.GetComponentsInChildren<NotebookTextField>().FirstOrDefault(f => f.isFocused);
            if (focused != null && (caretSamples < 30 || lastCaretAnchor != focused.selectionStringAnchorPosition || lastCaretEnd != focused.selectionStringFocusPosition))
            {
                nextCaretSample = Time.unscaledTime + 0.35f; caretSamples++;
                lastCaretAnchor = focused.selectionStringAnchorPosition; lastCaretEnd = focused.selectionStringFocusPosition;
                var caret = focused.textViewport.Find("Caret");
                var renderer = caret?.GetComponent<CanvasRenderer>();
                var mesh = renderer?.GetMesh();
                Debug.Log("Notebook caret sample: field=" + focused.name + " focus=" + focused.isFocused +
                    " anchor=" + focused.selectionStringAnchorPosition + " end=" + focused.selectionStringFocusPosition +
                    " color=" + focused.caretColor + " rendererColor=" + (renderer != null ? renderer.GetColor().ToString() : "none") +
                    " shader=" + (renderer != null && renderer.materialCount > 0 ? renderer.GetMaterial(0).shader.name : "none") +
                    " vertexColor=" + (mesh != null && mesh.vertexCount > 0 ? mesh.colors32[0].ToString() : "none") +
                    " culled=" + (renderer != null && renderer.cull) + " vertices=" + (mesh != null ? mesh.vertexCount : -1) +
                    " visible=" + Traverse.Create(focused).Field("m_CaretVisible").GetValue<bool>() +
                    " inPlace=" + Traverse.Create(focused).Method("InPlaceEditing").GetValue<bool>() +
                    " meshBounds=" + (mesh != null ? mesh.bounds.ToString() : "none") +
                    " rect=" + (caret != null ? ((RectTransform)caret).rect.ToString() : "none") +
                    " position=" + (caret != null ? caret.localPosition.ToString() : "none"));
            }
        }
        if (canvas == null || canvas.GetInstanceID() == lastCanvas) return;
        lastCanvas = canvas.GetInstanceID();
        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            if (graphic.name == "Writing page" || graphic.name == "Entry index" || graphic.name == "Notebook")
                Debug.Log("Notebook palette: " + graphic.name + " color=" + graphic.color + " shader=" + graphic.material.shader.name +
                    " materialTint=" + (graphic.material.HasProperty("_Color") ? graphic.material.GetColor("_Color").ToString() : "none"));
        foreach (var field in canvas.GetComponentsInChildren<NotebookTextField>())
            Debug.Log("Notebook canvas field: " + field.name + " chars=" + field.text.Length +
                " font=" + field.textComponent.font.name + " fontSize=" + field.textComponent.fontSize +
                " scale=" + canvas.GetComponent<UnityEngine.UI.CanvasScaler>().scaleFactor +
                " viewport=" + field.textViewport.rect.size + " scrollbar=" + (field.verticalScrollbar != null) +
                " caretCreated=" + (field.textViewport.Find("Caret") != null));
    }
    private void CheckRecipePredictions()
    {
        try
        {
            NotebookWindow.Instance.Close();
            var compendium = Watchman.Get<Compendium>();
            var axe = Watchman.Get<HornedAxe>();
            var verb = "library.desk.reading.consider";
            var ordered = compendium.GetValidRecipesForUnstartedVerb(verb).ToArray();
            var cases = 0;
            foreach (var level in Enumerable.Range(1, 9))
            foreach (var paper in new[] { false, true })
            foreach (var optional in new[] { "", "ashartine", "mem.sight", "typewriter" })
            {
                // Real merged element aspects, plus the native skill-up mutations.
                // This audits recipe prediction, not physical optional-slot legality.
                var aspects = new AspectsDictionary(compendium.GetEntityById<Element>("s.stitching.binding").AspectsIncludingSelf);
                foreach (var aspect in new[] { "skill", "heart", "sky" }) aspects[aspect] += level - 1;
                aspects.CombineAspects(compendium.GetEntityById<Element>("xcho").AspectsIncludingSelf);
                if (paper) aspects.CombineAspects(compendium.GetEntityById<Element>("quire.paper").AspectsIncludingSelf);
                if (optional != "") aspects.CombineAspects(compendium.GetEntityById<Element>(optional).AspectsIncludingSelf);
                var context = axe.GetAspectsInContext(aspects);
                var matches = ordered.Where(r => r.CanExecuteInContext(context)).ToArray();
                var notebooks = matches.Where(r => r.Id.StartsWith("playernotebook.", StringComparison.Ordinal)).ToArray();
                var vanilla = matches.FirstOrDefault(r => !r.Id.StartsWith("playernotebook.", StringComparison.Ordinal));
                var selected = compendium.GetFirstMatchingRecipe(context, verb);
                if (paper && optional == "")
                {
                    var expected = "playernotebook.craft.brown." + (level < 4 ? "plain" : "decorated");
                    if (notebooks.Length != 1 || selected?.Id != expected) throw new Exception("Wrong notebook prediction at " + level);
                }
                else if (notebooks.Length != 0 || selected?.Id != vanilla?.Id)
                    throw new Exception("Vanilla prediction changed for excluded input at " + level + "/" + paper + "/" + optional);
                Debug.Log("Notebook recipe audit: level=" + level + " paper=" + paper + " optional=" + optional +
                    " selected=" + selected?.Id + " vanilla=" + vanilla?.Id + " notebookMatches=" + notebooks.Length);
                cases++;
            }
            status = "PASS " + cases + " loaded Reading desk recipe prediction cases; log records vanilla reservation.";
            Debug.Log("Notebook recipe matrix PASS: " + cases + " cases; skill 1-9; paper/no paper; no optional/ink/memory/tool; excluded cases preserve vanilla prediction");
        }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }
    private Token[] NotebookTokens() => Watchman.Get<HornedAxe>().GetSpheres().SelectMany(s => s.GetTokens())
        .Where(t => PlayerNotebook.IsNotebook(t.Payload)).ToArray();

    private IEnumerator CheckShelving()
    {
        NotebookWindow.Instance.Close();
        var cards = NotebookTokens();
        var card = cards.FirstOrDefault(t => t.Location.AtSpherePath.ToString().Contains("portage")) ?? cards.First();
        var before = ((ElementStack)card.Payload).GetIllumination(NotebookCodec.IlluminationKey);
        var id = card.Payload.Id;
        var origin = card.Location.AtSpherePath.ToString();
        var shelf = Watchman.Get<HornedAxe>().GetExteriorSpheres().OfType<ShelfSpaceSphere>()
            .FirstOrDefault(s => s != card.Sphere && s.CanAcceptToken(card));
        if (shelf == null) { status = "No other legal shelf available"; yield break; }
        var destination = shelf.GetAbsolutePath().ToString();
        if (!shelf.TryAcceptToken(card, Context.Unknown())) throw new Exception("Native shelving rejected token");
        yield return new WaitForSecondsRealtime(1);
        if (card.Payload.Id != id || card.Location.AtSpherePath.ToString() != destination ||
            ((ElementStack)card.Payload).GetIllumination(NotebookCodec.IlluminationKey) != before ||
            NotebookTokens().Length != cards.Length)
            throw new Exception("Native shelving changed identity/content or lost notebook");
        foreach (var other in cards.Where(t => t != card))
            if (((ElementStack)card.Payload).CanMergeWith(other.Payload) || ((ElementStack)other.Payload).CanMergeWith(card.Payload))
                throw new Exception("Live merge guard allowed notebook merging");
        status = "PASS native shelving, unchanged data/count, live bidirectional merge guard.";
        Debug.Log("Notebook shelving PASS: " + id + " from=" + origin + " to=" + destination + "; payload unchanged; count=" + cards.Length);
        InspectPlacement();
    }

    private int fixtureSwitch;
    private IEnumerator CheckFixtureSwitch()
    {
        var name = fixtureSwitch++ % 2 == 0 ? "NotebookSwitchLong" : "NotebookMovement";
        var path = Path.Combine(NotebookTestEnvironment.SaveRoot, name + ".json");
        var expected = JObject.Parse(File.ReadAllText(path)).DescendantsAndSelf().OfType<JObject>()
            .Where(o => o["Illuminations"]?[NotebookCodec.IlluminationKey] != null)
            .ToDictionary(o => (string)o["Id"], o => (string)o["Illuminations"][NotebookCodec.IlluminationKey]);
        if (expected.Count == 0) throw new Exception("Fixture contains no notebook payload");
        OpenNext();
        if (!NotebookWindow.Instance.IsOpen) throw new Exception("Editor not open before fixture switch");
        var stage = Watchman.Get<StageHand>();
        stage.LoadGameInPlayfieldWithLoadingScreen(new NamedGamePersistenceProvider(name), stage.GetForemostScene());
        if (NotebookWindow.Instance.IsOpen) throw new Exception("Editor survived native fixture-switch cleanup");
        status = "Loading fixture " + name + "...";
        Debug.Log("Notebook fixture switch: editor closed before loading " + name);
        yield return new WaitForSecondsRealtime(2);
        var deadline = Time.realtimeSinceStartup + 120;
        while (!stage.SceneIsActive(Watchman.Get<Compendium>().GetSingleEntity<Dictum>().PlayfieldScene))
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Fixture load timed out");
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1);
        var actual = NotebookTokens();
        if (NotebookWindow.Instance.IsOpen || actual.Length != expected.Count ||
            actual.Any(t => !expected.ContainsKey(t.Payload.Id) ||
                ((ElementStack)t.Payload).GetIllumination(NotebookCodec.IlluminationKey) != expected[t.Payload.Id]))
            throw new Exception("Fixture notebook identity/text/count mismatch or leaked editor");
        var nexus = Watchman.Get<LocalNexus>();
        if (nexus.PlayerInputDisabled()) throw new Exception("Notebook input lock leaked into loaded fixture");
        Debug.Log("Notebook fixture switch PASS: " + name + "; exact native identity/payload/count=" + actual.Length + "; editor closed; input restored");
        status = "PASS fixture switch " + name + "; exact notebook data and no editor/input leak.";
    }
    private IEnumerator CheckModal()
    {
        var window = NotebookWindow.Instance;
        window.Close();
        var heart = Watchman.Get<Heart>();
        var nexus = Watchman.Get<LocalNexus>();
        var ui = UnityEngine.Object.FindObjectOfType<UIController>();
        var priorSpeed = heart.GetEffectiveGameSpeed();
        var testOwnsInputLock = false;
        if (nexus.PlayerInputDisabled() || heart.Metapaused)
        { status = "Close native dialogs before pause/input test."; yield break; }
        status = "Running native heartbeat/modal checks...";
        try
        {
            foreach (var speed in new[] { GameSpeed.Normal, GameSpeed.Fast, GameSpeed.Paused })
            {
                heart.RespondToSpeedControlCommand(new SpeedControlEventArgs { ControlPriorityLevel = 2, GameSpeed = speed });
                if (heart.GetEffectiveGameSpeed() != speed) throw new Exception("Native override prevents test speed " + speed);
                OpenNext();
                if (!window.IsOpen || !nexus.PlayerInputDisabled() || !ui.IsEditingText() || !ui.HotkeysDisabled())
                    throw new Exception("Notebook did not own input/text/hotkey modal");
                var beats = NotebookTestBeatProbe.Count;
                yield return new WaitForSecondsRealtime(1);
                if (NotebookTestBeatProbe.Count != beats || heart.GetEffectiveGameSpeed() != speed)
                    throw new Exception("Simulation advanced or speed changed with notebook open: " + speed);
                window.Close();
                if (nexus.PlayerInputDisabled() || heart.GetEffectiveGameSpeed() != speed)
                    throw new Exception("Close did not restore prior input/speed: " + speed);
                beats = NotebookTestBeatProbe.Count;
                var seconds = NotebookTestBeatProbe.Seconds;
                yield return new WaitForSecondsRealtime(0.5f);
                if (NotebookTestBeatProbe.Count <= beats) throw new Exception("Native heartbeat did not resume");
                if (speed == GameSpeed.Paused ? NotebookTestBeatProbe.Seconds != seconds : NotebookTestBeatProbe.Seconds <= seconds)
                    throw new Exception("Wrong simulated-time behavior after close: " + speed);
                Debug.Log("Notebook modal PASS: " + speed + "; no heartbeat while open, native input/speed restored, time correct after close");
            }
            nexus.DisablePlayerInput(0);
            testOwnsInputLock = true;
            OpenNext();
            window.Close();
            if (!nexus.PlayerInputDisabled()) throw new Exception("Notebook released another modal's input lock");
            nexus.EnablePlayerInput();
            testOwnsInputLock = false;
            Debug.Log("Notebook modal PASS: pre-existing input lock preserved");
            status = "PASS pause/input checks: Normal, Fast, Paused; foreign lock preserved.";
        }
        finally
        {
            window.Close();
            if (testOwnsInputLock) nexus.EnablePlayerInput();
            heart.RespondToSpeedControlCommand(new SpeedControlEventArgs { ControlPriorityLevel = 2, GameSpeed = priorSpeed });
            if (!status.StartsWith("PASS pause/input")) status = "Pause/input test incomplete; inspect Player.log.";
        }
    }
    private int next;
    private void TestRebinding()
    {
        try
        {
            NotebookWindow.Instance.Close();
            var axe = Watchman.Get<HornedAxe>();
            var bindingDesk = axe.GetRegisteredSituations().Single(s => s.VerbId == "library.desk.reading.consider");
            var colorStation = axe.GetRegisteredSituations().SingleOrDefault(s => s.VerbId == "library.prospect.loggia");
            if (colorStation == null)
            {
                var command = new SituationCreationCommand("library.prospect.loggia").WithVerbId("library.prospect.loggia").AlreadyInState(StateEnum.Unstarted);
                command.Dominions.Add(new SecretHistories.Commands.SituationCommands.PopulateDominionCommand(
                    SituationDominionEnum.VerbThresholds, Watchman.Get<Compendium>().GetEntityById<Verb>("library.prospect.loggia").Thresholds));
                colorStation = (Situation)new TokenCreationCommand(command, new TokenLocation(bindingDesk.Token))
                    .Execute(Context.Unknown(), bindingDesk.Token.Sphere).Payload;
                Debug.Log("Notebook test: created native Loggia Prospect situation in isolated fixture; not a room-unlock test");
            }
            var notebook = axe.GetSpheres().SelectMany(s => s.GetTokens())
                .First(t => NotebookRebinding.CanRebind(t.Payload) &&
                    NotebookCodec.Read(((ElementStack)t.Payload).GetIllumination(NotebookCodec.IlluminationKey), t.Payload.Id).Entries.Count > 0);
            var card = (ElementStack)notebook.Payload;
            var stackId = card.Id;
            var original = Newtonsoft.Json.Linq.JObject.Parse(card.GetIllumination(NotebookCodec.IlluminationKey));
            var cases = NotebookBinding.Palette.Select(p => new { Level = 1, Color = p.Color, Press = false }).ToList();
            cases.Add(new { Level = 4, Color = "", Press = true });
            cases.AddRange(NotebookBinding.Palette.Select(p => new { Level = 1, Color = p.Color, Press = false }));
            cases.Add(new { Level = 4, Color = "", Press = true });
            cases.Add(new { Level = 4, Color = "blue", Press = false });
            var onlyColor = Environment.GetEnvironmentVariable("PLAYERNOTEBOOK_TEST_BINDING_COLOR");
            if (!string.IsNullOrEmpty(onlyColor))
            {
                if (!NotebookBinding.Palette.Any(p => p.Color == onlyColor)) throw new Exception("Unknown test binding color");
                cases = new[] { new { Level = 1, Color = onlyColor, Press = false } }.ToList();
            }
            var checkedDeskPaint = false;
            foreach (var test in cases)
            {
                var desk = test.Press ? bindingDesk : colorStation;
                var level = test.Level;
                var before = NotebookCodec.Read(card.GetIllumination(NotebookCodec.IlluminationKey), card.Id);
                var color = test.Press ? before.Color : test.Color;
                var design = test.Press ? (before.Design == "plain" ? "decorated" : "plain") : before.Design;
                var binding = NotebookBinding.Palette.Single(p => p.Color == color);
                if (desk.StateIdentifier != StateEnum.Unstarted || desk.GetInteriorSpheres().Any(s => s.Tokens.Count != 0))
                    throw new InvalidOperationException("Workstation must be unstarted and empty for test: " + desk.VerbId);
                var storage = desk.GetSingleSphereByCategory(SphereCategory.SituationStorage);
                var skillCommand = new ElementStackCreationCommand(test.Press ? "s.stitching.binding" : "s.inks.containment", 1);
                skillCommand.Mutations["skill"] = level - 1;
                skillCommand.Mutations[test.Press ? "sky" : "winter"] = level - 1;
                skillCommand.Mutations[test.Press ? "heart" : "moon"] = level - 1;
                var skill = new TokenCreationCommand(skillCommand, new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                var soul = new TokenCreationCommand(new ElementStackCreationCommand(test.Press ? "xcho" : "xfet", 1), new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                var slots = desk.GetInteriorSpheres();
                // Desk paint admission has been removed, even with a notebook present.
                if (!test.Press && !checkedDeskPaint)
                {
                    var exterior = bindingDesk.Token.Sphere;
                    var blue = new TokenCreationCommand(new ElementStackCreationCommand("paint.blue", 1), new TokenLocation(Vector3.zero, exterior)).Execute(Context.Unknown(), exterior);
                    if (!bindingDesk.GetInteriorSpheres().Single(s => s.Id == "i1").TryAcceptToken(notebook, Context.Unknown()))
                        throw new Exception("Desk rejected notebook during paint admission check");
                    if (bindingDesk.GetInteriorSpheres().Single(s => s.Id == "i2").CanAcceptToken(blue))
                        throw new Exception("Desk still admits paint");
                    checkedDeskPaint = true;
                }
                foreach (var pair in new[] { new { Id = "s", Token = skill }, new { Id = "a", Token = soul }, new { Id = "i1", Token = notebook } })
                    if (!slots.Single(s => s.Id == pair.Id).TryAcceptToken(pair.Token, Context.Unknown()))
                        throw new Exception("Rebinding desk rejected " + pair.Id);
                Token paint = null;
                if (test.Press || binding.Pigment != null)
                {
                    paint = new TokenCreationCommand(new ElementStackCreationCommand(test.Press ? "book.press" : binding.Pigment, 1), new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                    if (!slots.Single(s => s.Id == "i2").TryAcceptToken(paint, Context.Unknown()))
                        throw new Exception("Rebinding desk rejected paint " + color);
                }
                desk.UpdateRecipePrediction();
                var expected = "playernotebook.rebind." + (test.Press ? "design." : "color.") + color + "." + design;
                if (desk.CurrentRecipeId != expected) throw new Exception("Wrong rebinding prediction: " + desk.CurrentRecipeId);
                desk.TryStart();
                for (int step = 0; step < 30 && desk.StateIdentifier != StateEnum.Complete; step++) desk.ExecuteHeartbeat(10, 10);
                if (desk.StateIdentifier != StateEnum.Complete) throw new Exception("Rebinding did not complete");
                var output = desk.GetInteriorSpheres().SelectMany(s => s.GetTokens()).Where(t => PlayerNotebook.IsNotebook(t.Payload)).ToArray();
                if (output.Length != 1 || output[0] != notebook || card.Id != stackId || card.Quantity != 1 ||
                    card.EntityId != "playernotebook." + color + "." + design ||
                    (paint != null && (test.Press ? paint.Payload.Defunct || paint.Payload.EntityId != "book.press" : paint.Payload.Defunct || paint.Payload.EntityId != binding.Residue)))
                    throw new Exception("Rebinding failed: outputs=" + output.Length + " sameToken=" + (output.Length == 1 && output[0] == notebook) +
                        " stack=" + card.Id + " entity=" + card.EntityId + " quantity=" + card.Quantity +
                        " optional=" + paint?.Payload.EntityId + " defunct=" + paint?.Payload.Defunct);
                var saved = Newtonsoft.Json.Linq.JObject.Parse(card.GetIllumination(NotebookCodec.IlluminationKey));
                if ((string)saved["Color"] != color || (string)saved["Design"] != design) throw new Exception("Binding metadata mismatch");
                saved["Color"] = original["Color"].DeepClone(); saved["Design"] = original["Design"].DeepClone();
                if (!Newtonsoft.Json.Linq.JToken.DeepEquals(original, saved)) throw new Exception("Rebinding changed document contents");
                var cover = ResourcesManager.GetSpriteForBookCover(card.Icon);
                var spine = ResourcesManager.GetSpriteForBookSpine(card.Icon);
                if (cover == null || spine == null || ResourcesManager.SpriteIsPlaceholder(cover) || spine.name == "_x_" ||
                    cover.rect.width != 128 || cover.rect.height != 192 || spine.rect.width != 24 || spine.rect.height != 192)
                    throw new Exception("Missing binding artwork: " + card.Icon);
                Debug.Log("Notebook rebind PASS: " + color + "/" + design + " skill=" + level + " station=" + desk.VerbId + " press=" + test.Press + " pigment=" + binding.Pigment + " remainder=" + paint?.Payload.EntityId + " same stack/document/entries/bookmark; pigment consumed, press retained");
                desk.Conclude();
                for (int step = 0; step < 5 && desk.StateIdentifier != StateEnum.Unstarted; step++) desk.ExecuteHeartbeat(0, 0);
            }
            status = "PASS " + cases.Count + " native rebinding executions; all pigments/plain/decorated preserve writing. Save and cold reload next.";
        }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }

    private void TestCrafting()
    {
        try
        {
            NotebookWindow.Instance.Close();
            var axe = Watchman.Get<HornedAxe>();
            var compendium = Watchman.Get<Compendium>();
            var desk = axe.GetRegisteredSituations().Single(s => s.VerbId == "library.desk.reading.consider");
            foreach (var level in new[] { 1, 4 })
            {
                if (desk.StateIdentifier != StateEnum.Unstarted || desk.GetInteriorSpheres().Any(s => s.Tokens.Count != 0))
                    throw new InvalidOperationException("Reading desk must be unstarted and empty for test");
                var storage = desk.GetSingleSphereByCategory(SphereCategory.SituationStorage);
                var skillCommand = new ElementStackCreationCommand("s.stitching.binding", 1);
                skillCommand.Mutations["skill"] = level - 1;
                skillCommand.Mutations["sky"] = level - 1;
                skillCommand.Mutations["heart"] = level - 1;
                var skill = new TokenCreationCommand(skillCommand, new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                var paper = new TokenCreationCommand(new ElementStackCreationCommand("quire.paper", 1), new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                var soul = new TokenCreationCommand(new ElementStackCreationCommand("xcho", 1), new TokenLocation(Vector3.zero, storage)).Execute(Context.Unknown(), storage);
                var slots = desk.GetInteriorSpheres();
                foreach (var pair in new[] { new { Id = "s", Token = skill }, new { Id = "i1", Token = paper }, new { Id = "a", Token = soul } })
                {
                    if (!slots.Single(s => s.Id == pair.Id).TryAcceptToken(pair.Token, Context.Unknown()))
                        throw new Exception("Desk rejected " + pair.Id + " at skill " + level);
                }
                desk.UpdateRecipePrediction();
                var expected = "playernotebook.craft.brown." + (level < 4 ? "plain" : "decorated");
                if (desk.CurrentRecipeId != expected) throw new Exception("Wrong desk prediction: " + desk.CurrentRecipeId);
                Debug.Log("Notebook craft prediction PASS level=" + level + " recipe=" + desk.CurrentRecipeId);
                desk.TryStart();
                for (int step = 0; step < 30 && desk.StateIdentifier != StateEnum.Complete; step++) desk.ExecuteHeartbeat(10, 10);
                if (desk.StateIdentifier != StateEnum.Complete) throw new Exception("Craft did not complete: " + desk.StateIdentifier);
                var output = desk.GetInteriorSpheres().SelectMany(s => s.GetTokens()).Select(t => t.Payload)
                    .OfType<ElementStack>().Single(s => PlayerNotebook.IsNotebook(s));
                var document = NotebookCodec.Read(output.GetIllumination(NotebookCodec.IlluminationKey), output.Id);
                if (document.Title != "" || document.Entries.Count != 0 || output.Quantity != 1 || !paper.Payload.Defunct)
                    throw new Exception("Output not blank/single or paper not consumed");
                Debug.Log("Notebook craft completion PASS level=" + level + " output=" + output.EntityId + " id=" + document.Id + " paper consumed");
                desk.Conclude();
                for (int step = 0; step < 5 && desk.StateIdentifier != StateEnum.Unstarted; step++) desk.ExecuteHeartbeat(0, 0);
            }
            status = "PASS real Reading desk: skill 1 plain, skill 4 decorated; paper consumed; blank independent outputs.";
        }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }
    private void InspectPlacement()
    {
        try
        {
            var axe = Watchman.Get<HornedAxe>();
            var compendium = Watchman.Get<Compendium>();
            var starter = compendium.GetEntityById<Recipe>("setup.librarian.bookcase");
            Debug.Log("Notebook starter merged effects: " + JsonConvert.SerializeObject(starter.Effects));
            foreach (var id in new[] { "playernotebook.brown.plain", "playernotebook.brown.decorated" })
            {
                var element = compendium.GetEntityById<Element>(id);
                var cover = ResourcesManager.GetSpriteForBookCover(element.Icon);
                var spine = ResourcesManager.GetSpriteForBookSpine(element.Icon);
                Debug.Log("Notebook art: " + id + " icon=" + element.Icon + " cover=" + cover.name + " " + cover.texture.width + "x" + cover.texture.height + " spine=" + spine.name + " " + spine.texture.width + "x" + spine.texture.height);
            }
            var images = (System.Collections.IDictionary)AccessTools.Field(typeof(SecretHistories.Infrastructure.Modding.ModManager), "_images").GetValue(Watchman.Get<SecretHistories.Infrastructure.Modding.ModManager>());
            foreach (var key in images.Keys) Debug.Log("Notebook loaded image key: " + key);
            var cards = axe.GetSpheres().SelectMany(s => s.GetTokens()).Where(t => PlayerNotebook.IsNotebook(t.Payload)).ToArray();
            Debug.Log("Notebook physical count: " + cards.Length);
            foreach (var notebook in cards)
                Debug.Log("Notebook movement state: id=" + notebook.Payload.Id + " path=" + notebook.Location.AtSpherePath +
                    " position=" + notebook.TokenRectTransform.position + " draggable=" + notebook.CanBeDraggedInThisState() +
                    " payload=" + ((ElementStack)notebook.Payload).GetIllumination(NotebookCodec.IlluminationKey));
            var card = cards.First();
            Debug.Log("Notebook physical identity: " + card.Payload.Id + " icon=" + ((ElementStack)card.Payload).Icon + " manifestation=" + card.GetManifestation().GetType().Name + " path=" + card.Location.AtSpherePath);
            foreach (var t in card.Sphere.GetTokens()) Debug.Log("Notebook shelf occupant: " + t.Payload.EntityId + " id=" + t.Payload.Id);
            foreach (var shelf in axe.GetExteriorSpheres().OfType<ShelfSpaceSphere>().Where(s => s.GetAbsolutePath().ToString().Contains("lodge")))
                Debug.Log("Notebook lodge shelf " + shelf.GetAbsolutePath() + " occupied=" + shelf.Tokens.Count +
                    " match=" + shelf.GetMatchForTokenPayload(card.Payload).MatchType +
                    " mix=" + shelf.WillMixBooksAndThings(card) + " accepts=" + shelf.CanAcceptToken(card));
            foreach (var desk in axe.GetRegisteredSituations().Where(s => s.VerbId.StartsWith("library.desk.")))
            {
                Debug.Log("Notebook desk " + desk.VerbId + " state=" + desk.StateIdentifier + " path=" + desk.Token.Location.AtSpherePath);
                foreach (var slot in desk.GetInteriorSpheres())
                    Debug.Log("Notebook slot " + slot.GetAbsolutePath() + " spec=" + JsonConvert.SerializeObject(slot.GoverningSphereSpec));
            }
            NotebookWindow.Instance.Close();
            var position = card.TokenRectTransform.position;
            Watchman.Get<CamOperator>().PointCameraAtTableLevel_ZoomClose(new Vector2(position.x, position.y), 0.5f, null);
            status = "Located " + card.Payload.Id + "; placement diagnostics in Player.log";
        }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }
    private void OpenNext()
    {
        try
        {
            var cards = Watchman.Get<HornedAxe>().GetSpheres().SelectMany(s => s.GetTokens())
                .Select(t => t.Payload).OfType<ElementStack>().Where(s => PlayerNotebook.IsNotebook(s)).ToArray();
            if (cards.Length == 0) { status = "No notebook cards in this game."; return; }
            var card = cards[(next++) % cards.Length];
            NotebookWindow.Instance.Open(card);
            status = "Opened " + card.Id + " (" + cards.Length + " notebooks)";
        }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }
    private async void Save()
    {
        try { status = await Watchman.Get<GameGateway>().TryNamedSave("NotebookMovement") ? "Saved isolated NotebookMovement.json" : "Save refused"; }
        catch (Exception ex) { status = ex.ToString(); Debug.LogError(status); }
    }
}

[HarmonyPatch(typeof(Token), nameof(Token.OnBeginDrag))]
internal static class NotebookTestDragProbe
{
    static void Prefix(Token __instance, PointerEventData eventData)
    {
        Debug.Log("Notebook drag input: entity=" + __instance.Payload.EntityId + " button=" + eventData.button +
            " draggable=" + __instance.CanBeDraggedInThisState() + " position=" + eventData.position);
    }
}

[HarmonyPatch(typeof(Heart), nameof(Heart.Beat))]
internal static class NotebookTestBeatProbe
{
    internal static int Count;
    internal static double Seconds;
    static void Postfix(float seconds) { Count++; Seconds += seconds; }
}
#endif
