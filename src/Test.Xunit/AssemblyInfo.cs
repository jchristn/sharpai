// Touchstone descriptors share suite-level state through BeforeSuite/AfterSuite lifecycle hooks and are
// designed to run sequentially. Disable xUnit's default test parallelization so the fact-style RunAll
// (which owns suite setup/teardown) does not run concurrently with the per-case theory rows.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
