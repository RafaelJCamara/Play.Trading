using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Identity.Contracts;
using Play.Inventory.Contracts;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.Settings;
using Play.Trading.Service.StateMachines;

namespace Play.Trading.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services
                .AddMongo()
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddJwtBearerAuthentication();

            AddMassTransit(services);

            services
                .AddControllers(options =>
            {
                /*
                    because there's a bug in .net, and when we do thins like CreatedAtAction with async method, the async name from the methods gets removed
                    and we don't want this to happen
                 */
                options.SuppressAsyncSuffixInActionNames = false;
            })
                // what this will do is whenever the output response contains null values, they will not be outputed
                .AddJsonOptions(
                        options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                );
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Trading.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Trading.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void AddMassTransit(IServiceCollection services)
        {
            services
                .AddMassTransit(configure =>
                {
                    configure.UsingPlayEconomyRabbitMq(retryConfigurator =>
                    {
                        retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                        retryConfigurator.Ignore(typeof(UnknownItemException));
                    });
                    configure.AddConsumers(Assembly.GetEntryAssembly());
                    configure
                        .AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaConfigurator =>
                        {
                            /*
                                We are performing this configuration here because we just want to send messages to the other participants in the saga whenever we transition to the appropriate state.
                                For example, we want to send message to grant items, only when we are in the accepted state.
                             */
                            sagaConfigurator.UseInMemoryOutbox();
                        })
                        .MongoDbRepository(repository =>
                        {
                            var serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                            var mongoSettings = Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
                            repository.Connection = mongoSettings.ConnectionString;
                            repository.DatabaseName = serviceSettings.ServiceName;
                        });
                });

            var queueSettings = Configuration.GetSection(nameof(QueueSettings)).Get<QueueSettings>();
            /*
                What this is saying is that whenever we want to send a command/message of type GrantItems, we should send it to the queue defined below
             */
            EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
            EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
            EndpointConvention.Map<SubtracItems>(new Uri(queueSettings.SubtractItemsQueueAddress));

            services
                .AddMassTransitHostedService();

            //this is because we use the request client inside the controller
            services
                .AddGenericRequestClient();
        }

    }
}
