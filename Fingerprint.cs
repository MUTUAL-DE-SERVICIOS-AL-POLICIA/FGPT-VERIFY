using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.AFIS.FourprintSegmentation;

namespace DermalogMultiScannerDemo
{
    public class Fingerprint : IDisposable
    {
        public Template Template
        {
            get;
            set;
        }

        public Image Image
        {
            get;
            set;
        }

        public int NFIQ2
        {
            get;
            set;
        }

        private HandPositions _hand = HandPositions.Unknown;
        public HandPositions Hand
        {
            get { return _hand; }
            set { _hand = value; }
        }
                
        public uint Position
        {
            get;
            set;
        }

        public void Dispose()
        {
            if(Template != null)
            {
                Template.Dispose();
                Template = null;
            }
        }
    }
}
