# Notebook rebinding

Implemented matrix, 2026-11-03, based on installed FEATHER 2026.1.f.3 data.
The original paper recipes create new notebooks only; they remain unchanged.

| Input in Papers | Skill | Input in With | Result on the same notebook |
| --- | --- | --- | --- |
| Existing notebook | Stitching & Binding 1+ | Empty | Brown, same design |
| Existing notebook | Stitching & Binding 1+ | Paint: Blue / Green / Red | Matching color, same design |
| Plain notebook | Stitching & Binding 4+ | Book Press | Decorated, same color |
| Decorated notebook | Stitching & Binding 4+ | Book Press | Plain, same color |

Use an existing desk accepting the skill and a compatible Soul, as with notebook creation.
Rebinding takes 60 seconds and fatigues the Soul. Paint is used up (native Dregs remain);
the notebook is not consumed.
No paper, ink or memory is used. The Book Press is returned. Recoloring never changes
decoration, even at lower skill; only the separate level 4+ press recipes change design.
Color and decoration are independent. No paint/tool returns the binding to brown.

These are mod rules, not vanilla recipes. The literal paint names supply the color mapping.
Only those three paints are accepted initially. The existing cover drawing is reused in
four original palettes; both cover and spine change together.

Implementation: allow our valid quantity-one notebooks in desk Papers slots, and
the three paints in With only while such a notebook is present. Keep native capacity,
state and movement checks. New recipes require the notebook-specific aspect and exact
paint, with exclusive design inputs. Only the press operation has a level 4 skill gate.
Native xtriggers transform that same ElementStack;
scoped code synchronizes the document binding while preserving all other fields.
No notebook receives readable/blank/ink/tool aspects, and no vanilla records are edited.

Isolated Reading Room tests passed two sequences of ten native crafting executions,
including recoloring decorated books at level 1, recoloring at level 4 without a design
change, and both press operations at level 4. Each step retained the same native token,
stack ID, document/entry IDs, title, complete text/order and bookmark. Paint became Dregs;
the press was retained. The test driver accelerated the situation heartbeat.

Native save after the return-to-brown sequence matched all three original notebook
payloads exactly. A second sequence ended blue: only its Color field differed across
the three documents. After a full game exit/restart, native save again matched all
three blue-save payloads exactly; physical double-click opened the blue book with its
original Unicode title and multiline entry. Two uninvolved notebooks remained unchanged.

Offline model/native checks also pass. Other desks, other mods, physical ingredient
drag/drop and an ordinary manually timed 60-second craft remain outside these checks.
