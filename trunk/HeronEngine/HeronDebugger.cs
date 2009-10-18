﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public static class HeronDebugger
    {
        static bool debugging = false;

        public static void PrintCallStack(HeronExecutor vm)
        {
            Environment env = vm.GetEnvironment();
            foreach (Frame f in env.GetFrames())
            {
                Console.WriteLine(f.SimpleDescription);
            }
        }

        public static void PrintCurrentFrame(HeronExecutor vm)
        {
            Frame f = vm.GetCurrentFrame();
            PrintFrame(f);
        }

        public static void PrintAllFrames(HeronExecutor vm)
        {
            foreach (Frame f in vm.GetEnvironment().GetFrames())
                PrintFrame(f);
        }

        public static void PrintFrame(Frame f)
        {
            Console.WriteLine(f.SimpleDescription);
            foreach (NameValueTable scope in f.GetScopes())
            {
                Console.WriteLine("scope : ");
                foreach (string s in scope.Keys)
                {
                    Console.WriteLine("  " + s + " = " + scope[s].ToString());
                }
            }
        }

        public static void Evaluate(HeronExecutor vm, string sArg)
        {
            if (sArg.Length == 0)
            {
                Console.WriteLine("No expression to evaluate");
                return;
            }
            try
            {
                Console.WriteLine("Evaluating expression : " + sArg);
                HeronValue val = vm.EvalString(sArg);
                Console.WriteLine("Result : " + val.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured : " + e.Message);
            }
        }

        public static void Start(HeronExecutor vm)
        {
            // Make sure that an exception triggered when evaluating from within debugger 
            // doesn't cause multiple instances of the debugger.
            if (debugging)
                return;

            try
            {
                debugging = true;
                Console.WriteLine("You are in the debugger. Type '?' for instructions and 'x' to exit.");
                while (true)
                {
                    string sInput = Console.ReadLine().Trim();
                    string sCommand = sInput;
                    string sArg = "";
                    int nSplit = sInput.IndexOf(' ');
                    if (nSplit >= 0)
                    {
                        sCommand = sInput.Substring(0, nSplit);
                        sArg = sInput.Substring(nSplit + 1).Trim();
                    }
                    switch (sCommand)
                    {
                        case "x":
                            debugging = false;
                            return;
                        case "s":
                            PrintCallStack(vm);
                            break;
                        case "f":
                            PrintCurrentFrame(vm);
                            break;
                        case "af":
                            PrintAllFrames(vm);
                            break;
                        case "e":
                            Evaluate(vm, sArg);
                            break;
                        default:
                            Console.WriteLine("? - prints instructions");
                            Console.WriteLine("x - leaves debugger");
                            Console.WriteLine("s - prints call stack");
                            Console.WriteLine("f - print contents of current frame");
                            Console.WriteLine("af - prints contents of all frames");
                            Console.WriteLine("e <expr> - evaluates an expression");
                            break;
                    }
                }
            }
            finally
            {
                debugging = false;
            }
        }
    }
}