using MangaApi.Data;
using MangaApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySql.EntityFrameworkCore.Extensions;
using System.Text;
using System.Linq; // ✅ Necesario para FirstOrDefault

var builder = WebApplication.CreateBuilder(args);

// ✅ Puerto para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Conexión a MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")!)
);

// ✅ Repositorios
builder.Services.AddScoped<IMangaRepository, MangaRepository>();
builder.Services.AddScoped<IPrestamoRepository, PrestamoRepository>();

// ✅ Configuración JWT
var claveSecreta = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("⚠️ No se encontró la clave secreta JWT en appsettings.json");

var key = Encoding.UTF8.GetBytes(claveSecreta);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// ✅ Swagger con JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Manga API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresa el token JWT. Ejemplo: Bearer {tu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manga API v1");
});

app.UseHttpsRedirection();

// ✅ FILTRO por IP pública usando el header X-Forwarded-For
app.Use(async (context, next) =>
{
    var remoteIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    var ipPermitida = "189.162.139.158"; // 👈 Reemplaza con tu IP pública

    if (remoteIp != ipPermitida)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Acceso denegado desde esta IP.");
        return;
    }

    await next();
});

// ✅ Middleware de seguridad
app.UseCors("PermitirTodo");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();





