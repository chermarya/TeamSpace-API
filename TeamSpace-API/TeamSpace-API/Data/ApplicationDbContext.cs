using Microsoft.EntityFrameworkCore;

namespace TeamSpace_API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //настройка связи между user и tag
        modelBuilder.Entity<User>()
            .HasMany(u => u.Tags)               //один пользователь имеет много тегов
            .WithOne(t => t.User)               //один тег связан с одним пользователем
            .HasForeignKey(t => t.UserId)       //внешний ключ в таблице Tag
            .OnDelete(DeleteBehavior.Cascade);  //при удалении пользователя его теги удаляются
    }
}
