using BuildingBlocks;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolManager.Notification.API.Realtime;
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

// Live chat (SignalR /hubs/chat): new messages and delivered / seen ticks reach the open apps without a refresh
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SubjectUserIdProvider>();
builder.Services.AddSingleton<IChatRealtimeNotifier, SignalRChatNotifier>();
// WebSockets cannot send an Authorization header: the hub takes the JWT from ?access_token=
// (so do the chat files: image / video / audio players can't always send headers, e.g. in a browser)
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    var previous = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context =>
    {
        if (previous is not null) await previous(context);
        var token = context.Request.Query["access_token"].ToString();
        if (string.IsNullOrEmpty(context.Token) && !string.IsNullOrEmpty(token) &&
            (context.HttpContext.Request.Path.StartsWithSegments(ChatHub.Path) ||
             context.HttpContext.Request.Path.StartsWithSegments(ChatMedia.UrlPrefix)))
        {
            context.Token = token;
        }
    };
});

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
app.MapHub<ChatHub>(ChatHub.Path);

app.Run();
