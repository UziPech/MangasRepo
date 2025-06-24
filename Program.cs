using MangaApi.Data;
using MangaApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Puerto para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// 🔐 CORS limitado (solo tu IP o dominios específicos si gustas)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SeguridadCors", policy =>
    {
        policy.WithOrigins("https://tudominio.com") // <-- si tienes dominio
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 🔗 Conexión a base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")!)
);

// 💾 Repositorios
builder.Services.AddScoped<IMangaRepository, MangaRepository>();
builder.Services.AddScoped<IPrestamoRepository, PrestamoRepository>();

// 🔑 Configuración JWT
var claveSecreta = builder.Configuration["Jwt:Key"] ?? throw new Exception("Falta la clave secreta JWT.");
var key = Encoding.UTF8.GetBytes(claveSecreta);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// 🧪 Swagger protegido con JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Manga API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresa el token: Bearer {tu_token}",
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 🔐 Middleware de filtro IP
app.Use(async (context, next) =>
{
    var remoteIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                  ?? context.Connection.RemoteIpAddress?.ToString();

    var ipPermitida = "189.162.139.158"; // CAMBIA a tu IP real

    if (remoteIp != ipPermitida)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Acceso denegado: IP no permitida.");
        return;
    }

    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manga API v1");
});

app.UseHttpsRedirection();

app.UseCors("SeguridadCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();






