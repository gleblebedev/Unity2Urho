using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphExpressionVisitor
    {
        static readonly private Dictionary<MemberInfo, Func<GraphOutPin, GraphOutPin>> unaryOperators_ = new Dictionary<MemberInfo, Func<GraphOutPin, GraphOutPin>>();
        static GraphExpressionVisitor()
        {
            RegisterMember(() => Vector3.one.magnitude, "Length");
        }

        private static void RegisterMember<T>(Expression<Func<T>> func, string nodeName)
        {
            if (func.Body is MemberExpression memberExpression)
            {
                var member = memberExpression.Member;
                if (member is FieldInfo fieldInfo)
                {
                    unaryOperators_[member] = _ =>
                    {
                        var node = new GraphNode(nodeName);
                        node.In.Add(new GraphInPin("x", _));
                        node.Out.Add(new GraphOutPin("out", fieldInfo.FieldType.ToVariantType()));
                        return node.Out.FirstOrDefault();
                    };

                }
                else if (member is PropertyInfo propertyInfo)
                {
                    unaryOperators_[member] = _ =>
                    {
                        var node = new GraphNode(nodeName);
                        node.In.Add(new GraphInPin("x", _));
                        node.Out.Add(new GraphOutPin("out", propertyInfo.PropertyType.ToVariantType()));
                        return node.Out.FirstOrDefault();
                    };
                }
            }
        }

        private readonly ParticleGraphBuilder _graph;
        private readonly GraphOutPin[] _args;
        private readonly Dictionary<Expression, GraphOutPin> _visitedNodes = new Dictionary<Expression, GraphOutPin>();

        public GraphExpressionVisitor(ParticleGraphBuilder graph, params GraphOutPin[] args)
        {
            _graph = graph;
            _args = args;
        }

        public GraphOutPin Visit(Expression node)
        {
            if (_visitedNodes.TryGetValue(node, out var res))
                return res;
            res = VisitImpl(node);
            _graph.Add(res.Node);
            _visitedNodes[node] = res;
            return res;
        }

        protected GraphOutPin VisitImpl(Expression node)
        {
            if (ExpressionAsConstant(node, out object obj))
            {
                return VisitConstant(obj);
            }

            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                    return this.VisitUnary((UnaryExpression)node);
                case ExpressionType.AddChecked:
                case ExpressionType.SubtractChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Power:
                    return this.VisitBinary((BinaryExpression)node);
                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)node);
                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)node);
                case ExpressionType.Add:
                    return VisitAdd((BinaryExpression)node);
                case ExpressionType.Multiply:
                    return VisitMultiply((BinaryExpression)node);
                case ExpressionType.Divide:
                    return VisitDivide((BinaryExpression)node);
                case ExpressionType.Subtract:
                    return VisitSubtract((BinaryExpression)node);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)node);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)node);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)node);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)node);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)node);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)node);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)node);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)node);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)node);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)node);
            }

            throw new NotImplementedException(node.NodeType.ToString());
        }

        private GraphOutPin VisitConditional(ConditionalExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitParameter(ParameterExpression node)
        {

            throw new NotImplementedException();
        }

        private GraphOutPin VisitMemberAccess(MemberExpression node)
        {
            if (unaryOperators_.TryGetValue(node.Member, out var func))
            {
                return func(Visit(node.Expression));
            }
            throw new NotImplementedException();
        }

        private bool ExpressionAsConstant(Expression nodeExpression, out object res)
        {
            switch (nodeExpression.NodeType)
            {
                case ExpressionType.Multiply:
                {
                    var op = (BinaryExpression)nodeExpression;
                    if (ExpressionAsConstant(op.Left, out var left) && ExpressionAsConstant(op.Right, out var right))
                    {
                        if (left is float l && right is float r)
                        {
                            res = l * r;
                            return true;
                        }
                    }
                    break;
                }
                case ExpressionType.MemberAccess:
                {
                    var parentMember = (MemberExpression)nodeExpression;
                    if (ExpressionAsConstant(parentMember.Expression, out var parent))
                    {
                        if (parentMember.Member is FieldInfo field)
                        {
                            res = field.GetValue(parent);
                            return true;
                        }

                        if (parentMember.Member is PropertyInfo property)
                        {
                            res = property.GetValue(parent);
                            return true;
                        }

                    }
                    res = null;
                    return false;
                }
                case ExpressionType.Constant:
                    res = ((ConstantExpression)nodeExpression).Value;
                    return true;
                default:
                    break;
            }
            res = null;
            return false;
        }

        private GraphOutPin VisitMethodCall(MethodCallExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitNew(NewExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitNewArray(NewArrayExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitInvocation(InvocationExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitMemberInit(MemberInitExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitTypeIs(TypeBinaryExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitListInit(ListInitExpression exp)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitBinary(BinaryExpression exp)
        {
            throw new NotImplementedException();
        }

        private GraphOutPin VisitUnary(UnaryExpression node)
        {
            throw new NotImplementedException();
        }
        
        private GraphOutPin VisitLambda(LambdaExpression node)
        {
            if (node.Parameters.Count != _args.Length)
                throw new ArgumentException("Number of parameters doesn't match lambda arguments");
            for (var index = 0; index < node.Parameters.Count; index++)
            {
                var parameterExpression = node.Parameters[index];
                _visitedNodes[parameterExpression] = _args[index];
            }

            return Visit(node.Body);
        }
        private GraphOutPin VisitSubtract(BinaryExpression node)
        {
            return new Subtract(Visit(node.Left), Visit(node.Right)).Out;
        }

        private GraphOutPin VisitDivide(BinaryExpression node)
        {
            return new Divide(Visit(node.Left), Visit(node.Right)).Out;
        }

        private GraphOutPin VisitMultiply(BinaryExpression node)
        {
            return new Multiply(Visit(node.Left), Visit(node.Right)).Out;
        }

        private GraphOutPin VisitAdd(BinaryExpression node)
        {
            return new Add(Visit(node.Left), Visit(node.Right)).Out;
        }

        protected GraphOutPin VisitConstant(ConstantExpression node)
        {
            return VisitConstant(node.Value);
        }
        protected GraphOutPin VisitConstant(object value)
        {
            if (value.GetType() == typeof(int))
            {
                return _graph.BuildConstant((int)value).Out.FirstOrDefault();
            }
            if (value.GetType() == typeof(float))
            {
                return _graph.BuildConstant((float)value).Out.FirstOrDefault();
            }
            throw new NotImplementedException();
        }
    }
}