using FluentValidation;
using SchoolManager.Notification.Application;
using SchoolManager.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddNotificationInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateNotificationCommand).Assembly));

builder.Services.AddAutoMapper(cfg => { }, typeof(CreateNotificationCommand).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(CreateNotificationCommand).Assembly);

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
