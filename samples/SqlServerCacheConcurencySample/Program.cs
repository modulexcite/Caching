// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Caching.SqlServer;
using Microsoft.Framework.Logging;

namespace SqlServerCacheConcurencySample
{
    /// <summary>
    /// This sample requires setting up a Microsoft SQL Server based cache database.
    /// 1. Create a new database or use as existing gone.
    /// 2. Run the command "dnx . install-sqlservercache <connectionstring-here> <name-of-table-to-be-created>"
    ///    to setup the table.
    /// </summary>
    public class Program
    {
        private const string Key = "MyKey";
        private static readonly Random Random = new Random();
        private DistributedCacheEntryOptions _cacheEntryOptions;

        public void Main()
        {
            _cacheEntryOptions = new DistributedCacheEntryOptions();
            _cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(10));

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            var cache = new SqlServerCache(new SqlServerCacheOptions()
            {
                ConnectionString = "Server=localhost;Database=ASPNET5CacheTest;Trusted_Connection=True;",
                TableName = "ASPNET5Cache",
                ExpirationScanFrequency = TimeSpan.FromSeconds(30)
            }, loggerFactory);

            SetKey(cache, "0");

            PriodicallyReadKey(cache, TimeSpan.FromSeconds(1));

            PeriodciallyRemoveKey(cache, TimeSpan.FromSeconds(11));

            PeriodciallySetKey(cache, TimeSpan.FromSeconds(13));

            Console.ReadLine();
            Console.WriteLine("Shutting down");
        }

        private void SetKey(IDistributedCache cache, string value)
        {
            Console.WriteLine("Setting: " + value);
            cache.Set(Key, Encoding.UTF8.GetBytes(value), _cacheEntryOptions);
        }

        private void PeriodciallySetKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    SetKey(cache, "A");
                }
            });
        }

        private void PriodicallyReadKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    if (Random.Next(3) == 0) // 1/3 chance
                    {
                        // Allow values to expire due to sliding refresh.
                        Console.WriteLine("Read skipped, random choice.");
                    }
                    else
                    {
                        Console.Write("Reading...");
                        object result = cache.Get(Key);
                        if (result != null)
                        {
                            cache.Set(Key, Encoding.UTF8.GetBytes("B"), _cacheEntryOptions);
                        }
                        Console.WriteLine("Read: " + (result ?? "(null)"));
                    }
                }
            });
        }

        private void PeriodciallyRemoveKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    Console.WriteLine("Removing...");
                    cache.Remove(Key);
                }
            });
        }
    }
}