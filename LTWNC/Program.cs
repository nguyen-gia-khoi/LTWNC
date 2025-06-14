//using LTWNC.Data;
//using LTWNC.Middleware;
//using LTWNC.Models;
//using LTWNC.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;


//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorPages(options =>
//{
//    options.Conventions.AuthorizeFolder("/Home");
//    options.Conventions.AllowAnonymousToPage("/Authentication/Login");
//});
//builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    var jwtSecurityScheme = new OpenApiSecurityScheme
//    {
//        BearerFormat = "JWT",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.Http,
//        Scheme = JwtBearerDefaults.AuthenticationScheme,
//        Description = "Enter your JWT Access Token",
//        Reference = new OpenApiReference
//        {
//            Type = ReferenceType.SecurityScheme,
//            Id = JwtBearerDefaults.AuthenticationScheme
//        }
//    };
//    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        { jwtSecurityScheme, Array.Empty<string>() }
//    });
//});
//builder.Services.AddSingleton<MongoDbService>();
//builder.Services.AddScoped<JwtServices>();
//builder.Services.AddScoped<IImageService, ImageService>();
//builder.Services.AddSingleton<IApiUrlService, ApiUrlService>();

//// Configure JWT Authentication
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
//        ValidAudience = builder.Configuration["JwtConfig:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(
//            Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Key"]))
//    };

//    // Configure events to handle token from cookie
//    options.Events = new JwtBearerEvents
//    {
//        OnMessageReceived = context =>
//        {
//            // Get token from cookie
//            var accessToken = context.Request.Cookies["accessToken"];
//            if (!string.IsNullOrEmpty(accessToken))
//            {
//                context.Token = accessToken;
//            }
//            return Task.CompletedTask;
//        }
//    };
//});

//builder.Services.AddAuthorization();

//// Add CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        builder =>
//        {
//            builder.AllowAnyOrigin()
//                   .AllowAnyMethod()
//                   .AllowAnyHeader();
//        });
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();

//// Use CORS
//app.UseCors("AllowAll");

//// Use custom authentication middleware
//app.UseMiddleware<AuthenticationMiddleware>();

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapRazorPages();
//app.MapControllers();

//// Set default route to Login page
////app.MapGet("/", context =>
////{
////    context.Response.Redirect("/Authentication/Login");
////    return Task.CompletedTask;
////});

//app.Run();


using CloudinaryDotNet;
using LTWNC.Data;
using LTWNC.Middleware;
using LTWNC.Models;
using LTWNC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages with authorization
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Home");
    options.Conventions.AllowAnonymousToPage("/Authentication/Login");
});

// Add app settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// ✅ Add Cloudinary settings (từ LTWNC.Models)
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// ✅ Add Cloudinary client
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
    return new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
});

// Add services
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<JwtServices>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddSingleton<IApiUrlService, ApiUrlService>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger + JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Enter your JWT Access Token",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Key"]))
    };

    // Get token from cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS
app.UseCors("AllowAll");

// Middleware
app.UseMiddleware<AuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
