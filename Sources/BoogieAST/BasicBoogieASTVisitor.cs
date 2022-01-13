namespace BoogieAST
{
    public class BasicBoogieASTVisitor : IBoogieASTVisitor
    {
        protected virtual bool CommonVisit(BoogieASTNode node)
        {
            return true;
        }

        protected virtual void CommonEndVisit(BoogieASTNode node)
        {
            // left empty
        }

        public virtual bool Visit(BoogieProgram node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieTypeCtorDecl node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieAttribute node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieProcedure node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieFunction node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieAxiom node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieImplementation node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieBasicType node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieCtorType node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieMapType node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieTypedIdent node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieConstant node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieGlobalVariable node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieLocalVariable node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieFormalParam node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieStmtList node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieBigBlock node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieAssignCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieCallCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieAssertCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieAssumeCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieLoopInvCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieReturnCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieReturnExprCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieGotoCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieHavocCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieSkipCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieCommentCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieCommentDeclaration node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieWildcardExpr node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieIfCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieWhileCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieBreakCmd node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieLiteralExpr node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieIdentifierExpr node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieMapSelect node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieMapUpdate node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieUnaryOperation node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieBinaryOperation node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieITE node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieQuantifiedExpr node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieFuncCallExpr node) { return CommonVisit(node); }
        public virtual bool Visit(BoogieTupleExpr node) { return CommonVisit(node); }

        public virtual void EndVisit(BoogieProgram node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieTypeCtorDecl node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieAttribute node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieProcedure node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieFunction node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieAxiom node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieImplementation node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieBasicType node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieCtorType node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieMapType node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieTypedIdent node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieConstant node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieGlobalVariable node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieLocalVariable node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieFormalParam node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieStmtList node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieBigBlock node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieAssignCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieCallCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieAssertCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieAssumeCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieLoopInvCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieReturnCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieReturnExprCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieGotoCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieHavocCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieSkipCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieCommentCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieCommentDeclaration node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieWildcardExpr node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieIfCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieWhileCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieBreakCmd node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieLiteralExpr node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieIdentifierExpr node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieMapSelect node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieMapUpdate node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieUnaryOperation node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieBinaryOperation node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieITE node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieQuantifiedExpr node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieFuncCallExpr node) { CommonEndVisit(node); }
        public virtual void EndVisit(BoogieTupleExpr node) { CommonEndVisit(node); }
    }
}