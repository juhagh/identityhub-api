using IdentityHub.API.Endpoints.Auth;
using IdentityHub.API.Endpoints.Users;
using IdentityHub.Application;
using IdentityHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddProblemDetails();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply migrations and create roles
await app.Services.InitializeInfrastructureAsync();

app.Run();