# Debugging

## Source debugging

Packages include SourceLink metadata in portable PDBs, distributed in the matching
`.snupkg` symbol packages. Enable Source Link support and the NuGet.org symbol server
in your debugger to step into the source for the exact package commit.

Build with the .NET 10 SDK from a Git checkout with a GitHub remote. SourceLink is
provided by the SDK for all supported target frameworks; it is not a runtime dependency.
GitHub Actions builds normalize source paths, while local builds retain local paths.
Generated source not tracked by Git is embedded in the PDB.
