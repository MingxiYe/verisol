using System;
using System.Text.RegularExpressions;
using SolToBoogie;

namespace SpecToBoogie
{
    public class Spec
    {
        public string freeVarsString { get; private set; }
        public string fairnessString { get; private set; }
        public string propertyString { get; private set; }

        public VarDeclList freeVars { get; private set; }
        public TempExpr property { get; private set; }
        public TempExpr fairness { get; private set; }
        public Spec(TranslatorContext ctxt, MapArrayHelper mapHelper, string specFile)
        {
            readSpec(specFile);
            
            if (propertyString == null && fairnessString == null)
            {
                throw new Exception($"Could not find specification in {specFile}");
            }
           
            SmartLTLReader reader = new SmartLTLReader(ctxt, mapHelper);
            if (freeVarsString != null)
            {
                freeVars = reader.ReadVars(freeVarsString);
                reader.freeVars = freeVars;
                Console.WriteLine($"// #LTLVariables: {freeVars}");
            }

            if (fairnessString != null)
            {
                fairness = reader.ReadProperty(fairnessString);
                Console.WriteLine($"// #LTLFairness: {fairness}");
            }

            if (propertyString != null)
            {
                property = reader.ReadProperty(propertyString);
                Console.WriteLine($"// #LTLProperty: {property}");
            }
        }
            
        private void readSpec(string specFile)
        {
            string[] lines = System.IO.File.ReadAllLines(specFile);
            Regex varRegex = new Regex(@"^\s*//\s*#LTLVariables:");
            Regex fairRegex = new Regex(@"^\s*//\s*#LTLFairness:");
            Regex propRegex = new Regex(@"^\s*//\s*#LTLProperty:");
                
            foreach(string line in lines)
            {
                char[] whitespace = { ' ', '\t'};
                line.Trim(whitespace);

                if (varRegex.IsMatch(line))
                {
                    freeVarsString = line.Substring(line.IndexOf(':') + 1);
                }
                else if (fairRegex.IsMatch(line))
                {
                    fairnessString = line.Substring(line.IndexOf(':') + 1);
                }
                else if (propRegex.IsMatch(line))
                {
                    propertyString = line.Substring(line.IndexOf(':') + 1);
                }
            }
        }
    }
}