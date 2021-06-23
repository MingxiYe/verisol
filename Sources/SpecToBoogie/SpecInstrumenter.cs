using System;
using System.Text.RegularExpressions;
using BoogieAST;
using SolToBoogie;

namespace SpecToBoogie
{
    public class SpecInstrumenter : ProgramInstrumenter
    {
        private string specFile;
        public SpecInstrumenter(string specFile)
        {
            this.specFile = specFile;
        }
        public void instrument(TranslatorContext ctxt)
        {
            string[] lines = System.IO.File.ReadAllLines(specFile);
            Regex varRegex = new Regex(@"^\s*//\s*#LTLVariables:");
            Regex fairRegex = new Regex(@"^\s*//\s*#LTLFairness:");
            Regex propRegex = new Regex(@"^\s*//\s*#LTLProperty:");

            string freeVars = null;
            string fairness = null;
            string property = null;
            
            foreach(string line in lines)
            {
                char[] whitespace = { ' ', '\t'};
                line.Trim(whitespace);

                if (varRegex.IsMatch(line))
                {
                    freeVars = line.Substring(line.IndexOf(':') + 1);
                }
                else if (fairRegex.IsMatch(line))
                {
                    fairness = line.Substring(line.IndexOf(':') + 1);
                }
                else if (propRegex.IsMatch(line))
                {
                    property = line.Substring(line.IndexOf(':') + 1);
                }
            }

            if (property == null && fairness == null && property == null)
            {
                throw new Exception($"Could not find specification in {specFile}");
            }
            
            SmartLTLReader reader = new SmartLTLReader(ctxt);
            if (freeVars != null)
            {
                VarDeclList varList = reader.ReadVars(freeVars);
                reader.SetFreeVars(varList);
                Console.WriteLine($"// #LTLVariables: {varList}");
            }

            if (fairness != null)
            {
                SmartLTLNode head = reader.ReadProperty(fairness);
                Console.WriteLine($"// #LTLFairness: {head}");
            }

            if (property != null)
            {
                SmartLTLNode head = reader.ReadProperty(property);
                Console.WriteLine($"// #LTLProperty: {head}");
            }
            
            /*string test = "[](!finished(*, Wallet.balanceOf(null) != 0 || Wallet.balanceOf(this) != 0))";
            SmartLTLNode head = reader.ReadProperty(test);
            Console.WriteLine(head.ToString());*/
        }
    }
}