using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
// 1. THÊM THƯ VIỆN NÀY ĐỂ DÙNG IdentityDbContext
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser>(options) 
    {
        
        public DbSet<Category> Categories { get; set; }
        public DbSet<ItemInventory> ItemInventories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            
            // --- SEED ROLES ---
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "user-id", Name = "User", NormalizedName = "USER", ConcurrencyStamp = "STATIC-GUID-ROLE-USER" },
                new IdentityRole { Id = "admin-id", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "STATIC-GUID-ROLE-ADMIN" }
            );

            // --- SEED ADMIN USER ---
            var adminId = "admin-id";
            var adminUser = new AppUser
            {
                Id = adminId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@gmail.com",
                EmailConfirmed = true,
                SecurityStamp = "STATIC-GUID-SEC-12345",
                ConcurrencyStamp = "STATIC-GUID-CON-67890",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            // Password Hash cứng (Pass: Dat6112005nt!)
            adminUser.PasswordHash = "AQAAAAIAAYagAAAAELuWf8X8+7J8J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+A==";

            builder.Entity<AppUser>().HasData(adminUser);

            // --- ASSIGN ROLE ---
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "admin-id",
                    UserId = adminId
                }
            );

            // --- Cấu hình Entity ---
            builder.Entity<Transaction>().Property(t => t.TotalAmount).HasPrecision(18, 2);
            builder.Entity<TransactionDetail>().Property(td => td.Price).HasPrecision(18, 2);

        }
    }
}