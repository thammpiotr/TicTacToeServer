using TicTacToeServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed((host) => true);
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");
app.MapHub<TicTacToeHub>("/ticTacToeHub");
app.MapGet("/", () => "Hello from Render!");

app.Run();