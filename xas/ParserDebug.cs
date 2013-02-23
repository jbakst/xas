using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Antlr.Runtime.Debug;

namespace xas
{
    class ParserDebug : IDebugEventListener
    {

        public void AddChild(object root, object child)
        {

        }

        public void BecomeRoot(object newRoot, object oldRoot)
        {

        }

        public void BeginBacktrack(int level)
        {

        }

        public void BeginResync()
        {

        }

        public void Commence()
        {

        }

        public void ConsumeHiddenToken(Antlr.Runtime.IToken t)
        {

        }

        public void ConsumeNode(object t)
        {

        }

        public void ConsumeToken(Antlr.Runtime.IToken t)
        {

        }

        public void CreateNode(object node, Antlr.Runtime.IToken token)
        {

        }

        public void CreateNode(object t)
        {

        }

        public void EndBacktrack(int level, bool successful)
        {

        }

        public void EndResync()
        {
            
        }

        public void EnterAlt(int alt)
        {
            
        }

        public void EnterDecision(int decisionNumber, bool couldBacktrack)
        {
            
        }

        public void EnterRule(string grammarFileName, string ruleName)
        {
            Console.WriteLine("EnterRule {0} {1}", grammarFileName, ruleName);
        }

        public void EnterSubRule(int decisionNumber)
        {
            
        }

        public void ErrorNode(object t)
        {
            
        }

        public void ExitDecision(int decisionNumber)
        {
            
        }

        public void ExitRule(string grammarFileName, string ruleName)
        {
            Console.WriteLine("ExitRule {0} {1}", grammarFileName, ruleName);
        }

        public void ExitSubRule(int decisionNumber)
        {

        }

        public void Initialize()
        {

        }

        public void LT(int i, object t)
        {

        }

        public void LT(int i, Antlr.Runtime.IToken t)
        {

        }

        public void Location(int line, int pos)
        {
            Console.WriteLine("Location {0} {1}", line, pos);
        }

        public void Mark(int marker)
        {

        }

        public void NilNode(object t)
        {

        }

        public void RecognitionException(Antlr.Runtime.RecognitionException e)
        {
            Console.WriteLine("RecognitionException {0} {1} {2}", e.Line, e.Message, e.Index);
        }

        public void Rewind()
        {

        }

        public void Rewind(int marker)
        {

        }

        public void SemanticPredicate(bool result, string predicate)
        {

        }

        public void SetTokenBoundaries(object t, int tokenStartIndex, int tokenStopIndex)
        {

        }

        public void Terminate()
        {

        }
    }
}
