var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// This enables built-in .NET 10 OpenAPI document generation
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Maps the endpoint to view your API documentation via /openapi/v1.json
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Initialize an in-memory database list for your todos
var todos = new List<Todo>
{
    new(1, "Create my first todo API", false)
};
var nextId = 2;

// 1. GET Endpoint: Retrieve all todos
app.MapGet("/todos", () => todos)
   .WithName("GetTodos");

// 2. GET Endpoint: Retrieve one todo by ID
app.MapGet("/todos/{id}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
})
   .WithName("GetTodoById");

// 3. POST Endpoint: Add a new todo item
app.MapPost("/todos", (CreateTodoRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    var todo = new Todo(nextId++, request.Title.Trim(), false);
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
})
   .WithName("CreateTodo");

// 4. PUT Endpoint: Update an existing todo item
app.MapPut("/todos/{id}", (int id, UpdateTodoRequest request) =>
{
    var index = todos.FindIndex(t => t.Id == id);
    if (index == -1) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    var updatedTodo = todos[index] with
    {
        Title = request.Title.Trim(),
        IsCompleted = request.IsCompleted
    };

    todos[index] = updatedTodo;
    return Results.Ok(updatedTodo);
})
   .WithName("UpdateTodo");

// 5. DELETE Endpoint: Remove a todo by ID
app.MapDelete("/todos/{id}", (int id) => 
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound();
    
    todos.Remove(todo);
    return Results.NoContent();
})
   .WithName("DeleteTodo");

app.Run();

// The Todo data structure
public record Todo(int Id, string Title, bool IsCompleted);
public record CreateTodoRequest(string Title);
public record UpdateTodoRequest(string Title, bool IsCompleted);
