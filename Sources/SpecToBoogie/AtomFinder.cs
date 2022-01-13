using System;
using System.Collections.Generic;

namespace SpecToBoogie
{
    public class AtomFinder : BasicLTLASTVisitor
    {
        public List<Atom> atoms;
        public AtomFinder()
        {
            atoms = new List<Atom>();
        }

        public override bool Visit(Atom node)
        {
            Console.WriteLine(node);
            if (node.tgtFn.ident.contract != null)
            {
                atoms.Add(node);
            }
            else if (node.tgtFn.def.Name.Equals("*"))
            {
                atoms.Add(node);
            }
            else if (node.tgtFn.ident.fnName.Equals("send"))
            {
                atoms.Add(node);
            }
            else
            {
                throw new Exception("Unrecognized" + node);
            }

            return true;
        }
    }
}