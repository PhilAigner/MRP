using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MRP
{
    /// <summary>
    /// Service for managing authentication tokens
    /// </summary>
    public sealed class TokenService
    {
        private static readonly ConcurrentDictionary<string, Guid> _tokens = new ConcurrentDictionary<string, Guid>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Generates a token for a user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="userId">User ID</param>
        /// <returns>Generated token string</returns>
        public string GenerateToken(string username, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            // Check if user already has a token
            var existingToken = _tokens.FirstOrDefault(kvp => kvp.Value == userId).Key;
            if (existingToken != null)
            {
                // Return existing token
                return existingToken;
            }

            // Generate new token: username-mrpToken
            string token = $"{username}-{new Guid()}-token";
            
            lock (_lock)
            {
                _tokens[token] = userId;
            }

            return token;
        }

        /// <summary>
        /// Validates a token and returns the associated user ID
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>User ID if token is valid, null otherwise</returns>
        public Guid? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            if (_tokens.TryGetValue(token, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Extracts the Bearer token from the Authorization header
        /// </summary>
        /// <param name="authHeader">Authorization header value</param>
        /// <returns>Token string without "Bearer " prefix</returns>
        public string? ExtractBearerToken(string? authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            const string bearerPrefix = "Bearer ";
            if (authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(bearerPrefix.Length).Trim();
            }

            return null;
        }

        /// <summary>
        /// Removes a token (logout)
        /// </summary>
        /// <param name="token">Token to remove</param>
        /// <returns>True if token was removed, false if not found</returns>
        public bool RevokeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return _tokens.TryRemove(token, out _);
        }

        /// <summary>
        /// Removes all tokens for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of tokens removed</returns>
        public int RevokeAllUserTokens(Guid userId)
        {
            var userTokens = _tokens.Where(kvp => kvp.Value == userId).Select(kvp => kvp.Key).ToList();
            int count = 0;

            foreach (var token in userTokens)
            {
                if (_tokens.TryRemove(token, out _))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets all active tokens (for debugging/admin purposes)
        /// </summary>
        /// <returns>Dictionary of tokens and user IDs</returns>
        public Dictionary<string, Guid> GetAllTokens()
        {
            return new Dictionary<string, Guid>(_tokens);
        }
    }
}
