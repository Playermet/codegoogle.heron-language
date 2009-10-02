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
        static HeronModule m;

        static public HeronProgram CreateProgram(AstNode x)
        {
            string name = GetNameNode(x);
            HeronProgram p = new HeronProgram(name);
            foreach (AstNode node in x.Children)
                p.AddModule(CreateModule(p, node));
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
        static public HeronModule CreateModule(HeronProgram p, AstNode x)
        {
            HeronModule r = new HeronModule(p);
            m = r; // Sets the current modul
            r.name = GetNameNode(x);
            for (int i=1; i < x.GetNumChildren(); ++i) {
                AstNode child = x.GetChild(i);
                switch (child.Label)
                {
                    case "class":
                        CreateClass(r, child);
                        break;
                    case "interface":
                        CreateInterface(r, child);
                        break;
                    case "enum":
                        CreateEnum(r, child);
                        break;
                    default:
                        throw new Exception("Unrecognize module sub-element " + child.Label);
                }
            }

            FinishModule(r, x);
            return r;
        }

        static public string GetNameNode(AstNode x)
        {
            AstNode name = x.GetChild("name");
            if (name == null)
                throw new Exception("Could not find name node");
            return name.ToString();
        }

        static public void FinishModule(HeronModule m, AstNode x)
        {
            Trace.Assert(m.name == GetNameNode(x), "The Module and AstNode are not the same");
            foreach (HeronInterface i in m.GetInterfaces())
                i.ResolveTypes();
            foreach (HeronClass c in m.GetClasses())
                c.ResolveTypes();
            foreach (HeronClass c in m.GetClasses())
                c.VerifyInterfaces();
        }

        static public HeronClass CreateClass(HeronModule m, AstNode x)
        {
            string name = x.GetChild("name").ToString();
            HeronClass r = new HeronClass(m, name);

            AstNode inherits = x.GetChild("inherits");
            if (inherits != null)
            {
                if (inherits.GetNumChildren() != 1)
                    throw new Exception("A class can only inherit from exactly one other class");
                AstNode type = inherits.GetChild(0);
                if (type.Label != "type")
                    throw new Exception("Can only inherit type expressions");
                string s = GetTypeName(type, null);
                if (s == null)
                    throw new Exception("Could not get the inherited type name");
                r.SetBaseClass(new UnresolvedType(s, m));
            }

            AstNode implements = x.GetChild("implements");
            if (implements != null)
            {
                foreach (AstNode node in implements.Children)
                {
                    if (node.Label != "type")
                        throw new Exception("Can only implement type expression");
                    string s = GetTypeName(node, null);
                    if (s == null)
                        throw new Exception("Could not get the inherited type name");
                    r.AddInterface(new UnresolvedType(s, m));
                }
            }

            AstNode methods = x.GetChild("methods");
            if (methods != null)
            {
                foreach (AstNode node in methods.Children)
                {
                    FunctionDefinition f = CreateFunction(node, r);
                    r.AddMethod(f);
                }
            }

            AstNode fields = x.GetChild("fields");
            if (fields != null)
                foreach (AstNode node in fields.Children)
                    r.AddField(CreateField(node));

            m.AddClass(r);
            return r;
        }

        static public HeronInterface CreateInterface(HeronModule m, AstNode x)
        {
            string name = x.GetChild("name").ToString();
            HeronInterface r = new HeronInterface(m, name);

            AstNode inherits = x.GetChild("inherits");
            if (inherits != null)
            {
                foreach (AstNode node in inherits.Children)
                {
                    string s = GetTypeName(node, null);
                    if (s == null)
                        throw new Exception("Could not get the type name");
                    r.AddBaseInterface(new UnresolvedType(s, m));
                }                    
            }

            AstNode methods = x.GetChild("methods");
            if (methods != null)
            {
                foreach (AstNode node in methods.Children)
                {
                    FunctionDefinition f = CreateFunction(node, r);
                    r.AddMethod(f);
                }
            }

            m.AddInterface(r);
            return r;
        }

        static public HeronEnum CreateEnum(HeronModule m, AstNode x)
        {
            string name = x.GetChild("name").ToString();
            HeronEnum r = new HeronEnum(m, name);

            AstNode values = x.GetChild("values");
            if (values != null)
            {
                foreach (AstNode node in values.Children)
                {
                    r.AddValue(node.ToString());
                }
            }

            m.AddEnum(r);
            return r;
        }

        static public string TypeToTypeName(AstNode x)
        {
            string r = x.GetChild("name").ToString();
            AstNode typeargs = x.GetChild("typeargs");
            if (typeargs == null) return r;
            r += "<";
            foreach (AstNode y in typeargs.Children)
                r += TypeToTypeName(y) + ";";
            return r + ">";
        }


        static public string GetTypeName(AstNode x, string def)
        {
            if (x.Label == "type")
                return TypeToTypeName(x);

            AstNode type = x.GetChild("type");
            if (type != null)
                return TypeToTypeName(type);

            return def;
        }

        static public HeronField CreateField(AstNode x)
        {
            HeronField r = new HeronField();
            r.name = x.GetChild("name").ToString();
            r.type = new UnresolvedType(GetTypeName(x, "Any"), m);
            return r;
        }

        static public FormalArg CreateFormalArg(AstNode x)
        {
            FormalArg r = new FormalArg();
            r.name = x.GetChild("name").ToString();
            r.type = new UnresolvedType(GetTypeName(x, "Any"), m);
            return r;            
        }

        static public HeronFormalArgs CreateFormalArgs(AstNode x)
        {
            HeronFormalArgs r = new HeronFormalArgs();
            foreach (AstNode node in x.Children)
                r.Add(CreateFormalArg(node));
            return r;
        }

        static public FunctionDefinition CreateFunction(AstNode x, HeronType parent)
        {
            HeronModule module = parent.GetModule();
            FunctionDefinition r = new FunctionDefinition(parent);
            AstNode fundecl = x.GetChild("fundecl");            
            r.name = fundecl.GetChild("name").ToString();
            r.formals = CreateFormalArgs(fundecl.GetChild("arglist"));
            r.rettype = new UnresolvedType(GetTypeName(x, "Void"), m);
            AstNode codeblock = x.GetChild("codeblock");
            r.body = CreateCodeBlock(codeblock);
            return r;
        }
        #endregion 

        #region statement parsing functions.
        static public Statement CreateStatement(AstNode x)
        {
            switch (x.Label)
            {
                case "codeblock":
                    return CreateCodeBlock(x);
                case "vardecl":
                    return CreateVarDecl(x);
                case "if":
                    return CreateIfStatement(x);
                case "switch":
                    return CreateSwitchStatement(x);
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
                    throw new Exception("Unrecognized statement node " + x.Label);
            }
        }

        static public CodeBlock CreateCodeBlock(AstNode x)
        {
            if (x == null)
                return new CodeBlock(null);
            CodeBlock r = new CodeBlock(x);
            foreach (AstNode node in x.Children)
                r.statements.Add(CreateStatement(node));
            return r;
        }

        static public VariableDeclaration CreateVarDecl(AstNode x)
        {
            VariableDeclaration r = new VariableDeclaration(x);
            r.name = x.GetChild("name").ToString();
            r.type = GetTypeName(x, "Object");
            AstNode tmp = x.GetChild("expr");
            if (tmp != null)
                r.value = CreateExpr(tmp);
            return r;
        }

        static public IfStatement CreateIfStatement(AstNode x)
        {
            IfStatement r = new IfStatement(x);
            r.condition = CreateExpr(x.GetChild(0).GetChild(0));
            r.ontrue = CreateStatement(x.GetChild(1));
            if (x.GetNumChildren() > 2)
                r.onfalse = CreateStatement(x.GetChild(2));
            return r;
        }

        static public CaseStatement CreateCaseStatement(AstNode x)
        {
            CaseStatement r = new CaseStatement(x);
            r.condition = CreateExpr(x.GetChild(0).GetChild(0));
            r.statement = CreateCodeBlock(x.GetChild(1));
            return r;
        }

        static public CodeBlock CreateDefaultStatement(AstNode x)
        {
            return CreateCodeBlock(x.GetChild(0));
        }

        static public SwitchStatement CreateSwitchStatement(AstNode x)
        {
            SwitchStatement r = new SwitchStatement(x);
            r.condition = CreateExpr(x.GetChild(0).GetChild(0));
            AstNode cases = x.GetChild(1);
            foreach (AstNode y in cases.Children)
            {
                CaseStatement cs = CreateCaseStatement(y);
                r.cases.Add(cs);
            }
            if (x.GetNumChildren() > 2)
            {
                r.ondefault = CreateDefaultStatement(x.GetChild(2));
            }
            return r;
        }

        static public ReturnStatement CreateReturnStatement(AstNode x)
        {
            ReturnStatement r = new ReturnStatement(x);
            r.expression = CreateExpr(x.GetChild(0));
            return r;
        }

        static public WhileStatement CreateWhileStatement(AstNode x)
        {
            WhileStatement r = new WhileStatement(x);
            r.condition = CreateExpr(x.GetChild(0).GetChild(0));
            r.body = CreateStatement(x.GetChild(1));
            return r;
        }

        static public ExpressionStatement CreateExprStatement(AstNode x)
        {
            ExpressionStatement r = new ExpressionStatement(x);
            r.expression = CreateExpr(x.GetChild(0));
            return r;
        }

        static public ForEachStatement CreateForEachStatement(AstNode x)
        {
            ForEachStatement r = new ForEachStatement(x);
            if (x.GetNumChildren() == 3)
            {
                r.name = x.GetChild(0).ToString();
                r.collection = CreateExpr(x.GetChild(1));
                r.body = CreateStatement(x.GetChild(2));
            }
            else if (x.GetNumChildren() == 4)
            {
                r.name = x.GetChild(0).ToString();
                r.type = x.GetChild(1).ToString();
                r.collection = CreateExpr(x.GetChild(2));
                r.body = CreateStatement(x.GetChild(3));
            }
            else
            {
                throw new Exception("Foreach statements should only have three or four nodes");
            }
            return r;
        }

        static public ForStatement CreateForStatement(AstNode x)
        {
            ForStatement r = new ForStatement(x);
            r.name = x.GetChild(0).ToString();
            r.initial = CreateExpr(x.GetChild(1));
            r.condition = CreateExpr(x.GetChild(2));
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
        #region Expression creatifunctions
        
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
            Assure(x, type.Label == "type", "new operator is missing type exprresion");
            AstNode args = x.GetChild(1);
            Assure(args, args.Label == "paranexpr", "new operator is missing argument list");
            return new New(type.ToString(), CreateArgList(args));
        }

        static Expression CreatePrimaryExpr(AstNode x, ref int i)
        {
            Assure(i < x.GetNumChildren(), "sub-expression index went out of bounds");
            AstNode child = x.GetChild(i);

            string sLabel = child.Label;
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

        static ExpressionList CreateArgList(AstNode x)
        {
            Assure(x, x.Label == "paranexpr", "Can only create argument lists from paranthesized expression");
            Assure(x, x.GetNumChildren() <= 1, "Paranthesized expression must contain at most one compound expression");
            ExpressionList r = new ExpressionList();

            // If there are no arguments, return an empty expression list
            if (x.GetNumChildren() == 0)
                return r;

            AstNode child = x.GetChild(0);
            int i = 0;
            while (i < child.GetNumChildren())
            {
                Expression tmp = CreateExpr(child, ref i);
                r.Add(tmp);
            }
            return r;
        }

        static Expression CreatePostfixExpr(AstNode x, ref int i)
        {
            int old = i;
            Expression r = CreatePrimaryExpr(x, ref i);
            Assure(x, r != null, "unable to create primary expression");

            while (i < x.GetNumChildren())
            {
                old = i;
                AstNode tmp = x.GetChild(i);
                
                if (tmp.Label == "bracketedexpr")
                {
                    i++;
                    Assure(x, tmp.GetNumChildren() == 1, "brackets must contain only a single sub-expression");
                    r = new ReadAt(r, CreateExpr(tmp.GetChild(0)));
                }
                else if (tmp.Label == "paranexpr")
                {
                    i++;
                    r = new FunCall(r, CreateArgList(tmp));
                }
                else if (tmp.ToString() == ".")
                {
                    i++;
                    Assure(x, x.GetNumChildren() > i, "a '.' operator must be followed by a valid name expression");
                    tmp = x.GetChild(i);
                    Assure(x, tmp.Label == "name", "a '.' operator must be followed by a valid name expression");
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

        static Expression CreateUnaryExpr(AstNode x, ref int i)
        {
            if (ChildNodeMatches(x, ref i, "-"))
            {
                Expression tmp = CreatePostfixExpr(x, ref i);
                Expression r = new UnaryOperator("-", tmp);
                return r;
            }
            if (ChildNodeMatches(x, ref i, "!"))
            {
                Expression tmp = CreatePostfixExpr(x, ref i);
                Expression r = new UnaryOperator("!", tmp);
                return r;
            }
            else
            {
                Expression r = CreatePostfixExpr(x, ref i);
                return r;
            }
        }

        static Expression CreateMultExpr(AstNode x, ref int i)
        {
            Expression r = CreateUnaryExpr(x, ref i);

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

        static Expression CreateAddExpr(AstNode x, ref int i)
        {
            Expression r = CreateMultExpr(x, ref i);

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

        static Expression CreateRelExpr(AstNode x, ref int i)
        {
            Expression r = CreateAddExpr(x, ref i);

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

        static Expression CreateTypeOpExpr(AstNode x, ref int i)
        {
            Expression r = CreateRelExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, "is"))
            {
                r = new BinaryOperator("is", r, CreateRelExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "as"))
            {
                r = new BinaryOperator("as", r, CreateRelExpr(x, ref i));
            }
            return r;

        }

        static Expression CreateEqExpr(AstNode x, ref int i)
        {
            Expression r = CreateTypeOpExpr(x, ref i);

            if (ChildNodeMatches(x, ref i, "=="))
            {
                r = new BinaryOperator("==", r, CreateTypeOpExpr(x, ref i));
            }
            else if (ChildNodeMatches(x, ref i, "!="))
            {
                r = new BinaryOperator("!=", r, CreateTypeOpExpr(x, ref i));
            }
            return r;
        }

        static Expression CreateXOrExpr(AstNode x, ref int i)
        {
            Expression r = CreateEqExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "^^")) 
                r = new BinaryOperator("^^", r, CreateEqExpr(x, ref i));
            return r;
        }

        static Expression CreateAndExpr(AstNode x, ref int i)
        {
            Expression r = CreateXOrExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "&&"))
                r = new BinaryOperator("&&", r, CreateXOrExpr(x, ref i));
            return r;
        }

        static Expression CreateOrExpr(AstNode x, ref int i)
        {
            Expression r = CreateAndExpr(x, ref i);
            if (ChildNodeMatches(x, ref i, "||"))
                r = new BinaryOperator("||", r, CreateAndExpr(x, ref i));
            return r;
        }

        static Expression CreateCondExpr(AstNode x, ref int i)
        {
            Expression r = CreateOrExpr(x, ref i);
            // TODO: support the ternary "a ? b : c" operator
            return r;
        }

        static Expression CreateAnonFunExpr(AstNode x, ref int i)
        {
            if (i >= x.GetNumChildren())
                throw new Exception("Internal parse error");
            AstNode child = x.GetChild(i);
            
            if (child.Label == "anonfxn")
            {
                ++i;
                AnonFunExpr r = new AnonFunExpr();
                r.formals = CreateFormalArgs(child.GetChild("arglist"));
                r.rettype = new UnresolvedType(GetTypeName(child, "Void"), m);
                r.body = CreateCodeBlock(child.GetChild("codeblock"));
                return r;
            }
            else
            {
                return CreateCondExpr(x, ref i);
            }
        }

        static Expression CreateAssignmentExpr(AstNode x, ref int i)
        {
            int old = i;
            Expression r = CreateAnonFunExpr(x, ref i);
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
        static Expression CreateExpr(AstNode x, ref int i)
        {
            Assure(x, x.Label == "expr", "Expected 'expr' node not '" + x.Label + "'");
            Assure(x.GetNumChildren() > 0, "Cannot create an expression from a node with no children");

            int old = i;
            Expression r = CreateAssignmentExpr(x, ref i);
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
        static public Expression CreateExpr(AstNode x)
        {
            int i = 0;
            Expression r = CreateExpr(x, ref i);
            Assure(i == x.GetNumChildren(), "Could not parse entire expression");
            return r;
        }
        #endregion

        #region static public functions    
        static public Expression ParseExpr(string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Expr(), s);
            if (node == null)
                return null;
            Expression r = HeronParser.CreateExpr(node);
            return r;
        }

        static public Statement ParseStatement(string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Statement(), s);
            if (node == null)
                return null;
            Statement r = HeronParser.CreateStatement(node);
            return r;
        }

        static public HeronModule ParseModule(HeronProgram p, string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Module(), s);
            if (node == null)
                return null;
            HeronModule r = HeronParser.CreateModule(p, node);
            return r;
        }

        static public HeronClass ParseClass(HeronModule m, string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Class(), s);
            if (node == null)
                return null;
            HeronClass r = HeronParser.CreateClass(m, node);
            return r;
        }

        static public HeronInterface ParseInterface(HeronModule m, string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Interface(), s);
            if (node == null)
                return null;
            HeronInterface r = HeronParser.CreateInterface(m, node);
            return r;
        }
        #endregion
    }
}
