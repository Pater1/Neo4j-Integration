using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace plm_common.Extentions
{
    public static class ExpressionsExtentions
    {
        // Produces an expression identical to 'expression'
        // except with 'source' parameter replaced with 'target' expression.     
        public static (Expression body, IEnumerable<ParameterExpression> param) Replace<TInput>
                        (this Expression<TInput> expression,
                        ParameterExpression source,
                        Expression target)
        {
            return new ParameterReplacerVisitor(source, target)
                        .VisitAndConvert(expression);
        }

        private class ParameterReplacerVisitor : ExpressionVisitor
        {
            private ParameterExpression _source;
            private Expression _target;

            public ParameterReplacerVisitor
                    (ParameterExpression source, Expression target)
            {
                _source = source;
                _target = target;
            }

            internal (Expression body, IEnumerable<ParameterExpression> param) VisitAndConvert<T>(Expression<T> root)
            {
                return VisitLambda(root);
            }

            protected new (Expression body, IEnumerable<ParameterExpression> param) VisitLambda<T>(Expression<T> node)
            {
                // Leave all parameters alone except the one we want to replace.
                var parameters = node.Parameters
                                    .Select(p => p != _source? p: null)
                                    .ToArray();

                return (Visit(node.Body), parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Replace the source with the target, visit other params as usual.
                return node == _source ? _target : base.VisitParameter(node);
            }
        }
    }
}
