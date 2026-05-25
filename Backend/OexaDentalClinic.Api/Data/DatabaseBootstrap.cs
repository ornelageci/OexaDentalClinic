using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Models;
using OexaDentalClinic.Api.Services;

namespace OexaDentalClinic.Api.Data
{
    /// <summary>
    /// Ensures schema repairs run when a migration was missed on Railway (e.g. manual migration without Designer).
    /// </summary>
    public static class DatabaseBootstrap
    {
        private const string DurationMigrationId = "20260522183000_AddTreatmentDuration";
        private const string TreatmentLinesMigrationId = "20260523140000_AddAppointmentTreatments";

        public static void ApplyMigrationsAndRepairs(AppDbContext db, ILogger logger)
        {
            logger.LogInformation("Applying database migrations...");
            db.Database.Migrate();
            logger.LogInformation("EF migrations applied.");

            EnsureDurationMinutesColumn(db, logger);
            EnsureAppointmentTreatmentsTable(db, logger);
            EnsureRoleAndCategoryTables(db, logger);
        }

        private static void EnsureRoleAndCategoryTables(AppDbContext db, ILogger logger)
        {
            var rolesTable = db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'UserRoleDefinitions'
                """
            ).AsEnumerable().FirstOrDefault();

            if (rolesTable == 0)
            {
                logger.LogWarning("UserRoleDefinitions table missing — applying schema repair.");
                db.Database.ExecuteSqlRaw(
                    """
                    CREATE TABLE `UserRoleDefinitions` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `Key` varchar(30) NOT NULL,
                        `DisplayName` varchar(80) NOT NULL,
                        `SortOrder` int NOT NULL,
                        `IsSystem` tinyint(1) NOT NULL,
                        PRIMARY KEY (`Id`)
                    )
                    """);
            }

            var catTable = db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'DentistCategories'
                """
            ).AsEnumerable().FirstOrDefault();

            if (catTable == 0)
            {
                logger.LogWarning("DentistCategories table missing — applying schema repair.");
                db.Database.ExecuteSqlRaw(
                    """
                    CREATE TABLE `DentistCategories` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `Key` varchar(50) NOT NULL,
                        `DisplayName` varchar(100) NOT NULL,
                        `SortOrder` int NOT NULL,
                        PRIMARY KEY (`Id`)
                    )
                    """);
            }

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

        private static void EnsureAppointmentTreatmentsTable(AppDbContext db, ILogger logger)
        {
            var tableExists = db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'AppointmentTreatments'
                """
            ).AsEnumerable().FirstOrDefault();

            if (tableExists == 0)
            {
                logger.LogWarning("AppointmentTreatments table missing — applying schema repair.");
                db.Database.ExecuteSqlRaw(
                    """
                    CREATE TABLE `AppointmentTreatments` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `AppointmentId` int NOT NULL,
                        `ProblemKey` varchar(80) NOT NULL,
                        `AssignedDentistUserId` int NULL,
                        `ScheduledStart` datetime(6) NOT NULL,
                        `DurationMinutes` int NOT NULL,
                        PRIMARY KEY (`Id`)
                    )
                    """);
                db.Database.ExecuteSqlRaw(
                    """
                    INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                    VALUES ({0}, '10.0.2')
                    """,
                    TreatmentLinesMigrationId);
            }

            BackfillAppointmentTreatmentLines(db, logger);
        }

        private static void BackfillAppointmentTreatmentLines(AppDbContext db, ILogger logger)
        {
            var appts = db.Appointments.AsNoTracking().ToList();
            var existing = db.AppointmentTreatments.Select(t => t.AppointmentId).Distinct().ToHashSet();
            var problems = db.DentalProblems.AsNoTracking().ToDictionary(p => p.Key, p => p, StringComparer.OrdinalIgnoreCase);
            var added = 0;

            foreach (var appt in appts)
            {
                if (existing.Contains(appt.Id)) continue;

                var keys = AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded);
                var orderedKeys = keys
                    .Select(k => problems.TryGetValue(k, out var p) ? p : null)
                    .Where(p => p != null)
                    .OrderBy(p => p!.DurationMinutes > 0 ? p!.DurationMinutes : 60)
                    .Select(p => p!.Key)
                    .Concat(keys.Where(k => !problems.ContainsKey(k)))
                    .ToList();

                var current = appt.PreferredDateTime;
                foreach (var key in orderedKeys)
                {
                    var duration = 60;
                    DentalProblem? prob = null;
                    if (problems.TryGetValue(key, out prob))
                        duration = prob.DurationMinutes > 0 ? prob.DurationMinutes : 60;

                    db.AppointmentTreatments.Add(new AppointmentTreatment
                    {
                        AppointmentId = appt.Id,
                        ProblemKey = prob?.Key ?? key,
                        AssignedDentistUserId = keys.Count == 1 ? appt.AssignedDentistUserId : null,
                        ScheduledStart = current,
                        DurationMinutes = duration
                    });
                    current = current.AddMinutes(duration);
                    added++;
                }
            }

            if (added > 0)
            {
                db.SaveChanges();
                logger.LogInformation("Backfilled {Count} appointment treatment lines.", added);
            }
        }
    }
}
