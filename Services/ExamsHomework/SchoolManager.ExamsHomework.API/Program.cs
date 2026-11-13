using BuildingBlocks;
using FluentValidation;
using SchoolManager.ExamsHomework.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExamsHomeworkInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SchoolManager.ExamsHomework.Application.CreateExamCommand).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(SchoolManager.ExamsHomework.Application.CreateExamCommand).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(SchoolManager.ExamsHomework.Application.CreateExamCommand).Assembly);

// Validates the JWT and exposes the logged-in user (ICurrentUserService) for CreatedBy/UpdatedBy
builder.Services.AddJwtCurrentUser(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
