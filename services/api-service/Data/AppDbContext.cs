using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using api_service.Models;

namespace api_service.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {  
        }
        public DbSet<Documents> Documents => Set<Documents>();
    } 
}