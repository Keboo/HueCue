using System.Windows.Threading;

namespace HueCue;

public sealed class SystemDispatcher : IDispatcher
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    public T Invoke<T>(Func<T> func)
    {
        return _dispatcher.Invoke(func);
    }
}
