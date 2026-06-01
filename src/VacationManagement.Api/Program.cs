var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Minimal scaffold endpoint. Real endpoints, auth, Swagger, logging and
// persistence are added in the following commits.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
