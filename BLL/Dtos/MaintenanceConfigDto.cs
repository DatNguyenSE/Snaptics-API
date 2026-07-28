using System;

namespace BLL.Dtos
{
    public class MaintenanceConfigDto
    {
        public bool IsMaintenance { get; set; }
        public string? Message { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
