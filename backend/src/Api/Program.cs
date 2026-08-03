// Program.cs = app.js/index.js de Express 
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// 1. Crea la aplicación.
// const express = require("express");
// const app = express();
var builder = WebApplication.CreateBuilder(args);

// 2. Configura los servicios de la aplicación. "Mi API tendrá Swagger." Como postman pero https://localhost:5001/swagger y probar todos los endpoints.
//app.use(routes);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
//"Cuando alguien necesite la base de datos, usa SQLite."
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=mesa.db"));

// Leemos la config del JWT (como process.env.JWT_SECRET en Node)
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtHoras = int.Parse(builder.Configuration["Jwt:ExpiraHoras"]!);

// Registramos el TokenService para poder inyectarlo en los controllers
builder.Services.AddSingleton(new TokenService(jwtSecret, jwtHoras));

// Esto es el equivalente al middleware de "verify JWT" que pondrías en cada ruta protegida
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });
builder.Services.AddAuthorization();

// CORS — para que el frontend en :5173 pueda llamar a esta API
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Le decimos a Swagger: "existe un esquema de auth llamado Bearer, tipo JWT"
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Pega solo el token, sin la palabra 'Bearer'."
    });

    // Le decimos a Swagger: "aplica ese esquema a todas las rutas que lo requieran"
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
//3. Construye la aplicación.
// const app = express();
var app = builder.Build();

// Si estoy desarrollando, activa Swagger.
// if(process.env.NODE_ENV==="development")
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Hace que HTTP se convierta en HTTPS. Si alguien hace una petición HTTP, lo redirige a HTTPS.
app.UseHttpsRedirection();


app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));

app.UseCors("Frontend");
app.UseAuthentication();  // "¿quién eres?" — lee el token
app.UseAuthorization();   // "¿qué puedes hacer?" — revisa permisos

//app.get("/", (req,res)=>{
app.MapControllers();



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();           // aplica migraciones automáticamente (requisito del enunciado)
    Infrastructure.Data.DataSeeder.Seed(db); // siembra los datos si está vacía
}
//4. Ejecuta la aplicación. Corre el servidor en el puerto 3000.
// app.listen(3000)
app.Run();
