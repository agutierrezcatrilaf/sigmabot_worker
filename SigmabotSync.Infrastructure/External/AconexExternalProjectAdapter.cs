using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Interfaces;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>Adaptador de <see cref="IExternalApiClient"/> sobre <see cref="AconexProjectClient"/>.</summary>
    public sealed class AconexExternalProjectAdapter : IExternalApiClient
    {
        private readonly AconexProjectClient _client;

        public AconexExternalProjectAdapter(string username, string password, string integrationId)
        {
            _client = new AconexProjectClient(username, password, integrationId);
        }

        public Task<List<Project>> GetProjectsAsync()
        {
            return _client.GetUserProjectsAsync();
        }
    }
}
