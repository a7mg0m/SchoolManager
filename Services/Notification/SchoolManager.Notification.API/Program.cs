using BuildingBlocks;
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
{
    cfg.RegisterServicesFromAssembly(typeof(CreateNotificationCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddExceptionHandler<SchoolManager.Notification.API.ChatExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAutoMapper(cfg => { }, typeof(CreateNotificationCommand).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(CreateNotificationCommand).Assembly);

// Validates the JWT and exposes the logged-in user (ICurrentUserService) for CreatedBy/UpdatedBy
builder.Services.AddJwtCurrentUser(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Pictures of notifications / events: wwwroot/{notifications,events,default} served at /notification-files/…
// (the gateway forwards /notification-files to this service)
var webRoot = ImageFileStore.WebRoot(app.Environment);
foreach (var folder in ImageFileStore.Folders)
    Directory.CreateDirectory(Path.Combine(webRoot, folder));
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = ImageFileStore.UrlPrefix,
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=86400"
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
