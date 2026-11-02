# Manual verification

## Current native Canvas editor

Isolated in-game checks on FEATHER2026.1.f.3,2026-10-31:

- Complete layout fits at1280×720,1920×1080 and5120×1440; A+ increases writing size.
- Existing entries open immediately. Creating an entry remains manual. Unicode typing
  works; Ctrl+Z removes a temporary edit and Ctrl+Y restores it.
- Last-entry memory (2026-11-01): a manually created second entry remained selected after
  closing, opening two other notebooks and returning. Native save and a full restart also
  reopened that entry. Original text/IDs/order and separate notebook contents were preserved;
  an empty notebook stayed empty. Reorder/copy/deletion fallback currently have model checks
  rather than feature-specific live UI verification.
- A manually created third notebook with an empty entry heading and Unicode body saved
  exactly; the original two notebook payloads remained byte-for-byte identical.
- A31,391-character note loaded, scrolled to Paragraph300, accepted a temporary manual
  edit, and returned to its exact original raw payload after undo and native save.
- Ctrl+End and Ctrl+Home now move both cursor and viewport immediately without typing.
  The editor supplements TMP's caret-follow using laid-out line geometry.
- Cursor fix: visible insertion carets in title, empty optional heading and body; native
  blinking confirmed. Word and wrapped multiline selections show a green highlight.
  Temporary Unicode typing, undo, close and native save preserved the complete original
  long-note payload exactly. Wheel and scrollbar track paging also passed; automated
  endpoint drags only paged the scrollbar, so continuous thumb dragging remains unverified.
- Normal/Fast/Paused heartbeat suspension and restored input ownership passed. Returning
  to menu closed the editor synchronously. Switching fixtures preserved exact data and
  restored input without a leftover editor.

The new entry index/reorder/delete implementation still needs broader user-interaction
coverage. The previous editor's results below do not substitute for those regressions.
Unicode display is limited by native font glyph coverage. Tested Japanese characters
render; native fallback-font atlas warnings have been observed. Exact colour matching
is difficult to assess from the overexposed computer-use captures.

## Earlier prototype verification

Verified live on FEATHER 2026.1.f.3, 2026-10-31, with saves redirected to a marked
workspace copy: GHIRBI/DLL loading; blank initial editor; Unicode title/heading/body;
Return line break; Ctrl+A/C/V between entries; move-up; two-step deletion; distinct second
notebook; native save and cold restart retaining exact text/identity; physical double-click;
native detail title; opaque editor; blocker prevents clicking Options beneath it.
Native Reading Room desk executions passed at skill 1 (plain) and 4 (decorated), with
paper consumption and independent blank outputs. Native Lodge unshrouding in a fresh
playthrough placed exactly one blank notebook among the original books; its brown spine
renders, shelf acceptance succeeds, and physical double-click opens it before any skills.
The full opening journey and manual timed crafting have not been replayed.
Long-text test: a manually pasted 31,391-character Unicode body survived native save and
cold reload intact. Wheel and scrollbar navigation reach Paragraph 300. Fixed the original
viewport-height clipping bug with an explicitly measured scrolling text area.
Ctrl+Home and Ctrl+End also move both cursor and viewport correctly in the live long note.
Native heartbeat checks passed at Normal, Fast and Paused speeds: no ticks while open,
same speed and input restored on close; a pre-existing input lock was preserved. Returning
to menu while editing closed the editor before native teardown, and Continue reloaded
the isolated playfield without a leftover veil. Short -> long -> short playthrough switching
with the editor open also passed exact notebook data/count and input restoration checks.
Resized binding artwork now matches neighboring shelf-book height. Native right-click to
portage and native API shelving preserved notebook data and identity; live merge checks
rejected both directions. Physical mouse drag/drop remains unverified.
The live Reading Room audit passed 72 loaded-compendium predictions (skill 1–9,
paper/no paper, no optional item/ink/memory/tool). All excluded cases retained vanilla
selection; this was a prediction audit, not 72 physical workstation executions.
Manually entering three spaces as a title preserved those spaces in the native payload
and displayed exactly Untitled Notebook in the physical detail, without creating an entry.

The remaining checklist below is broader than those verified results.

Use an isolated test playthrough and backed-up saves. Do not test against the only copy
of an established game. New games receive a Lodge starter and recipes provide further
notebooks. Dev-only controls are available through scripts/prepare-runtime-test.ps1;
verify TEST BUILD save-root and successful initialization logs before entering gameplay.

1. Install official GHIRBI and the prototype through normal local mod support, enable both,
   restart, inspect Player.log for successful PlayerNotebook initialisation and patch errors.
2. Spawn two separate quantity-one notebooks. Test movement, placement, shelving and
   dragging one onto the other: they must not merge or lose either card.
3. Double-click one. It should have no entries. Verify simulation and game shortcuts stop
   while editing; clicks should not interact with the room beneath the editor.
4. Add two entries. Test blank headings/body, Unicode, multiline pasted text, punctuation,
   a long document, keyboard selection/copy/paste/undo, and text cursor navigation.
5. Reorder and delete (including cancelling deletion by selecting another entry). Edit the
   title to empty, whitespace-only and a duplicate title. Check all card/detail/shelf names.
6. Close and reopen both notebooks. They must retain separate contents. Confirm closing
   restores camera/input and the prior simulation speed, including when previously paused.
7. Save, quit and relaunch. Verify exact text and order, title fallback and independent IDs.
   Repeat while editor is open during autosave; check that latest committed text is saved.
8. Load a second playthrough and return to the first. There must be no editor state or
   contents carried between cards/playthroughs. Return to menu while editing; test reload.
9. In a disposable copy, retire/delete one card and create another. No inherited data.
   Establish legal duplication paths and verify copy independence, or document impossibility.
10. On disposable data only, test a future schema and malformed payload: editing must be
    refused while original data is preserved. Test update/reinstall/uninstall with backups.

11. At a compatible desk, use one Quire of Paper, Stitching & Binding and a Soul; no
    memories, ink or tools. Test skill levels 1, 3, 4 and 9. Expect plain at 1–3 and
    decorated at 4+. Paper is consumed, output is quantity one, and existing notebooks
    remain unchanged. Repeat to verify fresh IDs and blank contents.
12. Add ink: notebook recipes must stop matching and manuscript crafting remain available.
    Remove paper: ordinary Stitching & Binding crafts must remain available. Add memory
    or tool: notebook recipes must stop matching.

Starter setup and original basic artwork are implemented and tested as described above.
Broader colors are not implemented. The art remains basic original geometric artwork.
The full acceptance checklist remains in the handoff. Record results with exact game/mod
versions and steps; leave unchecked tests explicitly unverified.
