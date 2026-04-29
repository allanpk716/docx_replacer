using System.Reflection;

//
// Manual assembly attributes for DocuFiller.
//
// GenerateAssemblyInfo is set to false in Directory.Build.props to avoid duplicate attribute
// errors in the WPF _wpftmp temporary project. However, without these explicit attributes,
// the C# compiler may default AssemblyVersion to 0.0.0.0 in certain CI environments
// (e.g. GitHub Actions with .NET 8 SDK), causing a BAML version mismatch at runtime.
//
// AssemblyVersion must remain 1.0.0.0 — WPF BAML hardcodes this version in resource URIs
// at compile time. Changing it would cause FileNotFoundException in InitializeComponent().
// Use <Version> in DocuFiller.csproj for NuGet/Velopack versioning instead.
//

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
