using Scalar.AspNetCore;
using MyLife.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// Services
// ----------------------------------------------------

// Add services to the dependency injection container.
builder.Services.AddControllers();

// Generate OpenAPI document
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//  Register the global exception handler with dependency injection..
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
//  Register the infrastructure for producing standardized Problem Details error responses.
builder.Services.AddProblemDetails();

/*
 * Health endpoint added before implementing authentication, CQRS, etc.
 * Useful for Azure, Kubernetes, load balancers, monitoring, deployment validation, and other platforms to check if the API is up and running.
*/
builder.Services.AddHealthChecks();

// Allow Angular frontend to call the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:58250")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

// ----------------------------------------------------
// Middleware pipeline
// ----------------------------------------------------

// Apply the global exception handler middleware to catch unhandled exceptions and return a JSON response to the client.
// This activates the exception-handling middleware in the request pipeline.
app.UseExceptionHandler();

// Expose OpenAPI JSON
//app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add Scalar UI
app.MapScalarApiReference();

// Apply CORS policy
app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

//app.UseAuthorization();

app.MapControllers();

app.Run();
