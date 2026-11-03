# Notebook rebinding

Implemented matrix, 2026-11-03, based on installed FEATHER 2026.1.f.3 data.
The original paper recipes create new notebooks only; they remain unchanged.

| Workstation | Notebook slot | Skill | Other input | Result on the same notebook |
| --- | --- | --- | --- | --- |
| Loggia Prospect | First + | Inks of Containment 1+ | Second + empty | Brown, same design |
| Loggia Prospect | First + | Inks of Containment 1+ | Paint: Blue / Green / Red in the second + | Matching color, same design |
| Compatible desk | Papers, plain notebook | Stitching & Binding 4+ | Book Press in With | Decorated, same color |
| Compatible desk | Papers, decorated notebook | Stitching & Binding 4+ | Book Press in With | Plain, same color |

Use a compatible Soul at either workstation. Recoloring is exclusively available at the
Loggia Prospect; unlocking that workstation is intentional progression. Inks of Containment
has Moon, which fits its Moon/Rose skill slot. Stitching & Binding remains the desk skill.
Making new notebooks from paper is unchanged.

Each operation takes 60 seconds and fatigues the Soul. Paint is used up (native Dregs
remain); the notebook is not consumed. No paper, ink or memory is used. The Book Press
is returned. Recoloring never changes decoration, even at lower skill; only the separate
level 4+ press recipes change design. Empty second + returns the binding to brown.

These are mod rules, not vanilla recipes. The literal paint names supply the color mapping.
Only those three paints are accepted initially. The existing cover drawing is reused in
four original palettes; both cover and spine change together.

Implementation: valid quantity-one notebooks are accepted in desk Papers and Loggia Prospect
first + slots. Pigment admission at the Loggia Prospect is native; the old desk paint allowance
has been removed. Native capacity, state and movement checks remain. Color recipes are
restricted to library.prospect.loggia and require Inks of Containment. Design recipes remain
restricted to desks and require Stitching & Binding level 4+. Native xtriggers transform
the same ElementStack; scoped code synchronizes binding fields while preserving writing.
No notebook receives readable/blank/ink/tool aspects; no vanilla records are edited.

Isolated verification passed ten native crafting executions: eight recolors at the
Loggia with Inks of Containment/Fet and two design changes at the Reading Room desk
with Stitching & Binding/Chor. Low-level recoloring retained decoration, and all steps
preserved the native token, stack/document/entry IDs, title, complete text/order and
bookmark. Paint became Dregs; the press was retained. Desk paint rejection with a
notebook present passed eight times. Situation heartbeat was accelerated for testing.

Native save retained all three original notebooks: only the target Color field changed
to blue; every other field and both uninvolved notebooks matched exactly. A full exit
and restart followed by another native save retained all three raw payloads exactly.
Physical double-click opened the original title and multiline entry.

The fixture creates a native Loggia situation with its real slot definitions when absent.
Room unlocking, physical ingredient drag/drop, other mods and an ordinary manually timed
60-second craft are outside these checks. Existing saved notebook colors remain supported.
