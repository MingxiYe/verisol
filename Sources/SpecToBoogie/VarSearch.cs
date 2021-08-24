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
            Console.WriteLine(node);
            if (node.tgtFn.ident.contract != null)
            {
                Console.WriteLine("Added");
                atomList.Add(node);
            }
            else if (node.tgtFn.def.Name.Equals("*"))
            {
                Console.WriteLine("Added");
                atomList.Add(node);
            }
            else
            {
                Console.WriteLine("Unrecognized" + node);
            }

            return true;
        }
    }
}