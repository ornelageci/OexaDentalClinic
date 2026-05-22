using Microsoft.EntityFrameworkCore;

namespace OexaDentalClinic.Api.Data
{
    /// <summary>
    /// Ensures schema repairs run when a migration was missed on Railway (e.g. manual migration without Designer).
    /// </summary>
    public static class DatabaseBootstrap
    {
        private const string DurationMigrationId = "20260522183000_AddTreatmentDuration";

        public static void ApplyMigrationsAndRepairs(AppDbContext db, ILogger logger)
        {
            logger.LogInformation("Applying database migrations...");
            db.Database.Migrate();
            logger.LogInformation("EF migrations applied.");

            EnsureDurationMinutesColumn(db, logger);
        }

        private static void EnsureDurationMinutesColumn(AppDbContext db, ILogger logger)
        {
            var columnExists = db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'DentalProblems'
                  AND COLUMN_NAME = 'DurationMinutes'
                """
            ).AsEnumerable().FirstOrDefault();

            if (columnExists > 0)
                return;

            logger.LogWarning("DurationMinutes column missing — applying schema repair.");

            db.Database.ExecuteSqlRaw(
                "ALTER TABLE `DentalProblems` ADD COLUMN `DurationMinutes` int NOT NULL DEFAULT 60");

            db.Database.ExecuteSqlRaw(
                """
                INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                VALUES ({0}, '10.0.2')
                """,
                DurationMigrationId);

            logger.LogInformation("DurationMinutes column added successfully.");
        }
    }
}
