﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Peg;

namespace HeronEngine
{
    /// <summary>
    /// The HeronParser class creates typed representations of a Heron syntax tree.
    /// It is constructed from the concrete syntax tree generated by the PEG parser.
    /// Essentially this contains the objects that represent the classes, 
    /// functions, statements, and expressions that make up a Heron program. 
    /// One thing I do that is non-standard is convert expressions from a linear 
    /// form generated by the parser into their correct precedences. 
    /// My rationale is that parsers should not know about operator precedence.
    /// </summary>
    static public class HeronParser
    {
        static public HeronProgram CreateProgram(AstNode x)
        {
            HeronProgram p = new HeronProgram();
            foreach (AstNode node in x.GetChildren())
                p.modules.Add(CreateModule(node));
            return p;
        }

        static private void Assure(AstNode x, bool b, string s)
        {
            if (!b)
                throw new Exception("Expression error: " + s);
        }

        static private void Assure(bool b, string s)
        {
            if (!b)
                throw new Exception("Expression error: " + s);
        }

        #region construct parsing functions
        static public Module CreateModule(AstNode x)
        {
            Module r = new Module();
            Trace.Assert(x.GetNumChildren() > 1);
            Trace.Assert(x.GetChild(0).GetLabel() == "name");
            r.name = x.GetChild(0).ToString();
            for (int i=1; i < x.GetNumChildren(); ++i) {
                // Note : an interface is treated as a kind of class.
                r.classes.Add(CreateClass(x.GetChild(i)));
            }
            return r;
        }

        static public HeronClass CreateClass(AstNode x)
        {
            HeronClass r = new HeronClass();
            r.name = x.GetChild("name").ToString();

            AstNode methods = x.GetChild("methods");
            if (methods != null)
            {
                foreach (AstNode node in methods.GetChildren())
                {
                    Function f = CreateFunction(node);
                    f.hclass = r;
                    r.AddMethod(f);
                }
            }

            AstNode fields = x.GetChild("fields");
            if (fields != null)
                foreach (AstNode node in fields.GetChildren())
                    r.AddField(CreateField(node));

            return r;
        }

        static public Field CreateField(AstNode x)
        {
            Field r = new Field();
            r.name = x.GetChild("name").ToString();
            r.type = x.GetChild("type").ToString();
            return r;
        }

        static public FormalArg CreateFormalArg(AstNode x)
        {
            FormalArg r = new FormalArg();
            r.name = x.GetChild("name").ToString();
            AstNode type = x.GetChild("type");
            if (type != null)
                r.type = "void";
            return r;            
        }

        static public FormalArgs CreateFormalArgs(AstNode x)
        {
            FormalArgs r = new FormalArgs();
            foreach (AstNode node in x.GetChildren())
                r.Add(CreateFormalArg(node));
            return r;
        }

        static public Function CreateFunction(AstNode x)
        {
            Function r = new Function();
            AstNode fundecl = x.GetChild("fundecl");            
            r.name = fundecl.GetChild("name").ToString();
            r.formals = CreateFormalArgs(fundecl.GetChild("arglist"));

            AstNode rettype = fundecl.GetChild("type");
            if (rettype != null)
                r.rettype = rettype.ToString(); else
                r.rettype = "void";

            r.body = CreateCodeBlock(x.GetChild("codeblock"));
            return r;
        }
        #endregion 

        #region statement parsing functions.
        static public Statement CreateStatement(AstNode x)
        {
            switch (x.GetLabel())
            {
                case "codeblock":
                    return CreateCodeBlock(x);
                case "vardecl":
                    return CreateVarDecl(x);
                case "if":
                    return CreateIfStatement(x);
                case "switch":
                    throw new Exception("unimplemented");
                case "foreach":
                    return CreateForEachStatement(x);
                case "for":
                    return CreateForStatement(x);
                case "while":
                    return CreateWhileStatement(x);
                case "return":
                    return CreateReturnStatement(x);
                case "exprstatement":
                    return CreateExprStatement(x);
                case "delete":
                    throw new Exception("unimplemented");
                default:
                    throw new Exception("Unrecognized statement node " + x.GetLabel());
            }
        }

        static public CodeBlock CreateCodeBlock(AstNode x)
        {
            CodeBlock r = new CodeBlock();
            foreach (AstNode node in x.GetChildren())
                r.statements.Add(CreateStatement(node));
            return r;
        }

        static public VarDecl CreateVarDecl(AstNode x)
        {
            VarDecl r = new VarDecl();
            r.name = x.GetChild("name").ToString();
            AstNode type = x.GetChild("type");
            if (type != null)
                r.type = type.ToString();
            else
                r.type = "Object";
            AstNode tmp = x.GetChild("expr");
            if (tmp != null)
                r.init = CreateExpr(tmp);
            return r;
        }

        static public If CreateIfStatement(AstNode x)
        {
            If r = new If();
            r.cond = CreateExpr(x.GetChild(0).GetChild(0));
            r.ontrue = CreateStatement(x.GetChild(1));
            if (x.GetNumChildren() > 2)
                r.onfalse = CreateStatement(x.GetChild(2));
            return r;
        }

        static public Return CreateReturnStatement(AstNode x)
        {
            Return r = new Return();
            r.expr = CreateExpr(x.GetChild(0));
            return r;
        }

        static public While CreateWhileStatement(AstNode x)
        {
            While r = new While();
            r.cond = CreateExpr(x.GetChild(0).GetChild(0));
            r.body = CreateStatement(x.GetChild(1));
            return r;
        }

        static public ExprStatement CreateExprStatement(AstNode x)
        {
            ExprStatement r = new ExprStatement();
            r.expr = CreateExpr(x.GetChild(0));
            return r;
        }

        static public ForEach CreateForEachStatement(AstNode x)
        {
            ForEach r = new ForEach();
            r.name = x.GetChild(0).ToString();
            r.coll = CreateExpr(x.GetChild(1));
            r.body = CreateStatement(x.GetChild(2));
            return r;
        }

        static public For CreateForStatement(AstNode x)
        {
            For r = new For();
            r.name = x.GetChild(0).ToString();
            r.init = CreateExpr(x.GetChild(1));
            r.cond = CreateExpr(x.GetChild(2));
            r.next = CreateExpr(x.GetChild(3));
            r.body = CreateStatement(x.GetChild(4));
            return r;
        }
        #endregion

        #region utility functions
        static public char ToSpecialChar(char c)
        {
            switch (c)
            {
                case 't':
                    return '\t';
                case 'n':
                    return '\n';
                case '\\':
                    return '\\';
                case '\"':
                    return '\"';
                case '\'':
                    return '\'';
                default:
                    Assure(false, "illegal char escape sequence \\" + c);
                    // Unreachable code added to avoid compilation error
                    return '?';
            }
        }

        static public char CharFromLiteral(string s)
        {
            Assure(s.Length == 3 || s.Length == 4, "invalid char literal");
            Assure(s[0] == '\'', "expected single quotation marks around char");
            Assure(s[s.Length - 1] == '\'', "expected single quotation marks around char");
            if (s.Length == 3)
                return s[1];
            Assure(s[1] == '\\', "invalid char literal");
            return ToSpecialChar(s[2]);

        }

        static public string StringFromLiteral(string s)
        {
            Assure(s.Length >= 2, "invalid string");
            Assure(s[0] == '"', "Expected quotation marks around string");
            Assure(s[s.Length - 1] == '"', "Expected quotation marks around string");
            StringBuilder r = new StringBuilder();
            for (int i = 1; i < s.Length - 1; ++i)
            {
                if (s[i] == '\\')
                {
                    i++;
                    r.Append(ToSpecialChar(s[i]));
                    i++;
                }
                else
                {
                    r.Append(s[i]);
                }
            }
            return r.ToString();
        }
        #endregion

        /// The following functions create an expression tree from a list of 
        /// nodes representing expressions. When parsing, expressions are treated as a flat group
        /// These function use implicit precedence rules to construct the appropriate tree structure.
        /// Most of these functions will return an Expr object. They also take a single AstNode 
        /// representing the list of parsed expression nodes, and a current index into the list called 
        /// "i". 
        #region Expression creation functions
        
        static private bool ChildNodeMatches(AstNode x, ref int i, string s)
        {
            if (i >= x.GetNumChildren())
                return false;
            if (x.GetChild(i).ToString() == s)
            {
                i++;
                return true;
            }
            return false;
        }

        static New CreateNewExpr(AstNode x)
        {
            Assure(x, x.GetNumChildren() == 2, "new operator must be followed by type expression and arguments in paranthesis");
            AstNode type = x.GetChild(0);
            Assure(x, type.GetLabel() == "type", "new operator is missing type exprresion");
            AstNode args = x.GetChild(1);
            Assure(args, args.GetLabel() == "paranexpr", "new operator is missing argument list");
            return new New(type.ToString(), CreateArgList(args));
        }

        static Expr CreatePrimaryExpr(AstNode x, ref int i)
        {
            Assure(i < x.GetNumChildren(), "sub-expression index went out of bounds");
            AstNode child = x.GetChild(i);

            string sLabel = child.GetLabel();
            string sVal = child.ToString();
            Assure(x, sVal.Length > 0, "illegal zero-length child expression");
            switch (sLabel)
            {
                case "new":
                    i++;
                    return CreateNewExpr(child);
                case "name":
                    i++;
                    return new Name(child.ToString());
                case "int":
                    i++;
                    return new IntLiteral(int.Parse(sVal));
                case "char":
                    i++;
                    return new CharLiteral(CharFromLiteral(sVal));
                case "string":
                    i++;
                    return new StringLiteral(StringFromLiteral(sVal));
                case "float":
                    i++;
                    return new FloatLiteral(float.Parse(sVal));
                case "bin":
                    i++;
                    throw new Exception("binary literals not yet supported");
                case "hex":
                    i++;
                    throw new Exception("hexadecimal literals not yet supported");
                case "paranexpr":
                    Assure(child, child.GetNumChildren() == 1, "can only have one expression node in a paranthesized expression");
                    AstNode tmp = child.GetChild(0);
                    i++;
                    return CreateExpr(tmp);
                default:
                    Assure(child, false, "unrecognized primary expression: '" + sLabel + "'");
                    return null; // unreachable
            }
        }

        static ExprList CreateArgList(AstNode x)
        {
            Assure(x, x.GetLabel() == "paranexpr", "Can only create argument lists from paranthesized expression");
            Assure(x, x.GetNumChildren() <= 1, "Paranthesized expression must contain at most one compound expression");
            ExprList r = new ExprList();

            // If there are no arguments, return an empty expression list
            if (x.GetNumChildren() == 0)
                return r;

            AstNode child = x.GetChild(0);
            int i = 0;
            while (i < child.GetNumChildren())
            {
                Expr tmp = CreateExpr(child, ref i);
                r.Add(tmp);
            }
            return r;
        }

        static Expr CreatePostfixExpr(AstNode x, ref int i)
        {
            int old = i;
            Expr r = CreatePrimaryExpr(x, ref i);
            Assure(x, r != null, "unable to create primary expression");

            while (i < x.GetNumChildren())
            {
                old = i;
                AstNode tmp = x.GetChild(i);
                
                if (tmp.GetLabel() == "bracketedexpr")
                {
                    i++;
                    Assure(x, tmp.GetNumChildren() == 1, "brackets must contain only a single sub-expression");
                    r = new ReadAt(r, CreateExpr(tmp.GetChild(0)));
                }
                else if (tmp.GetLabel() == "paranexpr")
                {
                    i++;
                    r = new FunCall(r, CreateArgList(tmp));
                }
                else if (tmp.ToString() == ".")
                {
                    i++;
                    Assure(x, x.GetNumChildren() > i, "a '.' operator must be followed by a valid name expression");
                    tmp = x.GetChild(i);
                    Assure(x, tmp.GetLabel() == "name", "a '.' operator must be followed by a valid name expression");
                    string sName = tmp.ToString();
                    Assure(x, sName.Length > 0, "Name expression must have non-zero length");
                    i++;
                    r = new SelectField(r, sName);
                }
                else if (tmp.ToString() == "++")
                {                    
                    // You can't have anything to the left of ++
                    i++;
                    return new Assignment(r, new BinaryOperator("+", r, new IntLiteral(1)));
                }
                else
                {
                    return r;
                }
                Assure(i > 0, "internal error, failed to advance sub-expression index");
            }

            return r;
        }

        static Expr CreateUnaryExpr(AstNode x, ref int i)
        {
            if (ChildNodeMatches(x, ref i, "-"))
            {
                Expr tmp = CreatePostfixExpr(x, ref i);
                Expr r = new UnaryOperator("-", tmp);
                return r;
            }
            if (ChildNodeMatches(x, ref i, "!"))
            {
                Expr tmp = CreatePostfixExpr(x, ref i);
                Expr r = new UnaryOperator("!", tmp);
                return r;
            }
            else
            {
                Expr r = CreatePostfixExpr(x, ref i);
                return r;
            }
        }

        static Expr CreateMultExpr(AstNode x, ref int i)
        {
            Expr r = CreateUnaryExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, "*"))
            {
                r = new BinaryOperator("*", r, CreateUnaryExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "/"))
            {
                r = new BinaryOperator("/", r, CreateUnaryExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "%"))
            {
                r = new BinaryOperator("%", r, CreateUnaryExpr(x, ref i));
            }
            return r;
        }

        static Expr CreateAddExpr(AstNode x, ref int i)
        {
            Expr r = CreateMultExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, "+"))
            {
                r = new BinaryOperator("+", r, CreateMultExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "-"))
            {
                r = new BinaryOperator("-", r, CreateMultExpr(x, ref i));
            }
            return r;
        }

        static Expr CreateRelExpr(AstNode x, ref int i)
        {
            Expr r = CreateAddExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, ">"))
            {
                r = new BinaryOperator(">", r, CreateAddExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "<"))
            {
                r = new BinaryOperator("<", r, CreateAddExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, ">="))
            {
                r = new BinaryOperator(">=", r, CreateAddExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "<="))
            {
                r = new BinaryOperator("<=", r, CreateAddExpr(x, ref i));
            }
            return r;
        }

        static Expr CreateEqExpr(AstNode x, ref int i)
        {
            Expr r = CreateRelExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, "=="))
            {
                r = new BinaryOperator("==", r, CreateRelExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "!="))
            {
                r = new BinaryOperator("!=", r, CreateRelExpr(x, ref i));
            }
            return r;
        }

        static Expr CreateXOrExpr(AstNode x, ref int i)
        {
            Expr r = CreateEqExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "^^")) 
                r = new BinaryOperator("^^", r, CreateEqExpr(x, ref i));
            return r;
        }

        static Expr CreateAndExpr(AstNode x, ref int i)
        {
            Expr r = CreateXOrExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "&&"))
                r = new BinaryOperator("&&", r, CreateXOrExpr(x, ref i));
            return r;
        }

        static Expr CreateOrExpr(AstNode x, ref int i)
        {
            Expr r = CreateAndExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "||"))
                r = new BinaryOperator("||", r, CreateAndExpr(x, ref i));
            return r;
        }

        static Expr CreateCondExpr(AstNode x, ref int i)
        {
            Expr r = CreateOrExpr(x, ref i);
            // TODO: support the "a ? b : c" operator
            return r;
        }


        static Expr CreateAssignmentExpr(AstNode x, ref int i)
        {
            int old = i;
            Expr r = CreateCondExpr(x, ref i);
            Assure(x, r != null, "failed to create expression");
            Assure(x, i > old, "internal error, expression index not updated");
            if (i >= x.GetNumChildren())
                return r;
            string sOp = x.GetChild(i).ToString();
            switch (sOp)
            {
                case "=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, CreateCondExpr(x, ref i));
                case "+=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, new BinaryOperator("+", r, CreateCondExpr(x, ref i)));
                case "-=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, new BinaryOperator("-", r, CreateCondExpr(x, ref i)));
                case "*=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, new BinaryOperator("*", r, CreateCondExpr(x, ref i)));
                case "/=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, new BinaryOperator("/", r, CreateCondExpr(x, ref i)));
                case "%=":
                    if (++i >= x.GetNumChildren())
                        throw new Exception("illegal expression");
                    return new Assignment(r, new BinaryOperator("%", r, CreateCondExpr(x, ref i)));
                default:
                    // TODO: support other assignment operators.
                    return r;
            }
        }

        /// <summary>
        /// This function might be called by the top-level CreateExpr(AstNode x), 
        /// or by CreateArgList(AstNode x)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        static Expr CreateExpr(AstNode x, ref int i)
        {
            Assure(x, x.GetLabel() == "expr", "Expected 'expr' node not '" + x.GetLabel() + "'");
            Assure(x.GetNumChildren() > 0, "Cannot create an expression from a node with no children");

            int old = i;
            Expr r = CreateAssignmentExpr(x, ref i);
            Assure(x, r != null, "is not a valid expression");
            Assure(x, i > old, "internal error, current expression index was not updated");

            // Are there more expressions in the list?
            if (i < x.GetNumChildren())
            {
                // Make sure that they are separated by "," which is its own expression
                AstNode tmp = x.GetChild(i);
                Assure(x, tmp.ToString() == ",", "compound expressions must be separated by ','");
                
                // don't forget to advance the index, so the next node parsed is not the comma.
                i++;
            }
            return r;
        }

        /// <summary>
        /// This is the top-level expression creation function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static public Expr CreateExpr(AstNode x)
        {
            int i = 0;
            Expr r = CreateExpr(x, ref i);
            Assure(i == x.GetNumChildren(), "Could not parse entire expression");
            return r;
        }
        #endregion
    }
}
