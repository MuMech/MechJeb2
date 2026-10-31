# Porting a MechJeb window to the new UI (MechJeb2.UI)

MechJeb2.UI draws MechJeb's `DisplayModule` windows with **KodeKit** (`KodeKit/`), a retained uGUI + TextMeshPro
layer built on KodeUI. IMGUI stays the fallback: a window is drawn by the new UI only when Settings →
"Use the new UI for ported windows" is on **and** a view exists for its module type.

## How a port is wired

1. Create `src/<Name>View.cs`: a class deriving from `KodeView`, marked `[KodeViewFor(typeof(MechJebModuleX))]`,
   with a public constructor taking the module. `KodeHost` finds it by reflection; nothing else to register.
2. Override `MinWidth` (use the width from the module's `WindowOptions()`), and `BuildContents(KodeWindow w)`,
   called **once**. Build every row there; things that come and go use `w.Visible(go, () => condition)`.
3. Per-frame model work that IMGUI's `WindowGUI` did before drawing (selecting a node, refreshing a cache,
   pushing settings into a controller each frame) goes in `protected override void BeforeSync()`.
4. If the structure must change at runtime (a list grows), set `RebuildRequested = true`; the host rebuilds the view.

Examples: `UtilitiesView` (toggles with tones, fields, ImguiBlock fallback), `NodeEditorView` (selectors, repeat
buttons, state in the module), `SettingsView` (registry toggles, title, skin buttons).

## Port row for row

Follow the module's `WindowGUI` in order. Same texts (`#MechJeb_...` keys: pass the key string, KodeKit localizes
it and re-localizes on language switch), same conditions, same actions. Keep the logic in MechJeb: if the IMGUI
code computes something inline that the view also needs, move it into an `internal` method on the module and make
`WindowGUI` call it too, so both UIs share one implementation. Make private fields/methods `internal` when the view
needs them (`InternalsVisibleTo("MechJeb2.UI")` is set). Don't change behaviour.

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
| `GUILayout.Toggle` | `w.Toggle(p, text, () => get, v => set)`; coloured: the `Func<KodeWindow.Tone>` overload |
| `GuiUtils.SimpleTextBox(label, editable, unit, width)` | Row + Label + `EditFields.Field(w, row, () => ed.Text, s => ed.Text = s, width, () => ed.IsParsed)` + Label(unit) |
| `GuiUtils.ToggledTextBox` | Row + `w.Toggle` + `EditFields.Field` + unit label |
| `GuiUtils.ArrowSelector` | `w.ArrowSelector(p, get, set, count or () => count, () => label)` |
| `GuiUtils.ComboBox.Box` / `GuiUtils.DropDownMenu` | `new ComboBox(w, p, get, set, () => entries)` |
| `GUILayout.SelectionGrid` / `Toolbar` | `w.SelectionGrid(p, items, get, set)` |
| `GUILayout.HorizontalSlider` | `w.Slider(p, get, set, min, max)` |
| `GUILayout.TextArea` / long help text | `KodeWindow.Paragraph(p, text)` (wraps) |
| tooltip | `Tooltips.Add(gameObject, "#key or text")` |
| title-bar extra button | `w.TitleButton("E", action)` |
| scrolling list | KodeUI `ScrollView` (`KodeUI/ScrollView.cs`) |
| anything not worth porting now (a GeneralInfoItem method, a complex custom draw) | `new ImguiBlock(p, () => module.ImguiMethod())` (IMGUI drawn inside the new window) |

A `[GeneralInfoItem]` method is ported once, as a builder in `src/InfoItems/` registered in `GeneralItemBuilders` by id
(`General:<Module>.<Method>`); windows that draw the same rows (Utilities, Landing Guidance, Warp Helper) call
`GeneralItemBuilders.Build` instead of duplicating them. Lists whose length changes use `LabelValueRows` (row pool).

Live numbers that change every frame: prefer `CharText` (garbage-free formatting, `KodeKit/CharFormat.cs`) in a
`KodeWindow.LiveLabel(p, template, flexible)` cell, committed in a sync. Strings are fine for things that change rarely.

## Rules (each was a real bug)

- A label whose text changes must not drive layout: use `BoundLabel`/`LiveLabel` (fixed-size cells). Size them from
  a realistic template; never build one with `""`.
- Bindings run every frame, even while their row is hidden: getters must not throw (null vessel, no target, no
  maneuver nodes, `patchedConicSolver == null`, editor scene where `Vessel` is null). Guard them.
- Don't allocate per frame in getters (string concatenation, `Localizer.Format`, LINQ): cache by value.
- Create everything in `BuildContents`; never create UI objects in syncs.

## Building

From the repository root: `dotnet build MechJeb2.UI/MechJeb2.UI.csproj -c Release`. A worktree needs
`Directory.Build.props.user` (copy it from the main checkout). `KodeUI/` is a copy of the sources of
https://github.com/sarbian/KodeUI (MIT, see `KodeUI/LICENSE.md`): change KodeUI there and copy it back, without the
UTF-8 BOMs its files have, which CI rejects.
Don't set `KSPDIR` (it copies the build into KSP).
