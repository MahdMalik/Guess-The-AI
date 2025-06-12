using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic;
using Classes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseWebSockets();

//app.Use is a way to add middleware.
app.Use(async (context, next) =>
{
    //checks if they're trying to connect to a ws endpoint
    if (context.Request.Path == "/ws")
    {
        //if they are, check if it's a web socket request
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            //this will be our buffer
            await SocketHandler.HandleSocket(socket);
        }
        //if not, don't want to connect so give them an error
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    //otherwise, it passes the task down to the rest of the middleware. Next is a function representing
    //the next piece of middleware
    else
    {
        await next(context);
    }
});

GameQueue oneBotQueue = new GameQueue("One Bot Game", 4);


// Minimal test endpoint
app.MapGet("/", () => "Hello from your backend!");

app.MapGet("/test", () =>
{
    string returnMessage = "Hi, backend here!";
    return Results.Ok(new
    {
        success = true,
        message = returnMessage
    });
});

app.Run();