using System.Reflection;
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

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a903d9fe-4604-47b8-b9d9-95728538f769")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.5.1.0")]     // We should not change it anymore. It break mods that links MJ ( cf http://support.microsoft.com/kb/556041 )
[assembly: AssemblyFileVersion("2.9.2.0")] // this one we can change all we want
[assembly: AssemblyInformationalVersion("")] // Displayed in the window title if not empty (used to display dev #)

[assembly: KSPAssembly("MechJeb2", 2, 5)]
