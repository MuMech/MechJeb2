# Build a window, or add to one

Paths are from the repository root.

## Add to an existing window

| What | Where |
|---|---|
| A row, setting or button in a window | that window's `MechJeb2.UI/src/<Name>View.cs`; `OptionRows.cs` for a block several windows share |
| An info item (`[GeneralInfoItem]`) | a builder in `MechJeb2.UI/src/InfoItems/`, registered in `GeneralItemBuilders` by id |
| A maneuver operation's settings | a panel in `MechJeb2.UI/src/Operations/OperationPanels.cs`, in its `builders` |
| Logic that isn't drawing | an `internal` method on the module, where kOS and other views reach it |

## A new window

1. **Module** (`MechJeb2/`): a `DisplayModule` with `GetName()`, `IconName()` and the window's state. No drawing code.
2. **View** (`MechJeb2.UI/src/<Name>View.cs`): found by its attribute, nothing to register.
3. **Menu:** add the module to `MenuView.Group`, which matches type names and puts any it doesn't know in the last group.
4. **Tech:** a window that tech should lock also needs its lines in the `Parts/` cfgs.

## Texts

Every new `#MechJeb_NewUI_*` key goes in **both** `Localization/en-us.cfg` and `Localization/zh-cn.cfg`, which have the same set. The other languages have none.

## The shape of a view

```csharp
using KodeKit;
using KodeUI;

namespace MuMech.UI
{
    [KodeViewFor(typeof(MechJebModuleExampleWindow))]
    public class ExampleView : KodeView
    {
        readonly MechJebModuleExampleWindow window;

        public ExampleView(MechJebModuleExampleWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 240;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            w.SwitchRow(r, "#MechJeb_NewUI_ExampleOn", () => window.On, v => window.On = v);
            EditUi.Row(w, r, "#MechJeb_NewUI_ExampleLimit", () => window.Limit, "m/s²", 60);

            // no label before it in its row, so it gets a name
            new ComboBox(w, KodeWindow.Row(r), () => window.Choice, i => window.Choice = i, () => window.Choices,
                name: "#MechJeb_NewUI_Label_ExampleChoice") { Filter = true };

            Layout live = KodeWindow.Row(r);
            KodeWindow.Label(live, "#MechJeb_NewUI_ExampleNow", true);
            var now = new CharText(KodeWindow.LiveLabel(live, "000.00", false).tmpText);
            w.AddSync(() =>
            {
                now.Clear().Append(window.Now(), 2); // runs every frame, shown or not: it must not throw or allocate
                now.Commit();
            });

            UIButton reset = null;
            reset = KodeWindow.Button(r, "#MechJeb_NewUI_ExampleReset", () => Dialogs.Confirm(reset.gameObject,
                "#MechJeb_NewUI_ExampleReset", "#MechJeb_NewUI_ExampleResetAsk", "#MechJeb_NewUI_ExampleReset", window.Reset));
        }
    }
}
```

## Easy to get wrong

- A setter given to a widget is a commit. A button's action and `BeforeSync` are not: agents and undo see the press, not the values it wrote. The module sets `Dirty = true` when a button changes saved settings.
- `w.Toggle` is a switch with its label. A checkbox is `w.CheckBox`.
- A label whose text changes is a `BoundLabel` or a `LiveLabel` with a realistic template, never "".
- A string shown every frame comes from `Memo.Of` or a `CharText`. Cast an enum key to int in `Memo.Of`.
- A list that can be long is a `ComboBox` with `Filter = true`, or a `VirtualList`.
- Rows that come and go use `w.Visible`. A change of structure sets `RebuildRequested`. Nothing is created in a sync outside a pool.
