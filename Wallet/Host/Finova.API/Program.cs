using Banks.Presentation.Controllers;
using Finova.API.Extensions;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(BankController).Assembly);


builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 2 * 1024 * 1024; });

builder.Services.AddFinovaServices(builder.Configuration);

var app = builder.Build();

await app.Services.MigrateFinovaDatabasesAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("allowAll");
app.UseRequestTimeouts();

app.MapControllers();

app.Run();