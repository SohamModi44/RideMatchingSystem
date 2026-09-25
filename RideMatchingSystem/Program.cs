using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.Hubs;
using RideMatchingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));
    });

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddScoped<
    DriverLocationService>();

builder.Services.AddScoped<
    RideMatchingService>();

builder.Services.AddHostedService<
    RideMatchingWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowAnyOrigin();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.MapControllers();

app.MapHub<RideHub>(
    "/hubs/ride");

app.MapFallbackToFile(
    "index.html");

app.Run();