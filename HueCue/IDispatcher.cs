namespace HueCue;

public interface IDispatcher
{
    T Invoke<T>(Func<T> func);
}
