using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace SpecToBoogie
{
    class SmartLTLVisitor : SmartLTLBaseVisitor<object>
    {
        public override object VisitAtom(SmartLTLParser.AtomContext context)
        {
            Console.WriteLine("Atom: " + context.GetText());
            return base.VisitAtom(context);
        }

        public override object VisitConstraint(SmartLTLParser.ConstraintContext context)
        {
            Console.WriteLine("Constraint: " + context.GetText());
            return base.VisitConstraint(context);
        }

        public override object VisitSmartltl(SmartLTLParser.SmartltlContext context)
        {
            Console.WriteLine("SmartLTL: " + context.GetText());
            return base.VisitSmartltl(context);
        }

        public override object VisitArgList(SmartLTLParser.ArgListContext context)
        {
            Console.WriteLine("arglist: " + context.GetText());
            return base.VisitArgList(context);
        }
        

        public override object VisitAtomFn(SmartLTLParser.AtomFnContext context)
        {
            Console.WriteLine("atom: " + context.GetText());
            return base.VisitAtomFn(context);
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            String test = "[](<>(finished(Wallet.started(reverted, test), reverted + willSucceed)))";
            /*AntlrInputStream input = new AntlrInputStream(test);
            SmartLTLLexer lexer = new SmartLTLLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            SmartLTLParser parser = new SmartLTLParser(tokenStream);
            SmartLTLParser.LineContext line = parser.line();
            SmartLTLVisitor visitor = new SmartLTLVisitor();
            visitor.Visit(line);*/
            //SmartLTLReader reader = new SmartLTLReader();
            //SmartLTLNode ast = reader.Read(test);
            //Console.WriteLine(ast);
        }
    }
}