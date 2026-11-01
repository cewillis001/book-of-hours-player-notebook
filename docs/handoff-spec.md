# Book of Hours — Player Notebook Mod

Coding-agent handoff specification · Draft v0.1 · October 31, 2026

**Objective:** Add physical, player-writable notebooks to *Book of Hours*, using existing workstations and manuscript-like crafting to produce additional notebooks with customizable bindings.

The mod should make keeping a commonplace book, research journal, recipe collection, or bibliography a natural part of playing the Librarian.

The player decides what to record. **No automatic recipe discovery, transcription, or journal population.**

## 1. Product requirements

### 1.1 Notebook objects

Every notebook is a distinct, persistent in-game object with:

- A unique identity.
- An editable title, initially empty.
- A collection of separately editable entries.
- A binding color and design established by its crafting recipe.
- A physical location in Hush House, using normal game object interactions wherever supported.

The notebook's visible UI name must be the player's title when nonblank, or exactly **Untitled Notebook** otherwise. Do not append binding descriptions, identifiers, or automatic numbering.

Duplicate titles are permitted. Clearing a title must restore the fallback name without changing any other property.

### 1.2 Journal editor

Opening a notebook should present an editor with a list of entries and a writing area.

Each entry has an optional heading and an editable, multiline body. The player can create, open, edit, reorder, and delete entries. Entries must remain in their original notebook, even when other notebooks have identical titles or appearances.

The editor must support ordinary keyboard input, copy/paste, and reasonably long text. It should preserve line breaks and punctuation, including Unicode text.

Editing should be free: no ink, paper, crafting ingredients, skill checks, or in-game time consumption.

A manual save control is acceptable, but edits must not be silently lost when the editor closes. The implementation should define a reliable commit/autosave policy.

### 1.3 Starting notebook

Place one blank, usable notebook in the first accessible room of Hush House, subject to verification of the game's starting-room and item-placement mechanisms.

It must be usable before the player has unlocked bookmaking.

### 1.4 Notebook crafting

The player creates additional notebooks at an **existing in-game workstation**, using:

- Paper.
- A suitable skill.
- No ink.

Use the game's manuscript-crafting conventions wherever practical. The notebook is a reusable object, not a consumable manuscript or a one-time text output.

Binding color and design are determined by ingredients or recipe variants, rather than a separate customization menu.

The agent must investigate how existing ingredients can express binding choices without creating unreasonable recipe collisions or requiring unavailable materials.

### 1.5 Skill-based craftsmanship

Support a progression from simple to more elaborate bindings. Higher-quality designs may require greater skill levels or particular skills.

Color and decorative sophistication should be independent wherever practical: an ornate notebook need not be restricted to a single color.

The exact skill thresholds, eligible skills, and binding recipes are implementation decisions to propose after inspecting game data. They should not be invented as if they were established game mechanics.

### 1.6 Manual research only

No automatic recording of crafting successes, failed experiments, discovered recipes, books read, or game knowledge.

The notebook is player-authored. A player can write an incorrect recipe, an incomplete bibliography, or an entirely fictional account.

There is no correctness validation, hidden knowledge database, or spoiler-revealing integration in version 1.

## 2. User interaction flows

### 2.1 Opening and writing

1. Player selects a physical notebook.
2. Player opens its editor through an appropriate card interaction.
3. The editor displays the notebook title and ordered entry list.
4. Player can edit the title or create an entry.
5. Player can enter a heading and body, revise either, or delete the entry.
6. Closing the editor commits all changes.
7. Reopening the same notebook restores its content.

The editor should not automatically create an entry simply because the notebook is opened.

### 2.2 Making a notebook

1. Player brings paper and an eligible skill to an existing workstation.
2. Player optionally supplies ingredients that determine binding appearance.
3. The workstation executes the notebook recipe using normal crafting behavior.
4. The output is a new physical notebook with an empty title and no entries.
5. Player can move, shelve, open, and write in it independently of other notebooks.

A crafting recipe must not modify an existing notebook by accident.

### 2.3 Naming behavior

| Stored title | Display name |
| --- | --- |
| `""` | Untitled Notebook |
| `"A Register of Inks"` | A Register of Inks |
| `"Recipes"` | Recipes |
| `"   "` | Untitled Notebook |
| `"Untitled Notebook"` | Untitled Notebook |

Preserve the player's original text in storage. Whitespace-only titles count as untitled for display purposes; do not silently replace their stored value with the fallback.

## 3. Suggested data model

This is a logical schema, not a claim about the Secret Histories engine's native serialization format.

```json
{
  "schemaVersion": 1,
  "notebooks": [
    {
      "id": "unique-persistent-id",
      "title": "",
      "binding": {
        "color": "green",
        "design": "plain"
      },
      "entries": [
        {
          "id": "unique-entry-id",
          "heading": "Inks",
          "body": "Try a different memory.",
          "order": 0
        }
      ]
    }
  ]
}
```

Implementation requirements:

- Generate a stable ID for each notebook instance, never from its title or item type.
- Keep IDs unique within a save.
- Associate player-authored data with the correct game save, not globally across all playthroughs.
- Support migration through an explicit schema version.
- Use atomic writes or an equivalent corruption-resistant save strategy.
- Preserve Unicode, line breaks, and empty fields.
- Ensure deleted notebooks cannot accidentally transfer their contents to newly created notebooks.
- Do not use notebook titles as storage keys.

If the game duplicates a notebook, the agent must establish whether that action is actually possible and define the appropriate behavior. The preferred behavior is a new notebook ID with an independent copy of the original contents.

## 4. Binding and crafting system

The implementation should offer a small, coherent initial set of binding variants rather than attempting an enormous catalogue.

### Visual directions

- **Plain binding:** Plain brown cloth-bound vintage notebook. Accessible at the lowest eligible skill level.
- **Colored cloth:** A simple cloth-bound notebook, such as blue cloth with an embossed border. Color is selected by ingredient or recipe variant.
- **Decorative binding:** A decorative leather or cloth cover, such as green leather with botanical blind embossing. Requires more advanced craftsmanship.
- **Ornate binding:** An elaborate high-tier cover, such as red leather with gold tooling. This is a higher-tier decorative option.

These are visual directions, not finalized art assets or approved recipes.

### Crafting constraints

The agent should prioritize existing ingredients and skills. If there is no suitable vanilla ingredient for a particular color, it should propose either a new material or a reduced palette, rather than arbitrarily mapping unrelated ingredients to colors.

Every recipe must have a deterministic outcome. If several notebook recipes can match the same inputs, the agent must investigate the game's recipe-selection rules and eliminate ambiguity.

The agent should also check whether binding variants can be distinguished by item artwork without generating a separate full set of incompatible item types.

## 5. Technical investigations

These are explicit discovery tasks, not assumptions the agent may silently make.

| Priority | Investigation | Required result |
| --- | --- | --- |
| P0 | How does FEATHER load JSON and DLL mods? | Confirm supported packaging and code-mod permission mechanism. |
| P0 | How are individual card instances identified and serialized? | Demonstrate stable notebook identity through save/load. |
| P0 | Can a physical card's display name change dynamically? | Working prototype of editable title and fallback. |
| P0 | Can a custom text editor be opened from a card? | Working editor with multiline text. |
| P0 | Can new crafting outputs be added to an existing workstation? | Successful notebook creation without breaking vanilla recipes. |
| P1 | How can notebook contents be stored per save? | Tested persistence implementation. |
| P1 | How can binding ingredients select artwork? | Deterministic recipe-to-cover mapping. |
| P1 | Which workstation, paper types, and skills are appropriate? | Evidence-based recipe proposal. |
| P1 | How can the starter notebook be placed? | Confirmed placement in a new and existing playthrough. |
| P2 | How do mod updates and uninstallation affect saves? | Documented compatibility and recovery behavior. |

Potential starting points to inspect and verify:

- [FEATHER modding announcement](https://store.steampowered.com/news/app/1028310/view/535502917902273119)
- [Book of Hours code-mod examples](https://github.com/chepash/book_of_hours_mods)
- `bh_Data/StreamingAssets/bhcontent/core/recipes/DLC_HOL_manuscripting_write.json` — verify this path against the installed game before relying on it.
- [Hush House Catalogue](https://github.com/SunsetFi/bookofhours-catalog) — examples of inspecting objects, workstations, and recipes; its automatic recipe functionality is outside this mod's scope.

Do not assume that older pre-FEATHER limitations on custom images, DLL loading, or content structure remain applicable.

## 6. Acceptance criteria

These are the minimum conditions for declaring version 1 complete.

### Verification checklist

- [ ] A new playthrough contains one blank, usable starter notebook in the first accessible room of Hush House.
- [ ] The starter notebook is usable before the player unlocks notebook crafting.
- [ ] Opening a notebook does not create an entry automatically.
- [ ] The player can create, open, edit, reorder, and delete separate entries.
- [ ] Each entry supports an optional heading and multiline body text.
- [ ] Ordinary keyboard input and copy/paste work in the editor.
- [ ] Unicode, punctuation, line breaks, empty headings, and empty bodies survive closing and reopening the editor.
- [ ] Editing consumes no ink, paper, other ingredients, skill checks, or in-game time.
- [ ] The player can edit a notebook title and can leave it blank.
- [ ] A blank or whitespace-only title displays exactly **Untitled Notebook** without overwriting the stored title value.
- [ ] A nonblank title is used as the notebook's visible UI name without binding text, identifiers, or automatic numbering.
- [ ] Duplicate displayed titles are allowed.
- [ ] Closing the editor commits changes according to a documented, reliable save policy; edits are not silently lost.
- [ ] Notebook titles, entries, entry order, and binding properties survive save, quit, and reload.
- [ ] Player-authored data is scoped to the correct game save and does not leak between playthroughs.
- [ ] Two untitled notebooks with identical bindings can hold different text; after save, quit, and reload, each retains the correct independent contents.
- [ ] Every newly crafted notebook receives a unique persistent identity, an empty title, and no entries.
- [ ] Deleting a notebook does not cause a later notebook to inherit its ID or contents.
- [ ] If notebook duplication is possible, duplication behavior is tested and documented; the preferred result is a new ID with an independent copy of the content.
- [ ] At least one additional notebook can be crafted at an existing workstation using paper and an eligible skill, without ink.
- [ ] Notebook crafting does not modify an existing notebook accidentally.
- [ ] Notebook recipes do not break or ambiguously override vanilla recipes at the selected workstation.
- [ ] Binding ingredients or recipe variants produce deterministic cover color and design outcomes.
- [ ] The first release includes a coherent progression from a simple binding to at least one more elaborate skill-gated design.
- [ ] Color and decorative tier remain independent wherever the verified game systems permit it.
- [ ] Crafted notebooks behave as reusable physical objects that can be moved, placed, shelved, opened, and written in using normal game interactions wherever supported.
- [ ] No crafting result, failed experiment, discovered recipe, book read, or other game knowledge is recorded automatically.
- [ ] The installable package loads through the supported mod mechanism on the documented game version.
- [ ] Installation, updates, save compatibility, recovery behavior, and uninstallation effects are documented and manually tested.
- [ ] The release package includes all required source, assets, dependencies, build instructions, installation instructions, and test instructions.

## 7. Implementation milestones

1. **Technical proof of concept**

   Demonstrate a physical notebook card with editable title, independent entry storage, and save/load persistence. This is the highest-risk milestone.

2. **Bookmaking integration**

   Add notebook recipes to an existing workstation, verify paper and skill requirements, and create independent notebook instances.

3. **Binding variants**

   Add ingredient-controlled binding appearances, artwork, and skill-based decoration.

4. **Player experience and distribution**

   Add the starting notebook, polish the editor, handle error recovery, package the mod, and complete the acceptance tests.

## 8. Deliverables

The coding agent must provide:

- The complete source repository.
- Build instructions.
- An installable mod package.
- A dependency list.
- The supported game version.
- Automated tests where practical.
- Manual test instructions.
- A concise description of known limitations.
- A documented recipe matrix for the selected workstation, paper, eligible skills, skill thresholds, binding ingredients, cover colors, cover designs, and deterministic outputs.
- A technical-investigation report recording the findings and decisions from Section 5.

The agent should document its technical findings before committing to a persistence or UI architecture. If a critical feature is impossible through the supported mod interfaces, it must report the limitation and propose alternatives rather than silently dropping the requirement.

## 9. Scope boundaries

Version 1 explicitly excludes:

- Automatic recipe logging.
- Auto-generated bibliographies.
- OCR.
- Full-text search across notebooks.
- Automatic references to in-game books.
- Multiplayer synchronization.
- A separate notebook database accessible outside the game.
- Rich-text formatting.
- Embedded drawings.
- Actual handwritten rendering.

The editor may use a book-like visual presentation, but plain editable text is sufficient.

## Definition of done

A player can find a blank notebook, write in it, give it a title, craft another notebook with a chosen binding using an existing workstation, write different content in that second notebook, place both in Hush House, and recover both exactly after reloading.

The specification is sufficiently defined for an agent to begin implementation. The remaining unknowns are mostly about the game's actual interfaces, not about the intended player experience.

One design detail is intentionally provisional: **which ingredients correspond to which binding colors and designs**. The agent must return a proposed recipe matrix based on actual game data before implementing the artwork.
