# Notebook rebinding

Implemented matrix, 2026-11-03, based on installed FEATHER 2026.1.f.3 data.
The original paper recipes create new notebooks only; they remain unchanged.

| Workstation | Notebook slot | Skill | Other input | Result on the same notebook |
| --- | --- | --- | --- | --- |
| Loggia Prospect | First + | Inks of Containment 1+ | Second + empty | Brown, same design |
| Loggia Prospect | First + | Inks of Containment 1+ | Any supported pigment in the second + | Matching color, same design |
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

These are mod rules. All 16 installed pigment items are supported, plus undyed brown.
Named paints use their named shades. The other mappings follow the installed names and
color descriptions; exact RGB shades and the dawn gradient are original interpretations.

| Pigment | Cover shade | Basis |
| --- | --- | --- |
| Paint: Blue / Green / Red | Blue / Green / Red | Named colors |
| Paint: Dusk-Blue / Palest Blue | Dusk blue / Pale blue | Named colors |
| Paint: Bosk-Green / Moth-Gold / Pink / Yellow | Matching named shade | Named colors |
| Black Sapphire Wash | Black-sapphire navy | Name and Moon imagery |
| Iotic Essence | Reddened copper | Description of reddening; copper tone is an interpretation |
| Xanthotic Essence | Rich gold | Explicit gold description |
| Ichor Auroral | Dawn rose fading to lavender | Multicolored dawn description; palette is an interpretation |
| Ichor Vitreous | Pale glass-grey | Pale fluid description; grey is an interpretation |
| Refulgin | White | Explicit white description |
| Bitterblack Salts | Near-black | Name and dark description |

The complete palette and ingredient mapping is in bindings.json. The build generates
the definitions and artwork from that catalog and embeds it for binding validation.
Recoloring remains at
Loggia with Inks of Containment level 1+. All shades preserve the existing design.
Plain/decorated conversion remains at desks with Stitching & Binding level 4+ and a
Book Press. Liquids leave native Dregs; solid pigments leave native Residue.

Implementation: valid quantity-one notebooks are accepted in desk Papers and Loggia Prospect
first + slots. Pigment admission at the Loggia Prospect is native; the old desk paint allowance
has been removed. Native capacity, state and movement checks remain. Color recipes are
restricted to library.prospect.loggia and require Inks of Containment. Design recipes remain
restricted to desks and require Stitching & Binding level 4+. Native xtriggers transform
the same ElementStack; scoped code synchronizes binding fields while preserving writing.
No notebook receives readable/blank/ink/tool aspects; no vanilla records are edited.

Expanded-palette verification passed 37 native crafting executions: all 17 shades in
plain and decorated designs, both Book Press operations and recoloring at level 4 without
changing design. Every step preserved the native token, stack/document/entry IDs,
quantity, title, full text/order and bookmark. All 34 cover/spine pairs loaded without
placeholders at the intended dimensions. Liquid pigments left Dregs and the two solids
left Residue. Desk pigment rejection was also checked. The situation heartbeat was
accelerated for these isolated tests.

Native save after the full cycle retained all three original notebook documents with
only the target Color field changed. A targeted Auroral craft, native save and full
exit/restart also passed: all three raw notebook payloads matched exactly after reload,
and opening the notebook showed its original title and multiline Unicode entry.

The fixture creates a native Loggia situation with its real slot definitions when absent.
Room unlocking, physical ingredient drag/drop, other mods and an ordinary manually timed
60-second craft are outside these checks. Existing saved notebook colors remain supported.
