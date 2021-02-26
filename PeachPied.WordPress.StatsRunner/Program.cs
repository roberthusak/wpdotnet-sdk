using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Pchp.Core;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.StatsRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            string configuration = args[0];
            string solutionDir = Path.GetFullPath("..");
            string wpDir = $"{solutionDir}/wordpress";
            string proofFile = $"{solutionDir}/PeachPied.WordPress.Stats/proofs/{configuration}.html";

            var assembly = Assembly.LoadFrom($"{wpDir}/bin/{configuration}/netstandard2.0/PeachPied.WordPress.{configuration}.dll");
            Context.AddScriptReference(assembly);

            RunWordPress(wpDir, proofFile);

            Console.WriteLine(RuntimeCounters.RoutineCalls);
            Console.WriteLine(RuntimeCounters.GlobalFunctionCalls);
            Console.WriteLine(RuntimeCounters.OriginalOverloadCalls);
            Console.WriteLine(RuntimeCounters.SpecializedOverloadCalls);
            Console.WriteLine(RuntimeCounters.BranchedCallChecks);
            Console.WriteLine(RuntimeCounters.BranchedCallOriginalSelects);
            Console.WriteLine(RuntimeCounters.BranchedCallSpecializedSelects);
        }

        private static void RunWordPress(string wpDir, string proofFile)
        {
            // Clean up the output file before the run
            File.WriteAllText(proofFile, "");

            var binOut = new MemoryStream();
            var textOut = new StreamWriter(binOut);
            var ctx = Context.CreateConsole("index.php");
            ctx.RootPath = ctx.WorkingDirectory = wpDir;
            ctx.OutputStream = binOut;
            ctx.Output = textOut;
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

            // Output the result to the file
            textOut.Flush();
            File.WriteAllBytes(proofFile, binOut.ToArray());
        }
    }
}
