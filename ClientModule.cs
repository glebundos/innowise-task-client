using innowise_task_server;
namespace innowise_task_client
{
    public static class ClientModule
    {
        public static IServiceCollection AddClientModule(this IServiceCollection services)
        {
            services.AddServerModule();
            return services;
        }
    }
}
