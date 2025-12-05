using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<MySqlConnection>(_ =>
{
    return new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
});
// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options => 
    { options.AddPolicy("OpenPolicy", builder => 
        { builder.AllowAnyOrigin() .AllowAnyMethod() .AllowAnyHeader(); 
        }); 
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("OpenPolicy");

app.MapControllers();

app.Run();
