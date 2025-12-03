using System;
using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// ======= CORS =======
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ======= Swagger =======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ======= DbContext עם Environment Variable =======
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");

// החלפת PLACEHOLDER בסיסמה מהסביבה
var passwordFromEnv = Environment.GetEnvironmentVariable("DB_PASSWORD");
if (!string.IsNullOrEmpty(passwordFromEnv))
{
    connectionString = connectionString.Replace("PLACEHOLDER", passwordFromEnv);
}

// שימוש ב־AutoDetect לגרסת MySQL
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

var app = builder.Build();

// ======= Middleware =======
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// ======= Endpoints =======
app.MapGet("/", () => "API is running");

app.MapGet("/tasks", async (ToDoDbContext context) => await context.Items.ToListAsync());

app.MapPost("/tasks", async (ToDoDbContext context, Item newItem) =>
{
    context.Items.Add(newItem);
    await context.SaveChangesAsync();
    return Results.Created($"/tasks/{newItem.Id}", newItem);
});

app.MapPut("/tasks/{id}", async (ToDoDbContext context, int id, Item updatedItem) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null) return Results.NotFound();
    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;
    await context.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/tasks/{id}", async (ToDoDbContext context, int id) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null) return Results.NotFound();
    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
