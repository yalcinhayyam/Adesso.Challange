using FluentValidation;

using Services.Features;
using WebAPI.Contracts;

namespace WebAPI;



public class DrawRequestValidator : AbstractValidator<DrawRequest>
{
    public DrawRequestValidator()
    {
        RuleFor(x => x.DrawerName)
            .NotEmpty().WithMessage("DrawerName is required");

        RuleFor(x => x.NumberOfGroups)
            .GreaterThan(0).WithMessage("NumberOfGroups must be greater than 0");
    }
}

public static class Routes
{
    public static void MapRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/draws").WithOpenApi();
        group.WithTags("Draws");

        ConfigureRoutes(group);
    }

    private static void ConfigureRoutes(RouteGroupBuilder group)
    {
        // POST: /api/draws
        group.MapPost("/", async (
            DrawRequest request,
            IDrawService drawService,
            IValidator<DrawRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            try
            {
                var result = await drawService.CreateDrawAsync(request.DrawerName, request.NumberOfGroups);

                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return Results.Problem("Internal server error occurred while creating draw");
            }
        })
        .WithName("CreateDraw")
        .Produces<DrawResponse>(200)
        .ProducesValidationProblem()
        .Produces(400)
        .Produces(500);

        // GET: /api/draws
        group.MapGet("/", async (IDrawService drawService) =>
        {
            try
            {
                var results = await drawService.GetAllDrawsAsync();
                return Results.Ok(results);
            }
            catch (Exception)
            {
                return Results.Problem("Internal server error occurred while getting draws");
            }
        })
        .WithName("GetAllDraws")
        .Produces<List<DrawResponse>>(200)
        .Produces(500);

        // GET: /api/draws/{id}
        group.MapGet("/{id:int}", async (int id, IDrawService drawService) =>
        {
            try
            {
                var result = await drawService.GetDrawByIdAsync(id);
                return result is not null
                    ? Results.Ok(result)
                    : Results.NotFound($"Draw with id {id} not found");
            }
            catch (Exception)
            {
                return Results.Problem("Internal server error occurred while getting draw");
            }
        })
        .WithName("GetDrawById")
        .Produces<DrawResponse>(200)
        .Produces(404)
        .Produces(500);
    }
}
