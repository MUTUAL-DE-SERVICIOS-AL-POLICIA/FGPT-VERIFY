using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.Imaging.Capturing;
using Dermalog.Afis.NistQualityCheck;
using System.ComponentModel;

namespace DermalogMultiScannerDemo
{
    public abstract class FPScannerSingleFinger : FPScanner
    {
        public FPScannerSingleFinger(DeviceIdentity deviceIdentity, int index, CaptureMode captureMode)
            : base(deviceIdentity, index, captureMode)
        {
        }

        public void setLeds(bool enable)
        {
            setGreenLed(enable);
            setRedLed(enable);
        }

        #region SingleFinger-Scanner specific functions to implement
        public abstract void setGreenLed(bool enable);
        public abstract void setRedLed(bool enable);
        #endregion

        #region Implementation of abstract methods in base-class
        public override void StartCapturing()
        {
            base.Start();

            setGreenLed(true);
        }

        public override void StopCapturing()
        {
            base.Stop();

            setLeds(false);
        }

        protected override void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {               
                setGreenLed(false);
                setRedLed(true);

                List<Fingerprint> fps = new List<Fingerprint>();
                fps.Add(ProcessImage(e.Argument as System.Drawing.Image));

                base.InvokeFingerprintsDetected(fps);

                setGreenLed(true);
                setRedLed(false);
            }catch(Exception ex)
            {
                InvokeScannerError(sender, new ScannerErrorEventArgs("Processing error: "+ex.Message, ex));
            }
        }
        #endregion
    }
}
