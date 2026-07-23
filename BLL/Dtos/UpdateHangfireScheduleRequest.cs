using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos
{
    public class UpdateHangfireScheduleRequest
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
}
