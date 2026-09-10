namespace PersonalProject.Models.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalNurses { get; set; }
        public int ActiveNurses { get; set; }

        public int TotalProxies { get; set; }
        public int ActiveProxies { get; set; }

        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }

        public int TotalProxyLinks { get; set; }
    }
}