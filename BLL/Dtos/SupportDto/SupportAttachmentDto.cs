using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.Support
{
    public class SupportAttachmentDto
    {
        public int Id { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}