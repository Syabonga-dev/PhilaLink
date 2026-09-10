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
            // NOTE: patientId/proxyId/nurseId here are the Patient/Proxy/Nurse
            // *profile* ids (Patient.Id etc.), not User.Id — that's what
            // ProxyLink's foreign keys actually target. Previously this
            // validated against the Users table instead, which meant it
            // either always failed with "Invalid user(s) provided" or threw
            // a foreign-key violation on save depending on what was passed.
            var patient = await _context.Patients.FindAsync(patientId);
            var proxy = await _context.Proxies.FindAsync(proxyId);
            var nurse = await _context.Nurses.FindAsync(nurseId);

            if (patient == null || proxy == null || nurse == null)
                return "Invalid patient, proxy, or nurse id provided.";

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

        public async Task<string> AssignProxyByAdminAsync(Guid patientId, Guid proxyId, Guid adminUserId)
        {
            // adminUserId is the User.Id from the JWT (ClaimTypes.NameIdentifier)
            // — resolved here to the Admin profile id, same pattern as above.
            var patient = await _context.Patients.FindAsync(patientId);
            var proxy = await _context.Proxies.FindAsync(proxyId);
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == adminUserId);

            if (patient == null || proxy == null || admin == null)
                return "Invalid patient/proxy id, or the caller isn't a recognized admin.";

            var exists = await _context.ProxyLinks
                .AnyAsync(p => p.PatientId == patientId && p.ProxyId == proxyId);

            if (exists)
                return "Proxy already assigned";

            var link = new ProxyLink
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                ProxyId = proxyId,
                AssignedByAdminId = admin.Id,
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