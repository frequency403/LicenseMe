using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Interfaces;

internal interface ILicenseCollectionWriter
{
    void SetLicenses(IReadOnlyCollection<OsiLicense> licenses);
}