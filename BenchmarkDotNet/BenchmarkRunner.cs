﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Plugins.Toolchains;
using BenchmarkDotNet.Plugins.Toolchains.Results;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IBenchmarkPlugins plugins = null)
        {
            Plugins = plugins ?? BenchmarkPluginBuilder.CreateDefault().Build();
        }

        private IBenchmarkPlugins Plugins { get; }

        private static int benchmarkRunIndex = 0;

        internal IEnumerable<BenchmarkReport> Run(List<Benchmark> benchmarks, string competitionName = null)
        {
            benchmarkRunIndex++;
            if (competitionName == null)
                competitionName = $"BenchmarkRun-{benchmarkRunIndex:##000}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}";
            using (var logStreamWriter = new StreamWriter(competitionName + ".log"))
            {
                var logger = new BenchmarkCompositeLogger(Plugins.CompositeLogger, new BenchmarkStreamLogger(logStreamWriter));
                var reports = Run(benchmarks, logger);
                Plugins.CompositeExporter.ExportToFile(reports, competitionName);
                return reports;
            }
        }

        private List<BenchmarkReport> Run(List<Benchmark> benchmarks, IBenchmarkLogger logger)
        {
            logger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
            logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.Description}");
            logger.NewLine();

            var importantPropertyNames = benchmarks.Select(b => b.Properties).GetImportantNames();

            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                if (benchmark.Task.ParametersSets.IsEmpty())
                {
                    var report = Run(logger, benchmark, importantPropertyNames);
                    reports.Add(report);
                    if (report.Runs.Count > 0)
                    {
                        var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                        logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                        logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                    }
                }
                else
                {
                    var parametersSets = benchmark.Task.ParametersSets;
                    foreach (var parameters in parametersSets.ToParameters())
                    {
                        var report = Run(logger, benchmark, importantPropertyNames, parameters);
                        reports.Add(report);
                        if (report.Runs.Count > 0)
                        {
                            var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                            logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                            logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                        }
                    }
                }
                logger.NewLine();
            }
            logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            logger.NewLine();

            BenchmarkMarkdownExporter.Default.Export(reports, logger);

            var warnings = Plugins.CompositeAnalyser.Analyze(reports).ToList();
            if (warnings.Count > 0)
            {
                logger.NewLine();
                logger.WriteLineError("// *** Warnings *** ");
                foreach (var warning in warnings)
                    logger.WriteLineError($"{warning.Message}");
            }

            logger.NewLine();
            logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return reports;
        }

        private BenchmarkReport Run(IBenchmarkLogger logger, Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters = null)
        {
            var toolchain = Plugins.CreateToolchain(benchmark, logger);

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmark.Description);

            var generateResult = Generate(logger, toolchain);
            if (!generateResult.IsGenerateSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var buildResult = Build(logger, toolchain, generateResult);
            if (!buildResult.IsBuildSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var runReports = Exec(logger, benchmark, importantPropertyNames, parameters, toolchain, buildResult);
            return new BenchmarkReport(benchmark, runReports, parameters);
        }

        private BenchmarkGenerateResult Generate(IBenchmarkLogger logger, IBenchmarkToolchainFacade toolchain)
        {
            logger.WriteLineInfo("// *** Generate *** ");
            var generateResult = toolchain.Generate();
            if (generateResult.IsGenerateSuccess)
            {
                logger.WriteLineInfo("// Result = Success");
                logger.WriteLineInfo($"// {nameof(generateResult.DirectoryPath)} = {generateResult.DirectoryPath}");
            }
            else
            {
                logger.WriteLineError("// Result = Failure");
                if (generateResult.GenerateException != null)
                    logger.WriteLineError($"// Exception: {generateResult.GenerateException.Message}");
            }
            logger.NewLine();
            return generateResult;
        }

        private BenchmarkBuildResult Build(IBenchmarkLogger logger, IBenchmarkToolchainFacade toolchain, BenchmarkGenerateResult generateResult)
        {
            logger.WriteLineInfo("// *** Build ***");
            var buildResult = toolchain.Build(generateResult);
            if (buildResult.IsBuildSuccess)
            {
                logger.WriteLineInfo("// Result = Success");
            }
            else
            {
                logger.WriteLineError("// Result = Failure");
                if (buildResult.BuildException != null)
                    logger.WriteLineError($"// Exception: {buildResult.BuildException.Message}");
            }
            logger.NewLine();
            return buildResult;
        }

        private List<BenchmarkRunReport> Exec(IBenchmarkLogger logger, Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters, IBenchmarkToolchainFacade toolchain, BenchmarkBuildResult buildResult)
        {
            logger.WriteLineInfo("// *** Exec ***");
            var processCount = Math.Max(1, benchmark.Task.ProcessCount);
            var runReports = new List<BenchmarkRunReport>();

            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");
                if (parameters != null)
                    logger.WriteLineInfo($"// {parameters.ToInfo()}");
                if (importantPropertyNames.Any())
                {
                    logger.WriteInfo("// ");
                    foreach (var name in importantPropertyNames)
                        logger.WriteInfo($"{name}={benchmark.Properties.GetValue(name)} ");
                    logger.NewLine();
                }

                var execResult = toolchain.Exec(buildResult, parameters, Plugins.CompositeDiagnoser);

                if (execResult.FoundExecutable)
                {
                    var iterRunReports = execResult.Data.Select(line => BenchmarkRunReport.Parse(logger, line)).Where(r => r != null).ToList();
                    runReports.AddRange(iterRunReports);
                }
                else
                {
                    logger.WriteLineError("Executable not found");
                }
            }
            logger.NewLine();
            return runReports;
        }
    }
}