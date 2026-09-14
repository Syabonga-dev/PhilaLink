using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IProxyService
    {
        Task AssignProxyAsync(Guid patientId, Guid proxyId, Guid performedByUserId);

        Task RemoveProxyAsync(Guid proxyLinkId, Guid performedByUserId);

        Task<List<PatientProxyResponseDto>> GetPatientProxiesAsync(Guid patientId, Guid performedByUserId);

        Task<List<ProxyPatientResponseDto>> GetMyPatientsAsync(Guid proxyUserId);
    }
}