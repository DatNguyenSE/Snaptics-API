using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos
{
    public class PaginatedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Size);
    }
}