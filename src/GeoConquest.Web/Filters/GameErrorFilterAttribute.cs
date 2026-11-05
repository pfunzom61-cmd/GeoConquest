using GeoConquest.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GeoConquest.Web.Filters;

/// <summary>
/// Turns the game's expected failures into JSON the board can show: a broken rule is a 400 with the rule's message,
/// and a game changed elsewhere is a 409. Anything else is left to the normal error handling.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GameErrorFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            GameRuleException => StatusCodes.Status400BadRequest,
            ConcurrencyException => StatusCodes.Status409Conflict,
            _ => 0,
        };

        if (statusCode == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new { error = context.Exception.Message }) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
