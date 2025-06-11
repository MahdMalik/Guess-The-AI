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