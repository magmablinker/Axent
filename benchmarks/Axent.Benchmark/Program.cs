using Axent.Benchmark;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run([
    typeof(SourceGeneratedSenderBenchmarks),
    typeof(MediatorSenderBenchmarks),
]);
