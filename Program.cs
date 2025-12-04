using System;
using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// ======= CORS & Swagger =======
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ======= DbContext: קריאה ישירה ממשתני סביבה =======

// 1. נסה לקרוא את מחרוזת החיבור המלאה מ-Render (משתנה סביבה)
var finalConnectionString = Environment.GetEnvironmentVariable("ToDoDB");

// 2. אם המשתנה לא נמצא, קח את זה מ-appsettings.json כגיבוי.
if (string.IsNullOrEmpty(finalConnectionString))
{
    finalConnectionString = builder.Configuration.GetConnectionString("ToDoDB");
    Console.WriteLine("Warning: Using Connection String from appsettings.json. Ensure security locally.");
}
else
{
    // מדפיס רק את השרת והמשתמש כדי לוודא שימוש במחרוזת הנכונה (בלי לחשוף סיסמה)
    // הערה: החלפת נקודה פסיק ב-& לצורך יצירת URI תקין להדפסה בלבד.
    Console.WriteLine($"Using DB connection for server: {new System.Uri(finalConnectionString.Replace(";", "&")).Host}");
}


// 3. הגדרת ה-DbContext - ללא Try/Catch! אם הסיסמה שגויה, השרת ייכבה.
builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    options.UseMySql(finalConnectionString, ServerVersion.AutoDetect(finalConnectionString));
});


var app = builder.Build();

// ======= Middleware & Endpoints (ללא שינוי מהותי) =======
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "API is running");

// Endpoints (אפשרי להסיר את ה-try/catch גם מה-Endpoints, אך נשאיר אותם לטיפול בשגיאות זמן ריצה)
app.MapGet("/tasks", async (ToDoDbContext context) =>
{
    try
    {
        var tasks = await context.Items.ToListAsync();
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error fetching tasks: " + ex.Message);
        return Results.Problem("Failed to fetch tasks.");
    }
});
// ... (השארת MapPost, MapPut, MapDelete כפי שהם בגרסה הקודמת) ...

app.MapPost("/tasks", async (ToDoDbContext context, Item newItem) =>
{
    try
    {
        context.Items.Add(newItem);
        await context.SaveChangesAsync();
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
        if (item == null) return Results.NotFound();

        item.Name = updatedItem.Name;
        item.IsComplete = updatedItem.IsComplete;
        await context.SaveChangesAsync();
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
        if (item == null) return Results.NotFound();

        context.Items.Remove(item);
        await context.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error deleting task: " + ex.Message);
        return Results.Problem("Failed to delete task.");
    }
});

app.Run();