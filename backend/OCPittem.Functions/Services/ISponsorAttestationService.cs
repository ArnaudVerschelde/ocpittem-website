namespace OCPittem.Functions.Services;

public interface ISponsorAttestationService
{
    Task<byte[]> GenerateAttestationAsync(
        string companyName,
        string street,
        string houseNumber,
        string postalCode,
        string city,
        string enterpriseNumber,
        decimal amount,
        DateTime date);
}
