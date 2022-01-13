using System.Collections.Generic;

namespace SpecToBoogie
{
    public class CallFinder : BasicLTLASTVisitor
    {
        public LinkedList<Function> fns { get; }
        
        public LinkedList<Fsum> fsums { get; }
        
        public CallFinder()
        {
            fns = new LinkedList<Function>();
            fsums = new LinkedList<Fsum>();
        }

        public override bool Visit(Function fn)
        {
            fns.AddFirst(fn);

            return true;
        }

        public override bool Visit(Fsum fsum)
        {
            fsums.AddFirst(fsum);

            return true;
        }
    }
}