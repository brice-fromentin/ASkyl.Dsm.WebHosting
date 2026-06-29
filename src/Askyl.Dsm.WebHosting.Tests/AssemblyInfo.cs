using Xunit;

// Run test collections sequentially to avoid resource contention between
// CPU-heavy Roslyn analyzer tests and disk-heavy file I/O tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
