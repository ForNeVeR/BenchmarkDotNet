﻿using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkConfiguration
    {
        public BenchmarkMode Mode { get; }
        public BenchmarkPlatform Platform { get; }
        public BenchmarkJitVersion JitVersion { get; }
        public BenchmarkFramework Framework { get; }
        public BenchmarkToolchain Toolchain { get; }
        public BenchmarkRuntime Runtime { get; }
        public int WarmupIterationCount { get; }
        public int TargetIterationCount { get; }

        public string Caption => Mode + RuntimeSummary + "_" + Platform + "_" + JitVersion + "_NET-" + Framework;

        private string RuntimeSummary
        {
            get
            {
                string result = string.Empty;
                switch (Toolchain)
                {
                    case BenchmarkToolchain.Classic:
                        switch (Runtime)
                        {
                            case BenchmarkRuntime.Clr:
                                result = string.Empty;
                                break;
                            default:
                                result = Runtime.ToString();
                                break;
                        }
                        break;
                }
                return (string.IsNullOrEmpty(result) ? string.Empty : "_") + result;
            }
        }

        public BenchmarkConfiguration(
            BenchmarkMode mode,
            BenchmarkPlatform platform,
            BenchmarkJitVersion jitVersion,
            BenchmarkFramework framework,
            BenchmarkToolchain toolchain,
            BenchmarkRuntime runtime,
            int warmupIterationCount,
            int targetIterationCount)
        {
            Mode = mode;
            Platform = platform;
            JitVersion = jitVersion;
            Framework = framework;
            Toolchain = toolchain;
            Runtime = runtime;
            WarmupIterationCount = warmupIterationCount;
            TargetIterationCount = targetIterationCount;
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(Mode), Mode.ToString());
                yield return new BenchmarkProperty(nameof(Platform), Platform.ToString());
                yield return new BenchmarkProperty(nameof(JitVersion), JitVersion.ToString());
                yield return new BenchmarkProperty(nameof(Framework), Framework.ToString());
                yield return new BenchmarkProperty(nameof(Toolchain), Toolchain.ToString());
                yield return new BenchmarkProperty(nameof(Runtime), Runtime.ToString());
                yield return new BenchmarkProperty(nameof(WarmupIterationCount), WarmupIterationCount.ToString());
                yield return new BenchmarkProperty(nameof(TargetIterationCount), TargetIterationCount.ToString());
            }
        }

        public string ToCtorDefinition()
        {
            var builder = new StringBuilder();
            builder.Append($"{nameof(Mode).ToCamelCase()}: {nameof(BenchmarkMode)}.{Mode}, ");
            builder.Append($"{nameof(Platform).ToCamelCase()}: {nameof(BenchmarkPlatform)}.{Platform}, ");
            builder.Append($"{nameof(JitVersion).ToCamelCase()}: {nameof(BenchmarkJitVersion)}.{JitVersion}, ");
            builder.Append($"{nameof(Framework).ToCamelCase()}: {nameof(BenchmarkFramework)}.{Framework}, ");
            builder.Append($"{nameof(Toolchain).ToCamelCase()}: {nameof(BenchmarkToolchain)}.{Toolchain}, ");
            builder.Append($"{nameof(Runtime).ToCamelCase()}: {nameof(BenchmarkRuntime)}.{Runtime}, ");
            builder.Append($"{nameof(WarmupIterationCount).ToCamelCase()}: {WarmupIterationCount}, ");
            builder.Append($"{nameof(TargetIterationCount).ToCamelCase()}: {TargetIterationCount}");
            return builder.ToString();
        }
    }
}