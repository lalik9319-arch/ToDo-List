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

// הדפסת ה-connection string (לבדיקה בלבד, בלי סיסמה אמיתית)
Console.WriteLine("Connection string being used: " + connectionString.Replace(passwordFromEnv ?? "", "*****"));

builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        Console.WriteLine("DbContext configured successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error configuring DbContext: " + ex.Message);
    }
});

var app = builder.Build();

// ======= Middleware =======
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// ======= Endpoints =======
app.MapGet("/", () => "API is running");

app.MapGet("/tasks", async (ToDoDbContext context) =>
{
    try
    {
        var tasks = await context.Items.ToListAsync();
        Console.WriteLine($"Retrieved {tasks.Count} tasks from DB.");
        return tasks;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error fetching tasks: " + ex.Message);
        return Results.Problem("Failed to fetch tasks.");
    }
});

app.MapPost("/tasks", async (ToDoDbContext context, Item newItem) =>
{
    try
    {
        context.Items.Add(newItem);
        await context.SaveChangesAsync();
        Console.WriteLine($"Created new task with ID {newItem.Id}");
        return Results.Created($"/tasks/{newItem.Id}", newItem);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error creating task: " + ex.Message);
        return Results.Problem("Failed to create task.");
    }
});

app.MapPut("/tasks/{id}", async (ToDoDbContext context, int id, Item updatedItem) =>
{
    try
    {
        var item = await context.Items.FindAsync(id);
        if (item == null)
        {
            Console.WriteLine($"Task with ID {id} not found for update.");
            return Results.NotFound();
        }
        item.Name = updatedItem.Name;
        item.IsComplete = updatedItem.IsComplete;
        await context.SaveChangesAsync();
        Console.WriteLine($"Updated task with ID {id}");
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error updating task: " + ex.Message);
        return Results.Problem("Failed to update task.");
    }
});

app.MapDelete("/tasks/{id}", async (ToDoDbContext context, int id) =>
{
    try
    {
        var item = await context.Items.FindAsync(id);
        if (item == null)
        {
            Console.WriteLine($"Task with ID {id} not found for deletion.");
            return Results.NotFound();
        }
        context.Items.Remove(item);
        await context.SaveChangesAsync();
        Console.WriteLine($"Deleted task with ID {id}");
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error deleting task: " + ex.Message);
        return Results.Problem("Failed to delete task.");
    }
});

app.Run();
