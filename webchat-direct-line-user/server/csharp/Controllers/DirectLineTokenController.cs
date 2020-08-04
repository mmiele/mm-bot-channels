using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TokenApi.Models;
using TokenApi.Services;

namespace TokenApi.Controllers
{
    [ApiController]
    public class DirectLineTokenController : ControllerBase
    {
        private readonly DirectLineTokenService _directLineTokenService;
        private readonly IConfiguration _configuration;

        public DirectLineTokenController(DirectLineTokenService directLineTokenService, IConfiguration configuration)
        {
            _directLineTokenService = directLineTokenService;
            _configuration = configuration;
        }

        // Endpoint for generating a Direct Line token bound to a random user ID
        // For simplicity, uses a CORS policy that allows requests from all origins
        // You should restrict this to specific domains
        [HttpPost]
        [Route("/api/direct-line-token")]
        [EnableCors("AllowAllPolicy")]
        public async Task<IActionResult> Post([FromBody] TokenRequest request)
        {
            if (string.IsNullOrEmpty(request.idToken))
            {
                return BadRequest(new { message = "invalid format for id_token parameter" });
            }

            // Validate ID token
            var validAudience = _configuration["TokenValidationSettings:ValidAudience"];
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
            
            var validatedToken = await ValidateToken(request.idToken, validAudience, configurationManager);
            if (validatedToken == null)
            {
                return BadRequest(new { message = "invalid token" });
            }

            // Extract user ID from ID token
            var userId = GetUserIdFromTokenClaims(validatedToken);
            if (userId == null)
            {
                return BadRequest(new { message = "token does not contain sub claim" });
            }

            // Get user-specific DirectLine token and return it
            var directLineSecret = _configuration["DirectLine:DirectLineSecret"];
            DirectLineTokenDetails directLineTokenDetails;
            try
            {
                directLineTokenDetails = await _directLineTokenService.GenerateTokenAsync(directLineSecret, userId);
            }
            catch (InvalidOperationException invalidOpException)
            {
                return BadRequest(new { message = invalidOpException.Message });
            }

            var response = new
            {
                conversationId = directLineTokenDetails.ConversationId,
                token = directLineTokenDetails.Token,
                expiresIn = directLineTokenDetails.ExpiresIn,
                userId = userId,
            };
            return Ok(response);
        }

        private static async Task<JwtSecurityToken> ValidateToken(
            string token,
            string audience,
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(audience)) throw new ArgumentNullException(nameof(audience));

            var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
            var signingKeys = discoveryDocument.SigningKeys;

            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out var rawValidatedToken);
                
                return (JwtSecurityToken)rawValidatedToken;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is SecurityTokenException)
            {
                return null;
            }
        }

        // Constructs a user ID from the claims in the given token
        // In this sample, we select the "subject" claim
        // Prefixed with "dl_", as required by the Direct Line API
        private static string GetUserIdFromTokenClaims(JwtSecurityToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            var subject = token.Subject;
            return String.IsNullOrEmpty(subject) ? null : $"dl_{subject}";
        }
    
        public class TokenRequest
        {
            [JsonPropertyName("id_token")]
            public string idToken { get; set; }
        }
    }
}
