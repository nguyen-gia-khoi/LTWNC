//using DnsClient.Protocol;
//using LTWNC.Data;
//using LTWNC.Middleware;
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



using LTWNC.Data;
using LTWNC.Middleware;
using LTWNC.Models;
using LTWNC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Home");
    options.Conventions.AllowAnonymousToPage("/Authentication/Login");
});
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddControllers();
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

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<JwtServices>();

// Cấu hình JWT
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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Lấy token từ cookie accessToken
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

// Cấu hình CORS chỉ cho phép Razor Pages truy cập
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("https://localhost:7009")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); // Nếu frontend gửi cookie
    });
});

var app = builder.Build();

// Bật Swagger trong môi trường Dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP => HTTPS
app.UseHttpsRedirection();

// Cho phép đọc file tĩnh (CSS/JS...)
app.UseStaticFiles();

// Routing
app.UseRouting();

// CORS phải đặt trước Authentication
app.UseCors("AllowFrontend");

// Middleware xác thực custom
app.UseMiddleware<AuthenticationMiddleware>();

// Xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Mapping Razor Pages và Controllers
app.MapRazorPages();
app.MapControllers();

app.Run();
