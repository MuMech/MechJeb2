# Add a widget, or behaviour to one

Look for it first: the widget map in `MechJeb2.UI/PORTING.md`, then "Index of the models" in `MechJeb2.UI/KodeKit/LAYOUTS.md`. Extend what is there before adding a widget (CONTRIBUTING.md, "Duplication").

Write down before the code: the gestures, each key and who owns it (check the settled table in SKILL.md), what is a commit and what is view state, and what an agent calls.

A new widget is all of these (paths under `MechJeb2.UI/KodeKit/`, the tests' from the repository root):

| Part | Where | Must |
|---|---|---|
| Model | `Headless/<Name>Model.cs` | C# 7.3 with the `#if !CSHARP_7_3` nullable header; no Unity, KSP or MuMech type; delegates to the real value (`ValueModel<T>` for one value); events for notifications; a summary listing its rules |
| Description | the model | `IDescribeParts`, or `[NotAWidget("why")]` on a class that is no widget |
| View | the KodeKit factory | draws the model and forwards events with `KodeWindow.Pointer` or `KodeWindow.Source(...)`, never a literal source; `NoSelect` on every Selectable made by hand |
| Look | `Headless/Skins.cs` | a `StyledPart` with a `StyleParts` name, and its rules for every skin |
| Commit | the window's `CommitPath` | only an edit of module state. A plain action is a press (`AnyAction`), and view state is neither |
| Agent | a value: `Headless/ControlInfo.cs`, `Headless/ControlActions.cs`. An action: `Agent/UiAgent.cs` | verbs that call the model's public methods (below) |
| Tests | `MechJebLibTest/KodeKitTests/` | one fact per rule, a no-allocation fact per per-frame path, a sample in `PatternChecksTests.Samples` |
| Docs | `LAYOUTS.md` | a section and a row in "Index of the models"; a row in PORTING's widget map when views use it |

How the agent reaches it depends on what it is:

- **It edits a value:** it has a commit target with the model on it. Its kind and verbs go in `ControlInfo` and `ControlActions`.
- **It runs an action** (button-like): it has no commit target, so don't invent a counter to make one. The bridge lists it as a button: put the model on a component of the button, read it in `UiAgent.FromButton` for the verbs and in `UiAgent.Run` to call the model as the agent.

A widget that takes the keyboard opens a `KeyboardScope`. A popup opens a layer with `Popups.Push` and is placed by `PopupPlacer`. An overlay takes a free order in `Overlay.cs`.

A timed model is handed the time by its host (`Tick(now)`, `Advance(dt)`): it reads no clock.
