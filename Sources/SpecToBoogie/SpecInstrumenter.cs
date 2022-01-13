using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using SolToBoogie;
using SolidityAST;

namespace SpecToBoogie
{
    public class SpecInstrumenter : ProgramInstrumenter
    {
        private string specFile;

        public SpecInstrumenter(string specFile)
        {
            this.specFile = specFile;
        }
        public void instrument(TranslatorContext ctxt, MapArrayHelper mapHelper)
        {
            Spec spec = new Spec(ctxt, mapHelper, specFile);

            if (spec.property == null && spec.fairness == null && spec.property == null)
            {
                throw new Exception($"Could not find specification in {specFile}");
            }

            AtomInstrumenter atomInstrumenter = new AtomInstrumenter(ctxt, spec);
            atomInstrumenter.instrument();

            if (spec.property != null)
            {
                BoogieCommentDeclaration propertyComment = new BoogieCommentDeclaration($"#LTLProperty: {spec.property.ToLTL()}");
                ctxt.Program.Declarations.Insert(0, propertyComment);
                Console.WriteLine($"// #LTLProperty: {spec.property.ToLTL()}");
            }
            if (spec.fairness != null)
            {
                BoogieCommentDeclaration fairnessComment = new BoogieCommentDeclaration($"#LTLFairness: {spec.fairness.ToLTL()}");
                ctxt.Program.Declarations.Insert(0, fairnessComment);
                Console.WriteLine($"// #LTLFairness: {spec.fairness.ToLTL()}");
            }
        }
    }
}