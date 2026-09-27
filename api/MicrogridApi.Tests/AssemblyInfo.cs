// =============================================================================
// File: AssemblyInfo.cs
// Description: Disable xUnit parallelization — Viman MongoDbIndexConfigurator uses
//              process-wide static verification state shared across unit tests.
// Author: Suwani (Component 4 integration safety)
// =============================================================================

using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
