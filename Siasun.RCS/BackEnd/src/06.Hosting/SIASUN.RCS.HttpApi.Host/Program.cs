using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SIASUN.RCS;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            string[] logoLines = new[]
            {
                @"      ___                       ___           ___           ___           ___                    ___           ___           ___     ",
                @"     /\  \          ___        /\  \         /\  \         /\__\         /\__\                  /\  \         /\  \         /\  \    ",
                @"    /::\  \        /\  \      /::\  \       /::\  \       /:/  /        /::|  |                /::\  \       /::\  \       /::\  \   ",
                @"   /:/\ \  \       \:\  \    /:/\:\  \     /:/\ \  \     /:/  /        /:|:|  |               /:/\:\  \     /:/\:\  \     /:/\ \  \  ",
                @"  _\:\~\ \  \      /::\__\  /::\~\:\  \   _\:\~\ \  \   /:/  /  ___   /:/|:|  |__            /::\~\:\  \   /:/  \:\  \   _\:\~\ \  \ ",
                @" /\ \:\ \ \__\  __/:/\/__/ /:/\:\ \:\__\ /\ \:\ \ \__\ /:/__/  /\__\ /:/ |:| /\__\          /:/\:\ \:\__\ /:/__/ \:\__\ /\ \:\ \ \__\",
                @" \:\ \:\ \/__/ /\/:/  /    \/__\:\/:/  / \:\ \:\ \/__/ \:\  \ /:/  / \/__|:|/:/  /          \/_|::\/:/  / \:\  \  \/__/ \:\ \:\ \/__/",
                @"  \:\ \:\__\   \::/__/          \::/  /   \:\ \:\__\    \:\  /:/  /      |:/:/  /              |:|::/  /   \:\  \        \:\ \:\__\  ",
                @"   \:\/:/  /    \:\__\          /:/  /     \:\/:/  /     \:\/:/  /       |::/  /               |:|\/__/     \:\  \        \:\/:/  /  ",
                @"    \::/  /      \/__/         /:/  /       \::/  /       \::/  /        /:/  /                |:|  |        \:\__\        \::/  /   ",
                @"     \/__/                     \/__/         \/__/         \/__/         \/__/                  \|__|         \/__/         \/__/    "
            };

            int[][] colors = new int[][]
            {
                new[] { 255, 248, 220 }, new[] { 255, 235, 160 }, new[] { 255, 215, 0 },
                new[] { 238, 201, 0 },   new[] { 218, 165, 32 },  new[] { 205, 155, 29 },
                new[] { 184, 134, 11 },  new[] { 160, 110, 10 },  new[] { 139, 101, 8 },
                new[] { 110, 75,  5 },   new[] { 80,  50,  0 }
            };

            Console.WriteLine();
            for (int i = 0; i < logoLines.Length; i++)
            {
                Console.WriteLine($"\x1b[38;2;{colors[i][0]};{colors[i][1]};{colors[i][2]}m{logoLines[i]}\x1b[0m");
            }
            Console.WriteLine();

            Log.Information("Starting SIASUN.RCS.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .WriteTo.Async(c => c.AbpStudio(services));
                });
            await builder.AddApplicationAsync<RCSHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
