using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SigmabotSync.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IExternalApiClient _apiClient;

        public ProjectService(IExternalApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task<List<Project>> GetProjectsAsync()
        {
            return _apiClient.GetProjectsAsync();
        }
    }
}
