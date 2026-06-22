using LicenseMe.Core.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia;


public partial class ReactiveProgressReporter<T> : ReactiveObject, IProgressReporter<T>
{
    [Reactive]
    private T? _reportedProgress; 
    
    public bool TryUpdateProgress(T progress)
    {
        ReportedProgress = progress;
        return true;
    }
}