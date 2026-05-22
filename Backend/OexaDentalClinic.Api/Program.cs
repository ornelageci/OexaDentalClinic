using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Configuration;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.PostConfigure<EmailSettings>(options =>
{
    var pwd = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")
        ?? Environment.GetEnvironmentVariable("Email__SmtpPassword");
    if (!string.IsNullOrWhiteSpace(pwd))
        options.SmtpPassword = pwd;
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<AppointmentReminderService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = ConnectionStringHelper.Resolve(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying database migrations...");
    db.Database.Migrate();
    logger.LogInformation("Migrations applied.");

    DbSeeder.Seed(db);
    logger.LogInformation("Database seed complete.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
