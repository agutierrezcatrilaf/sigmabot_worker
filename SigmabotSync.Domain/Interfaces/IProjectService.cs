using SigmabotSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Interfaces
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjectsAsync();
    }
}
