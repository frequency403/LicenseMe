namespace LicenseMe.Core.Interfaces;

public interface IProgressReporter<T>
{
    T ReportedProgress { get; }
    bool TryUpdateProgress(T progress);
}