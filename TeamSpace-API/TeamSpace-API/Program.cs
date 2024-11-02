using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using TeamSpace_API.Data;

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


//вывод информации для проверки подключения
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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();