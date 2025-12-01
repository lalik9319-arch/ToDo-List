using TodoApi;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()   // מאפשר גישה מכל דומיין
              .AllowAnyMethod()   // מאפשר כל HTTP method (GET, POST, PUT, DELETE)
              .AllowAnyHeader();  // מאפשר שליחת כל headers
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ToDoDbContext>();
var app = builder.Build();
app.UseCors();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

// שליפת כל המשימות
app.MapGet("/tasks", async (ToDoDbContext context) =>
    await context.Items.ToListAsync()
);

// הוספת משימה חדשה
app.MapPost("/tasks", async (ToDoDbContext context, Item newItem) =>
{
    context.Items.Add(newItem);
    await context.SaveChangesAsync();
    return Results.Created($"/tasks/{newItem.Id}", newItem);
});

// עדכון משימה
app.MapPut("/tasks/{id}", async (ToDoDbContext context, int id, Item updatedItem) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null) return Results.NotFound();

    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;

    await context.SaveChangesAsync();
    return Results.NoContent();
});

// מחיקת משימה
app.MapDelete("/tasks/{id}", async (ToDoDbContext context, int id) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null) return Results.NotFound();

    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
});
app.Run();
