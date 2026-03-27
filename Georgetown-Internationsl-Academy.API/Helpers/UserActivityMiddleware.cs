using Georgetown_International_Academy.API.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Azure.Core.HttpHeader;

namespace Georgetown_Internationsl_Academy.API.Helpers
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.ToString().Contains("Common"))
                return;
            if (context.Request.Path.ToString().Contains("GetEmployeeNotification"))
                return;
            if (context.Request.Path.ToString().Contains("GetSystemParameters"))
                return;


            var dbContext = context.RequestServices
                                   .GetRequiredService<ApplicationDBContext>();

            var email = context.User?.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                dbContext.UserLastActivity.Add(new Models.UserLastActivity
                {
                    Email = email,
                    LastAccessed = DateTime.Now,
                    Endpoint = context.Request.Path,
                    Method = context.Request.Method
                });

                await dbContext.SaveChangesAsync();
            }

            await _next(context);
        }
    }
}