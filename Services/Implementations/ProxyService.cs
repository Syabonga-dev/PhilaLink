using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class ProxyService : IProxyService
    {
        private readonly PhilaLinkDbContext _context;

        public ProxyService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<string> AssignProxyAsync(Guid patientId, Guid proxyId, Guid nurseId)
        {
            var patient = await _context.Users.FindAsync(patientId);
            var proxy = await _context.Users.FindAsync(proxyId);
            var nurse = await _context.Users.FindAsync(nurseId);

            if (patient == null || proxy == null || nurse == null)
                return "Invalid user(s) provided";

            var exists = await _context.ProxyLinks
                .AnyAsync(p => p.PatientId == patientId && p.ProxyId == proxyId);

            if (exists)
                return "Proxy already assigned";

            var link = new ProxyLink
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                ProxyId = proxyId,
                AssignedByNurseId = nurseId,
                AssignedAt = DateTime.UtcNow
            };

            _context.ProxyLinks.Add(link);
            await _context.SaveChangesAsync();

            return "Proxy assigned successfully";
        }

        public async Task<List<ProxyLink>> GetPatientProxiesAsync(Guid patientId)
        {
            return await _context.ProxyLinks
                .Include(p => p.Proxy)
                .Where(p => p.PatientId == patientId)
                .ToListAsync();
        }


        public async Task<List<ProxyLink>> GetProxyPatientsAsync(Guid proxyId)
        {
            return await _context.ProxyLinks
                .Include(p => p.Patient)
                .Where(p => p.ProxyId == proxyId)
                .ToListAsync();
        }

        public async Task<string> RemoveProxyAsync(Guid proxyLinkId)
        {
            var link = await _context.ProxyLinks.FindAsync(proxyLinkId);

            if (link == null)
                return "Proxy link not found";

            _context.ProxyLinks.Remove(link);
            await _context.SaveChangesAsync();

            return "Proxy removed successfully";
        }
    }
}

