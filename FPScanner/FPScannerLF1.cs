using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.Imaging.Capturing;

namespace DermalogMultiScannerDemo
{
    public class FPScannerLF1 : FPScannerSingleFinger
    {
        public FPScannerLF1(int index)
            : base(DeviceIdentity.FG_LF1, index, CaptureMode.PREVIEW_IMAGE_AUTO_DETECT)
        {
        }

        #region Implementation of abstract methods in base-class
        public override void setGreenLed(bool enable)
        {
            // LF1 - no settable green led
        }

        public override void setRedLed(bool enable)
        {
            // LF1 - no settable Ged led
        }
        #endregion
    }
}
