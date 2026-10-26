using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartera.BLL.Abstractions
{
    public static class ResultExtensions
    {
        public static ObjectResult ToProblem(this Result result)
        {
            if (result.IsSuccess)
                throw new InvalidOperationException("Cannot convert success result to problem.");

            var problemDetails = new ProblemDetails
            {
                Status = result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                Title = result.Error.Code,
                Detail = result.Error.Description,
            };

            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };
        }
    }
}
