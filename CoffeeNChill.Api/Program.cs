using System.Reflection;
using System.Text;
using Azure.Data.Tables;
using CoffeeNChill.Api.Middleware;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var storageConn = builder.Configuration["AzureWebJobsStorage"]
                  ?? "UseDevelopmentStorage=true";
var functionsBaseUrl = builder.Configuration["FunctionsBaseUrl"]
                       ?? "http://localhost:7071";
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CoffeeNChill";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CoffeeNChillClients";

// ---------- services ----------
builder.Services.AddSingleton(new TableServiceClient(storageConn));
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService>(
    new TokenService(jwtKey, jwtIssuer, jwtAudience));

builder.Services.AddHttpClient("functions", client =>
{
    client.BaseAddress = new Uri(functionsBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ---------- authentication ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---------- swagger ----------
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill API Gateway",
        Version = "v1",
        Description =
            "Unified gateway for the CoffeeNChill canteen system.\n\n" +
            "Browsing the menu and placing an order are public. Managing the menu, " +
            "staff documents and order statuses require an Admin or Staff token.\n\n" +
            "To call a protected endpoint: POST to /api/auth/login, copy the token, " +
            "click Authorize above and paste it in."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from /api/auth/login. Swagger adds the Bearer prefix."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware first, so it catches everything after it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger runs in every environment — the marker needs it on the live server.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CoffeeNChill API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "CoffeeNChill API Gateway";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();