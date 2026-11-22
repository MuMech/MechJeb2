# Agent-ready, and driving the UI

## When you build

A window is agent-ready when an agent can find every control by a name that stays the same in every language, act on it, and see the change as its own.

- **Value controls** are KodeKit widgets. They are named by their own text or by the label right before them. One after a switch, button, cell or field, or alone in its row, gets `label:` (`name:` on `ComboBox`, `ArrowSelector` and an empty `CheckBox`).
- **Buttons** get their id from their `'#key'`. A literal text ("+", "Copy") or an icon has none: use a key, or `KodeWindow.NameButton(b, "name")`.
- **Descriptions** are tooltips: `Tooltips.Add`, or a `key + "_tooltip"` text.
- **A list edited in place** exposes `target.Model = new ListEdits { ... }`.
- **Nothing to register:** the bridge reads what is on screen.

To prove it in game: every value control has a label and every button an id, the ids are unique in the window and the same in en-us and zh-cn, and one act per control changes what it should.

## When you drive it

The bridge is off unless KSP was launched with `-mechjeb-ui-agent <port>`. It is HTTP on 127.0.0.1: the endpoints and the token are in `MechJeb2.UI/KodeKit/LAYOUTS.md`, "The agent bridge".

1. `GET /snapshot` within one window (`within` takes the module's type name), or `GET /query` for one control, rather than a snapshot of every window.
2. In anything kept (a test, a script), act by `id` and `within`. A ref (`w3.b2`) holds for this launch only.
3. Read the values in the answer, or `GET /changes`: `ok` alone doesn't prove the value took.

- It acts with no confirmation, up to 100 steps in one frame, on covered and scrolled-away controls too. A step that engages an autopilot engages it: act on a game only when its player asked for that.
- While a dialog or the palette is open, only that takes steps.
- A 503 means nothing pumps the server (loading, main menu, space centre, game in the background): retry later.
- `/undo` only reaches windows that opted into undo.
