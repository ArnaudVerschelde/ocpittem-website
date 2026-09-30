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

        var orderType = request.OrderType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(orderType))
            orderType = CookieSale2026Catalog.StudentOrderType;

        if (orderType is not CookieSale2026Catalog.StudentOrderType
            and not CookieSale2026Catalog.StaffOrderType
            and not CookieSale2026Catalog.SupporterOrderType)
        {
            error = "Kies of je bestelt voor een leerling, als personeelslid of als sympathisant.";
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

        string? studentName = null;
        string? canonicalClass = null;
        string? canonicalStaffCategory = null;
        if (orderType == CookieSale2026Catalog.StudentOrderType)
        {
            studentName = request.StudentName?.Trim() ?? string.Empty;
            if (studentName.Length == 0 || studentName.Length > MaximumNameLength)
            {
                error = "Vul een geldige naam van de leerling in.";
                return false;
            }

            var requestedClass = request.ClassName?.Trim() ?? string.Empty;
            canonicalClass = allowedClasses.FirstOrDefault(
                className => string.Equals(className, requestedClass, StringComparison.OrdinalIgnoreCase));
            if (canonicalClass is null)
            {
                error = "Kies een geldige klas.";
                return false;
            }
        }
        else if (orderType == CookieSale2026Catalog.StaffOrderType)
        {
            var requestedStaffCategory = request.StaffCategory?.Trim() ?? string.Empty;
            canonicalStaffCategory = CookieSale2026Catalog.StaffCategories.FirstOrDefault(
                category => string.Equals(
                    category,
                    requestedStaffCategory,
                    StringComparison.OrdinalIgnoreCase));
            if (canonicalStaffCategory is null)
            {
                error = "Kies een geldige personeelsgroep.";
                return false;
            }
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
            orderType,
            studentName,
            email,
            canonicalClass,
            canonicalStaffCategory,
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
