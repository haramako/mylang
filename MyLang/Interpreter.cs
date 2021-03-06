﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MyLang.Ast;

namespace MyLang
{
    public class Env
    {
        Dictionary<string, float> variables_ = new Dictionary<string, float>();
        Dictionary<string, FunctionStatement> functions_ = new Dictionary<string, FunctionStatement>();

        float returnValue_;

        Env parentEnv_;

        public Env(Env parentEnv)
        {
            parentEnv_ = parentEnv;
        }

        public float Get(string name)
        {
            if (variables_.TryGetValue(name, out var found))
            {
                return found;
            }
            else
            {
                return parentEnv_.Get(name);
            }
        }

        public void Set(string name, float val)
        {
            variables_[name] = val;
        }

        public Ast.FunctionStatement GetFunction(string name)
        {
            if (functions_.TryGetValue(name, out var found))
            {
                return found;
            }
            else
            {
                return parentEnv_.GetFunction(name);
            }
        }

        public void SetFunction(string name, Ast.FunctionStatement func)
        {
            functions_[name] = func;
        }

        public void SetReturnValue(float val)
        {
            returnValue_ = val;
        }

        public float GetReturnValue()
        {
            return returnValue_;
        }
    }

    public class Interpreter
    {
        public class InterpreterJump : Exception { }
        public class BreakJump : InterpreterJump { }
        public class ReturnJump : InterpreterJump { }

        Env env_ = new Env(null);

        public Interpreter()
        {
        }

        public void Run(Ast.Ast ast)
        {
            if( ast is Exp)
            {
                var result = runExp((Ast.Exp)ast);
                Console.WriteLine(result);
            }
            else
            {
                runBlock(((Ast.Program)ast).Statements);
            }
        }

        public void runBlock(Ast.Statement[] statements)
        {
            foreach( var stat in statements)
            {
                if (stat is PrintStatement) {
                    var s = (PrintStatement)stat;
                    float value = runExp(s.Exp);
                    if (s.Format != null)
                    {
                        Console.WriteLine(s.Format, value);
                    }
                    else
                    {
                        Console.WriteLine(value);
                    }
                }
                else if( stat is AssignStatement)
                {
                    var s = (AssignStatement)stat;
                    float value = runExp(s.Exp);
                    env_.Set(((Ast.Symbol)s.Variable).Name, value);
                }
                else if (stat is FunctionStatement)
                {
                    var s = (FunctionStatement)stat;
                    env_.SetFunction(s.Name.Name, s);
                }
                else if (stat is ReturnStatement)
                {
                    var s = (ReturnStatement)stat;
                    float value = runExp(s.Exp);
                    env_.SetReturnValue(value);
                    throw new ReturnJump();
                }
                else if (stat is IfStatement)
                {
                    var s = (IfStatement)stat;
                    float value = runExp(s.Exp);
                    if( value != 0)
                    {
                        runBlock(s.ThenBody);
                    }
                    else
                    {
                        if (s.ElseBody != null)
                        {
                            runBlock(s.ElseBody);
                        }
                    }
                }
                else if (stat is LoopStatement)
                {
                    var s = (LoopStatement)stat;
                    while (true)
                    {
                        try
                        {
                            runBlock(s.Body);
                        }
                        catch (BreakJump)
                        {
                            // break によって終了する
                            break;
                        }
                    }
                }
                else if (stat is BreakStatement)
                {
                    var s = (BreakStatement)stat;
                    throw new BreakJump();
                }
                else
                {
                    throw new Exception("BUG");
                }
            }
        }

        float runExp(Exp exp)
        {
            if( exp is BinOp)
            {
                return runBinOp(exp as BinOp);
            }
            else if( exp is Number)
            {
                var number = exp as Number;
                return number.Value;
            }
            else if (exp is Symbol)
            {
                var symbol = exp as Symbol;
                return env_.Get(symbol.Name);
            }
            else if (exp is ApplyFunction)
            {
                return runApplyFunction(exp as ApplyFunction);
            }
            else
            {
                throw new Exception("BUG");
            }
        }

        float runBinOp(BinOp binop)
        {
            float lhsValue = runExp(binop.Lhs);
            float rhsValue = runExp(binop.Rhs);
            switch (binop.Operator)
            {
                case BinOpType.Add:
                    return lhsValue + rhsValue;
                case BinOpType.Sub:
                    return lhsValue - rhsValue;
                case BinOpType.Multiply:
                    return lhsValue * rhsValue;
                case BinOpType.Divide:
                    return lhsValue / rhsValue;
                case BinOpType.Equal:
                    return (lhsValue == rhsValue) ? 1 : 0;
                case BinOpType.NotEqual:
                    return (lhsValue != rhsValue) ? 1 : 0;
                case BinOpType.Less:
                    return (lhsValue < rhsValue) ? 1 : 0;
                case BinOpType.LessEqual:
                    return (lhsValue <= rhsValue) ? 1 : 0;
                case BinOpType.Greater:
                    return (lhsValue > rhsValue) ? 1 : 0;
                case BinOpType.GreaterEqual:
                    return (lhsValue >= rhsValue) ? 1 : 0;
                default:
                    throw new Exception($"Unkonwn operator {binop.Operator}");
            }
        }

        float runApplyFunction(ApplyFunction e)
        {
            var func = env_.GetFunction(e.Name.Name);
            var args = e.Args.Select(arg => runExp(arg)).ToArray();

            // closureの環境を作成する
            var oldEnv = env_;
            env_ = new Env(oldEnv);

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                env_.Set("@" + (i + 1), arg);
            }

            try
            {
                runBlock(func.Body);
            }
            catch (ReturnJump)
            {
                // return によって終了する
            }

            var returnValue = env_.GetReturnValue();

            env_ = oldEnv;

            return returnValue;
        }
    }
}
