# RussellTableGenerator

Generates `MechJebLib/Lambert/RussellGuessTables.cs`, the interpolation tables for the initial guesses of Russell's
vercosine Lambert solver (`MechJebLib/Lambert/Russell.cs` and `RussellGuess.cs`).

The tables follow the interpolation scheme of Russell, R. P., "Complete Lambert Solver Including Second-Order
Sensitivities," Journal of Guidance, Control, and Dynamics 45.2 (2022), Section II.D. The coefficients are fit to
solutions of Lambert's equation computed here; nothing is taken from ivLam.

Regenerate the tables (takes about 15 seconds, runs under mono outside of Windows) with:

    dotnet run -c Release --project Tools/RussellTableGenerator

or write them somewhere else with `dotnet run -c Release --project Tools/RussellTableGenerator -- <output path>`.

The output is deterministic, so a regenerated file should only differ when the generator or the solver it uses
(`Russell.GetW`, `Russell.TofDerivs`, `Russell.MultiRevBottom`, `RussellGuess`) changes. Check the result with the
Russell tests: `dotnet test MechJebLibTest --filter FullyQualifiedName~RussellTests`.

This project is built with the solution (so CI keeps it compiling) but is not part of the MechJeb release.
