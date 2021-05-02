using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Filtering;

namespace WebApplication3
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
            services.AddMetrics(builder =>
            {
                builder.Configuration.Configure(opt =>
                {
                    opt.AddAppTag("helloApp");
                    opt.AddEnvTag("dev");
                    opt.AddServerTag("home");
                    opt.Enabled = true;
                    opt.ReportingEnabled = true;
                    opt.GlobalTags.Add("HH", "WW");
                });
                builder.OutputMetrics.AsGrafanaCloudHostedMetricsGraphiteSyntax(TimeSpan.FromSeconds(10));
                builder.Report.ToHostedMetrics(options =>
                {
                    options.HostedMetrics.BaseUri = new Uri("https://graphite-us-central1.grafana.net/metrics");
                    options.HostedMetrics.ApiKey = "id:apiKey";
                    options.HttpPolicy.BackoffPeriod = TimeSpan.FromSeconds(30);
                    options.HttpPolicy.FailuresBeforeBackoff = 5;
                    options.HttpPolicy.Timeout = TimeSpan.FromSeconds(10);
                    options.Filter = new MetricsFilter().WhereType(MetricType.Timer);
                    options.FlushInterval = TimeSpan.FromSeconds(10);
                }).Build();
                
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApplication3", Version = "v1" });
            });

            services.AddMetricsTrackingMiddleware(builder=>
            {
                builder.ApdexTrackingEnabled = true;
                builder.OAuth2TrackingEnabled = true;
                builder.UseBucketHistograms = true;
                builder.ApdexTSeconds = 1.0;
            });
            services.AddMetricsReportingHostedService();
            services.AddMvc().AddMetrics();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApplication3 v1"));
            }
            app.UseMetricsAllMiddleware();
            //app.UseMetricsAllEndpoints();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
