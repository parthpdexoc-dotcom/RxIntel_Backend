namespace Rxintel.Generic.Services.Interfaces;

public interface IBackgroundTaskRunner
{
    void Enqueue(Func<IServiceProvider, Task> work);
}
