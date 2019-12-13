using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using Dermalog.AFIS.FourprintSegmentation;
using Dermalog.Imaging.Capturing;
using Dermalog.Imaging.Capturing.Enums;
using Dermalog.Afis.NistQualityCheck;
using System.ComponentModel;

namespace DermalogMultiScannerDemo
{
    public class FPScannerZF10 : FPScanner
    {
        private FourprintSegmenation _fourprint;

        public FPScannerZF10(int index)
            : base(DeviceIdentity.FG_ZF10, index)
        {
            try
            {
                _fourprint = new FourprintSegmenation();
            }
            catch (Exception e)
            {
                Dispose();
                throw e;
            }
        }

        #region Led methods
        public void SetAllFingerLeds(ZF10LedColor color)
        {
            try
            {
                int leds = (int)ZF10MultiLed.LEFT_LITTLE;
                leds += (int)ZF10MultiLed.LEFT_RING;
                leds += (int)ZF10MultiLed.LEFT_MIDDLE;
                leds += (int)ZF10MultiLed.LEFT_INDEX;
                leds += (int)ZF10MultiLed.LEFT_THUMB;
                leds += (int)ZF10MultiLed.RIGHT_THUMB;
                leds += (int)ZF10MultiLed.RIGHT_INDEX;
                leds += (int)ZF10MultiLed.RIGHT_MIDDLE;
                leds += (int)ZF10MultiLed.RIGHT_RING;
                leds += (int)ZF10MultiLed.RIGHT_LITTLE;
                leds += (int)color;

                base.setDeviceProperty(PropertyType.FG_LEDS, leds);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        public int GetLed(HandPositions hand, uint position)
        {
            int led = 0;
            switch (hand)
            {
                case HandPositions.Left:
                    switch (position)
                    {
                        case 1:
                            led = (int)ZF10MultiLed.LEFT_LITTLE;
                            break;
                        case 2:
                            led = (int)ZF10MultiLed.LEFT_RING;
                            break;
                        case 3:
                            led = (int)ZF10MultiLed.LEFT_MIDDLE;
                            break;
                        case 4:
                            led = (int)ZF10MultiLed.LEFT_INDEX;
                            break;
                        default:
                            break;
                    }
                    break;
                case HandPositions.Right:
                    switch (position)
                    {
                        case 1:
                            led = (int)ZF10MultiLed.RIGHT_INDEX;
                            break;
                        case 2:
                            led = (int)ZF10MultiLed.RIGHT_MIDDLE;
                            break;
                        case 3:
                            led = (int)ZF10MultiLed.RIGHT_RING;
                            break;
                        case 4:
                            led = (int)ZF10MultiLed.RIGHT_LITTLE;
                            break;
                        default:
                            break;
                    }
                    break;
                case HandPositions.Thumbs:
                    switch (position)
                    {
                        case 1:
                            led = (int)ZF10MultiLed.LEFT_THUMB;
                            break;
                        case 2:
                            led = (int)ZF10MultiLed.RIGHT_THUMB;
                            break;
                        default:
                            break;
                    }
                    break;
                case HandPositions.Unknown:
                    break;
                default:
                    break;
            }

            return led;
        }

        public void SetLeds(HandPositions hand, uint[] positions, ZF10LedColor color)
        {
            try
            {
                int leds = 0;

                foreach (uint position in positions)
                {
                    leds += GetLed(hand, position);
                }

                leds += (int)color;

                base.setDeviceProperty(PropertyType.FG_LEDS, leds);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
        #endregion

        #region Implementation of abstract methods in base-class
        public override void StartCapturing()
        {
            base.Start();
        }

        public override void StopCapturing()
        {
            base.Stop();

            SetAllFingerLeds(ZF10LedColor.OFF);
        }

        protected override void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var localImage = e.Argument as Image;

                var fps = new List<Fingerprint>();

                uint count = _fourprint.GetSegmentationCount(localImage);
                if (count == 0)
                {
                    InvokeScannerError(sender, new ScannerErrorEventArgs("Processing error: No finger found in the image",null));
                    return;
                }

                for (uint i = 0; i < count; i++)
                {
                    var finger = _fourprint.GetSegmentedFingerprint(i);
                    fps.Add(ProcessImage(finger.Image, finger.ImageData, finger.Position, finger.Hand));
                }

                SetAllFingerLeds(ZF10LedColor.OFF);

                uint[] positions = new uint[fps.Count];
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = fps[i].Position;
                }
                SetLeds(fps[0].Hand, positions, ZF10LedColor.GREEN);

                base.InvokeFingerprintsDetected(fps);
            }
            catch (Exception ex)
            {
                InvokeScannerError(sender, new ScannerErrorEventArgs("Processing error: " + ex.Message, ex));
            }
        }
        #endregion
    }
}
