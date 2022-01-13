namespace BoogieAST
{
    public interface IBoogieASTVisitor
    {
        bool Visit(BoogieProgram node);
        bool Visit(BoogieTypeCtorDecl node);
        bool Visit(BoogieAttribute node);
        bool Visit(BoogieProcedure node);
        bool Visit(BoogieFunction node);
        bool Visit(BoogieAxiom node);
        bool Visit(BoogieImplementation node);
        bool Visit(BoogieBasicType node);
        bool Visit(BoogieCtorType node);
        bool Visit(BoogieMapType node);
        bool Visit(BoogieTypedIdent node);
        bool Visit(BoogieConstant node);
        bool Visit(BoogieGlobalVariable node);
        bool Visit(BoogieLocalVariable node);
        bool Visit(BoogieFormalParam node);
        bool Visit(BoogieStmtList node);
        bool Visit(BoogieBigBlock node);
        bool Visit(BoogieAssignCmd node);
        bool Visit(BoogieCallCmd node);
        bool Visit(BoogieAssertCmd node);
        bool Visit(BoogieAssumeCmd node);
        bool Visit(BoogieLoopInvCmd node);
        bool Visit(BoogieReturnCmd node);
        bool Visit(BoogieReturnExprCmd node);
        bool Visit(BoogieGotoCmd node);
        bool Visit(BoogieHavocCmd node);
        bool Visit(BoogieSkipCmd node);
        bool Visit(BoogieCommentCmd node);
        bool Visit(BoogieCommentDeclaration node);
        bool Visit(BoogieWildcardExpr node);
        bool Visit(BoogieIfCmd node);
        bool Visit(BoogieWhileCmd node);
        bool Visit(BoogieBreakCmd node);
        bool Visit(BoogieLiteralExpr node);
        bool Visit(BoogieIdentifierExpr node);
        bool Visit(BoogieMapSelect node);
        bool Visit(BoogieMapUpdate node);
        bool Visit(BoogieUnaryOperation node);
        bool Visit(BoogieBinaryOperation node);
        bool Visit(BoogieITE node);
        bool Visit(BoogieQuantifiedExpr node);
        bool Visit(BoogieFuncCallExpr node);
        bool Visit(BoogieTupleExpr node);
        
        void EndVisit(BoogieProgram node);
        void EndVisit(BoogieTypeCtorDecl node);
        void EndVisit(BoogieAttribute node);
        void EndVisit(BoogieProcedure node);
        void EndVisit(BoogieFunction node);
        void EndVisit(BoogieAxiom node);
        void EndVisit(BoogieImplementation node);
        void EndVisit(BoogieBasicType node);
        void EndVisit(BoogieCtorType node);
        void EndVisit(BoogieMapType node);
        void EndVisit(BoogieTypedIdent node);
        void EndVisit(BoogieConstant node);
        void EndVisit(BoogieGlobalVariable node);
        void EndVisit(BoogieLocalVariable node);
        void EndVisit(BoogieFormalParam node);
        void EndVisit(BoogieStmtList node);
        void EndVisit(BoogieBigBlock node);
        void EndVisit(BoogieAssignCmd node);
        void EndVisit(BoogieCallCmd node);
        void EndVisit(BoogieAssertCmd node);
        void EndVisit(BoogieAssumeCmd node);
        void EndVisit(BoogieLoopInvCmd node);
        void EndVisit(BoogieReturnCmd node);
        void EndVisit(BoogieReturnExprCmd node);
        void EndVisit(BoogieGotoCmd node);
        void EndVisit(BoogieHavocCmd node);
        void EndVisit(BoogieSkipCmd node);
        void EndVisit(BoogieCommentCmd node);
        void EndVisit(BoogieCommentDeclaration node);
        void EndVisit(BoogieWildcardExpr node);
        void EndVisit(BoogieIfCmd node);
        void EndVisit(BoogieWhileCmd node);
        void EndVisit(BoogieBreakCmd node);
        void EndVisit(BoogieLiteralExpr node);
        void EndVisit(BoogieIdentifierExpr node);
        void EndVisit(BoogieMapSelect node);
        void EndVisit(BoogieMapUpdate node);
        void EndVisit(BoogieUnaryOperation node);
        void EndVisit(BoogieBinaryOperation node);
        void EndVisit(BoogieITE node);
        void EndVisit(BoogieQuantifiedExpr node);
        void EndVisit(BoogieFuncCallExpr node);
        void EndVisit(BoogieTupleExpr node);
    }
}