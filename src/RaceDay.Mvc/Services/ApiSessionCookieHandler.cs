using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RaceDay.Mvc.Services
{
    /// <summary>
    /// Keeps the API's session cookie in the MVC user's session so API calls are
    /// authorised as that user, instead of relying on the HttpClient handler's
    /// shared (and periodically recycled) cookie jar.
    /// </summary>
    public class ApiSessionCookieHandler : DelegatingHandler
    {
        private const string SessionKey = "ApiCookie";

        private readonly IHttpContextAccessor _accessor;

        public ApiSessionCookieHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var session = _accessor.HttpContext?.Session;

            var cookie = session?.GetString(SessionKey);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (session != null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                // Keep only the name=value part of each cookie.
                var pairs = setCookies
                    .Select(value => value.Split(';')[0])
                    .Where(value => value.Contains('='))
                    .Distinct()
                    .ToList();

                if (pairs.Count > 0)
                {
                    session.SetString(SessionKey, string.Join("; ", pairs));
                }
            }

            return response;
        }
    }
}
