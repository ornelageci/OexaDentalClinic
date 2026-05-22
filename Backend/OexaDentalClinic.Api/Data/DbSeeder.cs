using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            SeedProblems(db);
            SeedUsers(db);
            EnsureDentist(db, "ornela.braho@oexa.com", "Ornela", "Braho", "general");
            EnsureDentist(db, "xhesika.azizaj@oexa.com", "Xhesika", "Azizaj", "cosmetic");
            SeedPromotions(db);
        }

        private static void SeedProblems(AppDbContext db)
        {
            var defaults = new[]
            {
                new DentalProblem { Key = "general-checkup", Name = "General check-up & tooth pain", Description = "Routine exam, cleaning, pain relief", BasePrice = 50, DurationMinutes = 45, DentistCategoryKey = "general" },
                new DentalProblem { Key = "cavity-filling", Name = "Cavity filling", Description = "Fill cavities and restore teeth", BasePrice = 80, DurationMinutes = 60, DentistCategoryKey = "general" },
                new DentalProblem { Key = "root-canal", Name = "Root canal therapy", Description = "Treat infection inside the tooth", BasePrice = 180, DurationMinutes = 90, DentistCategoryKey = "general" },
                new DentalProblem { Key = "dental-cleaning", Name = "Professional dental cleaning", Description = "Scaling and polish", BasePrice = 70, DurationMinutes = 45, DentistCategoryKey = "general" },
                new DentalProblem { Key = "braces-adjustment", Name = "Braces / orthodontics", Description = "Braces fitting and adjustments", BasePrice = 120, DurationMinutes = 60, DentistCategoryKey = "orthodontics" },
                new DentalProblem { Key = "aligner-consult", Name = "Clear aligner consultation", Description = "Assessment for invisible aligners", BasePrice = 90, DurationMinutes = 45, DentistCategoryKey = "orthodontics" },
                new DentalProblem { Key = "teeth-whitening", Name = "Teeth whitening", Description = "Cosmetic whitening treatment", BasePrice = 150, DurationMinutes = 120, DentistCategoryKey = "cosmetic" },
                new DentalProblem { Key = "veneers-consult", Name = "Veneers & smile design", Description = "Cosmetic veneers consultation", BasePrice = 100, DurationMinutes = 60, DentistCategoryKey = "cosmetic" },
                new DentalProblem { Key = "child-dental", Name = "Child dental care", Description = "Pediatric dental visit", BasePrice = 60, DurationMinutes = 45, DentistCategoryKey = "pediatric" },
                new DentalProblem { Key = "child-preventive", Name = "Child preventive care", Description = "Sealants and fluoride for children", BasePrice = 55, DurationMinutes = 40, DentistCategoryKey = "pediatric" },
                new DentalProblem { Key = "tooth-extraction", Name = "Tooth extraction / oral surgery", Description = "Surgical extraction", BasePrice = 200, DurationMinutes = 75, DentistCategoryKey = "oral-surgery" },
                new DentalProblem { Key = "dental-implant", Name = "Dental implant consultation", Description = "Implant assessment and planning", BasePrice = 120, DurationMinutes = 60, DentistCategoryKey = "oral-surgery" },
                new DentalProblem { Key = "gum-treatment", Name = "Gum disease treatment", Description = "Periodontal care", BasePrice = 110, DurationMinutes = 60, DentistCategoryKey = "general" },
                new DentalProblem { Key = "emergency-visit", Name = "Emergency dental visit", Description = "Urgent pain or trauma", BasePrice = 90, DurationMinutes = 60, DentistCategoryKey = "general" }
            };

            if (!db.DentalProblems.Any())
            {
                db.DentalProblems.AddRange(defaults);
                db.SaveChanges();
                return;
            }

            foreach (var d in defaults)
            {
                var existing = db.DentalProblems.FirstOrDefault(p => p.Key == d.Key);
                if (existing == null)
                {
                    db.DentalProblems.Add(d);
                }
                else
                {
                    existing.DurationMinutes = d.DurationMinutes;
                    existing.BasePrice = d.BasePrice;
                    existing.Name = d.Name;
                    existing.Description = d.Description;
                    existing.DentistCategoryKey = d.DentistCategoryKey;
                }
            }

            db.SaveChanges();
        }

        private static void SeedUsers(AppDbContext db)
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
                    new User { Email = "patient@oexa.com", Password = "patient123", FirstName = "Demo", LastName = "Patient", Role = "Patient", PhoneNumber = "+355696851089" }
                );
                db.SaveChanges();
            }
        }

        private static void SeedPromotions(AppDbContext db)
        {
            var today = DateTime.Today;

            EnsurePromotion(db, "teeth-whitening", "Teeth Whitening -20%", "Professional whitening package", 20, today, today.AddMonths(2));
            EnsurePromotion(db, "child-dental", "Child Visit -15%", "Pediatric discount", 15, today, today.AddMonths(1));
            EnsurePromotion(db, "general-checkup", "New Client Check-up Offer", "First visit discount on general check-up", 15, today, today.AddMonths(1));
            EnsurePromotion(db, "dental-cleaning", "Spring Cleaning -10%", "Professional cleaning promotion", 10, today, today.AddMonths(2));
        }

        private static void EnsurePromotion(AppDbContext db, string problemKey, string title, string description, int discount, DateTime start, DateTime end)
        {
            var promo = db.Promotions.FirstOrDefault(p => p.ProblemKey == problemKey && p.IsActive);
            if (promo == null)
            {
                db.Promotions.Add(new Promotion
                {
                    Title = title,
                    Description = description,
                    DiscountPercent = discount,
                    StartDate = start,
                    EndDate = end,
                    IsActive = true,
                    TargetAudience = "All patients",
                    ProblemKey = problemKey
                });
            }
            else
            {
                promo.Title = title;
                promo.Description = description;
                promo.DiscountPercent = discount;
                promo.StartDate = start;
                promo.EndDate = end;
                promo.IsActive = true;
                promo.ProblemKey = problemKey;
            }

            db.SaveChanges();
        }

        private static void EnsureDentist(AppDbContext db, string email, string firstName, string lastName, string serviceKey)
        {
            if (db.Users.Any(u => u.Email.ToLower() == email.ToLower())) return;

            db.Users.Add(new User
            {
                Email = email,
                Password = "dentist123",
                FirstName = firstName,
                LastName = lastName,
                Role = "Dentist",
                DentistServiceKey = serviceKey
            });
            db.SaveChanges();
        }
    }
}
