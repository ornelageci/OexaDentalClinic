using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (!db.Users.Any())
            {
                db.Users.AddRange(
                    new User { Email = "admin@oexa.com", Password = "admin123", FirstName = "System", LastName = "Admin", Role = "Admin" },
                    new User { Email = "manager@oexa.com", Password = "manager123", FirstName = "Reception", LastName = "Manager", Role = "Manager" },
                    new User { Email = "marketer@oexa.com", Password = "marketer123", FirstName = "Marketing", LastName = "Staff", Role = "Marketer" },
                    new User { Email = "alkeo@oexa.com", Password = "dentist123", FirstName = "Alkeo", LastName = "Gaci", Role = "Dentist", DentistServiceKey = "general" },
                    new User { Email = "ornela@oexa.com", Password = "dentist123", FirstName = "Ornela", LastName = "Geci", Role = "Dentist", DentistServiceKey = "orthodontics" },
                    new User { Email = "xhule@oexa.com", Password = "dentist123", FirstName = "Xhule", LastName = "Metaj", Role = "Dentist", DentistServiceKey = "cosmetic" },
                    new User { Email = "alesja@oexa.com", Password = "dentist123", FirstName = "Alesja", LastName = "Toci", Role = "Dentist", DentistServiceKey = "pediatric" },
                    new User { Email = "elvis@oexa.com", Password = "dentist123", FirstName = "Elvis", LastName = "Sula", Role = "Dentist", DentistServiceKey = "oral-surgery" },
                    new User { Email = "patient@oexa.com", Password = "patient123", FirstName = "Demo", LastName = "Patient", Role = "Patient", PhoneNumber = "+355691234567" }
                );
                db.SaveChanges();
            }

            if (!db.Promotions.Any())
            {
                var today = DateTime.UtcNow.Date;
                db.Promotions.AddRange(
                    new Promotion
                    {
                        Title = "Teeth Whitening -20%",
                        Description = "Professional whitening package",
                        DiscountPercent = 20,
                        StartDate = today,
                        EndDate = today.AddMonths(2),
                        IsActive = true,
                        TargetAudience = "All patients"
                    },
                    new Promotion
                    {
                        Title = "New Client Check-up Offer",
                        Description = "First visit discount",
                        DiscountPercent = 15,
                        StartDate = today,
                        EndDate = today.AddMonths(1),
                        IsActive = true,
                        TargetAudience = "New clients"
                    }
                );
                db.SaveChanges();
            }
        }
    }
}
