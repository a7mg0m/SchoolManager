using BuildingBlocks.Messaging.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging
{
    public static class MessagingServiceCollectionExtensions
    {
        /// <summary>Registers the Kafka producer (<see cref="IEventPublisher"/>) using the "Kafka" settings.</summary>
        public static IServiceCollection AddKafkaPublisher(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
            return services;
        }

        /// <summary>
        /// Consumes <typeparamref name="TEvent"/> from <paramref name="topic"/> in the background and handles
        /// each message with <typeparamref name="THandler"/> (scoped). Uses the "Kafka" settings (GroupId required).
        /// </summary>
        public static IServiceCollection AddKafkaConsumer<TEvent, THandler>(
            this IServiceCollection services, IConfiguration configuration, string topic)
            where TEvent : IntegrationEvent
            where THandler : class, IIntegrationEventHandler<TEvent>
        {
            services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
            services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
            services.AddSingleton<IHostedService>(sp => new KafkaConsumerService<TEvent>(
                topic,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptions<KafkaSettings>>(),
                sp.GetRequiredService<ILogger<KafkaConsumerService<TEvent>>>()));
            return services;
        }
    }
}
