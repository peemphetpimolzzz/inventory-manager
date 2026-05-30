// Tests share a single SQL Server database, so they must not run concurrently.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
