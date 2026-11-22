# Test it

1. **Build and unit tests:** "Building" in `MechJeb2.UI/PORTING.md`, the KodeKit tests from SKILL.md, then the whole suite as CI runs it: `dotnet test MechJebLibTest/MechJebLibTest.csproj --filter "FullyQualifiedName!~PSGTests.AscentTests.KerbinTests.MainSailTinCan"`. No new warning in the files touched.
2. **CI runs the tests on Mono:** compare sequences of enums with `AssertionExtensions.EqualSequence`, and see "How to test a model" in LAYOUTS for allocation checks there.
3. **Break it on purpose:** for each new rule, break the code (return early, drop the call), see a fact fail, restore. A fact that still passes tests nothing: fix it. Report what was caught and what was missed.
4. **In game,** when the developer has KSP and asked for it: never launch KSP, deploy into it or send real mouse and keyboard input without asking. Use a Debug build with its PDBs. Clicks go through uGUI's event system, never by calling the setter.

What the change calls for in game:

| The change | Check |
|---|---|
| Draws something or changes text | UI scale 1 and 1.5, the Flat and Classic skins, zh-cn for new text; look at every screenshot |
| Adds or renames a control | The agent check in [agent-ready.md](agent-ready.md) |
| Runs per frame, per keystroke or at build | Garbage and time against dev |
| Reads input or lays out a window | A second OS, when one is available |

What wasn't run goes under "Not verified" in the report. Never report a run you didn't make.
