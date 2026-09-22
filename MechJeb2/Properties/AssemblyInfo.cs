using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("MuMechLib")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Multiversal Mechatronics")]
[assembly: AssemblyProduct("MuMechLib")]
[assembly: AssemblyCopyright("Copyright © Multiversal Mechatronics 2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: InternalsVisibleTo("MechJebLibTest")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a903d9fe-4604-47b8-b9d9-95728538f769")]

// We use a 2.[Major].[Minor/Patch].0 naming convention (2.x is always fixed)
// - Dev versions should always be one major ver ahead of the master branch.
// - To release dev to master, just use the ver that was on master.
// - After releasing dev to master, bump the major ver in dev.
// - For patch releases to master, bump the minor version.
// To merge dev to master, this should work:
//   % git checkout master
//   % git merge -X theirs dev
// In the unlikely event that doesn't work: merge master to dev, but keep dev's tree, then merge dev into master:
//   % git checkout dev
//   % git merge -s ours master
//   % git checkout master
//   % git merge dev
[assembly: AssemblyVersion("2.16.0.0")] // This should be bumped for major versions/breaking changes when the 2nd number changes
[assembly: AssemblyFileVersion("2.16.0.0")] // this one is bumped every single time for both minor/patch
[assembly: AssemblyInformationalVersion("")] // Displayed in the window title if not empty (used to display dev #)

[assembly: KSPAssembly("MechJeb2", 2, 16, 0)] // this one is bumped every single time for both minor/patch
