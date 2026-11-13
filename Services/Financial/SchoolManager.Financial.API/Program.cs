using BuildingBlocks;
using FluentValidation;
using SchoolManager.Financial.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddFinancialInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SchoolManager.Financial.Application.CreateFeeCommand).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(SchoolManager.Financial.Application.CreateFeeCommand).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(SchoolManager.Financial.Application.CreateFeeCommand).Assembly);

// Validates the JWT and exposes the logged-in user (ICurrentUserService) for CreatedBy/UpdatedBy
builder.Services.AddJwtCurrentUser(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
