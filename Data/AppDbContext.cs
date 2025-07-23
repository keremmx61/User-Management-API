using Microsoft.EntityFrameworkCore;
using UserManagementApi.Dtos;
using UserManagementApi.Models;

namespace UserManagementApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserWithRoleDto> UserWithRoleDtos { get; set; }


    }
}
