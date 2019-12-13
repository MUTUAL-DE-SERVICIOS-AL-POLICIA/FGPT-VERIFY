using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DermalogMultiScannerDemo
{
    public class LocalUser
    {
        public long ID { get; set; }
        public String Name { get; set; }
        public List<Fingerprint> Fingerprints { get; set; }

        public LocalUser()
        {
            Fingerprints = new List<Fingerprint>();
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", Name, ID);
        }
    }
}
