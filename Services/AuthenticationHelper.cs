using System;
using System.Net;

namespace MRP
{
    /// <summary>
    /// Helper class for authentication checks in endpoints
    /// </summary>
    public static class AuthenticationHelper
    {
        /// <summary>
        /// Validates the authentication token from the request
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="tokenService">Token service instance</param>
        /// <returns>User ID if authenticated, null otherwise</returns>
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

        /// <summary>
        /// Checks if the request is authenticated and returns an error response if not
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="response">HTTP response</param>
        /// <param name="tokenService">Token service instance</param>
        /// <param name="userId">Output parameter for authenticated user ID</param>
        /// <returns>True if authenticated, false otherwise</returns>
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

        /// <summary>
        /// Sends an unauthorized response
        /// </summary>
        public static async System.Threading.Tasks.Task SendUnauthorizedResponse(HttpListenerResponse response)
        {
            await HttpServer.Json(response, 401, new { error = "Unauthorized. Please provide a valid Bearer token." });
        }

        /// <summary>
        /// Sends a forbidden response
        /// </summary>
        public static async System.Threading.Tasks.Task SendForbiddenResponse(HttpListenerResponse response)
        {
            await HttpServer.Json(response, 403, new { error = "Forbidden. You don't have permission to access this resource." });
        }
    }
}
