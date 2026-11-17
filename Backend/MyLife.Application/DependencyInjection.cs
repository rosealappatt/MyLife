using Microsoft.Extensions.DependencyInjection;

namespace MyLife.Application
{
    /// <summary>
    /// Contains the dependency injection registrations required by the Application layer.
    /// Keeping registrations here prevents Program.cs from becoming cluttered as the application grows.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers Application-layer services.
        /// </summary>
        /*
         * IServiceCollection -> This is the collection where .NET registers services for Dependency Injection.
         * AddApplication() -> This is an extension method. 
         *                     Instead of writing all registrations inside Program.cs, we can write: -> builder.Services.AddApplication();
         * RegisterServicesFromAssembly() -> This tells MediatR where to search for handlers. You won't have to manually register every handler.
         */
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register MediatR and discover all request handlers in the assembly containing this class.
            // Later, when we create LoginCommandHandler, MediatR will discover and register it automatically
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
            return services;
        }

    }
}
