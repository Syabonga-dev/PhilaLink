using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly IProxyService _proxyService;

        public ProxyController(IProxyService proxyService)
        {
            _proxyService = proxyService;
        }

        // Assign proxy
        [HttpPost("assign")]
        public async Task<IActionResult> Assign(Guid patientId, Guid proxyId, Guid nurseId)
        {
            var result = await _proxyService.AssignProxyAsync(patientId, proxyId, nurseId);
            return Ok(result);
        }

        // Get patient proxies
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetPatientProxies(Guid patientId)
        {
            return Ok(await _proxyService.GetPatientProxiesAsync(patientId));
        }

        // Get proxy patients
        [HttpGet("proxy/{proxyId}")]
        public async Task<IActionResult> GetProxyPatients(Guid proxyId)
        {
            return Ok(await _proxyService.GetProxyPatientsAsync(proxyId));
        }

        // Remove proxy link
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            var result = await _proxyService.RemoveProxyAsync(id);
            return Ok(result);
        }
    }
}
