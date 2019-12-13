using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using Dermalog.Afis.NistQualityCheck;
using Dermalog.Imaging.Capturing;
using Dermalog.Imaging.Capturing.Enums;
using System.Collections;
using System.ComponentModel;
using Dermalog.Afis.ImageContainer;
using Dermalog.Afis.FingerCode3;

namespace DermalogMultiScannerDemo
{
    public abstract class FPScanner : IDisposable
    {
        #region Fields
        public Device Device { get; internal set; }
        public DeviceIdentity DeviceIdentity { get; internal set; }
        public Dermalog.Afis.FingerCode3.Encoder Encoder { get; internal set; }

        CaptureMode captureMode;
        NativeInterfaceVersion nativeInterfaceVersion;

        protected BackgroundWorker worker = new BackgroundWorker();

        #endregion

        #region Event declarations


        public class ScannerErrorEventArgs : EventArgs
        {
            public string Error { get; internal set; }
            public Exception Exception { get; internal set; }            

            internal ScannerErrorEventArgs(string error, Exception e)
            {
                Error = error;
                Exception = e;                
            }

            internal ScannerErrorEventArgs(ErrorEventArgs error)
            {
                Error = error.Error;
                Exception = error.InternalException;
            }
        }

        public delegate void FingerprintsReceivedEventHandler(List<Fingerprint> fingerprints);
        public delegate void OnScannerImageEventHandler(Image image);
        public delegate void OnScannerDetectEventHandler(Image image);
        public delegate void OnScannerErrorEventHandler(object sender, ScannerErrorEventArgs e);

        public event FingerprintsReceivedEventHandler OnFingerprintsDetected;
        public event OnScannerImageEventHandler OnScannerImage;
        public event OnScannerDetectEventHandler OnScannerDetect;
        public event OnScannerErrorEventHandler OnScannerError;
        #endregion

        public FPScanner(DeviceIdentity deviceIdentity, int index, CaptureMode captureMode = CaptureMode.PREVIEW_IMAGE_AUTO_DETECT, NativeInterfaceVersion nativeInterfaceVersion = NativeInterfaceVersion.VC3v2)
        {
            this.DeviceIdentity = deviceIdentity;            
            this.captureMode = captureMode;
            this.nativeInterfaceVersion = nativeInterfaceVersion;
            
            worker.DoWork += worker_DoWork;

            InitScanner(index);

            this.Encoder = new Dermalog.Afis.FingerCode3.Encoder();            
        }

        protected abstract void worker_DoWork(object sender, DoWorkEventArgs e);        
        
        static DeviceIdentity[] FilterImplementedDeviceIdentities(DeviceIdentity[] ids)
        {
            var implemented = GetImplementedDeviceIdentities();
            List<DeviceIdentity> filtered = new List<DeviceIdentity>();
            foreach(var id in ids)
            {
                if(Array.IndexOf(implemented, id) > -1)
                {
                    filtered.Add(id);
                }
            }
            return filtered.ToArray<DeviceIdentity>();
        }

        static DeviceIdentity[] GetImplementedDeviceIdentities()
        {
            DeviceIdentity[] ids = { DeviceIdentity.FG_LF1, DeviceIdentity.FG_LF10, DeviceIdentity.FG_ZF1, DeviceIdentity.FG_ZF10, DeviceIdentity.FG_ZF2 };
            return ids;
        }



        /// <summary>
        /// Factory to get FPScanner object from a given DeviceIdentity
        /// </summary>
        /// <param name="deviceIdentity"></param>
        /// <returns></returns>
        public static FPScanner GetFPScanner(DeviceIdentity deviceIdentity, int index)
        {
            FPScanner scanner = null;
            switch (deviceIdentity)
            {
                case DeviceIdentity.FG_LF1:
                    scanner = new FPScannerLF1(index);
                    break;
                case DeviceIdentity.FG_LF10:
                    scanner = new FPScannerLF10(index);
                    break;
                case DeviceIdentity.FG_PLS1:
                    throw new Exception("FG_PLS1 is deprecated. Please use FG_ZF1");
//                     scanner = new FPScannerZF1(deviceIdentity, CaptureMode.PLAIN_FINGER, NativeInterfaceVersion.VC3v1);
//                     break;

                case DeviceIdentity.FG_ZF1:
                    scanner = new FPScannerZF1(deviceIdentity, index);
                    break;
                case DeviceIdentity.FG_ZF10:
                    scanner = new FPScannerZF10(index);
                    break;
                case DeviceIdentity.FG_ZF2:
                    scanner = new FPScannerZF2(index);
                    break;
                default:
                    throw new Exception("DeviceIdentity not supported: " + deviceIdentity);
            }
            return scanner;
        }

        class DeviceIdentityComparer : IComparer  {
            
              int IComparer.Compare( Object x, Object y )  {
                  return x.ToString().CompareTo(y.ToString());
              }

        }

        /// <summary>
        /// Get all available Frame-Grabbers
        /// </summary>
        /// <returns></returns>
        public static DeviceIdentity[] GetDevices()
        {
            var availible = DeviceManager.GetDeviceIdentities();
            var filtered = FilterImplementedDeviceIdentities(availible);

            Array.Sort(filtered, new DeviceIdentityComparer());
            return filtered;
        }

        /// <summary>
        /// Get all Devices of current Frame-Grabber
        /// </summary>
        /// <returns></returns>
        public static DeviceInformations[] GetAttachedDevices(DeviceIdentity id)
        {
            return DeviceManager.GetAttachedDevices(id);
        }        

        /// <summary>
        /// Set Property of Device
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="value"></param>
        public void setDeviceProperty(PropertyType propertyType, int value)
        {
            if (this.Device != null)
                this.Device.Property[propertyType] = value;
        }

        #region Start/Stop capturing
        /// <summary>
        /// Start capturing
        /// </summary>
        public void Start()
        {
            if (this.Device == null)
                return;

            if (!this.Device.IsCapturing)
            {
                lock (this.Device)
                {
                    this.Device.Start();
                }
            }
        }


        /// <summary>
        /// Stop capturing, Don't call this from Scanner-Thread -> Deadlock!
        /// </summary>
        public void Stop()
        {
            if (this.Device == null)
                return;

            lock (this.Device)
            {
                this.Device.Stop();
            }
        }

        public void Freeze(bool freeze)
        {
            this.Device.Freeze(freeze);      
        }
        #endregion

        #region Scanner Init/Deinit
        private void InitScanner(int index)
        {
            if (this.Device != null)
            {
                this.Device.Dispose();
                this.Device = null;
            }

            this.Device = DeviceManager.GetDevice(this.DeviceIdentity, index);
            this.Device.CaptureMode = captureMode;            

            this.Device.OnImage += _device_OnImage;
            this.Device.OnDetect += _device_OnDetect;
            this.Device.OnError += _device_OnError;
        }

        private void DeinitScanner()
        {
            if (this.Device != null)
            {
                StopCapturing();

                this.Device.OnImage -= _device_OnImage;
                this.Device.OnDetect -= _device_OnDetect;
                this.Device.OnError -= _device_OnError;

                this.Device.Dispose();
                this.Device = null;
            }
        }
        #endregion

        #region processing

        protected Fingerprint ProcessImage(System.Drawing.Image img, uint fingerPosition = 0, Dermalog.AFIS.FourprintSegmentation.HandPositions hand = Dermalog.AFIS.FourprintSegmentation.HandPositions.Unknown)
        {
            using (RawImage rawImage = RawImageHelperForms.FromBitmap(img as Bitmap))
            {
                return ProcessImage(img, rawImage, fingerPosition, hand);
            }
        }

        protected Fingerprint ProcessImage(System.Drawing.Image img, byte[] rawData, uint fingerPosition  = 0, Dermalog.AFIS.FourprintSegmentation.HandPositions hand = Dermalog.AFIS.FourprintSegmentation.HandPositions.Unknown)
        {
            using(Dermalog.Afis.ImageContainer.Decoder decoder = new Dermalog.Afis.ImageContainer.Decoder())
            {
                using (RawImage rawImage = decoder.Decode(rawData))
                {
                    return ProcessImage(img, rawImage, fingerPosition, hand);
                }
            }
        }

        protected Fingerprint ProcessImage(System.Drawing.Image img, RawImage rawImg, uint fingerPosition = 0, Dermalog.AFIS.FourprintSegmentation.HandPositions hand = Dermalog.AFIS.FourprintSegmentation.HandPositions.Unknown)
        {
            Fingerprint fp = new Fingerprint();
            fp.Image = img;
            fp.Template =  Encoder.Encode(rawImg);
            fp.NFIQ2 = DermalogNistQualityCheck.CheckNfiq2(rawImg);
            fp.Hand = hand;
            fp.Position = fingerPosition;
            return fp;
        }

        #endregion

        #region Invoked Scanner Events
        public void InvokeOnImage(Image image)
        {
            if (OnScannerImage != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (OnScannerImage != null)
                            OnScannerImage(image);
                    }catch(Exception )
                    {
                        //Ignore
                    }
                }));
            }
        }

        public void InvokeOnDetect(Image image)
        {
            if (OnScannerDetect != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (OnScannerDetect != null)
                            OnScannerDetect(image);
                    }
                    catch (Exception)
                    {
                        //Ignore
                    }
                }));
            }
        }

        public void InvokeFingerprintsDetected(List<Fingerprint> fps)
        {
            if (OnFingerprintsDetected != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (OnFingerprintsDetected != null)
                        OnFingerprintsDetected(fps);
                }));
            }
        }

        public void InvokeScannerError(object sender, ScannerErrorEventArgs args)
        {
            if (OnScannerError == null)
                return;

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (OnScannerError != null)
                    OnScannerError(sender, args);
            }));
        }

        #endregion

        #region Scanner Events
        void _device_OnImage(object sender, ImageEventArgs e)
        {
            InvokeOnImage(e.Image.Clone() as Image);            
        }

        void _device_OnDetect(object sender, DetectEventArgs e)
        {
            this.Device.Freeze(true);
            InvokeOnDetect(e.Image.Clone() as Image);
            OnDetect(sender, e);            
        }

        void _device_OnError(object sender, ErrorEventArgs e)
        {
            if (OnScannerError != null)
                OnScannerError(sender, new ScannerErrorEventArgs(e));
        }
        #endregion

        #region Scanner specific functions to implement
        public abstract void StartCapturing();
        public abstract void StopCapturing();

        public virtual void OnDetect(object sender, DetectEventArgs e)
        {
            try
            {
                var device = sender as Device;
                device.Freeze(true); //pause captureing to reduce cpu load

                if (!worker.IsBusy)
                {
                    //leave native callback context
                    var localImage = e.Image.Clone() as Image;
                    //scanner specific image processing
                    worker.RunWorkerAsync(localImage);
                }
            }
            catch (Exception ex)
            {
                InvokeScannerError(sender, new ScannerErrorEventArgs("Processing error: " + ex.Message, ex));
            }
        }

        #endregion

        public void Dispose()
        {
            DeinitScanner();

            if(Encoder != null)
            {
                Encoder.Dispose();
                Encoder = null;
            }
        }
    }
}
