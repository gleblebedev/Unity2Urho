using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphExpressionVisitor
    {
        private readonly ParticleGraphBuilder _graph;
        private readonly Dictionary<Expression, GraphNode> _visitedNodes = new Dictionary<Expression, GraphNode>();

        public GraphExpressionVisitor(ParticleGraphBuilder graph)
        {
            _graph = graph;
        }

        public GraphNode Visit(Expression node)
        {
            if (_visitedNodes.TryGetValue(node, out var res))
                return res;
            res = VisitImpl(node);
            _visitedNodes[node] = res;
            return res;
        }

        protected GraphNode VisitImpl(Expression node)
        {
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

        private GraphNode VisitConditional(ConditionalExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitParameter(ParameterExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitMemberAccess(MemberExpression node)
        {
            if (ExpressionAsConstant(node, out object obj))
            {
                return VisitConstant(obj);
            }
            throw new NotImplementedException();
        }

        private bool ExpressionAsConstant(Expression nodeExpression, out object res)
        {
            switch (nodeExpression.NodeType)
            {
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
                    res = null;
                    return false;
            }
        }

        private GraphNode VisitMethodCall(MethodCallExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitNew(NewExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitNewArray(NewArrayExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitInvocation(InvocationExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitMemberInit(MemberInitExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitTypeIs(TypeBinaryExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitListInit(ListInitExpression exp)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitBinary(BinaryExpression exp)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitUnary(UnaryExpression node)
        {
            throw new NotImplementedException();
        }

        private GraphNode VisitLambda(LambdaExpression node)
        {
            return Visit(node.Body);
        }
        private GraphNode VisitSubtract(BinaryExpression node)
        {
            return _graph.Add(new Subtract(Visit(node.Left), Visit(node.Right)));
        }

        private GraphNode VisitDivide(BinaryExpression node)
        {
            return _graph.Add(new Divide(Visit(node.Left), Visit(node.Right)));
        }

        private GraphNode VisitMultiply(BinaryExpression node)
        {
            return _graph.Add(new Multiply(Visit(node.Left), Visit(node.Right)));
        }

        private GraphNode VisitAdd(BinaryExpression node)
        {
            return _graph.Add(new Add(Visit(node.Left), Visit(node.Right)));
        }

        protected GraphNode VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(int))
            {
                return _graph.BuildConstant((int)node.Value);
            }
            if (node.Type == typeof(float))
            {
                return _graph.BuildConstant((int)node.Value);
            }
            throw new NotImplementedException();
        }
        protected GraphNode VisitConstant(object value)
        {
            if (value.GetType() == typeof(int))
            {
                return _graph.BuildConstant((int)value);
            }
            if (value.GetType() == typeof(float))
            {
                return _graph.BuildConstant((float)value);
            }
            throw new NotImplementedException();
        }
    }
}