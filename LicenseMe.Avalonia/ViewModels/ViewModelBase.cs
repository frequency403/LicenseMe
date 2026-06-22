using System.Reactive.Disposables;
using ReactiveUI;

namespace LicenseMe.Avalonia.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    protected readonly CompositeDisposable Disposables = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
