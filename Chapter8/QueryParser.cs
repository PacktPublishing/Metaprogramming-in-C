using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chapter8;

public static class QueryParser
{
    static readonly ParameterExpression _dictionaryParameter = Expression.Parameter(typeof(IDictionary<string, object>), "input");

    public static Expression<Func<IDictionary<string, object>, bool>> Parse(JsonDocument json)
    {
        var element = json.RootElement;
        var query = NewMethod(element);
        return Expression.Lambda<Func<IDictionary<string, object>, bool>>(query, _dictionaryParameter);
    }

    static Expression NewMethod(JsonElement element)
    {
        Expression? currentExpression = null;

        foreach (var property in element.EnumerateObject())
        {
            Expression expression = property.Name switch
            {
                "$or" => GetOrExpression(currentExpression!, property),
                "$in" => Expression.Empty(),
                _ => NewMethod1(GetGetValueExpression(property), property)
            };

            if (currentExpression is not null)
            {
                currentExpression = Expression.And(currentExpression, expression);
            }
            else
            {
                currentExpression = expression;
            }
        }

        return currentExpression ?? Expression.Empty();
    }

    static Expression GetOrExpression(Expression expression, JsonProperty property)
    {
        foreach (var element in property.Value.EnumerateArray())
        {
            var elementExpression = NewMethod(element);
            expression = Expression.Or(expression, elementExpression);
        }

        return expression;
    }

    static Expression NewMethod1(Expression getValueExpression, JsonProperty property)
    {
        return property.Value.ValueKind switch
        {
            JsonValueKind.Object => GetFilterExpression(getValueExpression, property),
            _ => Expression.Equal(getValueExpression, GetValueConstantExpression(property))
        };
    }

    static Expression GetFilterExpression(Expression getValueExpression, JsonProperty property)
    {
        Expression? currentExpression = null;

        foreach (var expressionProperty in property.Value.EnumerateObject())
        {
            var valueConstantExpression = GetValueConstantExpression(expressionProperty);
            Expression comparisonExpression = expressionProperty.Name switch
            {
                "$lt" => Expression.LessThan(getValueExpression, valueConstantExpression),
                "$lte" => Expression.LessThanOrEqual(getValueExpression, valueConstantExpression),
                "$gt" => Expression.GreaterThan(getValueExpression, valueConstantExpression),
                "$gte" => Expression.GreaterThanOrEqual(getValueExpression, valueConstantExpression),
                _ => Expression.Empty()
            };

            if (currentExpression is not null)
            {
                currentExpression = Expression.And(currentExpression, comparisonExpression);
            }
            else
            {
                currentExpression = comparisonExpression;
            }
        }

        return currentExpression ?? Expression.Empty();
    }

    static Expression GetGetValueExpression(JsonProperty property)
    {
        var keyParam = Expression.Constant(property.Name);
        var indexer = typeof(IDictionary<string, object>).GetProperty("Item")!;
        var indexerExpr = Expression.Property(_dictionaryParameter, indexer, keyParam);

        return property.Value.ValueKind switch
        {
            JsonValueKind.Number => Expression.Unbox(indexerExpr, typeof(int)),
            _ => indexerExpr
        };
    }

    static Expression GetValueConstantExpression(JsonProperty property)
    {
        return property.Value.ValueKind switch
        {
            JsonValueKind.Number => Expression.Constant((object)property.Value.GetInt32()),
            JsonValueKind.String => Expression.Constant((object)property.Value.GetString()!),
            JsonValueKind.True or JsonValueKind.False => Expression.Constant((object)property.Value.GetBoolean()),
            _ => Expression.Empty()
        };
    }
}