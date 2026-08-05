using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Infrastructure.External
{
    public abstract class AconexClientBase
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _integrationId;

        protected HttpClient CreateClient()
        {
            var client = new HttpClient();
            var auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_username}:{_password}")
            );

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            client.DefaultRequestHeaders.Add("X-Application-Key", _integrationId);

            return client;
        }

        protected AconexClientBase(string username, string password, string integrationId)
        {
            _username = username;
            _password = password;
            _integrationId = integrationId;
        }
    }

}
