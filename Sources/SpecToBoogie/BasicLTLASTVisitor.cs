namespace SpecToBoogie
{
    public class BasicLTLASTVisitor : ILTLASTVisitor
    {
        protected virtual bool CommonVisit(SmartLTLNode node)
        {
            return true;
        }

        protected virtual void CommonEndVisit(SmartLTLNode node)
        {
            // left empty
        }
        public virtual bool Visit(VarDeclList declList) { return CommonVisit(declList); }

        public virtual bool Visit(TypeInfo info) { return CommonVisit(info); }

        public virtual bool Visit(VariableDecl decl) { return CommonVisit(decl); }

        public virtual bool Visit(TempBinOp binOp) { return CommonVisit(binOp); }

        public virtual bool Visit(TempUnOp unOp) { return CommonVisit(unOp); }

        public virtual bool Visit(Atom atom) { return CommonVisit(atom); }

        public virtual bool Visit(FunctionIdent ident) { return CommonVisit(ident); }

        public virtual bool Visit(FunctionDef def) { return CommonVisit(def); }

        public virtual bool Visit(Params paramList) { return CommonVisit(paramList); }

        public virtual bool Visit(BinaryOp binOp) { return CommonVisit(binOp); }

        public virtual bool Visit(LiteralVal val) { return CommonVisit(val); }

        public virtual bool Visit(UnaryOp unOp) { return CommonVisit(unOp); }

        public virtual bool Visit(Variable var) { return CommonVisit(var); }

        public virtual bool Visit(Member member) { return CommonVisit(member); }

        public virtual bool Visit(Index ind) { return CommonVisit(ind); }

        public virtual bool Visit(ArgList argList) { return CommonVisit(argList); }

        public virtual bool Visit(Function fn) { return CommonVisit(fn); }
        
        public virtual bool Visit(Fsum fn) { return CommonVisit(fn); }
        
        public virtual bool Visit(Csum fn) { return CommonVisit(fn); }
        
        public virtual bool Visit(UtilityCall fn) { return CommonVisit(fn); }

        public virtual void EndVisit(VarDeclList declList) { CommonEndVisit(declList); }

        public virtual void EndVisit(TypeInfo info) { CommonEndVisit(info); }

        public virtual void EndVisit(VariableDecl decl) { CommonEndVisit(decl); }

        public virtual void EndVisit(TempBinOp binOp) { CommonEndVisit(binOp); }

        public virtual void EndVisit(TempUnOp unOp) { CommonEndVisit(unOp); }

        public virtual void EndVisit(Atom atom) { CommonEndVisit(atom); }

        public virtual void EndVisit(FunctionIdent ident) { CommonEndVisit(ident); }

        public virtual void EndVisit(FunctionDef def) { CommonEndVisit(def); }

        public virtual void EndVisit(Params paramList) { CommonEndVisit(paramList); }

        public virtual void EndVisit(BinaryOp binOp) { CommonEndVisit(binOp); }

        public virtual void EndVisit(LiteralVal val) { CommonEndVisit(val); }

        public virtual void EndVisit(UnaryOp unOp) { CommonEndVisit(unOp); }

        public virtual void EndVisit(Variable var) { CommonEndVisit(var); }

        public virtual void EndVisit(Member member) { CommonEndVisit(member); }

        public virtual void EndVisit(Index ind) { CommonEndVisit(ind); }

        public virtual void EndVisit(ArgList argList) { CommonEndVisit(argList); }

        public virtual void EndVisit(Function fn) { CommonEndVisit(fn); }
        
        public virtual void EndVisit(Fsum fn) { CommonEndVisit(fn); }
        
        public virtual void EndVisit(Csum fn) { CommonEndVisit(fn); }
        
        public virtual void EndVisit(UtilityCall fn) { CommonEndVisit(fn); }
    }
}