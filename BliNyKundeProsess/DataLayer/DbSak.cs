using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class DbSak
    {
        public int Saksnummer { get; set; }
        public int Sakstype { get; set; }
        public string Kundenummer { get; set; }
        public string Kundenavn { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
