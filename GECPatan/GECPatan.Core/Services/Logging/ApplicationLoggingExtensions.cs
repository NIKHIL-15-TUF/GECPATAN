using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GECPatan.Core.Services.Logging
{
    public static class ApplicationLoggingExtensions
    {
        /// <summary>
        /// Stores ILogger output (Warning and above by default) in the ApplicationLogs table.
        /// Needs services.AddHttpContextAccessor() to be registered too.
        /// </summary>
        /// <param name="source">"Admin" or "Api".</param>
        /// <param name="configure">Usually: o => builder.Configuration.GetSection("ApplicationLogging").Bind(o)</param>
        /// <param name="runRetention">true in ONE app only (Admin): daily purge of old logs.</param>
        public static IServiceCollection AddApplicationLogging(this IServiceCollection services, string source,
            Action<ApplicationLogOptions>? configure = null, bool runRetention = false)
        {
            var options = new ApplicationLogOptions { Source = source };
            configure?.Invoke(options);
            options.Source = source;   // config can't override which app this is

            services.AddSingleton(options);
            services.AddSingleton<ApplicationLogQueue>();
            services.AddHostedService<ApplicationLogWriter>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DbLoggerProvider>());
            if (runRetention) services.AddHostedService<LogRetentionService>();
            return services;
        }
    }
}
