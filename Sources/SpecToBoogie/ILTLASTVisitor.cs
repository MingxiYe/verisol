namespace SpecToBoogie
{
    public interface ILTLASTVisitor
    {
        bool Visit(VarDeclList declList);
        bool Visit(TypeInfo info);
        bool Visit(VariableDecl decl);
        bool Visit(TempBinOp binOp);
        bool Visit(TempUnOp unOp);
        bool Visit(Atom atom);
        bool Visit(FunctionIdent ident);
        bool Visit(FunctionDef def);
        bool Visit(Params paramList);
        bool Visit(BinaryOp binOp);
        bool Visit(LiteralVal val);
        bool Visit(UnaryOp unOp);
        bool Visit(Variable var);
        bool Visit(Member member);
        bool Visit(Index ind);
        bool Visit(ArgList argList);
        bool Visit(Function fn);
        
        void EndVisit(VarDeclList declList);
        void EndVisit(TypeInfo info);
        void EndVisit(VariableDecl decl);
        void EndVisit(TempBinOp binOp);
        void EndVisit(TempUnOp unOp);
        void EndVisit(Atom atom);
        void EndVisit(FunctionIdent ident);
        void EndVisit(FunctionDef def);
        void EndVisit(Params paramList);
        void EndVisit(BinaryOp binOp);
        void EndVisit(LiteralVal val);
        void EndVisit(UnaryOp unOp);
        void EndVisit(Variable var);
        void EndVisit(Member member);
        void EndVisit(Index ind);
        void EndVisit(ArgList argList);
        void EndVisit(Function fn);
    }
}