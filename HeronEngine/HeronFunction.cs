﻿/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Peg;

namespace HeronEngine
{
    /// <summary>
    /// Represents the formal argument to a function
    /// </summary>
    public class FormalArg : VarDesc
    {
        public FormalArg(string name, TypeRef type)
            : base(name, type)
        {
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.FormalArgType; }
        }
    }

    /// <summary>
    /// Represents all of the formals arguments to a function.
    /// </summary>
    public class FormalArgs : List<FormalArg>
    {
        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (FormalArg a in this)
                a.type.Resolve(global, m);
        }
    }

    /// <summary>
    /// Represents the definition of a Heron member function in the source code.
    /// Like MethodINfo in C#
    /// Not to be confused with a FunctionValue which represents a value of function type.
    /// </summary>
    public class FunctionDefn : HeronValue
    {
        [HeronVisible] public string name = "_anonymous_";
        [HeronVisible] public CodeBlock body = new CodeBlock();
        [HeronVisible] public FormalArgs formals = new FormalArgs();
        [HeronVisible] public HeronType parent = null;
        [HeronVisible] public TypeRef rettype = new TypeRef("Void", false);
        [HeronVisible] public ExpressionList annotations = new ExpressionList();

        public ParseNode node;

        public FunctionDefn(HeronType parent)
        {
            this.parent = parent;
        }

        public ModuleDefn GetModule()
        {
            if (parent != null)
                return parent.GetModule();
            else
                return null;
        }

        public string FileName
        {
            get
            {
                if (GetModule() == null)
                    return "no file";
                else
                    return GetModule().FileName;
            }
        }

        public FunctionDefn(HeronType parent, string name)
        {
            this.parent = parent;
            this.name = name;
        }

        /// <summary>
        /// A function can be invoked if the 'this' value (called self) is supplied.
        /// A FunctionObject is created and then called.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="env"></param>
        /// <param name="funcs"></param>
        /// <returns></returns>
        public HeronValue Invoke(HeronValue self, VM vm, HeronValue[] args)
        {
            // TODO: in theory we can optimize this
            FunctionValue fo = new FunctionValue(self, this);
            return fo.Apply(vm, args);
        }

        public bool Matches(FunctionDefn f)
        {
            if (f.name != name)
                return false;
            int n = formals.Count;
            if (f.formals.Count != n)
                return false;
            for (int i = 0; i < n; ++i)
            {
                FormalArg arg1 = formals[i];
                FormalArg arg2 = f.formals[i];
                if (arg1.type != arg2.type)
                    return false;
            }
            return true;
        }

        public bool Matches(MethodInfo mi)
        {
            if (mi.Name != name)
                return false;
            int n = formals.Count;
            if (mi.GetParameters().Length != n)
                return false;
            for (int i = 0; i < n; ++i)
            {
                FormalArg arg1 = formals[i];
                ParameterInfo arg2 = mi.GetParameters()[i];

                // TODO: find a better way to compare types
                if (arg1.type.ToString() != arg2.ParameterType.Name)
                    return false;
            }
            return true;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.FunctionDefnType; }
        }

        public HeronType GetContainingType()
        {
            return parent;
        }

        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            // ResolveType the return type
            rettype.Resolve(global, m);

            // ResolveType the argument types
            foreach (FormalArg arg in formals)
                arg.type.Resolve(global, m);

            // Resolve the types of body
            foreach (Statement st in body.GetStatementTree())
                st.ResolveTypes(global, m);
        }

        public string GetSignature()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append("(");
            for (int i = 0; i < formals.Count; ++i)
            {
                if (i > 0)
                    sb.Append(", ");
                FormalArg arg = formals[i];
                sb.Append(arg.name).Append(" : ").Append(arg.type.ToString());
            }
            sb.Append(")");
            if (rettype != null)
                sb.Append(" : ").Append(rettype.ToString());

            return sb.ToString();
        }

        public override string ToString()
        {
            return GetSignature();
        }

        [HeronVisible]
        public IEnumerable<Statement> GetStatements()
        {
            foreach (Statement st in body.GetSubStatements())
                yield return st;
        }
    }
}
