using Microsoft.EntityFrameworkCore;
using Model.Model;

namespace Model;

public class ContextClass  : DbContext
{
    public DbSet<User> Users { get; set; }
 
    public ContextClass(DbContextOptions<ContextClass> options) : base(options) { }
}