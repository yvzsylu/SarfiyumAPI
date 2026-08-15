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
                        // Eskiden burada her authenticated istekte bir DB sorgusu vardı
                        // (AGENTS.md P5). Artık önbellekli servis üzerinden okunuyor;
                        // önbellek login/logout anında noktasal olarak güncellendiği için
                        // tek cihaz kuralı aynı anındalıkta çalışmaya devam ediyor.
                        var sessionCache = context.RequestServices.GetRequiredService<IActiveSessionCache>();
                        var currentSessionId = await sessionCache.GetSessionIdAsync(userId, clientSource);

                        if (currentSessionId == null || currentSessionId != sessionId)
                        {
                            context.Response.StatusCode = 401; // Yetkisiz
                            await context.Response.WriteAsync("Session expired or logged in from another device.");
                            return;
                        }
                    }
                }
            }
            await _next(context);
        }
    }
}