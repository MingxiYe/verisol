using System;
using BoogieAST;

namespace SpecToBoogie
{
    public class FunctionSearch : BasicBoogieASTVisitor
    {
        private String searchName;
        public BoogieImplementation desired;

        public FunctionSearch(string name)
        {
            searchName = name;
        }

        public override bool Visit(BoogieImplementation node)
        {
            if (searchName.Equals(node.Name))
            {
                desired = node;
                return true;
            }
            return false;
        }
    }
}