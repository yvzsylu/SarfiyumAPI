using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SarfiyumAPI.Middlewares
{
    public class SessionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userId = context.User.FindFirst("userId")?.Value;
                var sessionId = context.User.FindFirst("SessionId")?.Value;
                var clientSourceStr = context.User.FindFirst("ClientSource")?.Value;

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(clientSourceStr))
                {
                    if (Enum.TryParse<ClientSource>(clientSourceStr, out var clientSource))
                    {
                        using (var scope = context.RequestServices.CreateScope())
                        {
                            var sessionRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserActiveSession>>();

                            // Senin Repository yapına uygun sorgu
                            var currentSession = await sessionRepo
                                .Where(x => x.UserId == userId && x.ClientSource == clientSource)
                                .FirstOrDefaultAsync();

                            if (currentSession == null || currentSession.SessionId != sessionId)
                            {
                                context.Response.StatusCode = 401; // Yetkisiz
                                await context.Response.WriteAsync("Session expired or logged in from another device.");
                                return;
                            }
                        }
                    }
                }
            }
            await _next(context);
        }
    }
}