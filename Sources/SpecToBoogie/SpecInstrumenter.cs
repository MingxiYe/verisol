using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using SolToBoogie;

namespace SpecToBoogie
{
    public class SpecInstrumenter : ProgramInstrumenter
    {
        private string specFile;
        public SpecInstrumenter(string specFile)
        {
            this.specFile = specFile;
        }
        public void instrument(TranslatorContext ctxt)
        {
            string[] lines = System.IO.File.ReadAllLines(specFile);
            Regex varRegex = new Regex(@"^\s*//\s*#LTLVariables:");
            Regex fairRegex = new Regex(@"^\s*//\s*#LTLFairness:");
            Regex propRegex = new Regex(@"^\s*//\s*#LTLProperty:");

            string freeVars = null;
            string fairness = null;
            string property = null;
            
            foreach(string line in lines)
            {
                char[] whitespace = { ' ', '\t'};
                line.Trim(whitespace);

                if (varRegex.IsMatch(line))
                {
                    freeVars = line.Substring(line.IndexOf(':') + 1);
                }
                else if (fairRegex.IsMatch(line))
                {
                    fairness = line.Substring(line.IndexOf(':') + 1);
                }
                else if (propRegex.IsMatch(line))
                {
                    property = line.Substring(line.IndexOf(':') + 1);
                }
            }

            if (property == null && fairness == null && property == null)
            {
                throw new Exception($"Could not find specification in {specFile}");
            }
            
            SmartLTLReader reader = new SmartLTLReader(ctxt);
            if (freeVars != null)
            {
                VarDeclList varList = reader.ReadVars(freeVars);
                reader.SetFreeVars(varList);
                Console.WriteLine($"// #LTLVariables: {varList}");
            }

            VarSearch varSearch = new VarSearch();
            if (fairness != null)
            {
                SmartLTLNode head = reader.ReadProperty(fairness);
                head.Accept(varSearch);
                Console.WriteLine($"// #LTLFairness: {head}");
            }

            if (property != null)
            {
                SmartLTLNode head = reader.ReadProperty(property);
                head.Accept(varSearch);
                Console.WriteLine($"// #LTLProperty: {head}");
            }

            int varID = 0;
            List<String> varNames = new List<String>();
            Dictionary<String, List<String>> startedVars = new Dictionary<string, List<string>>();
            Dictionary<String, List<String>> willSucceedVars = new Dictionary<string, List<string>>();
            Dictionary<String, List<String>> finishedVars = new Dictionary<string, List<string>>();
            Dictionary<String, List<String>> revertedVars = new Dictionary<string, List<string>>();
            foreach (Atom atom in varSearch.atomList)
            {
                Console.WriteLine(atom);
                String function = atom.tgtFn.def.Name;
                String ident = atom.tgtFn.ident.contract.Name;
                
                AtomLoc loc = atom.loc;
                String functionName = function + "_" + ident;
                String varName = loc.ToString().ToLower() + "_" + function + varID;
                varID++;
                    
                varNames.Add(varName);
                if (loc.Equals(AtomLoc.STARTED))
                {
                    if (!startedVars.ContainsKey(functionName)) startedVars.Add(functionName, new List<string>());
                    startedVars[functionName].Add(varName);
                }
                else if (loc.Equals(AtomLoc.WILL_SUCCEED))
                {
                    if (!willSucceedVars.ContainsKey(functionName)) willSucceedVars.Add(functionName, new List<string>());
                    willSucceedVars[functionName].Add(varName);
                }
                else if (loc.Equals(AtomLoc.FINISHED))
                {
                    if (!finishedVars.ContainsKey(functionName)) finishedVars.Add(functionName, new List<string>());
                    finishedVars[functionName].Add(varName);
                }
                else if (loc.Equals(AtomLoc.REVERTED))
                {
                    if (!revertedVars.ContainsKey(functionName)) revertedVars.Add(functionName, new List<string>());
                    revertedVars[functionName].Add(varName);
                }
            }

            // Variable declaration
            for (int i = 0; i < ctxt.Program.Declarations.Count; i++)
            {
                BoogieDeclaration declaration = ctxt.Program.Declarations[i];
                if (declaration.GetType().Equals(typeof(BoogieGlobalVariable)))
                {
                    foreach (String varName in varNames)
                    {
                        ctxt.Program.Declarations.Insert(i, new BoogieGlobalVariable(new BoogieTypedIdent(varName, BoogieType.Bool)));
                    }
                    break;
                }
            }
            
            // started
            foreach (String functionName in startedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                ctxt.Program.Accept(fs);
                foreach (String varName in startedVars[functionName])
                {
                    fs.desired.StructuredStmts.PrependStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(false)));
                }
                foreach (String varName in startedVars[functionName])
                {
                    fs.desired.StructuredStmts.PrependStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(true)));
                }
            }
            // reverted
            foreach (String functionName in revertedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                BoogieIfCmd targetIf = null;
                ctxt.Program.Accept(fs);
                foreach (BoogieCmd boogieCmd in fs.desired.StructuredStmts.BigBlocks[0].SimpleCmds)
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
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
                foreach (String varName in revertedVars[functionName])
                {
                    targetIf.ThenBody.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(true)));
                }
                foreach (String varName in revertedVars[functionName])
                {
                    targetIf.ThenBody.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(false)));
                }
            }
            // will succeed
            foreach (String functionName in willSucceedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                BoogieIfCmd targetIf = null;
                ctxt.Program.Accept(fs);
                foreach (BoogieCmd boogieCmd in fs.desired.StructuredStmts.BigBlocks[0].SimpleCmds)
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
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
                
                foreach (String varName in willSucceedVars[functionName])
                {
                    targetIf.ElseBody.PrependStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(false)));
                }
                foreach (String varName in willSucceedVars[functionName])
                {
                    targetIf.ElseBody.PrependStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(true)));
                }
            }
            // finished
            foreach (String functionName in finishedVars.Keys)
            {
                FunctionSearch fs = new FunctionSearch(functionName);
                BoogieIfCmd targetIf = null;
                ctxt.Program.Accept(fs);
                foreach (BoogieCmd boogieCmd in fs.desired.StructuredStmts.BigBlocks[0].SimpleCmds)
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
                if (targetIf == null)
                {
                    Console.Error.WriteLine("If not found for " + functionName);
                }
                
                foreach (String varName in finishedVars[functionName])
                {
                    targetIf.ElseBody.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(true)));
                }
                foreach (String varName in finishedVars[functionName])
                {
                    targetIf.ElseBody.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(varName), new BoogieLiteralExpr(false)));
                }
            }
            /*string test = "[](!finished(*, Wallet.balanceOf(null) != 0 || Wallet.balanceOf(this) != 0))";
            SmartLTLNode head = reader.ReadProperty(test);
            Console.WriteLine(head.ToString());*/
        }
    }
}