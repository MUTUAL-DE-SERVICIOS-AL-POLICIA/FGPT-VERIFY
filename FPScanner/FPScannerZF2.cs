using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.AFIS.TwoPrintSegmentation;
using Dermalog.Imaging.Capturing;
using Dermalog.Afis.NistQualityCheck;
using System.ComponentModel;

namespace DermalogMultiScannerDemo
{
    public class FPScannerZF2 : FPScanner
    {
        private TwoPrintSegmentation _twoprint;

        public FPScannerZF2(int index)
            : base(DeviceIdentity.FG_ZF2, index)
        {
            try
            {
                _twoprint = new TwoPrintSegmentation();
            }
            catch (Exception e)
            {
                Dispose();
                throw e;
            }
        }

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

        public void setLeds(bool enable)
        {
            setGreenLed(enable);
            setRedLed(enable);
        }

        public void setGreenLed(bool enable)
        {
            int value = enable ? 1 : 0;
            base.setDeviceProperty(PropertyType.FG_GREEN_LED, value);
        }

        public void setRedLed(bool enable)
        {
            int value = enable ? 1 : 0;
            base.setDeviceProperty(PropertyType.FG_RED_LED, value);
        }

        protected override void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                setGreenLed(false);
                setRedLed(true);

                var localImage = e.Argument as Image;
                var fps = new List<Fingerprint>();

                int count = _twoprint.Segmentation(localImage);
                Console.WriteLine("GetSegmentationCount: " + count);
                for (int i = 0; i < count; i++)
                {
                    var segment = _twoprint.GetSegmentedImageEx(i);
                    fps.Add(ProcessImage(segment.Image, segment.ImageData, (uint)segment.Index));
                }

                setGreenLed(true);
                setRedLed(false);

                base.InvokeFingerprintsDetected(fps);
            }
            catch (Exception ex)
            {
                InvokeScannerError(sender, new ScannerErrorEventArgs("Processing error: " + ex.Message, ex));
            }
        }
    }
}
