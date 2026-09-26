using System.Net.Mail;
using OCPittem.Functions.Configuration;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

internal static class CookieSaleOrderValidator
{
    private const int MaximumNameLength = 200;
    private const int MaximumEmailLength = 320;

    internal static bool TryValidate(
        CreateCookieSaleCheckoutRequest? request,
        IReadOnlyList<string> allowedClasses,
        out ValidatedCookieSaleOrder? order,
        out string error)
    {
        order = null;

        if (request is null)
        {
            error = "Ongeldig verzoek.";
            return false;
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaximumNameLength)
        {
            error = "Vul een geldige naam in.";
            return false;
        }

        var studentName = request.StudentName?.Trim() ?? string.Empty;
        if (studentName.Length == 0 || studentName.Length > MaximumNameLength)
        {
            error = "Vul een geldige naam van de leerling in.";
            return false;
        }

        var email = request.Email?.Trim() ?? string.Empty;
        if (email.Length == 0
            || email.Length > MaximumEmailLength
            || !MailAddress.TryCreate(email, out var parsedEmail)
            || !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            error = "Vul een geldig e-mailadres in.";
            return false;
        }

        var requestedClass = request.ClassName?.Trim() ?? string.Empty;
        var canonicalClass = allowedClasses.FirstOrDefault(
            className => string.Equals(className, requestedClass, StringComparison.OrdinalIgnoreCase));

        if (canonicalClass is null)
        {
            error = "Kies een geldige klas.";
            return false;
        }

        if (request.CoteDorQuantity < 0 || request.LotusQuantity < 0)
        {
            error = "Aantallen mogen niet negatief zijn.";
            return false;
        }

        var totalPackages = request.CoteDorQuantity + request.LotusQuantity;
        if (totalPackages == 0)
        {
            error = "Kies minstens één pakket.";
            return false;
        }

        if (request.CoteDorQuantity > CookieSale2026Catalog.MaximumTotalPackages
            || request.LotusQuantity > CookieSale2026Catalog.MaximumTotalPackages
            || totalPackages > CookieSale2026Catalog.MaximumTotalPackages)
        {
            error = $"Maximum {CookieSale2026Catalog.MaximumTotalPackages} pakketten per bestelling.";
            return false;
        }

        order = new ValidatedCookieSaleOrder(
            name,
            studentName,
            email,
            canonicalClass,
            request.CoteDorQuantity,
            request.LotusQuantity,
            totalPackages,
            CookieSale2026Catalog.CalculateTotalAmountCents(
                request.CoteDorQuantity,
                request.LotusQuantity));
        error = string.Empty;
        return true;
    }
}
