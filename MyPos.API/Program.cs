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

// Mevcut validator'ları kaydet
builder.Services.AddValidatorsFromAssemblyContaining<ProductValidator>(); // ProductValidator ve ProductGroupValidator buraya eklendi
builder.Services.AddValidatorsFromAssemblyContaining<ProductGroupValidator>();

// VariantType ve VariantValue için mevcut validator'lar
builder.Services.AddScoped<IValidator<CreateVariantTypeDto>, CreateVariantTypeDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateVariantTypeDto>, UpdateVariantTypeDtoValidator>();
builder.Services.AddScoped<IValidator<CreateVariantValueDto>, CreateVariantValueDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateVariantValueDto>, UpdateVariantValueDtoValidator>();

// YENİ EKLENEN: ProductVariant DTO'ları için validator'lar
builder.Services.AddScoped<IValidator<CreateProductVariantDto>, CreateProductVariantDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateProductVariantDto>, UpdateProductVariantDtoValidator>();

// YENİ EKLENEN: UpdateProductWithImageDto için validator (ProductsController'da kullanıldığı için)
builder.Services.AddScoped<IValidator<UpdateProductWithImageDto>, UpdateProductWithImageDtoValidator>();


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

// 4. CORS Konfigürasyonu (Frontend için eklendi)
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

// DİKKAT: Eğer "AllowVueApp" politikasını kullanıyorsanız, "AllowFrontend" politikasına ihtiyacınız olmayabilir.
// Eğer "AllowFrontend" politikasını tanımlamadıysanız veya kullanmıyorsanız bu satırı kaldırabilirsiniz.
// app.UseCors("AllowFrontend");

app.UseStaticFiles(); // wwwroot klasöründeki statik dosyalara erişim için
app.UseRouting();

// CORS politikası UseRouting() çağrısından sonra, UseAuthentication() ve UseAuthorization() çağrılarından önce gelmelidir.
app.UseCors("AllowVueApp");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 7. Otomatik Migration (Opsiyonel - Development için)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyPosDbContext>();
    // Uygulama her başladığında bekleyen tüm migrasyonları uygular.
    // Üretim ortamında dikkatli kullanılmalıdır.
    db.Database.Migrate();
}

app.Run();
