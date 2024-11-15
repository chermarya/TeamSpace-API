using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TeamSpace_API.Data;
using TeamSpace_API.Models;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BCrypt.Net;


var builder = WebApplication.CreateBuilder(args);

string GetPostgresConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    Console.WriteLine($"Converted Connection String: {connectionString}");
    return connectionString;
}

var databaseUrl = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DATABASE_URL variable is missing.");
string connectionString = GetPostgresConnectionString(databaseUrl);

builder.Services.AddControllers();
builder.Services.AddAuthorization();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TeameSpaceApp",
            ValidAudience = "TeameSpaceUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TeameSpaceSecretKey1234567890asdffpoi"))
        };
    });


builder.Services.AddSwaggerGen();
var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//    // Проверяем, есть ли уже данные в таблице User
//    if (!dbContext.Users.Any())
//    {
//        dbContext.Users.Add(new User
//        {
//            Nickname = "TestUser1",
//            Email = "testuser1@example.com",
//            Password = "testpassword1",
//            Photo = "base64_encoded_string",
//            Description = "Test user description",
//            Country = "Ukraine"
//        });

//        dbContext.Users.Add(new User
//        {
//            Nickname = "TestUser2",
//            Email = "testuser2@example.com",
//            Password = "testpassword2",
//            Photo = "base64_encoded_string",
//            Description = "Another test user description",
//            Country = "USA"
//        });

//        // Сохраняем изменения в базе данных
//        dbContext.SaveChanges();
//    }
//}


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine($"Database Connection String: {connectionString}");
    Console.WriteLine($"Database connection successful: {dbContext.Database.CanConnect()}");
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseDeveloperExceptionPage();
app.MapControllers();
app.Run();

string GenerateJwtToken(User user)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TeameSpaceSecretKey1234567890asdffpoi"));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: "TeameSpaceApp",
        audience: "TeameSpaceUsers",
        claims: claims,
        expires: DateTime.Now.AddMinutes(120),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
