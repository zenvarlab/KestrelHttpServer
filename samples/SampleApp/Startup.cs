// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(LogLevel.Trace);
            //var logger = loggerFactory.CreateLogger("Default");
            var response = $"hello, world{Environment.NewLine}";
            var responseBytes = Encoding.ASCII.GetBytes(response);
            var responseLength = responseBytes.Length;
            var responseLengthString = responseBytes.Length.ToString();

            app.Run(async context =>
            {
                var connectionFeature = context.Connection;
                //logger.LogDebug($"Peer: {connectionFeature.RemoteIpAddress?.ToString()}:{connectionFeature.RemotePort}"
                //    + $"{Environment.NewLine}"
                //    + $"Sock: {connectionFeature.LocalIpAddress?.ToString()}:{connectionFeature.LocalPort}");

                context.Response.Headers["Content-Length"] = responseLengthString;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(responseBytes, 0, responseLength);
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // options.ThreadCount = 4;
                    options.NoDelay = true;
                    //options.UseHttps("testCert.pfx", "testPassword");
                    //options.UseConnectionLogging();
                })
                .UseUrls("http://*:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            // The following section should be used to demo sockets
            //var addresses = application.GetAddresses();
            //addresses.Clear();
            //addresses.Add("http://unix:/tmp/kestrel-test.sock");

            host.Run();
        }
    }
}