using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PlayerNotebooks;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A native Canvas editor. The document and native save contract are unchanged.
public sealed class NotebookWindow : MonoBehaviour
{
    public static NotebookWindow Instance { get; private set; }
    public bool IsOpen => root != null && root.activeSelf;
    private ElementStack stack;
    private NotebookDocument document;
    private int selected = -1;
    private bool confirmDelete;
    private LocalNexus lockedNexus;
    private GameObject root, editor, empty;
    private CanvasScaler scaler;
    private TMP_FontAsset bodyFont, headingFont;
    private Material panelMaterial;
    private NotebookTextField titleField, headingField, bodyField;
    private TextMeshProUGUI countLabel, entryLabel, deleteLabel, sizeLabel;
    private Button upButton, downButton, deleteButton;
    private RectTransform listContent;
    private ScrollRect listScroll;
    private readonly List<Button> entryButtons = new List<Button>();
    private float bodySize = 26;
    private readonly Color leather = Hex("#3b302b"), paper = Hex("#f5ecd8"), index = Hex("#e5d8bb");
    private readonly Color ink = Hex("#342f27"), muted = Hex("#786b52"), gold = Hex("#b49a67");
    private readonly Color teal = Hex("#334f49"), light = Hex("#fff6e0");

    void Awake() { Instance = this; }

    public void Open(ElementStack card)
    {
        Close();
        stack = card;
        document = null;
        string error = null;
        try
        {
            document = NotebookCodec.Read(card.GetIllumination(NotebookCodec.IlluminationKey), card.Id);
            Commit(); // Creates identity, never an entry.
        }
        catch (Exception ex) { error = "This notebook cannot be edited. Its original data has been preserved.\n\n" + ex.Message; }
        Watchman.Get<AureateNotifier>()?.HideAll();
        try { Build(error); }
        catch
        {
            Close();
            throw;
        }
        var nexus = Watchman.Get<LocalNexus>();
        if (nexus != null && !nexus.PlayerInputDisabled())
        {
            lockedNexus = nexus;
            nexus.DisablePlayerInput(0);
        }
        if (document != null)
        {
            titleField.LoadText(document.Title);
            RebuildIndex();
            SelectEntry(document.OpeningEntryIndex);
            listScroll.verticalNormalizedPosition = document.Entries.Count > 1
                ? 1f - (float)selected / (document.Entries.Count - 1) : 1f;
        }
    }

    private void Commit()
    {
        if (document == null || stack == null || stack.Defunct) return;
        var json = NotebookCodec.Write(document);
        stack.SetIllumination(NotebookCodec.IlluminationKey, json);
    }

    public void Close()
    {
        Commit();
        if (root != null)
        {
            if (EventSystem.current?.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(root.transform))
                EventSystem.current.SetSelectedGameObject(null);
            // Hide immediately: Destroy is deferred until end of frame.
            root.SetActive(false);
            Destroy(root);
        }
        root = null;
        if (lockedNexus != null) lockedNexus.EnablePlayerInput();
        lockedNexus = null;
        stack = null; document = null; selected = -1;
        entryButtons.Clear();
    }

    void Update()
    {
        if (!IsOpen) return;
        if (stack != null && (stack.Defunct || stack.GetToken() == null)) { Close(); return; }
        // Height controls comfortable reading size on ultrawide screens; width also
        // limits the panel so the complete editor remains visible on narrower displays.
        scaler.scaleFactor = Mathf.Min(Screen.height / 1000f, Screen.width / 1480f);
    }
    void OnDisable() { Close(); }
    void OnApplicationQuit() { Commit(); }
    void OnDestroy()
    {
        Close();
        if (bodyFont != null) { Destroy(bodyFont.material); Destroy(bodyFont); }
        if (headingFont != null) { Destroy(headingFont.material); Destroy(headingFont); }
        if (panelMaterial != null) Destroy(panelMaterial);
        if (Instance == this) Instance = null;
    }

    private void ResolveFonts()
    {
        if (bodyFont != null) return;
        var manager = Watchman.Get<LanguageManager>();
        var body = manager?.GetFont(LanguageManager.eFontStyle.BodyText, "latin") ?? TMP_Settings.defaultFontAsset;
        var heading = manager?.GetFont(LanguageManager.eFontStyle.Heading, "latin") ?? body;
        bodyFont = Instantiate(body);
        headingFont = Instantiate(heading);
        // Clone the font assets before adding fallbacks: never change global game fonts.
        foreach (var font in new[] { bodyFont, headingFont })
        {
            font.material = new Material(font.material);
            font.material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            font.fallbackFontAssetTable = new List<TMP_FontAsset>(font.fallbackFontAssetTable ?? new List<TMP_FontAsset>());
            foreach (var script in new[] { "latinplus", "jp", "cjk", "all" })
            {
                var fallback = manager?.GetFont(LanguageManager.eFontStyle.BodyText, script);
                if (fallback != null && fallback != body && fallback != heading && !font.fallbackFontAssetTable.Contains(fallback))
                    font.fallbackFontAssetTable.Add(fallback);
            }
        }
    }

    private void Build(string error)
    {
        ResolveFonts();
        if (panelMaterial == null)
        {
            panelMaterial = new Material(Shader.Find("UI/Default"));
            if (panelMaterial.HasProperty("_Color")) panelMaterial.SetColor("_Color", Color.white);
        }
        root = new GameObject("Player Notebook canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Min(Screen.height / 1000f, Screen.width / 1480f);
        var veil = Box(root.transform, "Modal veil", new Color(0.035f, 0.045f, 0.04f, 0.72f));
        Stretch(veil.rectTransform);
        var book = Box(root.transform, "Notebook", leather);
        book.rectTransform.anchorMin = book.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        book.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        book.rectTransform.sizeDelta = new Vector2(1360, 880);
        book.rectTransform.anchoredPosition = Vector2.zero;
        var outline = book.gameObject.AddComponent<Outline>(); outline.effectColor = gold; outline.effectDistance = new Vector2(2, -2);
        Label(book.transform, "COMMONPLACE BOOK", 18, gold, 40, 28, 950, 28);
        MakeButton(book.transform, "Close ×", 1176, 30, 140, 48, Close, Hex("#55473b"), light);
        Label(book.transform, "Game paused · Save the game before quitting.", 19, Hex("#d0bd94"), 40, 812, 850, 34);
        Label(book.transform, "Text size", 19, Hex("#d0bd94"), 1040, 812, 110, 34);
        MakeButton(book.transform, "A−", 1150, 804, 64, 46, () => ChangeSize(-2), leather, light);
        MakeButton(book.transform, "A+", 1260, 804, 64, 46, () => ChangeSize(2), leather, light);
        sizeLabel = Label(book.transform, "26", 18, gold, 1214, 812, 46, 34);
        sizeLabel.alignment = TextAlignmentOptions.Center;
        if (error != null)
        {
            var errorPaper = Box(book.transform, "Unreadable notebook", paper); Place(errorPaper.rectTransform, 20, 140, 1320, 656);
            Label(errorPaper.transform, error, 26, ink, 56, 60, 1208, 500);
            return;
        }
        titleField = MakeInput(book.transform, "Notebook title", "Untitled Notebook", 38, light, 40, 62, 1015, 60, false, headingFont);
        titleField.onValueChanged.AddListener(value => { document.Title = value; Commit(); });
        Rule(book.transform, 40, 116, 1015, gold);
        var sidebar = Box(book.transform, "Entry index", index); Place(sidebar.rectTransform, 20, 140, 300, 656);
        Label(sidebar.transform, "ENTRIES", 20, muted, 26, 28, 220, 30);
        countLabel = Label(sidebar.transform, "0", 20, muted, 254, 28, 30, 30);
        BuildEntryScroll(sidebar.transform);
        MakeButton(sidebar.transform, "+  New entry", 20, 584, 260, 50, NewEntry, teal, light);
        var page = Box(book.transform, "Writing page", paper); Place(page.rectTransform, 320, 140, 1020, 656);
        var gutter = Box(page.transform, "Gutter", Hex("#e7dcc3")); Place(gutter.rectTransform, 0, 0, 12, 656);
        editor = Rect(page.transform, "Selected entry").gameObject; Stretch((RectTransform)editor.transform);
        entryLabel = Label(editor.transform, "", 18, muted, 56, 28, 900, 30);
        headingField = MakeInput(editor.transform, "Entry heading", "Heading (optional)", 34, ink, 56, 64, 900, 60, false, headingFont);
        headingField.onValueChanged.AddListener(value =>
        {
            if (selected < 0) return;
            document.Entries[selected].Heading = value; Commit(); UpdateRow(selected);
        });
        Rule(editor.transform, 56, 124, 900, Hex("#cbbd9c"));
        bodyField = MakeInput(editor.transform, "Entry body", "Write here…", bodySize, ink, 56, 150, 944, 430, true, bodyFont);
        bodyField.onValueChanged.AddListener(value =>
        {
            if (selected < 0) return;
            document.Entries[selected].Body = value; Commit(); UpdateRow(selected);
        });
        upButton = MakeButton(editor.transform, "Move up", 44, 596, 130, 42, () => Move(-1), paper, muted);
        downButton = MakeButton(editor.transform, "Move down", 178, 596, 150, 42, () => Move(1), paper, muted);
        deleteButton = MakeButton(editor.transform, "Delete entry", 790, 596, 190, 42, DeleteEntry, paper, Hex("#89544b"));
        deleteLabel = deleteButton.GetComponentInChildren<TextMeshProUGUI>();
        empty = Rect(page.transform, "Blank notebook invitation").gameObject; Stretch((RectTransform)empty.transform);
        var welcome = Label(empty.transform, "A place for your own words.", 36, ink, 60, 216, 900, 64, headingFont);
        welcome.alignment = TextAlignmentOptions.Center;
        var hint = Label(empty.transform, "Keep observations, questions, or whatever you choose.", 24, muted, 60, 290, 900, 50);
        hint.alignment = TextAlignmentOptions.Center;
        MakeButton(empty.transform, "Write your first entry", 340, 370, 340, 56, NewEntry, teal, light);
        ChangeSize(0);
    }

    private void BuildEntryScroll(Transform parent)
    {
        var area = Rect(parent, "Entries scroll"); Place(area, 20, 80, 260, 480);
        listScroll = area.gameObject.AddComponent<ScrollRect>();
        var viewport = Box(area, "Viewport", index); Stretch(viewport.rectTransform);
        viewport.gameObject.AddComponent<RectMask2D>();
        listContent = Rect(viewport.transform, "Entries");
        listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = Vector2.one;
        listContent.pivot = new Vector2(0.5f, 1); listContent.sizeDelta = Vector2.zero; listContent.anchoredPosition = Vector2.zero;
        listScroll.viewport = viewport.rectTransform; listScroll.content = listContent;
        listScroll.horizontal = false; listScroll.vertical = true;
        listScroll.movementType = ScrollRect.MovementType.Clamped; listScroll.scrollSensitivity = 36;
    }

    private void RebuildIndex()
    {
        foreach (var button in entryButtons) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
        entryButtons.Clear();
        for (var i = 0; i < document.Entries.Count; i++)
        {
            var entryIndex = i;
            var button = MakeButton(listContent, "", 0, i * 88, 260, 78, () => SelectEntry(entryIndex), index, ink);
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            Place(text.rectTransform, 18, 2, 226, 44); text.font = headingFont; text.fontSize = 24;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Label(button.transform, "", 17, muted, 18, 44, 226, 24).name = "Preview";
            entryButtons.Add(button);
            UpdateRow(i);
        }
        listContent.sizeDelta = new Vector2(0, Mathf.Max(480, document.Entries.Count * 88));
        countLabel.text = document.Entries.Count.ToString();
    }

    private void UpdateRow(int i)
    {
        var entry = document.Entries[i]; var button = entryButtons[i];
        var texts = button.GetComponentsInChildren<TextMeshProUGUI>();
        texts[0].text = string.IsNullOrWhiteSpace(entry.Heading) ? "Untitled entry" : entry.Heading;
        var preview = entry.Body.Replace('\r', ' ').Replace('\n', ' ');
        texts[1].text = string.IsNullOrWhiteSpace(preview) ? "Blank entry" : preview.Substring(0, Math.Min(64, preview.Length));
        var active = i == selected;
        button.GetComponent<Image>().color = active ? teal : index;
        texts[0].color = active ? light : ink; texts[1].color = active ? Hex("#d3dece") : muted;
        foreach (var text in texts) { text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Ellipsis; }
    }

    private void SelectEntry(int i)
    {
        selected = i; confirmDelete = false;
        var entryId = i >= 0 ? document.Entries[i].Id : null;
        if (document.LastSelectedEntryId != entryId)
        {
            document.LastSelectedEntryId = entryId;
            Commit();
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        editor.SetActive(i >= 0); empty.SetActive(i < 0);
        if (i >= 0)
        {
            headingField.LoadText(document.Entries[i].Heading);
            bodyField.LoadText(document.Entries[i].Body);
            entryLabel.text = "ENTRY " + (i + 1).ToString("00");
            upButton.interactable = i > 0; downButton.interactable = i < document.Entries.Count - 1;
            deleteLabel.text = "Delete entry";
        }
        for (var row = 0; row < entryButtons.Count; row++) UpdateRow(row);
    }

    private void NewEntry()
    {
        document.Entries.Add(new NotebookEntry()); Commit();
        RebuildIndex(); SelectEntry(document.Entries.Count - 1);
        listScroll.verticalNormalizedPosition = 0;
        bodyField.ActivateInputField();
    }
    private void Move(int direction)
    {
        var target = selected + direction;
        if (target < 0 || target >= document.Entries.Count) return;
        document.MoveEntry(selected, target); Commit();
        RebuildIndex(); SelectEntry(target);
    }
    private void DeleteEntry()
    {
        if (!confirmDelete) { confirmDelete = true; deleteLabel.text = "Confirm delete?"; return; }
        document.Entries.RemoveAt(selected); Commit();
        var next = Math.Min(selected, document.Entries.Count - 1);
        RebuildIndex(); SelectEntry(next);
    }
    private void ChangeSize(float delta)
    {
        bodySize = Mathf.Clamp(bodySize + delta, 22, 36);
        if (bodyField != null) { bodyField.textComponent.fontSize = bodySize; bodyField.ForceLabelUpdate(); }
        if (sizeLabel != null) sizeLabel.text = bodySize.ToString("0");
    }

    private NotebookTextField MakeInput(Transform parent, string name, string hint, float size, Color color,
        float x, float y, float width, float height, bool multiline, TMP_FontAsset font)
    {
        var background = Box(parent, name, new Color(1, 1, 1, 0.015f)); Place(background.rectTransform, x, y, width, height);
        var field = background.gameObject.AddComponent<NotebookTextField>();
        // TMP creates its caret and hooks up scrolling in OnEnable, which first
        // ran before this runtime-created field had a text component. Enable it
        // again only after all references are assigned, as with a configured prefab.
        field.enabled = false;
        field.transition = Selectable.Transition.None; field.targetGraphic = background;
        var viewport = Rect(background.transform, "Text viewport"); Stretch(viewport, 2, 4, multiline ? 32 : 2, 4);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Label(viewport, "", size, color, 0, 0, 0, 0, font); Stretch(text.rectTransform);
        text.enableWordWrapping = multiline; text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(viewport, hint, size, new Color(color.r, color.g, color.b, 0.5f), 0, 0, 0, 0, font);
        Stretch(placeholder.rectTransform); placeholder.alignment = text.alignment;
        field.textViewport = viewport; field.textComponent = text; field.placeholder = placeholder;
        field.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        field.richText = false; field.isRichTextEditingAllowed = true;
        field.characterLimit = 0; field.onFocusSelectAll = false;
        field.resetOnDeActivation = false; field.restoreOriginalTextOnEscape = false;
        field.customCaretColor = true; field.caretColor = color; field.caretWidth = 2;
        field.selectionColor = new Color(0.15f, 0.32f, 0.27f, 0.8f);
        if (multiline)
        {
            var track = Box(background.transform, "Scrollbar", Hex("#e2d6bb")); Place(track.rectTransform, width - 16, 4, 8, height - 8);
            var thumb = Box(track.transform, "Handle", Hex("#9c8d6b")); Stretch(thumb.rectTransform);
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = thumb.rectTransform;
            bar.targetGraphic = thumb; bar.direction = Scrollbar.Direction.TopToBottom;
            bar.transition = Selectable.Transition.None;
            field.verticalScrollbar = bar; field.scrollSensitivity = 30;
        }
        field.enabled = true;
        // Keep the native caret/selection mesh on the same neutral UI material
        // as the page, rather than the game's shared default graphic material.
        var caret = viewport.Find("Caret")?.GetComponent<Graphic>();
        if (caret != null) { caret.material = panelMaterial; caret.raycastTarget = false; }
        return field;
    }

    private Button MakeButton(Transform parent, string text, float x, float y, float w, float h,
        UnityAction action, Color background, Color foreground)
    {
        var image = Box(parent, text == "" ? "Entry" : text, background); Place(image.rectTransform, x, y, w, h);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f); colors.disabledColor = new Color(1, 1, 1, 0.35f);
        button.colors = colors; button.onClick.AddListener(action);
        var label = Label(image.transform, text, 23, foreground, 6, 0, w - 12, h); label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private TextMeshProUGUI Label(Transform parent, string value, float size, Color color,
        float x, float y, float w, float h, TMP_FontAsset font = null)
    {
        var rect = Rect(parent, "Text"); Place(rect, x, y, w, h);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font ?? bodyFont; text.fontSize = size; text.color = color;
        text.richText = false; text.raycastTarget = false; text.text = value;
        text.enableWordWrapping = true; text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }
    private void Rule(Transform parent, float x, float y, float width, Color color)
    { var rule = Box(parent, "Rule", color); Place(rule.rectTransform, x, y, width, 1); rule.raycastTarget = false; }
    private static RectTransform Rect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private Image Box(Transform parent, string name, Color color)
    { var image = Rect(parent, name).gameObject.AddComponent<Image>(); image.material = panelMaterial; image.color = color; return image; }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }
    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }
}

// TMP's native field supports document navigation and selection, but has no undo stack.
// History belongs to this field and is cleared whenever a different entry is loaded.
public sealed class NotebookTextField : TMP_InputField
{
    private readonly List<string> undo = new List<string>(), redo = new List<string>();
    private string prior = "";
    private float lastEdit = -10;
    private bool applying;
    private bool revealCaret;
    internal void RevealCaretAfterKey() { revealCaret = true; }
    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (!revealCaret || !isFocused || lineType == LineType.SingleLine) return;
        revealCaret = false;
        // A masked off-screen caret can be culled before TMP's caret geometry
        // gets a chance to scroll its viewport. Reveal it from the laid-out text.
        textComponent.ForceMeshUpdate();
        var info = textComponent.textInfo;
        if (info.characterCount == 0) return;
        var character = 0;
        for (var i = 1; i < info.characterCount; i++)
        {
            if (info.characterInfo[i].index > selectionStringFocusPosition) break;
            character = i;
        }
        var line = info.lineInfo[info.characterInfo[character].lineNumber];
        var position = textComponent.rectTransform.anchoredPosition;
        var viewport = textViewport.rect;
        if (line.ascender + position.y > viewport.yMax) position.y = viewport.yMax - line.ascender;
        if (line.descender + position.y < viewport.yMin) position.y = viewport.yMin - line.descender;
        var maximum = Mathf.Max(0, textComponent.preferredHeight - viewport.height);
        position.y = Mathf.Clamp(position.y, 0, maximum);
        textComponent.rectTransform.anchoredPosition = position;
        if (verticalScrollbar != null) verticalScrollbar.SetValueWithoutNotify(maximum > 0 ? position.y / maximum : 0);
        ForceLabelUpdate();
#if NOTEBOOK_TESTING
        Debug.Log("Notebook navigation: focus=" + selectionStringFocusPosition + " chars=" + text.Length + " scroll=" + position.y);
#endif
    }
    protected override void Awake()
    {
        base.Awake();
        onValueChanged.AddListener(value =>
        {
            if (applying) return;
            var oneCharacter = Math.Abs(value.Length - prior.Length) == 1;
            if (undo.Count == 0 || !oneCharacter || Time.unscaledTime - lastEdit > 0.65f)
            {
                undo.Add(prior);
                if (undo.Count > 64) undo.RemoveAt(0);
            }
            redo.Clear(); prior = value; lastEdit = Time.unscaledTime;
        });
    }
    internal void LoadText(string value)
    {
        SetTextWithoutNotify(value); prior = value; undo.Clear(); redo.Clear(); lastEdit = -10;
        stringPosition = selectionStringFocusPosition = 0;
        if (textComponent != null) textComponent.rectTransform.anchoredPosition = Vector2.zero;
        ForceLabelUpdate();
    }
    internal bool HandleChord(Event evt)
    {
        if (!evt.control || evt.alt || (evt.keyCode != KeyCode.Z && evt.keyCode != KeyCode.Y)) return false;
        var isRedo = evt.keyCode == KeyCode.Y || evt.shift;
        var source = isRedo ? redo : undo; var target = isRedo ? undo : redo;
        if (source.Count > 0)
        {
            target.Add(text); var value = source[source.Count - 1]; source.RemoveAt(source.Count - 1);
            applying = true;
            try { text = value; prior = value; stringPosition = selectionStringFocusPosition = value.Length; ForceLabelUpdate(); }
            finally { applying = false; lastEdit = -10; }
        }
        return true;
    }
}

[HarmonyPatch(typeof(TMP_InputField), "KeyPressed")]
internal static class NotebookUndoPatch
{
    static void Postfix(TMP_InputField __instance)
    {
        if (__instance is NotebookTextField field) field.RevealCaretAfterKey();
    }
    static bool Prefix(TMP_InputField __instance, Event evt)
    {
        if (!(__instance is NotebookTextField field) || !field.HandleChord(evt)) return true;
        return false; // default EditState.Continue; other game's fields are unaffected.
    }
}
