using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using BoogieAST;

namespace SpecToBoogie
{
    using SolidityAST;
    using SolToBoogie;
    public class SmartLTLReader : SmartLTLBaseVisitor<SmartLTLNode>
    {
        private TranslatorContext transCtxt;
        private FunctionDef curFn;
        private MapArrayHelper mapHelper;
        public VarDeclList freeVars { get; set; }
        public static int UNKNOWN_ID = Int32.MinValue;
        private static int varId = 0;
        
        public SmartLTLReader(TranslatorContext solToBoogieContext, MapArrayHelper mapHelper)
        {
            this.transCtxt = solToBoogieContext;
            this.mapHelper = mapHelper;
        }

        public VarDeclList ReadVars(string varString)
        {
            AntlrInputStream input = new AntlrInputStream(varString); 
            SmartLTLLexer lexer = new SmartLTLLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            SmartLTLParser parser = new SmartLTLParser(tokenStream);
            SmartLTLParser.FreeListContext freeVars = parser.freeList();
            SmartLTLNode declsNode = this.Visit(freeVars);
            if (declsNode is VarDeclList declList)
            {
                return declList;
            }
            
            throw new Exception("For some reason we didn't get a variable declaration list back");
        }
        
        public TempExpr ReadProperty(String propertyStr)
        {
            AntlrInputStream input = new AntlrInputStream(propertyStr); 
            SmartLTLLexer lexer = new SmartLTLLexer(input);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            SmartLTLParser parser = new SmartLTLParser(tokenStream);
            SmartLTLParser.SmartltlContext ltlSpec = parser.smartltl();
            SmartLTLNode property = this.Visit(ltlSpec);
            if (property is TempExpr tempExpr)
            {
                return tempExpr;
            }
            
            throw new Exception("For some reason we didn't get a temporal expression list back");
        }

        public override SmartLTLNode VisitErrorNode(IErrorNode node)
        {
             throw new Exception("Error at " + node.SourceInterval);
        }

        public override SmartLTLNode VisitFreeList(SmartLTLParser.FreeListContext context)
        {
            if (context.ChildCount == 2)
            {
                SmartLTLNode typeNode = this.Visit(context.GetChild(0));
                string name = context.GetChild(1).GetText();

                if (typeNode is TypeInfo type)
                {
                    int id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                    VariableDecl decl = new VariableDecl(type, name, id);
                    transCtxt.IdToNodeMap.Add(id, decl.toSolidityAST());
                    List<VariableDecl> decls = new List<VariableDecl>();
                    decls.Add(decl);
                    return new VarDeclList(decls);
                }
            }
            else if (context.ChildCount == 4)
            {
                SmartLTLNode typeNode = this.Visit(context.GetChild(0));
                String name = context.GetChild(1).GetText();
                SmartLTLNode otherVars = this.Visit(context.GetChild(3));
                
                if (typeNode is TypeInfo type && otherVars is VarDeclList declList)
                {
                    int id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                    VariableDecl newDecl = new VariableDecl(type, name, id);
                    transCtxt.IdToNodeMap.Add(id, newDecl.toSolidityAST());
                    declList.decls.Add(newDecl);
                    return declList;
                }
            }

            throw new Exception("Exception");
        }

        public override SmartLTLNode VisitType(SmartLTLParser.TypeContext context)
        {
            string typeStr = context.GetText();
            String[] elementaryTypes = new[] {"address", "bool", "uint", "int"};
            if (elementaryTypes.Contains(typeStr))
            {
                return TypeInfo.GetElementaryType(typeStr);
            }
            
            throw new Exception($"{typeStr} unsupported, only [{string.Join(", ", elementaryTypes)}] supported");
        }

        public override SmartLTLNode VisitSmartltl(SmartLTLParser.SmartltlContext context)
        {
            if (context.exception != null)
            {
                throw context.exception;
            }
            
            if (context.ChildCount == 1)
            {
                return this.Visit(context.GetChild(0));
            }

            else if (context.ChildCount == 2)
            {
                SmartLTLNode subNode = this.Visit(context.GetChild(1));

                if (subNode is TempExpr subExpr)
                {
                    return new TempUnOp(context.GetChild(0).GetText(), subExpr);
                }
            }
            else if (context.ChildCount == 3)
            {
                if (context.GetChild(0).GetText().Equals("("))
                {
                    return this.Visit(context.GetChild(1));
                }
                
                SmartLTLNode lhsNode = this.Visit(context.GetChild(0));
                SmartLTLNode rhsNode = this.Visit(context.GetChild(2));

                if (lhsNode is TempExpr lhs && rhsNode is TempExpr rhs)
                {
                    return new TempBinOp(lhs, context.GetChild(1).GetText(), rhs);
                }
            }
            
            throw new Exception("Translation Error");
        }

        public FunctionDefinition GetImplicitAtomFunctionDefinition(string name, Params p)
        {
            if (name.Equals("send") && (p == null || p.paramList.Count == 3))
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = name;
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();
                fnDef.Payable = true;
                TypeInfo addrType = TypeInfo.GetElementaryType("address");
                TypeInfo uintType = TypeInfo.GetElementaryType("uint");
                
                UtilVariableDeclaration fromDecl = new UtilVariableDeclaration();
                fromDecl.Name = "from";
                fromDecl.TypeName = addrType.name;
                fromDecl.TypeDescriptions = addrType.description;
                fromDecl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                transCtxt.IdToNodeMap.Add(fromDecl.Id, fromDecl);
                
                UtilVariableDeclaration toDecl = new UtilVariableDeclaration();
                toDecl.Name = "to";
                toDecl.TypeName = addrType.name;
                toDecl.TypeDescriptions = addrType.description;
                toDecl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                transCtxt.IdToNodeMap.Add(toDecl.Id, toDecl);

                UtilVariableDeclaration amtDecl = new UtilVariableDeclaration();
                amtDecl.Name = "amount";
                amtDecl.TypeName = uintType.name;
                amtDecl.TypeDescriptions = uintType.description;
                amtDecl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                transCtxt.IdToNodeMap.Add(amtDecl.Id, amtDecl);
                
                fnDef.Parameters.Parameters.Add(fromDecl);
                fnDef.Parameters.Parameters.Add(toDecl);
                fnDef.Parameters.Parameters.Add(amtDecl);

                return fnDef;
            }
            if (name.Equals("*") && p == null)
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = "*";
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();

                return fnDef;
            }

            return null;
        }

        public FunctionDefinition GetImplicitExprFunctionDefinition(string name, ArgList args)
        {
            if (name.Equals("fsum") && args.args.Count == 3)
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = name;
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();

                VariableDeclaration fnDecl = new VariableDeclaration();
                fnDecl.Name = "fn";
                fnDecl.Id = UNKNOWN_ID;
                
                VariableDeclaration varDecl = new VariableDeclaration();
                varDecl.Name = "var";
                varDecl.Id = UNKNOWN_ID;
                
                VariableDeclaration condDecl = new VariableDeclaration();
                condDecl.Name = "cond";
                condDecl.Id = UNKNOWN_ID;
                
                fnDef.Parameters.Parameters.Add(fnDecl);
                fnDef.Parameters.Parameters.Add(varDecl);
                fnDef.Parameters.Parameters.Add(condDecl);
                
                fnDef.ReturnParameters = new ParameterList();
                fnDef.ReturnParameters.Parameters = new List<VariableDeclaration>();
                
                TypeInfo uintType = TypeInfo.GetElementaryType("uint");
                VariableDeclaration sumDecl = new VariableDeclaration();
                sumDecl.Name = "sum";
                sumDecl.TypeName = uintType.name;
                sumDecl.TypeDescriptions = uintType.description;
                sumDecl.Id = UNKNOWN_ID;

                fnDef.ReturnParameters.Parameters.Add(sumDecl);
                
                return fnDef;
            }
            if (name.Equals("csum") && args.args.Count == 1)
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = name;
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();
                fnDef.Payable = true;

                VariableDeclaration varDecl = new VariableDeclaration(); 
                varDecl.Name = "var";
                varDecl.Id = UNKNOWN_ID;

                fnDef.Parameters.Parameters.Add(varDecl);
                
                fnDef.ReturnParameters = new ParameterList();
                fnDef.ReturnParameters.Parameters = new List<VariableDeclaration>();
                
                TypeInfo uintType = TypeInfo.GetElementaryType("uint");
                VariableDeclaration sumDecl = new VariableDeclaration();
                sumDecl.Name = "sum";
                sumDecl.TypeName = uintType.name;
                sumDecl.TypeDescriptions = uintType.description;
                sumDecl.Id = UNKNOWN_ID;

                fnDef.ReturnParameters.Parameters.Add(sumDecl);

                return fnDef;
            }
            if (name.Equals("address") && args.args.Count == 1)
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = name;
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();
                fnDef.Payable = true;

                VariableDeclaration argDecl = new VariableDeclaration(); 
                argDecl.Name = "arg";
                argDecl.Id = UNKNOWN_ID;

                fnDef.Parameters.Parameters.Add(argDecl);
                
                fnDef.ReturnParameters = new ParameterList();
                fnDef.ReturnParameters.Parameters = new List<VariableDeclaration>();
                
                TypeInfo uintType = TypeInfo.GetElementaryType("address");
                VariableDeclaration retDecl = new VariableDeclaration();
                retDecl.Name = "ret";
                retDecl.TypeName = uintType.name;
                retDecl.TypeDescriptions = uintType.description;
                retDecl.Id = UNKNOWN_ID;

                fnDef.ReturnParameters.Parameters.Add(retDecl);

                return fnDef;
            }
            if (name.Equals("old") && args.args.Count == 1)
            {
                FunctionDefinition fnDef = new FunctionDefinition();
                fnDef.Name = name;
                fnDef.Parameters = new ParameterList();
                fnDef.Parameters.Parameters = new List<VariableDeclaration>();
                fnDef.Payable = true;

                VariableDeclaration argDecl = new VariableDeclaration(); 
                argDecl.Name = "arg";
                argDecl.Id = UNKNOWN_ID;

                fnDef.Parameters.Parameters.Add(argDecl);
                
                fnDef.ReturnParameters = new ParameterList();
                fnDef.ReturnParameters.Parameters = new List<VariableDeclaration>();

                TypeDescription desc = args.args[0].GetType(transCtxt);
                
                VariableDeclaration retDecl = new VariableDeclaration();
                retDecl.Name = "ret";
                retDecl.TypeName = null;
                retDecl.TypeDescriptions = desc;
                retDecl.Id = UNKNOWN_ID;

                fnDef.ReturnParameters.Parameters.Add(retDecl);

                return fnDef;
            }

            return null;
        }

        private string getAxiomVarName(AtomLoc loc, FunctionDef tgtFn)
        {
            String fnName = tgtFn.def.Name;
            bool isWildcard = fnName.Equals("*");
            String varName = loc.ToString().ToLower() + "_" + (isWildcard ? "wildcard" : fnName) + varId;
            varId++;
            return varName;
        }
        
        public override SmartLTLNode VisitAtom(SmartLTLParser.AtomContext context)
        {
            if (context.ChildCount != 4 && context.ChildCount != 6)
            {
                throw new Exception("Translation Error");
            }

            FunctionDef tgtFn = null;
            AtomLoc atomLoc;
            switch (context.GetChild(0).GetText())
            {
                case "finished":
                    atomLoc = AtomLoc.FINISHED;
                    break;
                case "started":
                    atomLoc = AtomLoc.STARTED;
                    break;
                case "reverted":
                    atomLoc = AtomLoc.REVERTED;
                    break;
                case "willSucceed":
                    atomLoc = AtomLoc.WILL_SUCCEED;
                    break;
                case "sent":
                    FunctionIdent ident = new FunctionIdent(null, "send");
                    FunctionDefinition fnDef = GetImplicitAtomFunctionDefinition("send", null);
                    if (fnDef == null)
                    {
                        throw new Exception("Could not find function for sent");
                    }
                    tgtFn = new FunctionDef(ident, null, fnDef);
                    atomLoc = AtomLoc.STARTED;
                    break;
                default:
                    throw new Exception("Translation Error");
            }

            if (tgtFn == null)
            {
                SmartLTLNode defNode = this.Visit(context.GetChild(2));
                if (defNode is FunctionDef def)
                {
                    tgtFn = def;
                }
                else
                {
                    throw new Exception("Translation Error");
                }
                
                curFn = tgtFn;
                Expr constraint = null;
                if (context.ChildCount == 6)
                {
                    SmartLTLNode constraintNode = this.Visit(context.GetChild(4));

                    if (constraintNode is Expr constr)
                    {
                        constraint = constr;
                        TypeDescription constraintType = constraint.GetType(transCtxt);
                        if (!constraintType.IsBool())
                        {
                            throw new Exception($"Constraint must have a type of bool, not {constraintType.TypeString}");
                        }
                    }
                    else
                    {
                        throw new Exception("Expected constraint to be an Expression");
                    }
                }

                curFn = null;
                return new Atom(atomLoc, tgtFn, constraint, getAxiomVarName(atomLoc, tgtFn));
            }
            else
            {
                curFn = tgtFn;
                SmartLTLNode constraintNode = this.Visit(context.GetChild(2));
                if (constraintNode is Expr constraint)
                {
                    return new Atom(atomLoc, tgtFn, constraint, getAxiomVarName(atomLoc, tgtFn));
                }
            }
            
            throw new Exception("Translation Error");
        }
        
        public override SmartLTLNode VisitVarAccess(SmartLTLParser.VarAccessContext context)
        {
            if (context.ChildCount == 1)
            {
                return this.Visit(context.GetChild(0));
            }

            if (context.ChildCount == 3)
            {
                SmartLTLNode baseNode = this.Visit(context.GetChild(0));
                string member = context.GetChild(2).GetText();

                if (baseNode is Expr baseExpr)
                {
                    return new Member(baseExpr, member);
                }
            }

            if (context.ChildCount == 4)
            {
                SmartLTLNode baseNode = this.Visit(context.GetChild(0));
                SmartLTLNode indexNode = this.Visit(context.GetChild(2));

                if (baseNode is Expr baseExpr && indexNode is Expr indexExpr)
                {
                    return new Index(baseExpr, indexExpr);
                }
            }
            
            throw new Exception("Translation Error");
        }

        private FunctionDefinition FindAtomFunction(String contractName, String fnName, Params p)
        {
            if (contractName != null)
            {
                List<FunctionDefinition> possibilities = FindContractFunction(contractName, fnName);

                if (p != null)
                {
                    possibilities = new List<FunctionDefinition>(possibilities.Where(fn =>
                                        fn.Parameters != null && fn.Parameters.Parameters != null &&
                                        fn.Parameters.Parameters.Count == p.paramList.Count));
                }
                
                if (possibilities.Count == 1)
                {
                    return possibilities[0];
                }
                
                throw new Exception($"Could not uniquely identify {contractName}.{fnName}");
            }

            FunctionDefinition fnDef = GetImplicitAtomFunctionDefinition(fnName, p);
            if (fnDef == null)
            {
                throw new Exception($"Could not find function {fnName}");
            }
            return fnDef;
        }
        
        private FunctionDefinition FindExprFunction(String contractName, String fnName, ArgList args)
        {
            if (contractName != null)
            {
                List<FunctionDefinition> possibilities = FindContractFunction(contractName, fnName);
                List<FunctionDefinition> filteredDefs = new List<FunctionDefinition>(possibilities.Where(fn =>
                    fn.Parameters != null && fn.Parameters.Parameters != null &&
                    fn.Parameters.Parameters.Count == args.args.Count));

                if (filteredDefs.Count == 1)
                {
                    return filteredDefs[0];
                }
                
                throw new Exception($"Could not uniquely identify {contractName}.{fnName}");
            }

            FunctionDefinition fnDef = GetImplicitExprFunctionDefinition(fnName, args);
            if (fnDef == null)
            {
                throw new Exception($"Could not find function {fnName}");
            }
            return fnDef;
        }
        
        private List<FunctionDefinition> FindContractFunction(String contractName, String fnName)
        {
            List<FunctionDefinition> possibleFns = new List<FunctionDefinition>();
            if (transCtxt.HasContractName(contractName))
            {
                ContractDefinition contractDef = transCtxt.GetContractByName(contractName);

                foreach (FunctionDefinition fnDef in transCtxt.GetFuncDefintionsInContract(contractDef))
                {
                    if (fnDef.Name.Equals(fnName))
                    {
                        possibleFns.Add(fnDef);
                    }
                }
            }

            return possibleFns;
        }
        
        public override SmartLTLNode VisitAtomFnName(SmartLTLParser.AtomFnNameContext context)
        {
            if (context.ChildCount == 1)
            {
                string[] contractlessFns = new[] {"send", "transfer"};
                String fnName = context.GetChild(0).GetText();

                if (!contractlessFns.Contains(fnName))
                {
                    throw new Exception($"Only {String.Join(", ",contractlessFns)} may be provided without a contract");
                }

                if (fnName.Equals("transfer"))
                {
                    fnName = "send";
                }

                return new FunctionIdent(null, fnName);
            }
            if (context.ChildCount == 3)
            {
                String contractName = context.GetChild(0).GetText();
                String fnName = context.GetChild(2).GetText();

                List<FunctionDefinition> possibilities = FindContractFunction(contractName, fnName);
                if (possibilities.Count == 0)
                {
                    if (transCtxt.HasContractName(contractName))
                    {
                        throw new Exception($"Could not find function {fnName} in {contractName}");
                    }
                                        
                    throw new Exception($"Could not find contract named {contractName}");
                }

                return new FunctionIdent(transCtxt.GetContractByName(contractName), fnName);
            }
            
            throw new Exception("Translation Error");
        }
        
        public override SmartLTLNode VisitFnName(SmartLTLParser.FnNameContext context)
        {
            if (context.ChildCount == 1)
            {
                string[] contractlessFns = new[] {"address", "csum", "old"};
                String fnName = context.GetChild(0).GetText();

                if (!contractlessFns.Contains(fnName))
                {
                    throw new Exception($"Only {String.Join(", ",contractlessFns)} may be provided without a contract");
                }

                return new FunctionIdent(null, fnName);
            }
            else if (context.ChildCount == 3)
            {
                String contractName = context.GetChild(0).GetText();
                String fnName = context.GetChild(2).GetText();

                List<FunctionDefinition> possibilities = FindContractFunction(contractName, fnName);
                if (possibilities.Count == 0)
                {
                    if (transCtxt.HasContractName(contractName))
                    {
                        throw new Exception($"Could not find function {fnName} in {contractName}");
                    }
                                        
                    throw new Exception($"Could not find contract named {contractName}");
                }

                return new FunctionIdent(transCtxt.GetContractByName(contractName), fnName);
            }
            
            throw new Exception("Translation Error");
        }

        
        
        public override SmartLTLNode VisitAtomFn(SmartLTLParser.AtomFnContext context)
        {
            if (context.ChildCount == 1)
            {
                SmartLTLNode identNode;
                if (context.GetChild(0).GetText().Equals("*"))
                {
                    identNode = new FunctionIdent(null, "*");
                }
                else
                {
                    identNode = this.Visit(context.GetChild(0));
                }
                
                if (identNode is FunctionIdent ident)
                {
                    string contractName = ident.contract == null ? null : ident.contract.Name;
                    return new FunctionDef(ident, null, FindAtomFunction(contractName, ident.fnName, null));
                }
            }
            else if (context.ChildCount == 4)
            {
                SmartLTLNode identNode = this.Visit(context.GetChild(0));
                SmartLTLNode paramsNode = this.Visit(context.GetChild(2));

                if (identNode is FunctionIdent ident && paramsNode is Params paramList)
                {
                    string contractName = ident.contract == null ? null : ident.contract.Name;
                    return new FunctionDef(ident, paramList, FindAtomFunction(contractName, ident.fnName, paramList));
                }
            }
            
            throw new Exception("Translation Error");
        }
        
        public override SmartLTLNode VisitFnCall(SmartLTLParser.FnCallContext context)
        {
            if (context.ChildCount == 4)
            {
                SmartLTLNode identNode = this.Visit(context.GetChild(0));
                SmartLTLNode argNode = this.Visit(context.GetChild(2));

                if (identNode is FunctionIdent ident && argNode is ArgList args)
                {
                    string contractName = ident.contract == null ? null : ident.contract.Name;
                    FunctionDefinition fnDef = FindExprFunction(contractName, ident.fnName, args);
                    List<VariableDeclaration> retDecls = fnDef.ReturnParameters.Parameters;

                    if (ident.contract == null && ident.fnName.Equals("csum"))
                    {
                        Variable var = args.args[0] as Variable;

                        if (var == null)
                        {
                            throw new Exception("csum must take a variable as an argument");
                        }

                        /*VariableDeclaration varDecl = transCtxt.IdToNodeMap[var.id] as VariableDeclaration;

                        if (varDecl == null)
                        {
                            throw new Exception($"Could not find a declaration for variable {var}");
                        }

                        string sumName = mapHelper.GetSumName(varDecl);
                        UtilVariableDeclaration sumDecl = new UtilVariableDeclaration();
                        sumDecl.Constant = false;
                        sumDecl.Indexed = false;
                        sumDecl.Name = sumName;
                        sumDecl.Value = null;
                        sumDecl.Visibility = EnumVisibility.DEFAULT;
                        sumDecl.StateVariable = true;
                        sumDecl.StorageLocation = EnumLocation.DEFAULT;
                        TypeInfo uintType = TypeInfo.GetElementaryType("uint");
                        sumDecl.TypeDescriptions = uintType.description;
                        sumDecl.TypeName = uintType.name;
                        sumDecl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                        transCtxt.IdToNodeMap.Add(sumDecl.Id, sumDecl);*/

                        return new Csum(var);
                    }

                    if (retDecls.Count != 1)
                    {
                        throw new Exception("Called functions must have a scalar return value");
                    }

                    VariableDeclaration retDecl = retDecls[0];
                    
                    UtilVariableDeclaration decl = new UtilVariableDeclaration();
                    decl.Constant = retDecl.Constant;
                    decl.Indexed = retDecl.Indexed;
                    decl.Name = $"{fnDef.Name}_{varId++}";
                    decl.Value = null;
                    decl.Visibility = EnumVisibility.DEFAULT;
                    decl.StateVariable = false;
                    decl.StorageLocation = EnumLocation.DEFAULT;
                    decl.TypeDescriptions = retDecl.TypeDescriptions;
                    decl.TypeName = retDecl.TypeName;
                    decl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                    transCtxt.IdToNodeMap.Add(decl.Id, decl);
                    
                    return new Function(ident, args, fnDef, decl);
                }
            }
            else if (context.ChildCount == 8)
            {
                String fnName = context.GetChild(0).GetText();
                SmartLTLNode fnNode = this.Visit(context.GetChild(2));
                SmartLTLNode sumVarNode = this.Visit(context.GetChild(4));
                SmartLTLNode constraintNode = this.Visit(context.GetChild(6));

                if (fnName == "fsum" && fnNode is FunctionDef fn && sumVarNode is Expr sumVar && constraintNode is Expr constraint)
                {
                    TypeInfo uintInfo = TypeInfo.GetElementaryType("uint");
                    UtilVariableDeclaration decl = new UtilVariableDeclaration();
                    decl.Constant = false;
                    decl.Indexed = false;
                    decl.Name = $"fsum_{varId++}";
                    decl.Value = null;
                    decl.Visibility = EnumVisibility.DEFAULT;
                    decl.StateVariable = false;
                    decl.StorageLocation = EnumLocation.DEFAULT;
                    decl.TypeDescriptions = uintInfo.description;
                    decl.TypeName = uintInfo.name;
                    decl.Id = transCtxt.IdToNodeMap.Keys.Min() - 1;
                    transCtxt.IdToNodeMap.Add(decl.Id, decl);
                    
                    return new Fsum(fn, sumVar, constraint, decl);
                }
            }

            throw new Exception("Translation Error");
        }
        
        public override SmartLTLNode VisitArgList(SmartLTLParser.ArgListContext context)
        {
            List<Expr> args = new List<Expr>();
            if (context.ChildCount == 0)
            {
                return new ArgList(args);
            }
            
            foreach(IParseTree tree in context.children)
            {
                SmartLTLNode argNode = this.Visit(tree);
                if (argNode is Expr arg)
                {
                    args.Add(arg);
                }
                else if (argNode is ArgList otherArgs)
                {
                    args.AddRange(otherArgs.args);
                }
            }
            
            return new ArgList(args);
        }

        public override SmartLTLNode VisitVarOrNum(SmartLTLParser.VarOrNumContext context)
        {
            if (context.ChildCount != 1)
            {
                throw new Exception("Translation Error");
            }

            return this.Visit(context.GetChild(0));
        }
        
        public override SmartLTLNode VisitNum(SmartLTLParser.NumContext context)
        {
            BigInteger val = BigInteger.Parse(context.GetText());
            return new LiteralVal(val);
        }

        public TypeDescription GetBinopResultType(String op, Expr lhs, Expr rhs)
        {
            TypeDescription lhsType = lhs.GetType(transCtxt);
            TypeDescription rhsType = rhs.GetType(transCtxt);

            switch (op)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    if (lhsType.IsUint() && rhsType.IsUint())
                    {
                        return lhsType;
                    }
                    if (lhsType.IsInt() && (rhsType.IsUint() || rhsType.IsInt()))
                    {
                        return lhsType;
                    }
                    if ((lhsType.IsUint() || lhsType.IsInt()) && rhsType.IsInt())
                    {
                        return rhsType;
                    }
                    throw new Exception($"{op} requires integer arguments, not {lhsType} + {rhsType}");
                case "&&":
                case "||":
                    if (lhsType.IsBool() && rhsType.IsBool())
                    {
                        return lhsType;
                    }
                    throw new Exception($"{op} expects boolean arguments, not {lhsType} + {rhsType}");
                case "==":
                case "!=":
                case "<=":
                case "<":
                case ">=":
                case ">":
                    if ((lhsType.IsInt() || lhsType.IsUint()) && (rhsType.IsInt() || rhsType.IsUint()))
                    {
                        return TypeInfo.GetElementaryType("bool").description;
                    }
                    if (lhsType.IsBool() && rhsType.IsBool())
                    {
                        return lhsType;
                    }
                    if (lhsType.IsAddress() && rhsType.IsAddress())
                    {
                        return TypeInfo.GetElementaryType("bool").description;
                    }
                    
                    throw new Exception($"{op} expects boolean, integer or address arguments, not {lhsType} + {rhsType}");
            }
            
            throw new Exception($"Unknown operation {op}");
        }
        
        public override SmartLTLNode VisitConstraint(SmartLTLParser.ConstraintContext context)
        {
            if (context.ChildCount == 1)
            {
                return this.Visit(context.GetChild(0));
            }
            else if (context.ChildCount == 2)
            {
                SmartLTLNode subExpr = this.Visit(context.GetChild(1));

                if (subExpr is Expr exp)
                {
                    return new UnaryOp(context.GetChild(0).GetText(), exp);
                }
                
                throw new Exception("Translation error");
            }
            else if (context.ChildCount == 3)
            {
                if (context.GetChild(0).GetText().Equals("("))
                {
                    return this.Visit(context.GetChild(1));
                }
                
                SmartLTLNode lhsNode = this.Visit(context.GetChild(0));
                SmartLTLNode rhsNode = this.Visit(context.GetChild(2));

                if (lhsNode is Expr lhs && rhsNode is Expr rhs)
                {
                    string op = context.GetChild(1).GetText();
                    TypeDescription resultType = GetBinopResultType(op, lhs, rhs);
                    return new BinaryOp(lhs, op, rhs, resultType);
                }
                
                throw new Exception("Translation error");
            }

            throw new Exception("Unknown constraint");
        }
        
        public override SmartLTLNode VisitParams(SmartLTLParser.ParamsContext context)
        {
            if (context.ChildCount == 1)
            {
                string name = context.GetChild(0).GetText();
                List<string> paramList = new List<string>();
                paramList.Add(name);
                return new Params(paramList);
            }
            if (context.ChildCount == 3)
            {
                string name = context.GetChild(0).GetText();
                SmartLTLNode paramListNode = this.Visit(context.GetChild(2));

                if (paramListNode is Params paramList)
                {
                    paramList.paramList.Insert(0, name);
                    return paramList;
                }
            }
            
            throw new Exception("translation error");
        }

        public VariableDeclaration GetImplicitDecl(string name)
        {
            switch (name)
            {
                case "msg":
                    VariableDeclaration msgDecl = new VariableDeclaration();
                    msgDecl.TypeDescriptions = new TypeDescription();
                    msgDecl.TypeDescriptions.TypeIndentifier = null;
                    msgDecl.TypeDescriptions.TypeString = "msg";
                    msgDecl.Id = UNKNOWN_ID;
                    return msgDecl;
                case "this":
                    String contractName = transCtxt.EntryPointContract;
                    if (curFn.ident.contract != null)
                    {
                        contractName = curFn.ident.contract.Name;
                    }
                    
                    VariableDeclaration thisDecl = new VariableDeclaration();
                    thisDecl.TypeDescriptions =
                        TypeInfo.GetContractType(transCtxt, contractName).description;
                    thisDecl.Id = UNKNOWN_ID;
                    return thisDecl;
            }

            return null;
        }
        public override SmartLTLNode VisitIdent(SmartLTLParser.IdentContext context)
        {
            string name = context.GetText();

            if (name.Equals("true"))
            {
                return new LiteralVal(true);
            }
            else if (name.Equals("false"))
            {
                return new LiteralVal(false);
            }
            
            VariableDeclaration decl = null;
            if (freeVars != null)
            {
                decl = freeVars.GetVarDecl(transCtxt, name);
            }
            
            if (decl == null && curFn != null)
            {
                decl = curFn.GetVarDecl(transCtxt, name);
            }

            if (decl == null)
            {
                decl = GetImplicitDecl(name);
            }

            if (decl == null)
            {
                throw new Exception($"Could not find the declaration for {name}");
            }

            Variable refVar = new Variable(name, decl.Id, decl.TypeDescriptions);
            
            return refVar;
        }
    }
}