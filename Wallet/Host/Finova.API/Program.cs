using Finova.Modules.Banks.Extensions;
using Finova.Modules.Banks.Presentation.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddApplicationPart(typeof(BankController).Assembly);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy(
        "allowAll",
        policy =>
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        }
    );
});

var connectionString = builder.Configuration.GetConnectionString("finovaWalletDb");

//Extensions
builder.Services.AddBanksExtensions(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("allowAll");

app.MapControllers();
app.Run();
