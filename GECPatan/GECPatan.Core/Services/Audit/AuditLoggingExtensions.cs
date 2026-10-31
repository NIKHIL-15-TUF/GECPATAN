using Microsoft.Extensions.DependencyInjection;

namespace GECPatan.Core.Services.Audit
{
    public static class AuditLoggingExtensions
    {
        /// <param name="source">"Admin" or "Api" - stored on every audit row.</param>
        /// <remarks>
        /// Also attach the interceptor to the DbContext (see Program.cs):
        ///   options.AddInterceptors(sp.GetRequiredService&lt;AuditSaveChangesInterceptor&gt;());
        /// </remarks>
        public static IServiceCollection AddAuditLogging(this IServiceCollection services, string source)
        {
            services.AddSingleton(new AuditOptions { Source = source });
            services.AddSingleton<AuditSaveChangesInterceptor>();
            services.AddScoped<AuditService>();   // manual events (exports, file downloads ...)
            return services;
        }
    }
}
