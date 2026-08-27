using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaOrdemServico.Data;
using SistemaOrdemServico.Domain.Entities;
using SistemaOrdemServico.Domain.Enums;
using SistemaOrdemServico.Domain.Interfaces;
using SistemaOrdemServico.Infrastructure.Security;
using SistemaOrdemServico.Middlewares;
using SistemaOrdemServico.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers e Swagger (com suporte a JWT no botão "Authorize")
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe: Bearer {seu token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// 2. Banco de dados — Postgres
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Repositórios
builder.Services.AddScoped<IFluxoRepository, FluxoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// 4. JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Seção 'Jwt' não configurada em appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// 5. CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("Api", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// 6. Migrations automáticas
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var seedAdminPassword = builder.Configuration["SeedAdminPassword"];

    if (!string.IsNullOrWhiteSpace(seedAdminPassword) && !await db.Usuarios.AnyAsync())
    {
        var admin = Usuario.Criar(
            nomeUsuario: "admin",
            senha: seedAdminPassword,
            perfil: TipoPerfil.Administrador,
            setor: null,
            hasher: PasswordHasher.Hash,
            nome: "Administrador");

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}

// 7. Middlewares
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Api");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();