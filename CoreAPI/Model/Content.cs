using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class Content
    {
        public int id { get; set; }

        public string title { get; set; }
        public string content { get; set; }

        public int status { get; set; }

        public DateTime addTime { get; set; } = DateTime.Now;

        public DateTime? modifyTime { get; set; } = DateTime.Now;
    }
}
