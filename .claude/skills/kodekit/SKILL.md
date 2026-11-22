---
name: kodekit
description: Use before planning, writing or reviewing any change the player would see, click or type into in MechJeb - any window, button, toggle, option, field, read-out, key or shortcut, confirmation, status or localized text - even when the request only names a window, an autopilot or a feature and never says UI. Also for anything in MechJeb2.UI (KodeKit, KodeView) and for the agent bridge.
---

# KodeKit

MechJeb's windows have no IMGUI: each is a view in `MechJeb2.UI/src/`, built on KodeKit (uGUI + TextMeshPro, all in code). A widget's behaviour is a Unity-free model in `MechJeb2.UI/KodeKit/Headless/`; the uGUI side draws it; every edit takes one commit path, which undo and the agent bridge read.

## 1. Read before the code

**First read, whole, the reference of each task that applies.** They are short. Then read the guide sections of that row.

| Task | 1. Read this file | 2. Then these sections |
|---|---|---|
| A window, a row or a setting in one | [build-window.md](references/build-window.md) | PORTING: Widget map, Edits go through the widgets, Rules. LAYOUTS: Conventions, and each widget used |
| A widget, or behaviour on one | [new-widget.md](references/new-widget.md) | LAYOUTS: The headless pattern, Keyboard scopes, Rules learned, and the closest widget |
| Names, ids, driving the UI | [agent-ready.md](references/agent-ready.md) | LAYOUTS: Names and descriptions, The agent bridge |
| Any change, before calling it done | [testing.md](references/testing.md) | LAYOUTS: How to test a model |

The guides:

- PORTING is [MechJeb2.UI/PORTING.md](../../../MechJeb2.UI/PORTING.md): how to write a window.
- LAYOUTS is [MechJeb2.UI/KodeKit/LAYOUTS.md](../../../MechJeb2.UI/KodeKit/LAYOUTS.md): every widget, about 150 KB. Never read it whole: search it for `^#` with line numbers, then read each section you need from its heading to the next one of the same level, to its end.

Then open the closest existing view or model and copy its shape.

## 2. Check the request against what is settled

Each of these was decided in review or was a real bug. When a request needs one broken, **don't build that part and don't work around it**: build the rest, say which rule it meets, and offer the alternative.

| Settled | Instead |
|---|---|
| No flight key acts on a hovered or clicked widget: Space (stage), Left Shift and Left Ctrl (throttle), the arrows, letters. Right Alt is unsafe too (AltGr sends Left Ctrl) | The pointer, or an opt-in action in `Shortcuts.Actions`. A KSP control lock on hover does not make the key safe, and holding the key is no exception |
| No `Input.GetKey*` in a view or a widget, no control lock taken by hand | A `KeyboardScope`, or the focused field's `Route` |
| No undo chords. Undo never re-engages a flight mode or an autopilot | Title-bar icons; `Undoable = false` |
| No shortcut is bound by default | `ShortcutMap`, bound by the player |
| The context menu is pointer-only and never the only way to an action | The same action on a button or icon |
| A palette command that changes the flight takes a second Enter | `.Confirm(...)` |
| Abort and Disengage are instant. What can't be brought back (delete, reset) asks once | `Dialogs.Confirm` for the second kind only |
| The agent bridge has no confirmation gates or marks: it is for tests and full control | Nothing to add |
| A new bar or preview in a flight window is off by default | A setting |
| A new widget's state look is never painted in a closure or by a uGUI tint | A `StyledPart` with a rule in `Headless.Skins` |
| No IMGUI drawing (`OnGUI`, `GUILayout`) for a MechJeb window, item or operation | A view, an info item builder or an operation panel in `MechJeb2.UI/src/` |

Only this table and the guides are grounds to refuse. For a doubt they don't cover, build what was asked and raise the doubt in the report.

## 3. Build and test

Follow "Building" in PORTING. **A plain build copies into the developer's KSP install**: when that wasn't asked for, use the build there that copies nothing.

```
dotnet test MechJebLibTest/MechJebLibTest.csproj --filter "FullyQualifiedName~KodeKitTests"
```

## 4. Report

End every UI change with these five lines, none left out:

- **Read:** the references and the guide sections.
- **Refused or changed:** each part of the request with its rule, or "none".
- **Agent:** how each new control is named and driven, and what makes no commit.
- **Verified:** the commands with their results, and the breaks the tests caught.
- **Not verified:** whichever of in game, other OS, looks, languages and performance weren't run.

## Keeping this skill true

A PR that renames or removes what this skill names, or changes one of its rules, changes the skill and the guide in the same PR.
