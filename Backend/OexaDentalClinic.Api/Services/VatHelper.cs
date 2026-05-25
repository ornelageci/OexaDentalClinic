namespace OexaDentalClinic.Api.Services
{
    public static class VatHelper
    {
        public const decimal Rate = 0.20m;
        public const int RatePercent = 20;

        public static (decimal Subtotal, decimal Vat, decimal Total) FromSubtotal(decimal subtotal)
        {
            subtotal = Math.Round(subtotal, 2);
            var vat = Math.Round(subtotal * Rate, 2);
            return (subtotal, vat, subtotal + vat);
        }

        public static (decimal Subtotal, decimal Vat, decimal Total) FromTotalIncludingVat(decimal total)
        {
            total = Math.Round(total, 2);
            var subtotal = Math.Round(total / (1 + Rate), 2);
            var vat = Math.Round(total - subtotal, 2);
            return (subtotal, vat, total);
        }

        public static void ApplyToReceipt(Models.Receipt receipt, decimal lineSubtotal)
        {
            var (subtotal, vat, total) = FromSubtotal(lineSubtotal);
            receipt.SubtotalBeforeVat = subtotal;
            receipt.VatAmount = vat;
            receipt.TotalAmount = total;
        }
    }
}
