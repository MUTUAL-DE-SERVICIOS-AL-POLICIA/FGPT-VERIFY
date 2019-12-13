using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.Imaging.Capturing;

namespace DermalogMultiScannerDemo
{
    public class FPScannerZF1 : FPScannerSingleFinger
    {
        public FPScannerZF1(DeviceIdentity id, int index, CaptureMode captureMode = CaptureMode.PREVIEW_IMAGE_AUTO_DETECT)
            : base(id, index, captureMode)
        {
        }

        #region Implementation of abstract methods in base-class
        public override void StartCapturing()
        {
            base.StartCapturing();
        }

        public override void StopCapturing()
        {
            base.StopCapturing();
        }

        public override void setGreenLed(bool enable)
        {
            int value = enable ? 1 : 0;
            base.setDeviceProperty(PropertyType.FG_GREEN_LED, value);
        }

        public override void setRedLed(bool enable)
        {
            int value = enable ? 1 : 0;
            base.setDeviceProperty(PropertyType.FG_RED_LED, value);
        }
        #endregion
    }
}
