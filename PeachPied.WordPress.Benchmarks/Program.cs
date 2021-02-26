using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Pchp.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    [Config(typeof(Config))]
    [MemoryDiagnoser]

    public class WpBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.LongRun
                    .WithLaunchCount(1)
                    .WithWarmupCount(25)
                    .WithIterationCount(25)
                    .WithUnrollFactor(4)
                    .WithInvocationCount(4)
                    .WithToolchain(CsProjCoreToolchain.From(
                        NetCoreAppSettings.NetCoreApp31
                            .WithTimeout(new TimeSpan(0, 0, 10)))));
            }
        }

        private const string SolutionDir = @"C:/iolevel/wpdotnet-sdk/";
        private static readonly string BenchmarksProjectDir = $"{SolutionDir}PeachPied.WordPress.Benchmarks/";
        private static readonly string WordPressProjectDir = $"{SolutionDir}wordpress/";

        private bool _assemblyLoaded;
        private string _configuration;

        private readonly MemoryStream binaryOutput = new MemoryStream();
        private readonly StreamWriter textOutput = new StreamWriter(new MemoryStream());

        private void LazyLoadPeachpieAssembly(string configuration)
        {
            if (!this._assemblyLoaded)
            {
                this._configuration = configuration;
                var assembly = Assembly.LoadFrom(@$"{WordPressProjectDir}/bin/{configuration}/netstandard2.0/PeachPied.WordPress.{configuration}.dll");
                Context.AddScriptReference(assembly);
                this._assemblyLoaded = true;
            }
        }

        public void RunBenchmark(string configuration)
        {
            LazyLoadPeachpieAssembly(configuration);

            // Reset streams
            this.binaryOutput.Position = 0;
            this.textOutput.BaseStream.Position = 0;

            // Initialize context
            using var ctx = Context.CreateConsole("index.php");
            ctx.RootPath = ctx.WorkingDirectory = WordPressProjectDir;
            ctx.OutputStream = this.binaryOutput;
            ctx.Output = this.textOutput;
            ctx.Server["HTTP_HOST"] = "localhost:5004";
            ctx.Server["REQUEST_URI"] = "/";

            // SALT
            ctx.DefineConstant("AUTH_KEY", "*Sr748b66z3R+(v%1z;|SCtBZz/cEvo1)mo|F&EO>5a^1aF6@C9^KIzG&MD?=Zmt");
            ctx.DefineConstant("SECURE_AUTH_KEY", "P]-!;,$G96Gf`8pO-1e;t%Y1hYfU{}lRdhgl#h./C`_gxJsd^`3[$yoz!pe4bX(U");
            ctx.DefineConstant("LOGGED_IN_KEY", "E$0Y`&8,IgAME5<OTD:*]x}$wEhEemY|2PVzQ!!96:F&0S{gu|S|TZ!} ^-l}xgh");
            ctx.DefineConstant("NONCE_KEY", "0)ET<zQ RlA$Gb5R*>UO]zKpgF-CxT?J0u8<m?;HhpAm!aY @qWTNI{A]>$Jow#>");
            ctx.DefineConstant("AUTH_SALT", "!|gQ:L<;]+F:mt<wV)]n &,7iv{D5dG+kLi<S$}Vp-*@Ev.+-P4p|lRQOnh]2jKV");
            ctx.DefineConstant("SECURE_AUTH_SALT", "wlk)xBD7EC0|zJCs&`&oK#3<O2THx,{=He|^]+PFwVN%{m38bK.||-]@-1:4,7}f");
            ctx.DefineConstant("LOGGED_IN_SALT", "g}oD ]M2)SMa^zPx(}~6RPXP{7{!|`(IQCnY.2xQHv4HxV9f8;CoH~+]M01w/o(y");
            ctx.DefineConstant("NONCE_SALT", "SVVq/47*B)T_&aFj.tN^c9U =uI>7QS+WSuR[leI+PpDbJ_K_fu06Qyrq~5s{3=-");

            ctx.Globals["peachpie_wp_loader"] = PhpValue.FromClass(new WpLoader(Enumerable.Empty<IWpPlugin>()));

            // Run the script
            var index = Context.TryGetDeclaredScript("index.php");
            try
            {
                index.Evaluate(ctx, PhpArray.NewEmpty(), null);
            }
            catch (ScriptDiedException died)
            {
                died.ProcessStatus(ctx);
            }
        }

        [Benchmark(Baseline = true)]
        public void Release() => RunBenchmark(nameof(Release));

        [Benchmark]
        public void PhpDocOverloadsBranch() => RunBenchmark(nameof(PhpDocOverloadsBranch));

        [Benchmark]
        public void CallSiteOverloadsBranch() => RunBenchmark(nameof(CallSiteOverloadsBranch));

        [Benchmark]
        public void PhpDocForce() => RunBenchmark(nameof(PhpDocForce));

        [Benchmark]
        public void PhpDocForceParams() => RunBenchmark(nameof(PhpDocForceParams));

        [Benchmark]
        public void PhpDocOverloadsStatic() => RunBenchmark(nameof(PhpDocOverloadsStatic));

        [Benchmark]
        public void CallSiteOverloadsStatic() => RunBenchmark(nameof(CallSiteOverloadsStatic));

        [Benchmark]
        public void PhpDocOverloadsDynamic() => RunBenchmark(nameof(PhpDocOverloadsDynamic));

        [Benchmark]
        public void PhpDocForceParamFunctions() => RunBenchmark(nameof(PhpDocForceParamFunctions));

        [Benchmark]
        public void UsageOverloadsStatic() => RunBenchmark(nameof(UsageOverloadsStatic));

        [Benchmark]
        public void UsageOverloadsBranch() => RunBenchmark(nameof(UsageOverloadsBranch));

        [GlobalCleanup]
        public void Cleanup()
        {
            string file = $"{BenchmarksProjectDir}proofs/{_configuration}.html";
            this.textOutput.Flush();
            File.WriteAllBytes(file, ((MemoryStream)this.textOutput.BaseStream).ToArray());
        }
    }
}
