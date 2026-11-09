using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Trip_Service.Api.Middleware;
using Trip_Service.Application.Services;
using Trip_Service.Application.Services.Client;
using Trip_Service.Application.Services.Impl;
using Trip_Service.Domain;
using Trip_Service.Infrastructure;
using Trip_Service.Infrastructure.Authentication;
using Trip_Service.Infrastructure.Persistence;
using Trip_Service.Infrastructure.Persistence.Impl;
using Trip_Service.Infrastructure.Persistence.Repositories;
using Trip_Service.Infrastructure.RabbitMQ.Consumer;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandler>();

// Database and Jwt
builder.Services.AddDbContext<TripDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Thiếu cấu hình Jwt`");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Rabbitmq
builder.Services.AddMassTransit(x =>
{
    // x.AddConsumer<DriverMatchedConsumer>();
    x.AddConsumer<PaymentRequestedConsumer>();
    x.AddConsumer<PaymentSettledConsumer>();

    // Send message to rabbitMQ configuration
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Inject core
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddHttpClient<MatchingPricingService>(httpClient =>
{
    var endpoint = builder.Configuration["ApiGateway:Endpoint"]
        ?? throw new InvalidOperationException("Thiếu cấu hình ApiGateway:Endpoint.");

    httpClient.BaseAddress = new Uri($"{endpoint.TrimEnd('/')}/");
});
builder.Services.AddHttpClient<PaymentService>(httpClient =>
{
    var endpoint = builder.Configuration["ApiGateway:Endpoint"]
        ?? throw new InvalidOperationException("Thiếu cấu hình ApiGateway:Endpoint.");

    httpClient.BaseAddress = new Uri($"{endpoint.TrimEnd('/')}/");
});
builder.Services.AddScoped<IMatchingPricingService>(serviceProvider =>
    serviceProvider.GetRequiredService<MatchingPricingService>());
builder.Services.AddScoped<IPaymentService>(serviceProvider =>
    serviceProvider.GetRequiredService<PaymentService>());

//   Default service 
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ISendNotificateService, SendNotificateService>();
// Application service
builder.Services.AddScoped<ITripService, global::Trip_Service.Application.Services.TripService>();

  // Application Repository
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripDriverRejectionRepository, TripDriverRejectionRepository>();

// 
var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider
        .GetRequiredService<TripDBContext>()
        .Database
        .MigrateAsync();
}



app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}




app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
