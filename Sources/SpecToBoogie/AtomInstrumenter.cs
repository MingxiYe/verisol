using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BoogieAST;
using SolidityAST;
using SolToBoogie;

namespace SpecToBoogie
{
    public class AtomInstrumenter
    {
        private TranslatorContext ctxt;
        private Spec spec;
        
        public Dictionary<String, List<Atom>> startedVars { get; private set; }
        public Dictionary<String, List<Atom>> willSucceedVars { get; private set; }
        public Dictionary<String, List<Atom>> finishedVars { get; private set; }
        public Dictionary<String, List<Atom>> revertedVars { get; private set; }

        private HashSet<Atom> initializedAtom;
        
        public List<Atom> sendSuccessBegin { get; private set; }
        
        public List<Atom> sendSuccessEnd { get; private set; }
        
        public List<Atom> sendFailBegin { get; private set; }
        
        public List<Atom> sendFailEnd { get; private set; }

        public HashSet<Tuple<ContractDefinition, FunctionDefinition>> visibleFns { get; private set; }

        private HashSet<string> revertHoldFns;

        public AtomInstrumenter(TranslatorContext ctxt, Spec spec)
        {
            this.ctxt = ctxt;
            this.spec = spec;
            initializedAtom = new HashSet<Atom>();
            startedVars = new Dictionary<string, List<Atom>>();
            willSucceedVars = new Dictionary<string, List<Atom>>();
            finishedVars = new Dictionary<string, List<Atom>>();
            revertedVars = new Dictionary<string, List<Atom>>();
            sendSuccessBegin = new List<Atom>();
            sendSuccessEnd = new List<Atom>();
            sendFailBegin = new List<Atom>();
            sendFailEnd = new List<Atom>();
            visibleFns = getVisibleFns(ctxt);
            revertHoldFns = new HashSet<string>();
            createTrackingVars();
        }

        private BoogieExpr translateExpr(Atom atom)
        {
            if (atom.constraint == null)
            {
                return new BoogieLiteralExpr(true);
            }
            
            Tuple<BoogieStmtList, BoogieExpr> translation = ctxt.procedureTranslator.TranslateSolExpr(atom.constraint.ToSolidityAST());
            /*if (translation.Item1.BigBlocks.Count != 0 && translation.Item1.BigBlocks[0].SimpleCmds.Count != 0)
            {
                throw new Exception("Translation must only contain expressions");
            }*/

            return translation.Item2;
        }

        private BoogieIfCmd findExceptionCheck(BoogieStmtList body)
        {
            BoogieIfCmd targetIf = null;
            foreach (BoogieCmd boogieCmd in body.BigBlocks[0].SimpleCmds)
            {
                if (boogieCmd.GetType().Equals(typeof(BoogieIfCmd)))
                {
                    BoogieIfCmd boogieIfCmd = (BoogieIfCmd) boogieCmd;
                    if (boogieIfCmd.Guard.GetType().Equals(typeof(BoogieIdentifierExpr)))
                    {
                        BoogieIdentifierExpr boogieIDExpr = (BoogieIdentifierExpr) boogieIfCmd.Guard;
                        if (boogieIDExpr.Name.Equals("__exception"))
                        {
                            targetIf = boogieIfCmd;
                            break;
                        }
                    }
                }
            }

            return targetIf;
        }

        public BoogieCallCmd findCall(Function fnCall, BoogieStmtList stmts)
        {
            foreach (BoogieCmd stmt in stmts.BigBlocks[0].SimpleCmds)
            {
                if (stmt is BoogieCallCmd call)
                {
                    if (call.Callee.Equals(TransUtils.GetCanonicalFunctionName(fnCall.def, ctxt)))
                    {
                        return call;
                    }
                }
            }

            return null;
        }

        public BoogieStmtList translateFnCalls(BoogieImplementation fn, HashSet<Function> fnCalls)
        {
            BoogieStmtList stmts = new BoogieStmtList();
            List<BoogieVariable> fnVars = new List<BoogieVariable>();
            foreach(Function fnCall in fnCalls)
            {
                if (!revertHoldFns.Contains(fn.Name))
                {
                    BoogieVariable revertHold = new BoogieLocalVariable(new BoogieTypedIdent("revertHold", BoogieType.Bool));
                    fn.LocalVars.Add(revertHold);
                    revertHoldFns.Add(fn.Name);
                }
                    
                
                
                Tuple<BoogieStmtList, BoogieExpr> translation = ctxt.procedureTranslator.TranslateSolExpr(fnCall.GetCallExpr());
                if (fnCall.isTypeCast())
                {
                    stmts.AppendStmtList(translation.Item1);
                }
                else {
                    BoogieCallCmd callCmd = findCall(fnCall, translation.Item1);
                    callCmd.Callee += "__success";
                    BoogieIdentifierExpr revertHoldExpr = new BoogieIdentifierExpr("revertHold");
                    BoogieIdentifierExpr revertExpr = new BoogieIdentifierExpr("revert");
                    BoogieAssignCmd notRevert = new BoogieAssignCmd(revertExpr, new BoogieLiteralExpr(false));
                    BoogieAssignCmd revertSave = new BoogieAssignCmd(revertHoldExpr, revertExpr);
                    BoogieAssignCmd revertRestore = new BoogieAssignCmd(revertExpr, revertHoldExpr);
                    stmts.AddStatement(revertSave);
                    stmts.AddStatement(notRevert);
                    stmts.AddStatement(callCmd);
                    stmts.AddStatement(revertRestore);
                }
                
                fnVars.Add(new BoogieLocalVariable(new BoogieTypedIdent(fnCall.retDecl.Name, TransUtils.GetBoogieTypeFromSolidityTypeName(fnCall.retDecl.TypeName))));
            }
            
            fn.LocalVars.AddRange(fnVars);
            return stmts;
        }
        
        private BoogieStmtList getImplicitFsumInstrumentation(Fsum fsum, bool isSend, BoogieStmtList translation)
        {
            if (isSend)
            {
                BoogieIfCmd ifCmd = null;
                foreach (BoogieCmd stmt in translation.BigBlocks[0].SimpleCmds)
                {
                    if (stmt is BoogieIfCmd cond)
                    {
                        ifCmd = cond;
                    }
                }

                if (ifCmd == null)
                {
                    throw new Exception("Could not find fsum if statement");
                }
                
                BoogieIdentifierExpr exception = new BoogieIdentifierExpr("__exception");
                BoogieExpr notException = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, exception);
                BoogieIdentifierExpr balance = new BoogieIdentifierExpr("Balance");
                BoogieIdentifierExpr from = new BoogieIdentifierExpr("from");
                BoogieIdentifierExpr amount = new BoogieIdentifierExpr("amount");
                BoogieExpr balanceFrom = new BoogieMapSelect(balance, from);
                BoogieExpr availableBal = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, balanceFrom, amount); 
                BoogieExpr check = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, notException, availableBal);
                BoogieExpr newGuard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ifCmd.Guard, check);
                ifCmd.Guard = newGuard;
            }

            return translation;
        }

        private void initializeVar(string varName, BoogieExpr initVal)
        {
            BoogieIdentifierExpr varExpr = new BoogieIdentifierExpr(varName);
            BoogieBinaryOperation eqZero = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, varExpr, initVal);
            BoogieAssumeCmd init = new BoogieAssumeCmd(eqZero);
            FunctionSearch fs = new FunctionSearch("main");
            ctxt.Program.Accept(fs);
            if (fs.desired == null)
            {
                throw new Exception("Could not find main");
            }
            
            fs.desired.StructuredStmts.PrependStatement(init);
        }
        
        private void instrumentFsum(HashSet<Fsum> fsums)
        {
            List<BoogieTypedIdent> fsumVars = new List<BoogieTypedIdent>();
            
            foreach (Fsum sum in fsums)
            {
                string boogieFnName = null;
                bool isSend = false;
                if (sum.tgtFn.ident.fnName.Equals("send") && sum.tgtFn.ident.contract == null)
                {
                    boogieFnName = "send__success";
                    isSend = true;
                }
                else
                {
                    boogieFnName = TransUtils.GetCanonicalFunctionName(sum.tgtFn.def, ctxt) + "__success";
                }
                
                FunctionSearch fs = new FunctionSearch(boogieFnName);
                ctxt.Program.Accept(fs);
                if (fs.desired == null)
                {
                    throw new Exception("Could not find " + sum.tgtFn.ident.fnName);
                }

                BoogieImplementation tgtFn = fs.desired;
                
                BoogieStmtList translation = ctxt.procedureTranslator.TranslateSolStmt(sum.GetAccExpr());
                translation = getImplicitFsumInstrumentation(sum, isSend, translation);

                if (isSend)
                {
                    tgtFn.StructuredStmts.InsertStmtList(1, translation);
                }
                else
                {
                    tgtFn.StructuredStmts.PrependStmtList(translation);
                }
                
                fsumVars.Add(new BoogieTypedIdent(sum.varDecl.Name, BoogieType.Int));
                initializeVar(sum.varDecl.Name, new BoogieLiteralExpr(BigInteger.Zero));
                /*BoogieIdentifierExpr varExpr = new BoogieIdentifierExpr(sum.varDecl.Name);
                BoogieLiteralExpr zeroExpr = new BoogieLiteralExpr(BigInteger.Zero);
                BoogieBinaryOperation eqZero = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, varExpr, zeroExpr);
                BoogieAssumeCmd init = new BoogieAssumeCmd(eqZero);
                fs = new FunctionSearch("main");
                ctxt.Program.Accept(fs);
                if (fs.desired == null)
                {
                    throw new Exception("Could not find main");
                }
                
                fs.desired.StructuredStmts.PrependStatement(init);*/
            }
            
            declareVars(fsumVars);
        }

        private BoogieExpr getImplicitAtomInstrumentation(Atom atom, bool isSend, BoogieExpr val)
        {
            if (isSend)
            {
                if (atom.loc == AtomLoc.FINISHED)
                {
                    BoogieIdentifierExpr success = new BoogieIdentifierExpr("success");
                    val = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, val, success);
                }
                else if (atom.loc == AtomLoc.REVERTED)
                {
                    BoogieIdentifierExpr success = new BoogieIdentifierExpr("success");
                    BoogieExpr notSuccess = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, success);
                    val = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, val, notSuccess);
                }
                else if (atom.loc == AtomLoc.WILL_SUCCEED)
                {
                    BoogieIdentifierExpr exception = new BoogieIdentifierExpr("__exception");
                    BoogieExpr notException = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, exception);
                    BoogieIdentifierExpr balance = new BoogieIdentifierExpr("Balance");
                    BoogieIdentifierExpr from = new BoogieIdentifierExpr("from");
                    BoogieIdentifierExpr amount = new BoogieIdentifierExpr("amount");
                    BoogieExpr balanceFrom = new BoogieMapSelect(balance, from);
                    BoogieExpr availableBal = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, balanceFrom, amount);
                    BoogieExpr check = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, notException, availableBal);
                    val = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, val, check);
                }
            }

            if (atom.tgtFn.ident.fnName == "*" && atom.contractConstraints.Count != 0)
            {
                if (atom.contractConstraints.Count != ctxt.ContractDefinitions.Count)
                {
                    BoogieExpr typeChk = null;
                    foreach (ContractDefinition def in atom.contractConstraints)
                    {
                        BoogieExpr DTypeExpr = new BoogieIdentifierExpr("DType");
                        BoogieExpr thisExpr = new BoogieIdentifierExpr("this");
                        BoogieExpr typeExpr = new BoogieIdentifierExpr(def.Name);
                        
                        BoogieMapSelect typeSel = new BoogieMapSelect(DTypeExpr, thisExpr);
                        BoogieBinaryOperation cmp = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, typeSel, typeExpr);
                        
                        if (typeChk == null)
                        {
                            typeChk = cmp;
                        }
                        else
                        {
                            typeChk = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, typeChk, cmp);
                        }
                    }
                    
                    val = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, val, typeChk);
                }
            }

            return val;
        }
        
        private HashSet<Fsum> instrumentAtom(BoogieImplementation fn, BoogieStmtList body, List<Atom> atoms, bool isSend, bool atEnd, int pos = 0)
        {
            HashSet<Function> fnCalls = new HashSet<Function>();
            HashSet<Fsum> fsums = new HashSet<Fsum>();
            foreach (Atom atom in atoms)
            {
                if (initializedAtom.Add(atom))
                {
                    initializeVar(atom.name, new BoogieLiteralExpr(false));
                }
                if (atom.constraint == null)
                {
                    continue;
                }
                
                CallFinder finder = new CallFinder();
                atom.constraint.Accept(finder);
                fnCalls.UnionWith(finder.fns);
                fsums.UnionWith(finder.fsums);
            }
            
            BoogieStmtList instrumentation = translateFnCalls(fn, fnCalls);
            
            foreach (Atom atom in atoms)
            {
                BoogieExpr val = translateExpr(atom);
                val = getImplicitAtomInstrumentation(atom, isSend, val);
                BoogieAssignCmd varSet = new BoogieAssignCmd(new BoogieIdentifierExpr(atom.name), val);
                instrumentation.AddStatement(varSet);
            }
            
            foreach (Atom atom in atoms)
            {
                BoogieExpr val = new BoogieLiteralExpr(false);
                BoogieAssignCmd varSet = new BoogieAssignCmd(new BoogieIdentifierExpr(atom.name), val);
                instrumentation.AddStatement(varSet);
            }

            if (atEnd)
            {
                body.AppendStmtList(instrumentation);
            }
            else
            {
                body.InsertStmtList(pos, instrumentation);
            }

            return fsums;
        }

        public void instrument()
        {
            
            if (spec.freeVars != null)
            {
                List<BoogieTypedIdent> freeVars = new List<BoogieTypedIdent>();
                foreach (VariableDecl decl in spec.freeVars.decls)
                {
                    freeVars.Add(new BoogieTypedIdent(decl.name, TransUtils.GetBoogieTypeFromSolidityTypeName(decl.type.name)));
                }
                declareVars(freeVars);
            }
            
            
            HashSet<Fsum> fsums = new HashSet<Fsum>();
            // started
            foreach (String functionName in startedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                ctxt.Program.Accept(fs);
                fsums.UnionWith(instrumentAtom(fs.desired, fs.desired.StructuredStmts, startedVars[functionName], false, false, 1));
            }
            
            // reverted
            foreach (String functionName in revertedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                ctxt.Program.Accept(fs);
                BoogieIfCmd targetIf = findExceptionCheck(fs.desired.StructuredStmts);
                
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
                
                fsums.UnionWith(instrumentAtom(fs.desired, targetIf.ThenBody, revertedVars[functionName], false, true));
            }
            
            // will succeed
            foreach (String functionName in willSucceedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                ctxt.Program.Accept(fs);
                BoogieIfCmd targetIf = findExceptionCheck(fs.desired.StructuredStmts);
                
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
               
                fsums.UnionWith(instrumentAtom(fs.desired, targetIf.ElseBody, willSucceedVars[functionName], false, false, 0));
            }
            
            // finished
            foreach (String functionName in finishedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                ctxt.Program.Accept(fs);
                BoogieIfCmd targetIf = findExceptionCheck(fs.desired.StructuredStmts);
                
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
               
                fsums.UnionWith(instrumentAtom(fs.desired, targetIf.ElseBody, finishedVars[functionName], false, true));
            }

            FunctionSearch sendSearch = new FunctionSearch("send__success");
            ctxt.Program.Accept(sendSearch);
            BoogieImplementation sendSuccess = sendSearch.desired;
            fsums.UnionWith(instrumentAtom(sendSuccess, sendSuccess.StructuredStmts, sendSuccessBegin, true, false, 1));
            fsums.UnionWith(instrumentAtom(sendSuccess, sendSuccess.StructuredStmts, sendSuccessEnd, true, true));
            
            sendSearch = new FunctionSearch("send__fail");
            ctxt.Program.Accept(sendSearch);
            BoogieImplementation sendFail = sendSearch.desired;
            fsums.UnionWith(instrumentAtom(sendFail, sendFail.StructuredStmts, sendFailBegin, true, false, 1));
            fsums.UnionWith(instrumentAtom(sendFail, sendFail.StructuredStmts, sendFailEnd, true, true));
            instrumentFsum(fsums);
        }

        private void addWildcardVar(Dictionary<String, List<Atom>> fnToVars, Atom atom)
        {
            foreach (Tuple<ContractDefinition,FunctionDefinition> func in visibleFns)
            {
                string boogieFnName = TransUtils.GetCanonicalFunctionName(func.Item2, ctxt);
                Console.WriteLine("Function Name: " + boogieFnName + '\n');
                if (!fnToVars.ContainsKey(boogieFnName)) fnToVars.Add(boogieFnName, new List<Atom>());
                fnToVars[boogieFnName].Add(atom);
            }
        }

        private void addSendVar(List<Atom> sendSuccessVars, List<Atom> sendFailVars, Atom atom)
        {
            sendSuccessVars?.Add(atom);
            sendFailVars?.Add(atom);
        }
        
        private void addFnVar(Dictionary<String, List<Atom>> fnToVars, Atom atom)
        {
            string boogieFnName = TransUtils.GetCanonicalFunctionName(atom.tgtFn.def, ctxt);
            if (!fnToVars.ContainsKey(boogieFnName)) fnToVars.Add(boogieFnName, new List<Atom>());
            fnToVars[boogieFnName].Add(atom);
        }

        private void addVar(Atom atom, Dictionary<String, List<Atom>> fnToVars, List<Atom> sendSuccessVars, List<Atom> sendFailVars)
        {
            String fnName = atom.tgtFn.def.Name;
            ContractDefinition contract = atom.tgtFn.ident.contract;
            
            bool isWildcard = fnName.Equals("*");
            bool isSend = fnName.Equals("send") && contract == null;
            
            if (isWildcard)
            {
                addWildcardVar(fnToVars, atom);
            }
            else if (isSend)
            {
                addSendVar(sendSuccessVars, sendFailVars, atom);
            }
            else
            {
                addFnVar(fnToVars, atom);
            }
        }

        private void declareVars(IEnumerable<BoogieTypedIdent> vars)
        {
            // Variable declaration
            for (int i = 0; i < ctxt.Program.Declarations.Count; i++)
            {
                BoogieDeclaration declaration = ctxt.Program.Declarations[i];
                if (declaration.GetType().Equals(typeof(BoogieGlobalVariable)))
                {
                    foreach (BoogieTypedIdent var in vars)
                    {
                        ctxt.Program.Declarations.Insert(i, new BoogieGlobalVariable(var));
                    }
                    break;
                }
            }
        }
        
        private void createTrackingVars()
        {
            AtomFinder atomFinder = readSpec();
            
            foreach (Atom atom in atomFinder.atoms)
            {
                AtomLoc loc = atom.loc;

                if (loc.Equals(AtomLoc.STARTED))
                {
                    addVar(atom, startedVars, sendSuccessBegin, sendFailBegin);
                }
                else if (loc.Equals(AtomLoc.WILL_SUCCEED))
                {
                    addVar(atom, willSucceedVars, sendSuccessBegin, null);
                }
                else if (loc.Equals(AtomLoc.FINISHED))
                {
                    addVar(atom, finishedVars, sendSuccessEnd, null);
                }
                else if (loc.Equals(AtomLoc.REVERTED))
                {
                    addVar(atom, revertedVars, sendSuccessEnd, sendFailEnd);
                }
            }
            
            declareVars(atomFinder.atoms.Select(a => new BoogieTypedIdent(a.name, BoogieType.Bool)));
        }

        private AtomFinder readSpec()
        {
            AtomFinder atomFinder = new AtomFinder();
            spec.fairness?.Accept(atomFinder);
            spec.property?.Accept(atomFinder);
            return atomFinder;
        }
        
        private static HashSet<Tuple<ContractDefinition, FunctionDefinition>> getVisibleFns(TranslatorContext ctxt) {
            HashSet<Tuple<ContractDefinition, FunctionDefinition>> visibleFns = new HashSet<Tuple<ContractDefinition, FunctionDefinition>>();
            foreach(ContractDefinition def in ctxt.ContractDefinitions)
            {
                foreach (FunctionDefinition fnDef in ctxt.GetVisibleFunctionsByContract(def))
                {
                    if (fnDef.Visibility == EnumVisibility.PUBLIC || fnDef.Visibility == EnumVisibility.EXTERNAL)
                    {
                        visibleFns.Add(new Tuple<ContractDefinition, FunctionDefinition>(def, fnDef));
                    }
                }
            }

            return visibleFns;
        }
    }
}