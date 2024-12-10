var builder = WebApplication.CreateBuilder(args);

// Set console encoding for UTF-8 support
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton<backend.Services.VectorService>();
builder.Services.AddSingleton<backend.Services.TextService>();
builder.Services.AddSingleton<backend.Services.ImageService>();

// Enable Swagger/OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin() // Allows requests from any origin
            .AllowAnyMethod() // Allows any HTTP method (GET, POST, etc.)
            .AllowAnyHeader(); // Allows any headers
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger in development environment
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

// Enable CORS middleware globally
app.UseCors("AllowAll");

// Routing and controllers
app.UseRouting();
app.MapControllers();

// Start the application
app.Run();
