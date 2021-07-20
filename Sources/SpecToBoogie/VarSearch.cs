using System;
using System.Collections.Generic;

namespace SpecToBoogie
{
    public class VarSearch : BasicLTLASTVisitor
    {
        public List<Atom> atomList;
        public VarSearch()
        {
            atomList = new List<Atom>();
        }

        public override bool Visit(Atom node)
        {
            if (node.tgtFn.ident.contract != null)
            {
                atomList.Add(node);
            }
            return true;
        }
    }
}