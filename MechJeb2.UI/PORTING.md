# Writing a MechJeb window (MechJeb2.UI)

MechJeb2.UI draws MechJeb's `DisplayModule` windows with **KodeKit** (`KodeKit/`), a retained uGUI + TextMeshPro
layer built on KodeUI. MechJeb's own windows have no IMGUI drawing code, so each of them needs a view here. IMGUI
only draws another mod's windows; another mod's info item shows as its name only, and another mod's maneuver operation
gets a note that its settings can't be shown (their IMGUI methods aren't run inside the new windows).

## How a view is wired

1. Create `src/<Name>View.cs`: a class deriving from `KodeView`, marked `[KodeViewFor(typeof(MechJebModuleX))]`,
   with a public constructor taking the module. `KodeHost` finds it by reflection; nothing else to register.
2. Override `MinWidth` and `BuildContents(KodeWindow w)`,
   called **once**. Build every row there; things that come and go use `w.Visible(go, () => condition)`.
3. Per-frame model work (selecting a node, refreshing a cache,
   pushing settings into a controller each frame) goes in `protected override void BeforeSync()`.
4. If the structure must change at runtime (a list grows), set `RebuildRequested = true`; the host rebuilds the view. A field
   being edited keeps the keyboard, its text, caret and edit across it (focus recovery, LAYOUTS.md), found again by its stable
   name or its place in the build. Fields whose number or order can change (one per list item) take a name from the view,
   `field.Named("item:" + id)`, and are found by it wherever they moved; a field found by its place must also still show the
   same value, so a rebuild that adds or removes fields above it hands the edit to no one rather than to another value's field:
   the edit ends there. A field that must follow its edit across such a rebuild takes a view name (`Named`).

Examples: `UtilitiesView` (toggles with tones, fields, shared info item builders), `NodeEditorView` (selectors, repeat
buttons, state in the module), `SettingsView` (registry toggles, title, skin buttons).

## Keep the logic in MechJeb

Texts are `#MechJeb_...` keys: pass the key string, and KodeKit localizes it and re-localizes on language switch. If
a view computes something that isn't drawing, it belongs in an `internal` method on the module, where kOS and other
views can reach it too. Make private fields/methods `internal` when the view needs them
(`InternalsVisibleTo("MechJeb2.UI")` is set).

## Widget map (KodeKit, `KodeWindow w`, parent `UIObject p`)

| IMGUI | KodeKit |
|---|---|
| `GUILayout.BeginHorizontal` | `KodeWindow.Row(p)` |
| `GUILayout.BeginVertical` (a group) | a vertical `Layout` (see `Panel` in NodeEditorView) |
| `GUILayout.Label("const")` | `KodeWindow.Label(p, "#key or text", flexible)` |
| `GUILayout.Label(changingText)` | `w.BoundLabel(p, () => text, flexible)` (pushed only on change; first value sizes the cell, never start empty) |
| `GUILayout.Button` | `KodeWindow.Button(p, text, action, flexible)` |
| button whose text changes | `UIButton b = KodeWindow.Button(...)` + `w.AddSync(...)` updating `b.Text(...)` on change |
| `GUILayout.RepeatButton` | `KodeWindow.RepeatButton(p, text, action)` (fires at once, then every 0.2 s) |
| `GUILayout.Toggle` | `w.Toggle(p, text, () => get, v => set)` (a switch); `w.CheckBox` for a checkbox; coloured: the `Func<KodeWindow.Tone>` overload |
| `GuiUtils.SimpleTextBox(label, editable, unit, width)` | `EditUi.Row(w, p, label, () => ed, unit, width)`, or Row + Label + `EditUi.Field(w, row, ed, width)` + Label(unit); a field is a number by default (on the right); a text field (a name, a title, a filter) passes `kind: FieldKind.Text` (on the left: LAYOUTS.md, "Where a field's text lines up") |
| `GuiUtils.ToggledTextBox` | Row + `w.Toggle` + `EditFields.Field` + unit label |
| a field whose recent values are worth offering again (a target altitude, a node time) | `EditFields.Field(...).History("window.quantity")`: Up/Down in the focused field walk the values the player committed (LAYOUTS.md, "Recent values in a field") |
| an editable shown as a value, edited now and then (a custom window's editable items) | `EditUi.Inline(grid, label, editable, unit, width)`: a read-out that turns into a field when clicked; written once at Enter or focus loss, Escape writes nothing (LAYOUTS.md, "A read-out edited in place") |
| `GuiUtils.ArrowSelector` | `w.ArrowSelector(p, get, set, count or () => count, () => label)`; the views use `new ComboBox(w, p, get, set, () => names)`, a dropdown drawn as `[<] entry [>]` when MechJeb's "no drop-down menus" setting is on |
| `GuiUtils.ComboBox.Box` / `GuiUtils.DropDownMenu` | `new ComboBox(w, p, get, set, () => entries)`; a list that can be long (bodies, sites, a pack's anything) adds `{ Filter = true, Keys = () => englishNames }`: from 12 entries its popup has a filter field and builds only the rows in view (`Headless.ComboModel`, LAYOUTS.md "Typeable drop-down") |
| `GUILayout.SelectionGrid` / `Toolbar` | `w.SelectionGrid(p, items, get, set)`; the views use `w.Segmented(p, items, get, set)` (`ChoiceStyle.Tabs` for navigation) |
| `GUILayout.HorizontalSlider` | `w.Slider(p, get, set, min, max)` |
| a graph (series against time or distance) | `new LineGraph(w, p, width, height, labels, scrollbarParent, view)`; X pans and zooms through `graph.View` (`ViewportModel`): give it `SetData` and `SetDefaultSpan`, refresh when `graph.ViewMoved` (KodeKit/LAYOUTS.md, Graphs); a range and marked points with `graph.EnableRanges(model)` (`RangeModel`), their values with `SampleStats`; the values under the pointer with `graph.EnableReadout()` and `Readout.AddRow(samples, name, format, shown, colour)` (`GraphReadoutModel`) |
| events or burns along time (a timeline) | `new Timeline(w, p, new TimelineModel(view), width)` (`TimelineModel`: spans and instants on lanes, laid out in rows without overlap, grouped when too close, one selection kept by id, a "now" line); on a graph's `View` it pans and zooms with the graph (KodeKit/LAYOUTS.md, Timeline) |
| `GUILayout.TextArea` / long help text | `KodeWindow.Paragraph(p, text)` (wraps) |
| a destructive button (delete, reset), or a question before it | `UIButton b = null; b = KodeWindow.Button(p, text, () => Dialogs.Confirm(b.gameObject, "#title", "#message", "#confirm", action), true)`: a modal dialog, Cancel the default (LAYOUTS.md, "Dialogs") |
| a short text asked for (a rename) | `Dialogs.Prompt(owner, "#title", "#message", initial, valid, "#confirm", text => ...)` |
| tooltip | `Tooltips.Add(gameObject, "#key or text")` |
| a keyboard key for a window or an action | nothing to write for a window: every menu window is a toggle action (`window:<module type>`) the player can bind in Settings > Keyboard shortcuts, none bound by default. A new kind of action goes in `Shortcuts.Actions` (`new UiAction(id, kind, label, run, available)`), never one that engages an autopilot without its own review; never read `Input.GetKey*` for a shortcut in a view: every key goes through the map's rules (LAYOUTS.md, "Keyboard shortcuts") |
| a command in the command palette | a window is in it by itself (from `Shortcuts.Actions`). Anything else goes in `MuMech.UI.PaletteCommands`: `new PaletteCommand(id, () => label, "English name", source => run)`, with `.When(can, () => why)` when it can't always run (it is then listed greyed with the reason), `.ListedWhen(...)` when it means nothing now, and `.Confirm(() => whatItDoes)` for **every command that changes the flight** (an autopilot, a node, a vessel setting): the palette runs it only on a second Enter (LAYOUTS.md, "Command palette") |
| a value on a scale with limits, drawn as a bar (a throttle against its limits) | `var g = new Headless.GaugeModel(() => value, min, max, low, high, optimum, (b, v) => b.Append(v * 100, 0).Append(" %"));` then `w.Meter(p, "#key", g, "100 % high")`. Bounds are delegates (null: none); the zones and ratings follow HTML's meter (LAYOUTS.md, "Meter"). Only real bounds a setting gives: an optimum is derived from those limits only (an invented one is a claim MechJeb doesn't make). A new bar in a flight window ships off by default behind a setting |
| an effect shown while a choice is hovered, before it is picked (the planner's ghost orbit) | `var m = new PreviewModel<int>(() => committed, k => hasPreview(k))`; `combo.EntryHovered += (k, s) => m.Hover(k, s, Time.unscaledTime)`, `combo.EntryLeft += m.Leave`, `combo.Model.Closed += m.Revert`, `combo.Picked += (k, s) => m.Commit(s)`; draw on `m.Previewed`, remove on `m.Reverted`, `m.Tick(now)` in a sync. Never call the setter or place a node for a preview; off by default behind a setting; a drawing that outlives its window, vessel or scene is wrong information (LAYOUTS.md, "Previews") |
| a tooltip with live values or several lines (a rich tooltip) | `Tooltips.Add(gameObject, card => { card.Title(...); card.Value("#label", ct => ct.Append(...)); card.Text(...); }, key, Tooltips.BesideWindow(row))`: a hover card, its values written 4 times a second without garbage, beside the window at its scale (LAYOUTS.md, "Hover cards"); a value a card shows must be computed anyway: never enable a module or call a getter only for a card (`InfoItemCard.Computed`) |
| a notice after a click (a copy, a delete) that has nowhere to show | `Toasts.Show("#key", ToastSeverity.Success)` (KSP's `ScreenMessages` stay for in-flight notices); after a confirmed destructive action, post it from the dialog's confirm action, once the action ran (Delete window, Restore factory defaults) |
| title-bar extra button | `w.TitleButton("E", action)` |
| a right-click on a value (copy it, act on it) | `KodeWindow.ContextMenu(row.gameObject, () => name, () => new List<MenuEntry> { MenuEntry.Caption(name), MenuEntry.Item("#key", source => action, () => enabled) })`: a pointer-only menu (no keyboard lock, no key read; a press outside, the pointer leaving or any key closes it, and the key still does its job); every entry must also exist elsewhere (a button, an icon): the menu is never the only way. Only where a menu is wanted: the row becomes a raycast target. None on a window that lets the pointer through. A title icon (`w.TitleIcon`) is in the title's menu by itself (LAYOUTS.md, "Context menus") |
| scrolling list | KodeUI `ScrollView` (`KodeUI/ScrollView.cs`) |
| a long list the user filters by typing | a field (`EditFields.Field`) over a `VirtualList`, `Headless.FuzzyFilter` ranking the items (label and English name); a keystroke sets the list's count and binds the rows in view again (`CustomWindowEditorView`); type-to-jump on a focused list: `Headless.TypeaheadModel` |
| a long list (`GUILayout.BeginScrollView` over a loop of rows) | `VirtualList.ListRows(w, p, height, count, i => text, i => click)` for one-line clickable rows, or `new VirtualList<TRow>(w, p, height, count, create, rowRoot, bind, spacing)` for rows of widgets: only the rows in view are built (`Headless.VirtualListModel`), rows of one height or measured ones (`SetRowHeights`), one row may be taller (`SetExpanded`) (`WaypointView`) |
| a list reordered with Up/Down buttons, items added from a second list | keep the buttons through `Headless.ReorderModel.MoveBy`, and add drag on the rows: `ListDrag.ForReorder(w, list, reorder, label)` and `ListDrag.ForTransfer(w, source, target, transfer, label)` (drop indicator, auto-scroll near the ends; a release outside cancels) (`CustomWindowEditorView`) |
| items grouped by category, or anything nested (folding headers over a flat list) | `Headless.TreeCollection<T>` (keys, `AddBranch`/`Add`, its `TreeState` kept on the module when the view is rebuilt) drawn by `new TreeList<T>(w, p, height, tree, row => text, (node, row) => click)`: a virtual list of the visible rows, a click folds a branch, `Find(query)` opens the branches with a match; a drag out of it with `ListDrag.ForTransfer` and `TransferModel.Allow` refusing branch rows (`CustomWindowEditorView`, "All") |

A `[GeneralInfoItem]` method is ported once, as a builder in `src/InfoItems/` registered in `GeneralItemBuilders` by id
(`General:<Module>.<Method>`); windows that draw the same rows (Utilities, Landing Guidance, Warp Helper) call
`GeneralItemBuilders.Build` instead of duplicating them. Lists whose length changes use `LabelValueRows` (row pool).

A window that can be resized adds `ResizeGrip.Add(w, resized, done)`: `resized(width, height)` gets the size from
the drag (`Headless.ResizeModel`), in the window's units, and the view clamps it to its own limits. Moving, clamping and
snapping are KodeKit's (`WindowBehaviour.Frame`, a `Headless.WindowFrameModel`); a view never moves its window during a
drag.

Popups place and close themselves through KodeKit: a drop-down's list, a dialog and the toast stack use `Headless.PopupPlacer`
(below the anchor, flipped above when there is no room, clamped on screen, at the window's scale) and the shared popup stack
`Popups.Layers` (outside press, Escape through the keyboard scope, the owner closing, a scene change). A view never places a
popup itself; a new kind of popup opens a layer with `Popups.Push` and places itself with the placer, as the context menu
does (LAYOUTS.md, "Where a popup goes and what closes it", "Context menus"). A hover card is placed by the placer too, but it is no layer: it
catches no pointer and closes on any press.

Live numbers that change every frame: prefer `CharText` (garbage-free formatting, `KodeKit/CharFormat.cs`) in a
`KodeWindow.LiveLabel(p, template, flexible)` cell, committed in a sync. Strings are fine for things that change rarely.

A table of live numbers (stages, bodies, a list of values per row) is a `Headless.TableModel` drawn by `TableGrid`
(LAYOUTS.md, "Tables"): the view gives the row count, a formatter that writes a cell into its `CharBuffer`, and the columns'
visibility and headers from MechJeb's state; it calls `model.Sync()` then `grid.Draw()` in its refresh. Don't build a grid of
labels by hand: the model keeps the widths growing only, the cells' buffers, the highlighted row and the selection. To let
players copy it, add `StageStatsTable.CopyButtons(parent, () => model)` (Copy and Copy table as Markdown: every row, the shown
columns); a column of buttons gets `Copied = false`.

Several rows of a list picked at once (remove or move them together) are a `Headless.SelectionSet` drawn by
`SelectionColumn.Add` (LAYOUTS.md, "Several rows selected"): a check box per row, never a Shift or Ctrl click (Left Shift and
Left Ctrl are KSP's throttle keys), and each action on the checked rows one commit of the list.

## Edits go through the widgets

A setter passed to a KodeKit widget runs only on the user's edit, through the window's commit path (`w.Commits`, see
`KodeKit/LAYOUTS.md`, "Edits and commits"): the setter, then `Edited` (the module is marked dirty), then a commit with the
value before and after and its source, which the agent bridge and an undo stack read. Code that changes a value directly
(a button's action, `BeforeSync`) makes no commit. To make a change that tests, agents or undo see, go through the widget's
target: `w.Commits.Find(control).Write(value, ChangeSource.Program)`.

Each input widget (field, switch, check box, Switch3, slider) draws a headless model, its commit target's `Model`
(LAYOUTS.md, "Input widgets on models"). A view that needs to change a widget's value as the player would (a test, the agent
bridge) calls the model (`ToggleModel.Flip`, `SliderModel.Set`, a field's `TextFieldModel.Begin`/`Type`/`Submit`) rather than
setting the uGUI control. A field's Escape is the model's: it writes back the text the edit began with through the setter,
so a view's `onEndEdit` listener sees the value as it was (as before); read the module, not the listener's text argument.

A view that edits a module's list in place (moves, adds, removes: the custom window editor's items) makes each change a commit
of one target for the list, `target.Apply(asked, act, source)` with the source of the gesture, and gives the target an
`IUndoState` of the list (`CustomWindowEditorView.ItemsTarget`). View state (a graph's zoom, a range picked on it, a timeline's
selection, folds, a highlight) is no commit: no module stores it and no undo takes it back. Notifications a view subscribes
to are events (`+=`); never assign a model's or a scope's event, and keep a model's owner callbacks (`Allow`, `Guard`) to the
view that owns the widget.

Every window has an undo stack (`w.Undo`, LAYOUTS.md "Undo and redo"), which records nothing until the view opts in. To give
a window undo, call `w.UndoButtons()` at the end of `BuildContents`: it turns recording on and adds the undo and redo icons
(there are no undo keys). Before that, set `w.Undo.Guard` to refuse the settings an engaged autopilot reads (the planner's
node executor settings, tagged with `w.Commits.Tagging("executor")`, while it flies a node).

**The rule: undo never re-engages a flight mode or an autopilot.** A window opts in only when none of its undoable setters
engages one: a setter that does (the Translatron's mode, SmartASS's, an autopilot's on/off) is marked `Undoable = false` on
its target, or the window doesn't opt in. A window that never opts in has no entries, so `KodeWindow.History` (the agent
bridge's `undo(source)`) can't write its setters back either. Buttons (Execute, Abort, Stage) are not commits and never undone.
Today the Maneuver Planner and the Node Editor opt in; their setters write settings and planner values only.

When a control's value isn't what its edit changes (a field holding a shift, a button moving a node), give its target an
`IUndoState` (`w.Commits.Find(control).State`) that takes and restores the real state, and refuses a restore that would
re-plan what the vessel has already done. Every change of that state must be a commit of the target (the node editor applies
a typed Δv with `target.Apply` when the field's edit ends): the stack undoes a state only while it is still what the change
left, so a change outside the path (a gizmo drag, KSP's own node panel) makes the entry stale rather than lost. Typing that
hasn't reached the state is no undo step. The node editor's state is the node's planned burn: undoing a Δv edit while flying
that burn by hand puts the planned burn back on the node, nothing already burnt (its guard holds the undo only while the node
executor flies the node).

**Name every control.** An agent, the pattern checks and an accessibility tree find a control by its name (LAYOUTS.md,
"Names and descriptions"). A control with a label right before it in its row is named by it; one after a switch, a button, a
cell or another field, one alone on its row, or a drop-down or segmented bar with no text before it needs a name: pass
`label:` (`name:` for `ComboBox`, `ArrowSelector` and an empty `CheckBox`), a `'#key'` where one exists, or a new
`#MechJeb_NewUI_Label_*` key in `en-us.cfg` and `zh-cn.cfg`. The pattern checks fail on a model without a name (in
MechJebLibTest, and in game over the bridge's controls: MechJeb2_UI's Spike 141). A control's tooltip is
its description: put a tooltip on the control or its row (`Tooltips.Add`), or add `key + "_tooltip"` for its label's key.

**The agent bridge sees what a view builds.** `UiAgent` (LAYOUTS.md, "The agent bridge") lists a window's commit targets,
buttons, folding headers, read-outs, tables and dialog from the objects on screen, and acts through the models: a view needs no
registration for it. What it needs: every value control a KodeKit widget (a setter on the commit path), every control named
(above), and a list a view edits in place exposed on its target (`target.Model = new ListEdits { Reorder, Transfer }`, the custom
window editor's items).

## A widget's behaviour: headless models

KodeKit widgets draw models in `KodeKit/Headless/` that hold their behaviour without Unity (`ChoiceModel` for `Segmented`,
`ComboModel` for `ComboBox`, `TableModel` for `TableGrid`, and so on), tested in MechJebLibTest. A view rarely makes one
itself: it gets one from the widget (`Segmented`'s, a commit target's `Model`) or passes one in where the widget asks
(`TableModel`, `GaugeModel`, `PreviewModel`, `TreeCollection`, `TimelineModel`, `SelectionSet`). A new widget, or new
behaviour on an existing one, goes in a model there first: the rules (C# 7.3, no Unity type, `IDescribeParts` or
`[NotAWidget]`, events for notifications, its tests and pattern-check sample) and an index of every model are in
`KodeKit/LAYOUTS.md`, "The headless pattern".

## Rules (each was a real bug)

- A label whose text changes must not drive layout: use `BoundLabel`/`LiveLabel` (fixed-size cells). Size them from
  a realistic template; never build one with `""`.
- Bindings run every frame, even while their row is hidden: getters must not throw (null vessel, no target, no
  maneuver nodes, `patchedConicSolver == null`, editor scene where `Vessel` is null). Guard them.
- Don't allocate per frame in getters (string concatenation, `Localizer.Format`, LINQ): cache by value.
- A modifier for a button must not be a flight key: a pointer over a window leaves the keyboard to KSP, so Left Shift
  (throttle up), Left Ctrl (throttle down) and the like reach the vessel while held over a button. The Stepper's ×10 is
  Right Shift or Left Alt for this reason. Right Alt is not safe either: on Windows, AltGr (the right Alt of AZERTY and
  QWERTZ layouts) also sends Left Ctrl. A key read while a field has the keyboard (and its lock) can use any modifier.
- A field's end-of-edit action (an `onEndEdit` listener: a typed value applied on Enter or focus loss) runs once per edit: a
  rebuild during the edit doesn't run it, since the edit goes on in the rebuilt field, unless that field is gone. Keep such
  actions in the field's `onEndEdit`, where focus recovery holds them back, not in code run when the window is destroyed.
- An input history key (`.History(key)`) names the quantity, never a label or a field's position: the lists are saved, and a
  key that changed meaning would offer one field's values in another. Opt a field in only when its old values are worth
  typing again; never a stepper's field (its Up and Down nudge).
- An inline edit writes only when its edit ends, so its parse check must not write: pass the editable's `Parses(text)`
  (`EditUi.Inline` does), never a check that sets the text and reads `IsParsed`.
- Anything that takes the keyboard (a popup that reads keys, a dialog, a palette, a field drawn by hand) goes through a
  `KeyboardScope` (LAYOUTS.md, "Keyboard scopes"): it holds KSP's keyboard lock, lets it go only once Escape and Enter are
  up, turns uGUI's navigation off when asked, and gives the keys to the top scope only. Never read keys with
  `Input.GetKeyDown` in a popup of its own, nor take `ControlTypes.KEYBOARDINPUT` by hand: a letter would fly the vessel, an
  Escape would pause the game behind the popup or close the popup under it too. A helper that reads keys in a focused field
  (Up and Down, Tab) asks the field's scope for them, `field.GetComponent<FieldKeyboardLock>().Route(keys, handler)`, so it
  gets none while a dialog is over the field.
- A Unity-side handler reports `KodeWindow.Pointer` or `KodeWindow.Source(ChangeSource.Keyboard)`, never the literal source:
  an agent's event inside `CommitPath.As(ChangeSource.Agent)` would be the player's (a unit test reads the sources).
- A button that destroys what can't be brought back (a window, the settings) asks first with `Dialogs.Confirm`, its owner the
  button itself: the dialog goes when the window is rebuilt, and its action runs only on confirm. Never ask twice (a dialog
  after an inline confirmation), and never ask before an action that must be instant (Abort, Disengage).
- A look that changes with a state (on, chosen, hovered, pressed, disabled, focused) comes from the skin's rules: a
  `StyledPart` with a `Headless.StyleParts` name, its On flag set from the model (LAYOUTS.md, "State looks from rule
  tables"). Never paint a state in a closure with colours of its own, nor a uGUI colour-tint transition (its tween makes
  garbage on every pointer event); a new kind of part gets its rules in `Headless.Skins.Common`, for every skin at once.
- Create everything in `BuildContents`, or from a pool that a sync grows and reuses (the waypoint rows, the custom
  window editor's lists, `LabelValueRows`, the stage stats cells): never create UI objects in a sync without one.

## Building

From the repository root: `dotnet build MechJeb2.UI/MechJeb2.UI.csproj -c Release`. A worktree needs
`Directory.Build.props.user` (copy it from the main checkout), or `-p:KspDir=` for a build that copies nothing. The tests,
the headless models' included: `dotnet test MechJebLibTest/MechJebLibTest.csproj` (the build compiles `KodeKit/Headless` at
C# 7.3 too, `Tools/HeadlessCSharp73`). `KodeUI/` is a copy of the sources of
https://github.com/sarbian/KodeUI (MIT, see `KodeUI/LICENSE.md`): change KodeUI there and copy it back, without the
UTF-8 BOMs its files have, which CI rejects.
Don't set `KSPDIR` (it copies the build into KSP).
