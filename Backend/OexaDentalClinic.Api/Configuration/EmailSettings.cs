namespace OexaDentalClinic.Api.Configuration
{
    public class EmailSettings
    {
        public bool Enabled { get; set; }
        public string FromAddress { get; set; } = "oexadentalclinic@gmail.com";
        public string FromName { get; set; } = "Oexa Dental Clinic";
        /// <summary>Inbox for clinic notifications (reviews, etc.).</summary>
        public string ClinicNotificationEmail { get; set; } = "oexadentalclinic@gmail.com";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "oexadentalclinic@gmail.com";
        public string SmtpPassword { get; set; } = "";
        public int ReminderHoursBefore { get; set; } = 24;
    }
}
