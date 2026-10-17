using FluentValidation;
using SchoolManager.Management.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddManagementInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SchoolManager.Management.Application.CreateGradeCommand).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(SchoolManager.Management.Application.CreateGradeCommand).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(SchoolManager.Management.Application.CreateGradeCommand).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
