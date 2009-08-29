/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Util;

namespace Peg
{
    /// <summary>
    /// Used to identify where an error occured in an input string. 
    /// Such as the line number and character number.
    /// </summary>
    public class ParseLocation
    {
        public ParseLocation(string input, int begin, int end)
        {
            this.input = input;
            this.begin = begin;
            this.end = end;
        }

        public string input;
        public int begin;
        public int end;
    }

    public class ParseExceptionContext
    {
        public ParseLocation location;
        public string msg;
        public int row;
        public int col;
        public string line;
        public string ptr;
        public Grammar.Rule rule;

        public ParseExceptionContext(string s, int begin, int end, Grammar.Rule r, string msg)
        {
            location = new ParseLocation(s, begin, end);
            this.msg = msg;
            s.GetRowCol(begin, out row, out col);
            line = s.GetLine(row);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < line.Length - 1; ++i)
                if (line[i] == '\t')
                    sb.Append('\t');
                else
                    sb.Append(' ');
            sb.Append('^');
            this.ptr = sb.ToString();
        }
    }
    
    public class ParsingException : Exception
    {
        public ParseExceptionContext context;

        public ParsingException(string s, int begin, int end, Grammar.Rule r, string msg)
        {          
            AddContext(s, begin, end, r, msg);
        }

        public void AddContext(string s, int begin, int end, Grammar.Rule r, string msg)
        {
            context = new ParseExceptionContext(s, begin, end, r, msg);
        }
    }
    
    /// <summary>
    /// Store everything related to the state of the parser, including the input string,
    /// current index, concrete syntax tree, and current tree node being build.
    /// </summary>
    public class ParserState
    {
        int mIndex;
        int mExtent;
        string mInput;
        AstNode mTree;
        AstNode mCur;

        public ParserState(string s)
        {
            mIndex = 0;
            mExtent = 0;
            mInput = s;
            mTree = new AstNode("ast", 0, mInput, null);
            mCur = mTree;
        }

        public int GetInputLength()
        {
            return mInput.Length;
        }

        public string GetInput()
        {
            return mInput;
        }

        public bool AtEnd()
        {
            return mIndex >= mInput.Length;
        }

        public int GetIndex()
        {
            return mIndex; 
        }

        public int GetExtent()
        {
            return mExtent;
        }

        public string CurrentLine
        {
            get
            {
                return mInput.Substring(mIndex, 20);
            }
        }

        public void SetPos(int pos)
        {
            mIndex = pos;
            if (mIndex > mExtent) mExtent = mIndex;
        }

        public void IncIndex()
        {
            if (++mIndex > mExtent)
                mExtent = mIndex;
        }

        public void GotoNext()
        {
            if (AtEnd())
            {
                throw new Exception("passed the end of input");
            }
            IncIndex();
        }

        public char GetChar()
        {
            if (AtEnd()) 
            { 
                throw new Exception("passed end of input"); 
            }
            return mInput[mIndex];
        }

        public AstNode GetCurrentNode()
        {
            return mCur;
        }

        public AstNode CreateNode(string sLabel)
        {
            Trace.Assert(mCur != null);
            mCur = mCur.Add(sLabel, this);
            Trace.Assert(mCur != null);
            return mCur;
        }

        public void AbandonNode()
        {
            Trace.Assert(mCur != null);
            AstNode tmp = mCur;
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
            mCur.Remove(tmp);
        }

        public void CompleteNode()
        {
            Trace.Assert(mCur != null);
            mCur.Complete(this);
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
        }

        public AstNode GetAst()
        {
            return mTree;
        }

        public AstNode Parse(Peg.Grammar.Rule g)
        {
            if (!g.Match(this))
                return null;
                            
            if (mCur != mTree)
                throw new Exception("internal error: parse tree and parse node do not match after parsing");
            
            mCur.Complete(this);
            return mTree;
        }
        
        public static AstNode Parse(Peg.Grammar.Rule g, string s)
        {
            ParserState p = new ParserState(s);
            AstNode node = p.Parse(g);
            if (node.GetLabel() != "ast")
                throw new Exception("no root AST node");
            if (node.GetNumChildren() != 1)
                throw new Exception("more than one child node parsed");
            return node.GetChild(0);
        }
    }
}
