using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Receipt> Receipts => Set<Receipt>();
        public DbSet<ReceiptMedication> ReceiptMedications => Set<ReceiptMedication>();
        public DbSet<TreatmentRecord> TreatmentRecords => Set<TreatmentRecord>();
        public DbSet<DentalProblem> DentalProblems => Set<DentalProblem>();
    }
}
