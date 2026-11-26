using FoodHub.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Middleware;

// Đổi lỗi nghiệp vụ thành mã HTTP. Domain/Application chỉ ném lỗi, không biết gì về HTTP.
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        // Thứ tự quan trọng: lớp con (ConcurrencyException) phải đứng trước lớp cha (DomainException)
        var status = ex switch
        {
            ConcurrencyException => StatusCodes.Status409Conflict,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception");

        if (context.Response.HasStarted) throw ex;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : ex.Message
        });
    }
}
