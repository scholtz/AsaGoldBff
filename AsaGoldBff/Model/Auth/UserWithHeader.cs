using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace AsaGoldBff.Model.Auth
{
    public class UserWithHeader
    {
        /// <summary>
        /// For testing purposes the empty constructor
        /// </summary>
        public UserWithHeader()
        {
        }

        /// <summary>
        /// This constructor must be primarly used in controllers
        /// </summary>
        /// <param name="user"></param>
        /// <param name="request"></param>
        /// <exception cref="Exception"></exception>
        public UserWithHeader(ClaimsPrincipal user, HttpRequest request)
        {
            if (string.IsNullOrEmpty(user?.Identity?.Name)) throw new Exception("Unauthorized");
            if (request.Headers == null) throw new Exception("request.Headers is null");
            this.Name = user.Identity.Name;
            request.Headers.TryGetValue("Authorization", out StringValues headerValue);
            Header = headerValue.ToString();
            if (string.IsNullOrEmpty(Header)) throw new Exception("headerValue is null");
        }
        public string Name { get; set; }
        public string Header { get; set; }
    }
}
