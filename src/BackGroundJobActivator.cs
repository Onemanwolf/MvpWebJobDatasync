using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
namespace src {
public class BackGroundActivator : IJobActivator
{
    private readonly IServiceProvider _serviceProvider;
    public BackGroundActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public T CreateInstance<T>()
    {
         object instance = _serviceProvider.GetRequiredService(typeof(T));
            return (T)instance;
    }
}
}