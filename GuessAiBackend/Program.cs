using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic;
using Classes;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

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
            SocketHandler newSocket = new SocketHandler(socket);
            await newSocket.HandleSocket();
            Console.WriteLine("Ended connection!");
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

_ = Task.Run(async () =>
{
    Stopwatch stopwatch = new Stopwatch();
    while (true)
    {
        try
        {
            //chheck if the queue is enough that we can pull them now
            if (Globals.oneBotQueue.GetQueueSize() >= Globals.oneBotQueue.GetPlayersPerMatch())
            {
                //first, check if the stopwatch has been started already
                if (stopwatch.IsRunning)
                {
                    if (stopwatch.ElapsedMilliseconds > 500)
                    {
                        stopwatch.Stop();
                        //now take them outta the queue, give them the message
                        (bool success, String message, LinkedList<Player> players) = Globals.oneBotQueue.GetPlayersForMatch();
                        if (success)
                        {
                            Console.WriteLine("Alright, let's start the game!");
                            Match newMatch = new Match(players, "One Bot Game");
                            String matchHash = newMatch.GetHash();
                            Globals.matches.Add(matchHash, newMatch);
                            //for each player, find the socket its associated with and tell it to send a message
                            foreach(Player plr in players)
                            {
                                plr.SetInMatch(true);
                                plr.SetInQueue(false);
                                SocketHandler theSocket = Globals.socketPlayerMapping[plr];
                                Globals.socketPlayerMapping.Remove(plr);

                                ServerMessage sentMessage = new ServerMessage();
                                sentMessage.success = true;
                                sentMessage.message = "Game Starting!";
                                sentMessage.server_id = matchHash;
                                
                                theSocket.GoToSendMessage(sentMessage);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error getting players for a match: {message}.");
                        }
                    }
                }
                else
                {
                    stopwatch.Restart();
                }
            }
        }
        catch (Exception err)
        {
            Console.WriteLine($"An error occured while getting players and stuff from the queue: {err}.");
            break;
        }
        await Task.Yield();

    }
});

app.Run();