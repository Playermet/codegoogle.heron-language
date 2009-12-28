﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// An instance of an interface type.
    /// </summary>
    public class InterfaceDefn : HeronUserType
    {
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<HeronType> basetypes = new List<HeronType>();

        public InterfaceDefn(ModuleDefn m, string name)
            : base(m, typeof(InterfaceDefn), name)
        {
        }
        public void ResolveTypes()
        {
            for (int i = 0; i < basetypes.Count; ++i)
            {
                HeronType t = basetypes[i];
                if (t is UnresolvedType)
                    basetypes[i] = (t as UnresolvedType).Resolve();
            }

            foreach (FunctionDefn f in GetAllMethods())
                f.ResolveTypes();
        }
        public void AddBaseInterface(HeronType t)
        {
            basetypes.Add(t);
        }
        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            throw new Exception("Cannot instantiate an interface");
        }

        [HeronVisible]
        public override IEnumerable<FunctionDefn> GetAllMethods()
        {
            foreach (FunctionDefn f in GetDeclaredMethods())
                yield return f;
            foreach (FunctionDefn f in GetInheritedMethods())
                yield return f;
        }

        [HeronVisible]
        public IEnumerable<InterfaceDefn> GetInheritedInterfaces()
        {
            foreach (InterfaceDefn i in basetypes)
                yield return i;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetDeclaredMethods()
        {
            return methods;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetInheritedMethods()
        {
            foreach (InterfaceDefn i in GetInheritedInterfaces())
                foreach (FunctionDefn f in i.GetAllMethods())
                    yield return f;
        }

        // TODO: see if I can remove all "HasMethod" calls.
        public bool HasMethod(string name)
        {
            foreach (FunctionDefn f in GetMethods(name))
                return true;
            return false;
        }
        public void AddMethod(FunctionDefn x)
        {
            methods.Add(x);
        }

        public bool InheritsFrom(InterfaceDefn i)
        {
            string s = i.name;
            if (s == name)
                return true;
            foreach (InterfaceDefn bi in basetypes)
                if (bi.InheritsFrom(i))
                    return true;
            return false;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.InterfaceType;
        }
    }
}
