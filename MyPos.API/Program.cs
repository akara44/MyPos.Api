using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyPos.Infrastructure.Persistence;
using MyPos.Application.Validators;
using MyPos.Application.Dtos;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext Konfigürasyonu 
builder.Services.AddDbContext<MyPosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. FluentValidation 
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ProductValidator>(); // Özel validator'ları kaydet
builder.Services.AddValidatorsFromAssemblyContaining<ProductGroupValidator>();
builder.Services.AddScoped<IValidator<CreateVariantTypeDto>, CreateVariantTypeDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateVariantTypeDto>, UpdateVariantTypeDtoValidator>();
builder.Services.AddScoped<IValidator<CreateVariantValueDto>, CreateVariantValueDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateVariantValueDto>, UpdateVariantValueDtoValidator>();
// 3. JWT Authentication 
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT key is missing!");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 4. CORS Konfigürasyonu(Frontend için eklendi)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vue uygulamanın adresi
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// 5. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Middleware'ler
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 7. Otomatik Migration (Opsiyonel - Development için)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyPosDbContext>();
    db.Database.Migrate();
}




app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowVueApp"); // CORS burada aktifleşir
app.UseAuthorization();
app.MapControllers();
app.Run();