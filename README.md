# Player Notebook for Book of Hours

Development prototype targeting the installed Windows FEATHER **2026.1.f.3** build.
Not a finished release. The live editor and native save/cold reload have passed isolated in-game testing.

The handoff is in [docs/handoff-spec.md](docs/handoff-spec.md); investigation evidence and
decisions are in [docs/technical-investigation.md](docs/technical-investigation.md).
Verified behavior and remaining checks are in [docs/manual-tests.md](docs/manual-tests.md).

## Implemented prototype

- FEATHER-native DLL entry point; no BepInEx installation.
- Physical notebooks with original brown covers and spines, in plain and decorated designs.
- Double-click opens a book-style editor with a left entry index, title, optional headings,
  multiline writing area, reorder controls and deletion confirmation. The editor scales
  with screen height and width; A−/A+ adjust writing text. Existing notes open immediately.
  Each notebook reopens its last selected entry; normal game saves preserve that choice.
- Schema-versioned data inside each card's native `Illuminations`, including notebook
  and entry identities. No global notebook store or external notebook database.
- Dynamic label fallback exactly `Untitled Notebook`; titles are preserved verbatim.
- Notebook stacking prevented; copied payloads assigned independent document identities.
- Simulation heartbeat suspended while editor is open; normal game input locked through
  the game's LocalNexus interface; native text-edit detection includes this editor.

Every text/control change serializes the document completely and then replaces the card's
single illumination field. Closing also commits. **Save the game to persist edits across
quitting.** The normal game save/autosave/backup system owns disk persistence. This follows
the user's clarification that base-game save resilience is sufficient. Crash recovery is
limited to the last successful normal game save, just like other game state.

Prototype crafting now includes plain and decorated brown notebooks at compatible desks:
one Quire of Paper, Stitching & Binding (levels 1–3 plain; 4+ decorated), and a compatible
Soul. Leave ink, memories and tools out. This deliberately reserves that paper-and-skill
combination; vanilla crafts remain available without paper. Ink, tools or memories disable
notebook recipes and preserve the game's ordinary recipe selection. Ink can still select
another vanilla craft when its requirements are met; check the desk's preview.
Both tiers have distinct basic brown cover/spine artwork. Native Reading Room desk tests
passed for both tiers, including paper consumption and blank independent outputs.
An in-game audit of 72 Reading Room recipe predictions covered skill levels 1–9,
paper/no paper, and ink/memory/tool exclusions. Other desks/mod combinations need more coverage.

A new playthrough gets one blank notebook alongside the Lodge's two initial books.
Native room setup testing preserved the original books and placed exactly one blank,
usable notebook on the Lodge shelf. The test revealed the room directly; the entire
opening journey has not been replayed manually. Existing playthroughs use crafting. Broader colors, release
polish and update/uninstall recovery tests remain outstanding. Short/long playthrough switching retained exact text and identity with no editor/input leak.

## Build

Requires Windows, .NET SDK 8 or later, the installed game, and NuGet access for initial
restore. Build dependencies: Microsoft.NETFramework.ReferenceAssemblies 1.0.3 and
Lib.Harmony 2.3.3. The model checks use Newtonsoft.Json 13.0.3. Runtime uses the game's
own Newtonsoft.Json and Unity/Secret Histories assemblies; these are never redistributed.

```powershell
.\scripts\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Book of Hours'
```

The script builds a .NET Framework 4.7.2 DLL, runs model and installed-engine serialization
checks, and packages `dist/PlayerNotebook-0.1.0-prototype.zip`. It does not install or enable
anything. The native check runs on .NET 8 because this game's interfaces use features the
desktop .NET Framework runtime cannot load; Unity's own Mono runtime is a separate target.

This repository contains the mod's source, original artwork, scripts and documentation.
Game assemblies, extracted game data, decompiled sources, player saves, local tools and
build output are excluded. Building requires your own installed copy of the game.
The development runtime-test harness also requires local disposable save fixtures, which
are not included. Never distribute a build compiled with `NOTEBOOK_TESTING`.

## Licensing

The project's original code, scripts, mod definitions and documentation are licensed
under [Zero-Clause BSD (0BSD)](LICENSE), except where a separate notice applies.
The original notebook cover/spine PNGs and interface mockup are dedicated under
[CC0 1.0 Universal](LICENSES/CC0-1.0.txt). See [artwork provenance and scope](docs/ASSETS.md)
(included as `ASSETS.md` in the packaged mod).

These permissions allow commercial reuse and modification without an attribution or
share-alike requirement for our original work. Third-party components retain their own
licenses: Lib.Harmony remains MIT licensed, and game resources are not redistributed
or relicensed. See [third-party notices](docs/THIRD-PARTY-NOTICES.md).

## Experimental installation

Use a disposable playthrough with backed-up saves. Extract `PlayerNotebook` from the zip
into the game's local `mods` folder (Options → Browse Files). Install and enable the game's
official GHIRBI permission mod, enable Player Notebook, and restart the game. DLL activation
requires restarting. The prototype does not change DLL permission or player settings.

New games receive a starter notebook in the Lodge. Prototype crafting recipes provide
additional notebooks; both tiers have completed at the Reading Room desk in isolated tests.

The editor uses the game's native Canvas and TextMeshPro controls and its existing serif
fonts, with fallback fonts for additional characters. Ctrl+Z undoes changes; Ctrl+Y or
Ctrl+Shift+Z redoes them. Undo history is kept while the field's current entry is open.
It is cleared when selecting another entry or closing the notebook. Normal game saves
preserve the text itself.

The replacement editor passed isolated in-game checks for Unicode input, undo/redo,
native save preservation, visible insertion cursors and selection highlights,
long-note wheel scrolling and Ctrl+Home/End cursor-follow.
Layouts fit at 1280×720, 1920×1080 and 5120×1440. Pause/input ownership, menu cleanup
and save switching were also checked. See `docs/manual-tests.md` for the verified scope
and remaining acceptance tests.

## Save compatibility

The data is stored on the native card in `playernotebook.document`. Unknown versions or
malformed documents are refused without overwriting their stored data. Version 1 has no
older schema to migrate; future versions must implement explicit migrations.

Uninstalling removes the notebook element definition and can cause the base game to discard
unknown items on load. Back up the entire save before uninstalling or updating. Restore
the matching mod and a pre-uninstall backup to recover. This behavior has not been tested;
do not save over the only copy of a notebook-containing playthrough without the mod.
