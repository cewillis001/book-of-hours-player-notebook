# Technical investigation — 2026-10-31

## Evidence and confidence

Installed game: `D:\SteamLibrary\steamapps\common\Book of Hours`.
`version.txt` reports `2026.1.f.3`. Installed `bh_Data/StreamingAssets/MODDING_README.txt`
is the primary packaging reference. Read-only inspection and decompilation were performed
locally with ilspycmd 9.1.0.7988. Proprietary sources are confined to ignored
`scratch/decompiled`; do not distribute them. All findings below concern this version.

The [FEATHER announcement](https://store.steampowered.com/news/app/1028310/view/535502917902273119)
identifies the release, but its article body was unavailable to the web reader.
The [linked examples](https://github.com/chepash/book_of_hours_mods) use BepInEx;
their README's claim that official support only handles JSON is contradicted by the
installed loader and installed modding README. Use the installed implementation.
The [Catalogue](https://github.com/SunsetFi/bookofhours-catalog) is a REST-based reference;
no automatic recipe-discovery functionality is included in this mod.

## Confirmed by installed implementation

| Topic | Owning source/type | Finding |
|---|---|---|
| Mod folders | MODDING_README.txt | synopsis.json, content/, images/, dll/ supported; new image paths include books and things. |
| Entry point | Mod.TryInitialiseAssembly | Synopsis name stripped to ASCII letters/digits/underscores; global class and DLL use that name. Public static Initialise() is supported. Loader also accepts one parameter. |
| DLL permission | ModManager.Safety | Enabled gatekeeper identified by exact name GHIRBI and official warning description; do not bypass or fabricate it. |
| Lifecycle | ModManager and modding README | DLL code loads before JSON; activation changes require restart. |
| Stable native identity | Element.DefaultUniqueTokenId; ElementStackCreationCommand.Execute | New IDs use FucineRoot's incrementing identity; saved creation commands reuse nonempty Id. |
| Save payload | ElementStack.Illuminations; ElementStackCreationCommand | Dictionary of arbitrary strings marked Encaust, restored by SetIllumination. Native storage is viable. |
| Dynamic title | ElementStack.Label | Virtual getter returns shared Element.Label by default; Harmony postfix replaces only notebook results. No shared entity mutation. |
| Card interaction | Token.OnPointerClick | Handles click/double-click and payload interaction. Prototype intercepts left double-click before vanilla action on that click. First-click behavior still needs testing. |
| Merging | ElementStack.CanMergeWith | Ordinary same-type cards can merge and do not compare illumination content. Notebook merge prefix blocks either direction. |
| Copying | Token.CalveToken | Splitting a quantity stack copies illuminations into a new native stack. Codec detects changed owner ID and rekeys notebook and entry IDs while copying text. Ordinary quantity-one notebooks cannot split through this method. Other duplication paths are unverified. |
| Simulation/input | Heart.FixedUpdate; LocalNexus.DisablePlayerInput | Editor suspends heartbeat and locks input. Live Normal/Fast/Paused checks pass; prior speed and input ownership are preserved. |
| Save resilience | GamePersistenceProvider | Normal and synchronous paths use File.WriteAllText; ordinary saves also generate timestamp backups (up to 14). User clarified base-game resilience is sufficient: do not add separate atomic-write machinery. |
| Recipe selection | Compendium.GetValidRecipesForUnstartedVerb/GetFirstMatchingRecipe | Descending Priority; returns first matching recipe. GetMatchingRecipesUntilBlocked stops at a matching Blocks recipe. Avoid ties and unintended matching sets. |

The installed engine's real `Encaustery<ElementStackCreationCommand>` was executed in an
offline test: native ElementStack -> creation command -> JSON -> recovered notebook.
Native stack ID, Unicode title and multiline entry survived. This demonstrates extraction
and serialization, **not** complete in-game creation/save/quit/reload or UI behavior.

## Persistence decision

One namespaced illumination contains schema version, UUID, native owner ID, raw title,
binding values, and ordered entries. No title keys, per-save filename heuristics, global
registries, or persistent caches. A freshly created card is initialized after its native
creation command has restored existing illuminations. Load keeps IDs; a copied payload
with a changed native owner gets new IDs. Retired/deleted cards cannot be looked up by
title or donate their content to new cards. IDs use random GUIDs (practical collision
resistance); a global collision audit is not implemented.

Model and native checks pass. Live native save, quit and cold load preserved two independent
notebooks, including exact Unicode and multiline text. Deletion/duplication paths still need
runtime coverage. Binding fields are initialized from each new notebook's entity ID;
saved binding data is retained.

## Crafting evidence and proposed direction

Verified installed paths:

- `core/recipes/DLC_HOL_manuscripting_write.json`
- `core/recipes/DLC_HOL_prototype_recipes.json`: `_manuscript.write`
- `core/verbs/workstations_library_world.json`: eight desk definitions
- `core/elements/aspecteditems.json`: `quire.paper`
- `core/elements/skills.json`: `s.stitching.binding`, `s.weaving.knotworking`

Vanilla manuscript prototype requires ability, skill, Quire of Paper, ink, absence of
readable/perinculate; consumes paper and links manuscript sophistication stages.
Do not inherit that prototype unchanged: it would require ink and manipulate manuscript
sophistication. Desk Papers slots accept readable or blank items; their With slots accept
ink or tools, not arbitrary fabrics or dyes. A color ingredient cannot simply be added to
a desk recipe without checking a legal input slot.

Stitching & Binding is an actual skill with base Sky 2 / Heart 1 / skill 1. Its skilling-up
triggers add one to skill and the two principles. Weaving & Knotworking is Heart 2 / Moth 1.
Stitching & Binding can match desks accepting Sky or Heart; not every desk accepts it.

Prototype recipe matrix (implemented mod rules, not vanilla game mechanics):

| Workstation | Paper | Skill | Proposed tier gate | Color/material | Design/output |
|---|---|---|---|---|---|
| Existing compatible desk; Reading Room is a candidate | 1 Quire of Paper | Stitching & Binding | skill 1–3 | Brown, no optional material | Plain brown notebook |
| Same | 1 Quire of Paper | Stitching & Binding | skill 4+ | Brown, no optional material | Decorated brown notebook |

Both use a compatible Soul and no ink; writing afterwards remains free. Mutually
exclusive skill ranges remove overlap between the two prototypes. Before shipping,
audit all vanilla matching recipes for each accepted input combination and determine
whether no-ink/paper-only combinations already perform useful crafts. High priority alone
is insufficient evidence of compatibility.

Start with a reduced brown palette rather than invent unrelated dye mappings. Investigate
actual tools/materials and existing sewing workstations before expanding colors. If no
appropriate dye fits supported slots, propose a new material or an explicit recipe-choice
mechanism. Original basic brown cover/spine PNGs are generated by
`scripts/render-bindings.ps1`; their in-game loading is under investigation.

## Remaining investigations

P0: finish visible-name surfaces, input/time isolation, long live documents, crafting conflict
audit. FEATHER load, editor Unicode/copy/paste/reorder/delete, native cold reload and two
native Reading Room desk recipe executions have now passed isolated in-game tests.
P1: starter room and new/existing-playthrough placement, suitable shelf acceptance,
material/slot-compatible bindings, legal duplication behavior.
P2: update, uninstall, recovery tests and packaging validation in a clean install.

## Testing and continued implementation

User authorized installation/testing and subsequently official GHIRBI installation/enablement
on 2026-10-31. Official Workshop item 3682369347 is installed and enabled alongside the
prototype. Live log confirms native DLL loading and successful Initialise. Root player files
were backed up to `scratch/player-backup-20261031-100147`; subsequent hash comparisons
found all six save/achievement JSON unchanged (RemoteConfigCache excluded).

Live tests in a redirected copy of the playthrough verified opening a physical notebook,
blank/no-entry initialization, Unicode title and multiline body, a separate blank second
notebook, copy/paste, entry reordering and deletion confirmation. A native named save and
cold restart restored exact text and independent document identities. The editor now has
an opaque panel, readable controls and a native raycast veil. Clicking the underlying Options
button did not activate it. UIController.IsEditingText includes the editor to suppress
native Abort/camera shortcuts. Live heartbeat probes verified zero Heart.Beat calls while
open at Normal, Fast and Paused speeds. Closing resumed native input and the same speed,
with correct positive/zero simulated time. A pre-existing foreign input lock remained locked.
The StageHand.CleanupNonPersistentFucineObjects prefix commits/closes before cards and old
services are cleared. Returning to menu with the editor open closed it synchronously; native
Continue reloaded the isolated playfield with no leftover veil. Full shortcut coverage
remains outstanding. Evidence: scratch/runtime-modal-pass.log.
Native playthrough switching also passed short-note -> long-note -> short-note loading
with the editor open before each transition. Each load retained exact native IDs, raw
illumination payloads and card count (1 long-note card, then 2 short-note cards), closed
the editor and released its input lock. Evidence: scratch/runtime-shelving-switch-pass.log.

Further inspection corrects the earlier priority discussion: `Recipe.Priority` is a
computed getter (1 for ordinary craftable recipes, 0 for hint-only recipes), with no JSON
setter. Prototype code now patches that getter ONLY for the two exact notebook recipe IDs,
returning 2. `GetFirstMatchingRecipe` therefore selects the matching notebook deterministically.
This is an explicit reservation of paper + Stitching & Binding + Soul with no memory/tool/ink,
not a claim that vanilla recipes cannot also match those aspects. Existing Prentice recipes
can match Heart/Sky 5; they remain selectable by removing paper. Paper contributes Nectar,
which does not help those Heart/Sky crafts. Adding ink disables notebook matching;
native vanilla selection can still prefer a non-manuscript craft at higher skill levels.
No vanilla recipe records are edited. Other-mod recipe compatibility remains untested.

Two prototype recipes now implement the proposed brown matrix. Plain uses native `skill:-4`
(below 4) plus the specific binding skill; decorated requires `skill:4`. Both consume one
Quire and output one notebook; optional ink/tool/memory/readable/perinculate inputs exclude
both. Native requirement tests cover levels 1–9 and each forbidden input. Creation hooks
derive binding color/design from the new entity ID only when no saved document exists.
Two native live Reading Room desk executions passed: level 1 predicted/completed plain,
level 4 predicted/completed decorated. Paper was consumed, outputs were quantity one with
empty titles/no entries and distinct IDs. The development driver places native tokens into
real desk slots and accelerates only that situation's heartbeat. A complete manual timed
craft and full vanilla/other-mod conflict matrix remain outstanding.

A live loaded-compendium audit now covers 72 Reading Room prediction cases: skill 1–9,
paper present/absent, and no optional item/ashartine ink/mem.sight/typewriter. Real merged
element aspects and native skill-up mutations were used with native CanExecuteInContext
and GetFirstMatchingRecipe. Paper with no optional input matched exactly one correct
notebook tier. Every excluded combination matched zero notebook recipes and retained the
first matching vanilla recipe. At skill 1, paper plus ink predicted the vanilla manuscript;
at higher levels ordinary Heart/Sky crafts can take precedence, just as without the mod.
This confirms the deliberate paper reservation and exclusion behavior for this matrix;
it does not verify every optional-slot combination, desk, Soul upgrade, or other mod.
Evidence: scratch/runtime-recipe-matrix-pass.log. No notebook text is generated by the audit.

Native checks additionally load the actual compiled production patch classes and apply
them through Harmony to actual engine objects. Dynamic label, whitespace fallback,
bidirectional merge blocking and scoped notebook recipe priority all pass offline.

An opt-in `NOTEBOOK_TESTING` build redirects MetaInfo.PersistentDataPath to an explicitly
marked test folder, keeping normal and named saves/backups isolated. Preparation/launch
script: `scripts/prepare-runtime-test.ps1`. This build is never included in the package;
normal DLL inspection confirms test types are absent. Isolation is verified inside Unity:
Player.log and native saved JSON point to scratch/runtime-test-saves. Confirm the test-root
log entry before entering any game, because the native loader catches initialization errors.
The test build still requires the user's official GHIRBI enablement and never bypasses it.

`starter.json` extends only the effects dictionary of setup.librarian.bookcase with one
plain notebook. The librarian legacy invokes this at ~/library!lodge/?. Loaded compendium
inspection confirms both original Travelling at Night volumes remain and exactly one
notebook is added. Live fresh-game testing invoked native Lodge.Unshroud directly: deferred
LateChamberlainData enacted the setup, producing exactly one blank notebook on the Lodge
shelf before any skill acquisition. Both original volumes and the three unidentified books
remained. Physical double-click opened the blank editor. This verifies room setup/usability;
the full opening beach-to-Lodge sequence has not been replayed manually.

Important native inheritance behavior: Element.InheritFrom unconditionally replaces
ManifestationType with the parent's value. Inheriting `_` therefore turned the intended book
into a Thing and caused missing-image triangles. The notebook now has direct thing:1 and
Book manifestation, with no parent/readable aspect. Runtime reports BookManifestation,
both original cover/spine resources resolve, the brown spine renders, and Lodge shelf
acceptance changed from false to true. BookManifestation.UpdateSize uses texture pixels
times 0.12 for physical dimensions. Reducing the original art to 128x192 covers and 24x192
spines brought notebook height into line with adjacent shelf books, verified after restart.
Native right-click moved a written notebook from Lodge shelf to the player's portage;
native ShelfSpaceSphere.TryAcceptToken then accepted it onto the Watchman's Tower desk
shelf. Raw text/identity and the two-card count stayed unchanged; live CanMergeWith rejected
both directions. Save/load preserved the moved notebook's data. Physical mouse drag/drop
remains unverified: automation attempts emitted no native OnBeginDrag events, despite
single/right/double-click interaction working. Do not infer successful physical dragging
from the native API shelving test.

Long-entry testing found that a stretching GUILayout.TextArea inside a GUILayout scroll
view remained viewport height and clipped the rest of the text. The editor now measures
content height explicitly and uses GUI.TextArea inside an explicit scroll view. A manually
pasted 31,391-character Unicode body survived native save/cold restart, and wheel/thumb
scrolling reaches all 300 labeled paragraphs. Unity's Windows TextEditor key map lacks
Ctrl+Home/End; the editor supplies those document-navigation chords and Shift selection.
Cursor-follow uses the same prefix-height calculation as wrapping, because Unity's cursor
pixel query undercounted the long Unicode test. Cache focus before consuming events:
Event.Use() can clear the focused-control name for the remainder of that GUI event.

## Native editor replacement

The current editor is a runtime Canvas/TMP layout in NotebookWindow.cs; the IMGUI
viewport notes above describe the earlier prototype. It uses the game's serif font assets,
cloned font materials and a private UI material, with fallback assets for additional scripts.
No font or Unity binaries are distributed. Title/index/optional heading/body remain plain
text. Screen height and width bound the layout; writing size can be adjusted from22 to36.
Existing entries open immediately; an empty notebook offers manual creation without
creating an entry on open. Font size is session-only. Per-field undo/redo is session-only,
limited to64 snapshots and reset on selecting a different entry or closing the notebook.

The optional schema1 field LastSelectedEntryId remembers each notebook's last selected
entry. Selection commits it to the native card, so normal game saves preserve the choice.
Opening resolves the stable entry UUID rather than its array position; reordering keeps
the same entry selected. Missing bookmarks (older saves) or missing entries fall back to
the first remaining entry; empty notebooks stay empty. Copied notebooks remap the bookmark
alongside their independent entry UUIDs. Deleting the selected entry remembers the adjacent
replacement chosen by the editor. This records UI state only, not game knowledge or text.
Isolated live tests on2026-11-01 confirmed selecting a new second entry, closing,
opening two other notebooks, and reopening the original restores its second entry.
The empty notebook stayed empty, and the other notebook kept its own text. Native save
and a full game restart also restored the second entry, with original entry text/IDs/order
preserved. Reorder, copied-ID remapping and deleted/missing-entry fallback passed model
checks; those feature-specific cases have not been replayed through the live UI.

Raw native save payload/schema and creation/merge/label/crafting patches are unchanged.
New Canvas tests have passed normal/fast/paused heartbeat and prior input ownership,
menu teardown, 1280x720 and1920x1080 fit, Unicode manual input and native JSON preservation.
A manually created third notebook/empty heading is independent of the original two.
The31,391-character fixture reaches Paragraph300 by wheel; native save after a temporary
manual edit and undo matches the original complete raw payload. Native TMP document
navigation did not consistently scroll an offscreen caret into view; a scoped reveal
using TMP line geometry passed live Ctrl+Home/End testing without typing. Additional glyph support remains bounded by native font coverage; Japanese
fallback atlas readability warnings were observed, although the tested characters render.

Computer-use captures overexpose both base-game and custom UI colours. Neutral private
materials did not resolve that capture effect. Retain normal palette RGB values rather
than compensating blindly; exact display colour matching is unverified. The user liked
the new interface on their display.



## Missing text cursor — corrected and verified live

User reported no insertion cursor, which obscured that existing text was editable.
Native TMP_InputField.OnEnable creates the Caret and registers text/scrollbar callbacks
only when its text component already exists. Runtime MakeInput added the component to
an active object before assigning that reference. It now disables the field immediately
and re-enables it after configuring text, viewport and scrollbar. The native Caret Graphic
uses the editor's private neutral UI material and does not intercept raycasts. Selection
uses a stronger translucent green; the earlier low-opacity colour was indistinct in the
overexposed capture. Changing the material alone did not resolve that contrast issue.

Isolated live checks on FEATHER2026.1.f.3 at1920x1080 confirmed visible insertion carets
in title, optional heading and body; native blink geometry alternates4/0 vertices. Word
and wrapped multiline selections show a green rectangle. Ctrl+Home/End, wheel scrolling
and scrollbar track paging work. Automated endpoint drags only paged the scrollbar, so
continuous thumb dragging remains unverified. Temporary manual typing and undo in the
31,391-character note followed by native save preserved its entire raw illumination exactly.
Normal production was rebuilt and installed after game exit; all7 current real-player
file hashes still match the pre-test baseline. Existing manual cursor-follow is retained.

## Cursor offset after scrolling — corrected and verified live

The desktop TMP input path can return from LateUpdate before synchronizing the Caret
sibling transform. Our multiline cursor-follow also moves the text transform directly.
Caret and selection geometry use text-local coordinates, so a stale sibling position
displaces both. NotebookTextField now synchronizes that transform before and after
the native LatePreRender Rebuild; native geometry generation can itself move the text.
This is confined to notebook fields and changes neither payloads nor other game inputs.

On 2026-11-02, isolated FEATHER2026.1.f.3 testing at 1920x1080 used the 31,391-character
fixture. Appending past the bottom, Up/Home, Ctrl+Home/End, Page Up, wheel scrolling,
clicking a line, selecting a word and replacing it all kept caret/selection aligned.
The test audit caught three stale transforms before correction; all 17 sampled rendered
states had zero positional difference. Undo and native save preserved all original text,
IDs, entry order and document fields, allowing only the existing last-entry bookmark.
This verifies the corrected path; an unfixed one-line-offset A/B reproduction was not run.

## Rebinding existing notebooks — 2026-11-03

The original level 4 paper recipe creates a new notebook; it never transforms an old one.
Rebinding now has separate color and design operations, per the user's clarification:
color at skill 1+ preserves design; Book Press at skill 4+ toggles design, preserving color.
See rebinding.md for the deterministic matrix and exact ingredients. Paint: Blue/Green/Red
and Book Press are actual installed items. Native `_liquid` fatiguing transforms used
paint to Dregs; this is normal consumption, while the press is returned.

Desk Papers/With slots normally reject notebooks/paint. A scoped GetMatchForTokenPayload
postfix admits only valid quantity-one notebook payloads into desk i1, and these three
paints into desk i2 only while i1 holds such a notebook. Native state/capacity/travel checks
remain. No global readable/blank aspect or vanilla record override is added. New recipes
require notebook-specific aspects and mutually exclusive source designs/ingredients;
their priority reservation applies only to our recipe namespace. Native save serialization
also visits recipes with null IDs; the priority hook explicitly permits that case.

Native xtrigger transformations use ElementStack.ChangeTo, retaining the existing stack,
illumination dictionary and notebook identity. A scoped prefix validates the current
document and synchronizes only binding fields before the native Fundamental change event
refreshes artwork. Malformed/future documents are refused by slot admission and the prefix.
All eight palette/design entities block merging through the existing notebook guard.

Offline checks pass 39 model cases, actual native serialization and compiled Harmony
patch checks (including null-ID priority), 72 recolor and 72 design requirement cases.
Isolated Reading Room testing passed ten native slot/prediction/start/completion/conclude
executions, with unchanged Token/stack/document/entry IDs, quantity, complete text/order
and bookmark at each step. The test accelerates situation heartbeat; it is not a manual
60-second playthrough. Native save after a complete return-to-brown cycle matches all
three original raw notebook payloads exactly, including the two uninvolved notebooks.
Another ten-execution sequence ended blue; native save changed only that Color field
across the three documents. Full exit/restart plus another native save retained all
three saved payloads exactly. Physical double-click on the blue book opened its original
Unicode title and multiline entry. Other desks/mods and physical ingredient drag/drop
remain unverified.

## Recoloring workstation separation — Loggia Prospect, 2026-11-03

User selected the Loggia Prospect and accepted its
room-unlock progression. Its actual verb is library.prospect.loggia, accepting Moon/Rose
Souls and skills. Inks of Containment has Winter 2 / Moon 1 and fits through Moon;
Stitching & Binding has no matching principle. Recoloring now requires Inks of Containment
level 1+ exclusively at the Loggia. The first + slot (i1) holds the notebook, the second
+ slot (i2) accepts pigment natively. An empty second + slot restores brown.

Only valid notebook admission in Loggia i1 is added. The earlier desk paint exception
is removed entirely. Desk notebook admission remains for the level 4+ Stitching & Binding
and Book Press design recipes. Paper creation, binding metadata, transformation and
save schema are unchanged. Named paint mappings and four-color artwork are unchanged.

Offline model/native checks pass, including color/design action scope, rejection of
Stitching & Binding for recoloring, and the existing identity/payload regressions.
Isolated testing passed eight recoloring executions at Loggia with Inks of Containment
and Fet, and two design changes at the Reading Room desk with Stitching & Binding and
Chor. Each step retained native token/stack/document/entry identity, quantity, complete
text/order and bookmark. Paint rejection at a desk with a notebook present passed eight
times. Native save changed only one Color field across the three original notebook
documents; all other fields and both uninvolved notebooks matched exactly.
The fixture creates a native Loggia situation with its actual Verb.Thresholds; this
verifies the workstation API, not room unlocking or a manually timed craft. The native
WindGust window-hide sound warning is nonfatal. A full exit/restart and native save retained all three raw notebook payloads exactly. Physical double-click opened the original title and multiline entry.

## Full pigment palette — 2026-11-03

Installed element definitions contain 16 pigments: nine named paints, Black Sapphire
Wash, Iotic/Xanthotic Essence, Ichor Auroral/Vitreous, Refulgin and Bitterblack Salts.
The last two inherit _material (fatiguing -> Residue); the other fourteen inherit
_liquid (fatiguing -> Dregs). All are natively accepted by Loggia's second + slot.

The original bindings.json catalog defines 17 shades including brown. It drives
generate-bindings.ps1, render-bindings.ps1 and embedded binding validation, preserving
the four existing color IDs. There are 34 quantity-one book variants, 68 cover/spine
PNGs and 68 exclusive color/design recipes. Named paints follow named shades; special
pigment interpretations are recorded in rebinding.md. No game artwork is distributed.

Offline checks pass 91 model cases, exact coverage of the installed pigment set,
306 recolor and 306 design requirement cases, and native serialization/compiled Harmony
checks. The expanded runtime test checks every shade in both designs, exact native
pigment remnants, cover/spine loading and dimensions, and complete document invariance
apart from binding fields. All 37 native executions passed, including every pigment in
both designs, two press operations and a high-level recolor. Native save retained all
three documents with only one Color change. A targeted Auroral craft and full exit/restart
also passed. Native save after reload retained all three raw notebook payloads exactly;
physical double-click opened the original title and multiline Unicode entry.
