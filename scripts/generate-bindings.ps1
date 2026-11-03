# Generate only this mod's definitions from its original palette manifest.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$palette = @(Get-Content -LiteralPath "$repo/mod/PlayerNotebook/bindings.json" -Raw | ConvertFrom-Json)
if (($palette.color | Sort-Object -Unique).Count -ne $palette.Count) { throw 'Duplicate binding color' }
$pigments = @($palette | Where-Object pigment)
if (($pigments.pigment | Sort-Object -Unique).Count -ne $pigments.Count) { throw 'Duplicate pigment mapping' }
$designs = @('plain', 'decorated')
$triggers = [ordered]@{}
foreach ($color in $palette) {
    if ($color.color -notmatch '^[a-z]+$') { throw 'Invalid color ID' }
    foreach ($design in $designs) { $triggers["notebook.rebind.$($color.color).$design"] = "playernotebook.$($color.color).$design" }
}
$elements = @(foreach ($color in $palette) {
    foreach ($design in $designs) {
        [ordered]@{
            id = "playernotebook.$($color.color).$design"; label = 'Untitled Notebook'
            desc = 'A reusable commonplace book. Double-click to write. Recolor at the Loggia Prospect with Inks of Containment and a compatible Soul: notebook in the first + slot, pigment in the second (empty for brown). Change decoration at a compatible desk with Stitching & Binding level 4+ and a Book Press. All writing is preserved.'
            icon = "playernotebook.$($color.color).$design"; manifestationtype = 'Book'
            aspects = [ordered]@{ thing = 1; 'notebook.bindable' = 1; "notebook.$design" = 1 }
            xtriggers = $triggers
        }
    }
})
$aspects = @(foreach ($key in @('notebook.bindable', 'notebook.plain', 'notebook.decorated') + @($triggers.Keys)) {
    [ordered]@{ id = $key; label = 'Notebook binding'; isAspect = $true; isHidden = $true; noartneeded = $true }
})
$recipes = @(foreach ($color in $palette) {
    foreach ($design in $designs) {
        $reqs = [ordered]@{ ability = 1; 's.inks.containment' = 1; skill = 1; 'notebook.bindable' = 1; "notebook.$design" = 1; 'quire.paper' = -1; ink = -1; tool = -1; memory = -1; readable = -1 }
        $effects = [ordered]@{}
        if ($color.pigment) {
            # Exact ingredient IDs exclude every other supported pigment.
            foreach ($other in $pigments) { $reqs[$other.pigment] = $(if ($other.pigment -eq $color.pigment) { 1 } else { -1 }) }
            $effects[$color.pigment] = -1
            $ingredient = "Uses one $($color.label) pigment."
        } else { $reqs['pigment'] = -1; $ingredient = 'Leave the second + slot empty for brown.' }
        [ordered]@{
            id = "playernotebook.rebind.color.$($color.color).$design"; actionid = 'library.prospect.loggia'
            label = "Recolor: $($color.label) ($design)"
            startdescription = "Change the cover to $($color.label); keep its $design design and all writing. [Inks of Containment level 1 or higher and a compatible Soul. Notebook in the first + slot; pigment in the second. $ingredient No paper, ink, tools or memories.]"
            desc = 'A new color; the same words within.'; reqs = $reqs; effects = $effects
            aspects = [ordered]@{ fatiguing = 1; "notebook.rebind.$($color.color).$design" = 1 }
            warmup = 60; craftable = $true
        }
    }
})
foreach ($color in $palette) {
    foreach ($old in $designs) {
        $new = if ($old -eq 'plain') { 'decorated' } else { 'plain' }
        $recipes += [ordered]@{
            id = "playernotebook.rebind.design.$($color.color).$new"; actionid = 'library.desk.*'
            label = $(if ($new -eq 'decorated') { 'Decorate the Binding' } else { 'Simplify the Binding' })
            startdescription = "Work a $new binding, preserving its $($color.label) color and all writing. [Stitching & Binding level 4 or higher, a compatible Soul and a Book Press in With. The press is returned. No paper, pigment, ink or memories.]"
            desc = 'The binding changes; its words remain.'
            reqs = [ordered]@{ ability = 1; 's.stitching.binding' = 1; skill = 4; "playernotebook.$($color.color).$old" = 1; 'book.press' = 1; pigment = -1; 'quire.paper' = -1; ink = -1; memory = -1; readable = -1 }
            effects = [ordered]@{}; aspects = [ordered]@{ fatiguing = 1; "notebook.rebind.$($color.color).$new" = 1 }
            warmup = 60; craftable = $true
        }
    }
}
$utf8 = [Text.UTF8Encoding]::new($false)
foreach ($output in @(
    @{ Path = 'content/elements/notebooks.json'; Data = @{ elements = $elements } },
    @{ Path = 'content/elements/binding-aspects.json'; Data = @{ elements = $aspects } },
    @{ Path = 'content/recipes/rebinding.json'; Data = @{ recipes = $recipes } }
)) {
    [IO.File]::WriteAllText((Join-Path "$repo/mod/PlayerNotebook" $output.Path), ($output.Data | ConvertTo-Json -Depth 10) + "`n", $utf8)
}
