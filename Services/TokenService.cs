using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MRP
{

    public sealed class TokenService
    {
        private static readonly ConcurrentDictionary<string, Guid> _tokens = new ConcurrentDictionary<string, Guid>();

        public string GenerateToken(string username, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            // Check if user already has a token
            var existingToken = _tokens.FirstOrDefault(kvp => kvp.Value == userId).Key;
            if (existingToken != null)
                return existingToken;

            // Generate new token
            string token = $"{username}-{Guid.NewGuid()}-{Guid.NewGuid()}-token";
            
            _tokens[token] = userId;
            
            return token;
        }


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


        public bool RevokeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return _tokens.TryRemove(token, out _);
        }


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

        public Dictionary<string, Guid> GetAllTokens()
        {
            return new Dictionary<string, Guid>(_tokens);
        }
    }
}
