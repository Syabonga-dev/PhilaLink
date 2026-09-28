using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IProxyService
    {
        // =====================================================
        // PROXY PROFILE
        // =====================================================

        Task<ProxyMeDto> GetMeAsync(
            Guid proxyUserId
        );

        Task<ProxyMeDto> UpdateMeAsync(
            Guid proxyUserId,
            UpdateProxyProfileDto dto
        );

        // =====================================================
        // ASSIGN PROXY
        // =====================================================

        Task AssignProxyAsync(
            Guid patientId,
            Guid proxyId,
            Guid performedByUserId
        );

        // =====================================================
        // REMOVE PROXY
        // =====================================================

        Task RemoveProxyAsync(
            Guid proxyLinkId,
            Guid performedByUserId
        );

        // =====================================================
        // GET PATIENT PROXIES
        // =====================================================

        Task<List<PatientProxyResponseDto>>
            GetPatientProxiesAsync(
                Guid patientId,
                Guid performedByUserId
            );

        // =====================================================
        // GET CURRENT PROXY PATIENTS
        // =====================================================

        Task<List<ProxyPatientResponseDto>>
            GetMyPatientsAsync(
                Guid proxyUserId
            );

        // =====================================================
        // PATIENT'S ASSIGNED PROXY
        // =====================================================

        Task<PatientAssignedWorkerDto?>
            GetMyAssignedWorkerAsync(
                Guid patientUserId
            );
    }
}