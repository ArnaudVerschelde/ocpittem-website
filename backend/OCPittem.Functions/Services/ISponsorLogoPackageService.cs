namespace OCPittem.Functions.Services;

public interface ISponsorLogoPackageService
{
    Task<byte[]> CreateLogosZipAsync();
}
