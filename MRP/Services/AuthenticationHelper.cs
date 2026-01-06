using System.Net;

namespace MRP
{
    public static class AuthenticationHelper
    {
        public static Guid? ValidateRequest(HttpListenerRequest request, TokenService tokenService)
        {
            var authHeader = request.Headers["Authorization"];
            
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            var token = tokenService.ExtractBearerToken(authHeader);
            
            if (token == null)
                return null;

            return tokenService.ValidateToken(token);
        }

        
        public static bool RequireAuthentication(HttpListenerRequest request, HttpListenerResponse response, TokenService tokenService, out Guid userId)
        {
            userId = Guid.Empty;
            
            var validatedUserId = ValidateRequest(request, tokenService);
            
            if (validatedUserId == null)
            {
                return false;
            }

            userId = validatedUserId.Value;
            return true;
        }

        public static async Task SendUnauthorizedResponse(HttpListenerResponse response)
        {
            await HttpServer.Json(response, 401, new { error = "Unauthorized. Please provide a valid Bearer token." });
        }

        public static async Task SendForbiddenResponse(HttpListenerResponse response)
        {
            await HttpServer.Json(response, 403, new { error = "Forbidden. You don't have permission to access this resource." });
        }
    }
}
