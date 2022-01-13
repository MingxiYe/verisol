using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.SymbolStore;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using System.Text;
using BoogieAST;
using SolidityAST;
using SolToBoogie;

namespace SpecToBoogie
{
    public enum AtomLoc
    {
        STARTED,
        FINISHED,
        REVERTED,
        WILL_SUCCEED
    }
    public interface SmartLTLNode
    {
        void Accept(ILTLASTVisitor visitor);
    }

    public interface Expr: SmartLTLNode
    {
        Expression ToSolidityAST();
        
        TypeDescription GetType(TranslatorContext ctxt);
    }

    public interface TempExpr : SmartLTLNode
    {
        string ToLTL();
    }

    public class VarDeclList : SmartLTLNode
    {
        public List<VariableDecl> decls { get; }

        public VarDeclList(List<VariableDecl> decls)
        {
            this.decls = decls;
        }
        
        public override string ToString()
        {
            if (decls.Count == 0)
            {
                return "";
            }
            
            StringBuilder builder = new StringBuilder();
            builder.Append(decls[0]);

            for (int i = 1; i < decls.Count; i++)
            {
                builder.Append(", ").Append(decls[i]);
            }

            return builder.ToString();
        }
        
        public VariableDeclaration GetVarDecl(TranslatorContext ctxt, String name)
        {
            foreach (var decl in decls)
            {
                if (decl.name.Equals(name))
                {
                    return decl.toSolidityAST();
                }
            }

            return null;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                foreach (VariableDecl decl in decls)
                {
                    decl.Accept(visitor);
                }
            }
            
            visitor.EndVisit(this);
        }
    }

    public class TypeInfo : SmartLTLNode
    {
        public TypeName name { get; }
        public TypeDescription description { get; }

        public TypeInfo(TypeName name, TypeDescription desc)
        {
            this.name = name;
            this.description = desc;
        }
        
        public override string ToString()
        {
            return $"{description.TypeString}";
        }
        
        public static TypeInfo GetElementaryType(string name)
        {
            TypeDescription desc = new TypeDescription();
            desc.TypeIndentifier = name;
            desc.TypeString = name;
            ElementaryTypeName typeName = new ElementaryTypeName();
            typeName.TypeDescriptions = desc;
            return new TypeInfo(typeName, desc);
        }

        public static TypeInfo GetContractType(TranslatorContext ctxt, string name)
        {
            if (!ctxt.HasContractName(name))
            {
                return null;
            }

            ContractDefinition def = ctxt.GetContractByName(name);
            UserDefinedTypeName contractType = new UserDefinedTypeName();
            contractType.Name = $"contract {name}";
            contractType.ReferencedDeclaration = def.Id;
            contractType.TypeDescriptions = new TypeDescription();
            contractType.TypeDescriptions.TypeString = $"contract {name}";
            return new TypeInfo(contractType, contractType.TypeDescriptions);
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            visitor.Visit(this);
            visitor.EndVisit(this);
        }
    }

    public class VariableDecl : SmartLTLNode
    {
        public TypeInfo type { get; }
        public String name { get; }
        
        public int id { get; }

        public VariableDecl(TypeInfo type, String name, int id)
        {
            this.type = type;
            this.name = name;
            this.id = id;
        }

        public override string ToString()
        {
            return $"{type} {name}";
        }
        
        public VariableDeclaration toSolidityAST()
        {
            UtilVariableDeclaration varDecl = new UtilVariableDeclaration();
            varDecl.Constant = false;
            varDecl.Indexed = false;
            varDecl.Name = name;
            varDecl.Visibility = EnumVisibility.DEFAULT;
            varDecl.StateVariable = false;
            varDecl.StorageLocation = EnumLocation.DEFAULT;
            varDecl.TypeDescriptions = type.description;
            varDecl.TypeName = type.name;
            varDecl.Id = id;
            return varDecl;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                type.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class TempBinOp : TempExpr
    {
        public TempExpr lhs { get; }
        public TempExpr rhs { get; }
        public String op { get; }

        public TempBinOp(TempExpr lhs, String op, TempExpr rhs)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.op = op;
        }
        
        public override string ToString()
        {
            return $"({lhs} {op} {rhs})";
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                lhs.Accept(visitor);
                rhs.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }

        public string ToLTL()
        {
            return $"({lhs.ToLTL()} {op} {rhs.ToLTL()})";
        }
    }

    public class TempUnOp : TempExpr
    {
        public String op { get; }
        public TempExpr expr { get; }

        public TempUnOp(String op, TempExpr expr)
        {
            this.expr = expr;
            this.op = op;
        }
        
        public override string ToString()
        {
            return $"{op}({expr})";
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                expr.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }

        public string ToLTL()
        {
            return $"{op}({expr.ToLTL()})";
        }
    }

    public class Atom : TempExpr
    {
        public AtomLoc loc { get; }
        public FunctionDef tgtFn { get; }
        public Expr constraint { get; }
        public string name { get; }
        public Atom(AtomLoc loc, FunctionDef tgtFn, Expr constraint, string name)
        {
            this.loc = loc;
            this.tgtFn = tgtFn;
            this.constraint = constraint;
            this.name = name;
        }
        
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            switch (loc)
            {
                case AtomLoc.STARTED:
                    builder.Append("started(");
                    break;
                case AtomLoc.FINISHED:
                    builder.Append("finished(");
                    break;
                case AtomLoc.REVERTED:
                    builder.Append("reverted(");
                    break;
                case AtomLoc.WILL_SUCCEED:
                    builder.Append("willSucceed(");
                    break;
            }

            builder.Append(tgtFn);

            if (constraint != null)
            {
                builder.Append(", ");
                builder.Append(constraint);
            }

            builder.Append(")");

            return builder.ToString();
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                tgtFn.Accept(visitor);
                if (constraint != null)
                {
                    constraint.Accept(visitor);
                }
            }
            
            visitor.EndVisit(this);
        }
        
        public string ToLTL()
        {
            return $"{name}";
        }
    }

    public class FunctionIdent : SmartLTLNode
    {
        public ContractDefinition contract { get; }
        public String fnName { get; }
        public FunctionIdent(ContractDefinition contract, string fnName)
        {
            this.contract = contract;
            this.fnName = fnName;
        }

        public override string ToString()
        {
            if (contract == null)
            {
                return fnName;
            }
            
            return $"{contract.Name}.{fnName}";
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            visitor.Visit(this);
            visitor.EndVisit(this);
        }
    }

    public class FunctionDef : SmartLTLNode
    {
        public FunctionIdent ident { get; }
        public Params paramList { get; }

        public FunctionDefinition def { get; }
        
        public FunctionDef(FunctionIdent ident, Params paramList, FunctionDefinition def)
        {
            this.ident = ident;
            this.paramList = paramList;
            this.def = def;
        }
        
        public override string ToString()
        {
            if (paramList == null)
            {
                return ident.ToString();
            }
            
            return $"{ident}({paramList})";
        }

        public VariableDeclaration GetVarDecl(TranslatorContext ctxt, String name)
        {
            if (paramList != null)
            {
                for (int i = 0; i < paramList.paramList.Count; i++)
                { 
                    if (paramList.paramList[i].Equals(name))
                    {
                        return def.Parameters.Parameters[i];
                    }
                }
            }

            if (ident.contract != null)
            {
                if (ctxt.StateVarNameResolutionMap.ContainsKey(name) &&
                    ctxt.StateVarNameResolutionMap[name].ContainsKey(ident.contract))
                {
                    return ctxt.StateVarNameResolutionMap[name][ident.contract];
                } 
            }
            
            return null;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (paramList == null) return;
            if (visitor.Visit(this))
            {
                ident.Accept(visitor);
                paramList.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class Params : SmartLTLNode
    {
        public List<string> paramList { get; }
        public Params(List<string> paramList)
        {
            this.paramList = paramList;
        }
        
        public override string ToString()
        {
            if (paramList == null || paramList.Count == 0)
            {
                return "";
            }
            
            StringBuilder builder = new StringBuilder();
            builder.Append(paramList[0]);

            for (int i = 1; i < paramList.Count; i++)
            {
                builder.Append(", ").Append(paramList[i]);
            }

            return builder.ToString();
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            visitor.Visit(this);
            visitor.EndVisit(this);
        }
    }

    public class BinaryOp : Expr
    {
        public Expr lhs { get; }
        public Expr rhs { get; }
        public String op { get; }

        public TypeDescription resultDesc { get; }

        public BinaryOp(Expr lhs, String op, Expr rhs, TypeDescription resultDesc)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.op = op;
            this.resultDesc = resultDesc;
        }
        public override string ToString()
        {
            return $"({lhs} {op} {rhs})";
        }
        
        public Expression ToSolidityAST()
        {
            BinaryOperation binOp = new BinaryOperation();
            binOp.Operator = op;
            binOp.LeftExpression = lhs.ToSolidityAST();
            binOp.RightExpression = rhs.ToSolidityAST();
            return binOp;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return resultDesc;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                lhs.Accept(visitor);
                rhs.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class LiteralVal : Expr
    {
        public string val { get; }
        public string kind { get; }

        public TypeInfo litType { get; }

        public LiteralVal(BigInteger val)
        {
            this.val = val.ToString();
            this.val = "number";
            this.litType = TypeInfo.GetElementaryType("uint");
        }

        public LiteralVal(bool val)
        {
            this.val = val.ToString();
            this.kind = "bool";
            this.litType = TypeInfo.GetElementaryType("bool");
        }
        
        public override string ToString()
        {
            return val;
        }

        public Expression ToSolidityAST()
        {
            Literal lit = new Literal();
            lit.IsConstant = true;
            lit.TypeDescriptions = litType.description;
            lit.Kind = kind;
            lit.Value = val;
            return lit;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return litType.description;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                litType.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }
    
    public class UnaryOp : Expr
    {
        public String op { get; }
        public Expr expr { get; }

        public UnaryOp(String op, Expr expr)
        {
            this.expr = expr;
            this.op = op;
        }
        
        public override string ToString()
        {
            return $"{op}({expr})";
        }

        public Expression ToSolidityAST()
        {
            UnaryOperation unOp = new UnaryOperation();
            unOp.Operator = op;
            unOp.SubExpression = expr.ToSolidityAST();
            return unOp;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return expr.GetType(ctxt);
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                expr.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class Variable : Expr
    {
        public String name { get; }
        public int id { get; }

        public TypeDescription typeDesc;

        public Variable(String name, int declId, TypeDescription typeDesc)
        {
            this.name = name;
            this.id = declId;
            this.typeDesc = typeDesc;
        }

        public Expression ToSolidityAST()
        {
            Identifier ident = new Identifier();
            ident.Name = name;
            ident.ReferencedDeclaration = id;
            ident.TypeDescriptions = typeDesc;
            return ident;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return typeDesc;
        }

        public override string ToString()
        {
            return name;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            visitor.Visit(this);
            visitor.EndVisit(this);
        }
    }

    public class Member : Expr
    {
        public Expr baseExpr { get; }
        public string memberName { get; }

        public Member(Expr baseExpr, string memberName)
        {
            this.baseExpr = baseExpr;
            this.memberName = memberName;
        }
        
        public override string ToString()
        {
            return $"{baseExpr}.{memberName}";
        }

        public Expression ToSolidityAST()
        {
            MemberAccess access = new MemberAccess();
            access.Expression = baseExpr.ToSolidityAST();
            access.MemberName = memberName;
            return access;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            TypeDescription memberType = new TypeDescription();
            if (memberName.Equals("length"))
            {
                memberType.TypeString = "uint";
                return memberType;
            }

            if (memberName.Equals("balance"))
            {
                memberType.TypeString = "uint";
                return memberType;
            }
            
            if (baseExpr.GetType(ctxt).IsStruct())
            {
                throw new Exception("Structs currently not supported");
            }


            if (baseExpr is Variable var)
            {
                if (var.name.Equals("msg"))
                {
                    if (memberName.Equals("sender"))
                    {
                        memberType.TypeString = "address";
                        return memberType;
                    }
                    else if (memberName.Equals("value"))
                    {
                        memberType.TypeString = "uint";
                        return memberType;
                    }

                    throw new Exception($"Unknown member for msg: {memberName}");

                }
                else if (var.name.Equals("this"))
                {
                    ContractDefinition def = ctxt.GetContractByName(TransUtils.GetContractName(var.typeDesc));
                    if (def == null)
                    {
                        throw new Exception($"Could not find the definition of ${var.typeDesc.TypeString}");
                    }

                    VariableDeclaration decl = ctxt.GetStateVarByDynamicType(memberName, def);
                    if (decl != null)
                    {
                        return decl.TypeDescriptions;
                    }
                    
                    if (memberName.Equals("balance"))
                    {
                        memberType.TypeString = "uint";
                        return memberType;
                    }
                    throw new Exception($"Unknown member for this: {memberName}");
                } 
                else if (var.name.Equals("block"))
                {
                    if (memberName.Equals("timestamp") || memberName.Equals("number"))
                    {
                        memberType.TypeString = "uint";
                        return memberType;
                    }

                    throw new Exception($"Unknown member for this: {memberName}");
                }

                if (var.id != SmartLTLReader.UNKNOWN_ID)
                {
                    ASTNode refDecl = ctxt.GetASTNodeById(var.id);

                    if (refDecl is EnumDefinition)
                    {
                        memberType.TypeString = "uint";
                        return memberType;
                    }
                }
            }
            throw new Exception($"Could not find member {memberName} for {baseExpr}");
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                baseExpr.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class Index : Expr
    {
        public Expr baseExpr { get; }
        public Expr indexExpr { get; }

        public Index(Expr baseExpr, Expr indexExpr)
        {
            this.baseExpr = baseExpr;
            this.indexExpr = indexExpr;
        }

        public override string ToString()
        {
            return $"{baseExpr}[{indexExpr}]";
        }

        public Expression ToSolidityAST()
        {
            IndexAccess access = new IndexAccess();
            access.BaseExpression = baseExpr.ToSolidityAST();
            access.IndexExpression = indexExpr.ToSolidityAST();
            return access;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return MapArrayHelper.InferValueTypeDescriptionFromTypeString(baseExpr.GetType(ctxt).TypeString);
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                baseExpr.Accept(visitor);
                indexExpr.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class ArgList : SmartLTLNode
    {
        public List<Expr> args { get; }

        public ArgList(List<Expr> args)
        {
            this.args = args;
        }

        public override string ToString()
        {
            if (args == null || args.Count == 0)
            {
                return "";
            }
            
            StringBuilder builder = new StringBuilder();
            builder.Append(args[0]);

            for (int i = 1; i < args.Count; i++)
            {
                builder.Append(", ").Append(args[i]);
            }

            return builder.ToString();
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                foreach (Expr arg in args)
                {
                    arg.Accept(visitor);
                }
            }
            
            visitor.EndVisit(this);
        }
    }

    public class Fsum : Expr
    {
        private static int curId = 0;
        public FunctionDef tgtFn { get; }
        public Expr sumExpr { get; }
        public Expr constraint { get; }
        
        public VariableDeclaration varDecl { get; }

        public Fsum(FunctionDef tgtFn, Expr sumExpr, Expr constraint, VariableDeclaration decl)
        {
            this.tgtFn = tgtFn;
            this.sumExpr = sumExpr;
            this.constraint = constraint;
            Literal zero = new Literal();
            zero.Kind = "uint";
            zero.Value = "0";
            zero.HexValue = "0x0";
            this.varDecl = decl;
            varDecl.Value = zero;
            curId++;
        }
        
        public override string ToString()
        {
            return $"fsum({tgtFn}, {sumExpr}, {constraint})";
        }
        
        public Statement GetAccExpr()
        {
            Identifier ident = new Identifier();
            ident.Name = varDecl.Name;
            ident.TypeDescriptions = varDecl.TypeDescriptions;
            ident.ReferencedDeclaration = varDecl.Id;
            
            Assignment assignment = new Assignment();
            assignment.Operator = "+=";
            assignment.LeftHandSide = ident;
            assignment.RightHandSide = sumExpr.ToSolidityAST();
            
            ExpressionStatement stmt = new ExpressionStatement();
            stmt.Expression = assignment;

            IfStatement ifStmt = new IfStatement();
            ifStmt.Condition = constraint.ToSolidityAST();
            ifStmt.FalseBody = null;
            ifStmt.TrueBody = stmt;

            return ifStmt;
        }

        public Expression ToSolidityAST()
        {
            Identifier ident = new Identifier();
            ident.Name = varDecl.Name;
            ident.ReferencedDeclaration = varDecl.Id;
            ident.TypeDescriptions = varDecl.TypeDescriptions;
            return ident;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return varDecl.TypeDescriptions;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                tgtFn.Accept(visitor);
                sumExpr.Accept(visitor);
                constraint.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }
    
    public class Csum : Expr
    {
        public Variable trackedVar { get; }

        public Csum(Variable trackedVar)
        {
            this.trackedVar = trackedVar;
        }
        
        public override string ToString()
        {
            return $"csum({trackedVar.ToString()})";
        }

        public Expression ToSolidityAST()
        {
            /*throw new Exception("this translation is not right, need to add something to the solidity ast");
            Identifier ident = new Identifier();
            ident.Name = sumVar.Name;
            ident.ReferencedDeclaration = sumVar.Id;
            ident.TypeDescriptions = sumVar.TypeDescriptions;
            return ident;*/
            
            Sum sum = new Sum();
            sum.ReferencedId = trackedVar.id;
            sum.SumExpression = trackedVar.ToSolidityAST();
            return sum;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return TypeInfo.GetElementaryType("uint").description;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                trackedVar.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }

    public class UtilityCall : Expr
    {
        public ArgList args { get; }
        public string name { get; }
        
        public TypeDescription retType { get; }

        public UtilityCall(TypeDescription retType, string name, ArgList args)
        {
            this.name = name;
            this.args = args;
            this.retType = retType;
        }

        public override string ToString()
        {
            return $"{name}({args})";
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                visitor.Visit(args);
            }
            visitor.EndVisit(this);
        }

        public Expression ToSolidityAST()
        {
            UtilityFnCall utilityCall = new UtilityFnCall();
            utilityCall.TypeDescriptions = retType;
            utilityCall.Name = name;
            utilityCall.Arguments = new List<Expression>();
            foreach (Expr expr in args.args)
            {
                utilityCall.Arguments.Add(expr.ToSolidityAST());
            }

            return utilityCall;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return retType;
        }
    }
    
    public class Function : Expr
    {
        public FunctionIdent fnIdent { get; }
        public ArgList args { get; }

        public FunctionDefinition def;
        public VariableDeclaration retDecl { get; }
        
        public Function(FunctionIdent ident, ArgList args, FunctionDefinition fnDef, VariableDeclaration retDecl)
        {
            this.fnIdent = ident;
            this.args = args;
            this.def = fnDef;
            this.retDecl = retDecl;
        }

        public override string ToString()
        {
            return $"{fnIdent}({args})";
        }

        public Expression GetCallExpr()
        {
            Identifier callIdent = new Identifier();
            callIdent.Name = def.Name;
            callIdent.ReferencedDeclaration = def.Id;
            
            FunctionCall call = new FunctionCall();
            call.Arguments = new List<Expression>();
            call.Kind = "functionCall";
            foreach (Expr expr in args.args)
            {
                call.Arguments.Add(expr.ToSolidityAST());
            }

            call.Expression = callIdent;

            call.Id = def.Id;
            
            Identifier ident = new Identifier();
            ident.Name = retDecl.Name;
            ident.ReferencedDeclaration = retDecl.Id;
            ident.TypeDescriptions = retDecl.TypeDescriptions;
            
            Assignment assignment = new Assignment();
            assignment.Operator = "=";
            assignment.LeftHandSide = ident;
            assignment.RightHandSide = call;
            
            return assignment;
        }

        public Expression ToSolidityAST()
        {
            Identifier nameIdent = new Identifier();
            nameIdent.Name = retDecl.Name;
            nameIdent.ReferencedDeclaration = retDecl.Id;
            nameIdent.TypeDescriptions = retDecl.TypeDescriptions;

            return nameIdent;
        }

        public TypeDescription GetType(TranslatorContext ctxt)
        {
            return retDecl.TypeDescriptions;
        }

        public void Accept(ILTLASTVisitor visitor)
        {
            if (visitor.Visit(this))
            {
                fnIdent.Accept(visitor);
                args.Accept(visitor);
            }
            
            visitor.EndVisit(this);
        }
    }
}