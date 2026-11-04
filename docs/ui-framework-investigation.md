# Notebook UI framework investigation — 2026-11-04

## Report and evidence boundary

The entry index is difficult to scroll once it contains many entries. The user uses
a mouse with notched and free-spinning wheel modes. Both modes should be usable.
The screenshots contain 14 entries. They establish different list positions, not
the number or magnitude of intervening wheel events. The user closed the game during
this investigation. No game launch, installation, source-code change or save edit
was performed. Findings below are static; the actual mouse needs live measurement.

## Current controls and likely cause

NotebookWindow.BuildEntryScroll already uses Unity uGUI ScrollRect with a clipped
viewport. Its sensitivity is 36, viewport height 480, and row pitch 88 UI units.
There is no index scrollbar. Scrolling itself does not rebuild the index; rebuilding
occurs on opening, adding, deleting and reordering entries. Opening also positions
the index approximately from the selected entry's ordinal.

Read-only decompilation of the installed UnityEngine.UI.dll confirms that OnScroll
immediately applies event delta times sensitivity to the content position. It does
not animate wheel movement. Drag inertia is a separate path. The installed
Unity.InputSystem.dll's InputSystemUIInputModule defaults scrollDeltaPerTick to 6
and multiplies the action value by that setting before dispatching the scroll event.
With an action value of one and the default setting, the index moves 216 UI units,
about 2.45 rows. This is a plausible explanation for skipping several entries per
notch, not proof of the running scene's serialized setting or this mouse's delta.
Do not modify the shared game input module to compensate.

The text editor uses TMP_InputField plus notebook-specific undo, caret alignment
and cursor reveal. Correcting list sensitivity does not replace that architecture.

## Framework options

| Option | Verified fit | Implication |
| --- | --- | --- |
| Existing uGUI/TMP | Already installed and integrated; list is a native ScrollRect | Smallest correction: measure wheel delta, calibrate notebook-only sensitivity, provide a real index scrollbar. A wheel animation would be extra behavior, not a built-in inertia switch. Existing text-field adaptations remain. |
| UniverseLib | Its official UI documentation says it wraps uGUI; original repository is archived | Convenience builders do not establish a different scrolling or text-editing engine. Not a compelling migration for these symptoms. Forks were not evaluated. |
| Installed UIWidgets | Installed assembly contains list-view and input-field helpers; official list-view docs require a ScrollRect | May help construct lists and virtualize large collections, but does not by itself establish a solution to wheel scaling or TMP caret behavior. The installed InputFieldExtended derives from Unity's InputField. No proprietary assembly would be redistributed. |
| Unity UI Toolkit | Installed UIElements module contains TextField, ScrollView, PanelSettings and UIDocument; installed TextField supports multiline | Strongest candidate for a genuine alternative text/layout system. Requires an isolated prototype to establish runtime panel/theme/font availability, game Input System integration, editing behavior and modal isolation. Presence of types is not runtime proof. |

## Recommended sequence

First measure the actual wheel event delta and active input-module multiplier on an
isolated, many-entry fixture. Verify notched movement, reversal, free-spin, list
dragging and selection without changing writing. This distinguishes excessive
distance from missing animation or lost events.

For a broader framework change, prototype UI Toolkit in an isolated development
build before replacing the installed editor. Keep the existing book presentation
and document/save model. Test a multiline TextField and scrolling list first:
Unicode/paste, caret and selection after overflowing, Home/End/Page navigation,
undo/redo, mouse-wheel/track/thumb behavior, focus, game pause/input ownership,
menu teardown and ultrawide scaling. Then compare complete notebook payloads through
native save and a cold restart. Only a passing prototype justifies migrating the
whole editor and retiring the TMP-specific workarounds.

## Primary references

- [Unity ScrollRect source](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/ScrollRect.cs)
- [UniverseLib UI documentation](https://github.com/sinai-dev/UniverseLib/wiki/User-Interface-(UI))
- [UniverseLib repository](https://github.com/sinai-dev/UniverseLib)
- [UIWidgets list-view documentation](https://ilih.name/unity-assets/UIWidgets/docs/widgets/collections/listview.html)
- [Unity UI Toolkit TextField documentation](https://docs.unity3d.com/Manual/UIE-uxml-element-TextField.html)

Online documentation can describe newer engine versions. Installed assemblies were
used to verify type availability and current input/scroll implementation. Local
proprietary decompilations remain in ignored scratch and are not distribution files.
