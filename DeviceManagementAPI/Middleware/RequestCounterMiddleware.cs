using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
namespace DeviceManagementAPI.Middleware
{




    public class RequestCounterMiddleware
    {
        private readonly RequestDelegate _next;

        // Thread-safe dictionary for counting requests per endpoint
        private static ConcurrentDictionary<string, int> _requestCounts = new ConcurrentDictionary<string, int>();

        public RequestCounterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.ToString().ToLower();

            // Increment counter
            _requestCounts.AddOrUpdate(path, 1, (key, oldValue) => oldValue + 1);

            Console.WriteLine($"Endpoint {path} has been called {_requestCounts[path]} times.");

            // Call the next middleware in the pipeline
            await _next(context);
        }

        // Optional: method to get counts
        public static ConcurrentDictionary<string, int> GetRequestCounts() => _requestCounts;
    }
}
