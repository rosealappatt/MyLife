using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Generate OpenAPI document
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

// Expose OpenAPI JSON
app.MapOpenApi();

// Add Scalar UI
app.MapScalarApiReference();

// Apply CORS policy
app.UseCors("AllowAngular");

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
