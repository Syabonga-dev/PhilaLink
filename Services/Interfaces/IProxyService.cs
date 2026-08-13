using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IProxyService
    {
        Task<string> AssignProxyAsync(Guid patientId, Guid proxyId, Guid nurseId);
        Task<List<ProxyLink>> GetPatientProxiesAsync(Guid patientId);
        Task<List<ProxyLink>> GetProxyPatientsAsync(Guid proxyId);
        Task<string> RemoveProxyAsync(Guid proxyLinkId);
    }
}


