# Original artwork and asset licensing

The project's contributors apply **CC0 1.0 Universal** to the original PNG artwork
listed below, to the extent they hold rights in it. The complete dedication and
fallback license are in `LICENSES/CC0-1.0.txt` at the root of both the source
repository and packaged mod. Canonical text: https://creativecommons.org/publicdomain/zero/1.0/.

## Notebook covers and spines

Source directory: `mod/PlayerNotebook/images/books/`.
Packaged mod directory: `images/books/`.

- `playernotebook.brown.plain.png` — plain cover.
- `playernotebook.brown.plain_.png` — plain spine.
- `playernotebook.brown.decorated.png` — decorated cover.
- `playernotebook.brown.decorated_.png` — decorated spine.

These four images are original geometric drawings made with System.Drawing by
`scripts/render-bindings.ps1`. The script draws coloured backgrounds, borders,
binding lines and diamond ornaments. It reads no game or third-party images.
Its source code is licensed under 0BSD; the resulting artwork is dedicated under CC0.

## Interface study

`outputs/notebook-ui-direction.png` is also dedicated under CC0. It is an original
design mockup drawn by `scripts/render-ui-mockup.ps1` using shapes and rendered text.
It is included in the source repository, not the playable mod package. The script
uses installed system fonts; no font files are included or relicensed.

## Game resources and dependencies

The playable editor draws its panels and controls in code and uses the installed
game's fonts, Unity UI and TextMeshPro at runtime. The game also provides its normal
book manifestation. Those resources remain under their owners' terms; this project's
licenses do not grant rights to them. No game artwork, fonts or proprietary runtime
assemblies are included in this repository or mod package.

Lib.Harmony is a separate code dependency under MIT; see THIRD-PARTY-NOTICES.md in
the packaged mod or [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) beside this file
in the source repository.
