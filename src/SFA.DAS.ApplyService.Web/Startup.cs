﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.ApplyService.Web.Infrastructure;

namespace SFA.DAS.ApplyService.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;
        private IApplyConfig _applyConfig;

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            _applyConfig = ConfigurationService.GetConfig(_hostingEnvironment, _configuration["EnvironmentName"], _configuration["ConfigurationStorageConnectionString"], "1.0", "SFA.DAS.ApplyService").Result;
            
            services.AddMvc()
                //.AddFluentValidation(fvc => fvc.RegisterValidatorsFromAssemblyContaining<Startup>())
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddJsonOptions(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            services.AddSession(opt => { opt.IdleTimeout = TimeSpan.FromHours(1); });
            
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = _applyConfig.SessionRedisConnectionString;
            });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}