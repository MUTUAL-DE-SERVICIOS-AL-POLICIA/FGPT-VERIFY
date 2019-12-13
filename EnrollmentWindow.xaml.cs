using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Dermalog.Imaging.Capturing;

namespace DermalogMultiScannerDemo
{
    /// <summary>
    /// Interaction logic for EnrollmentWindow.xaml
    /// </summary>
    public partial class EnrollmentWindow : Window
    {
        #region Fields
        private FPScanner _fpScanner;
        private LocalAFIS _afis;

        private LocalUser _enrolledUser;
        public LocalUser EnrolledUser
        {
            get { return _enrolledUser; }
            set { _enrolledUser = value; }
        }

        private String _username;
        private List<Fingerprint> _userFingerprints;

        private bool _displayOnImage = true;
        #endregion

        public EnrollmentWindow(FPScanner fpScanner, LocalAFIS afis)
        {
            InitializeComponent();

            xamlButtonEnroll.IsEnabled = false;

            _fpScanner = fpScanner;
            _afis = afis;
        }

        public void OpenEnterUsernameWindow()
        {
            displayMessage("Enter Name.");

            EnterUsernameWindow enterUsernameWindow = new EnterUsernameWindow();
            enterUsernameWindow.Owner = this;
            enterUsernameWindow.ShowDialog();

            if (enterUsernameWindow.DialogResult.HasValue && enterUsernameWindow.DialogResult.Value)
            {
                _username = enterUsernameWindow.Username;
                xamlLabelUsername.Content = _username;

                displayMessage("Please place finger(s) onto scanner.");
                StartCapturing();
            }
            else
            {
                this.Close();
            }
        }

        #region IFPScanner Events
        private void BindFPScannerEvents()
        {
            UnbindFPScannerEvents();

            _fpScanner.OnScannerImage += _fpScanner_OnScannerImage;
            _fpScanner.OnScannerDetect += _fpScanner_OnScannerDetect;
            _fpScanner.OnScannerError += _fpScanner_OnScannerError;
            _fpScanner.OnFingerprintsDetected += _fpScanner_OnFingerprintsDetected;
        }

        private void UnbindFPScannerEvents()
        {
            _fpScanner.OnScannerImage -= _fpScanner_OnScannerImage;
            _fpScanner.OnScannerDetect -= _fpScanner_OnScannerDetect;
            _fpScanner.OnScannerError -= _fpScanner_OnScannerError;
            _fpScanner.OnFingerprintsDetected -= _fpScanner_OnFingerprintsDetected;
        }

        void _fpScanner_OnScannerError(object sender, FPScanner.ScannerErrorEventArgs e)
        {
            MessageBox.Show(e.Error);
        }

        void _fpScanner_OnScannerImage(System.Drawing.Image image)
        {
            if (!_displayOnImage)
                return;

            Bitmap bmp = new Bitmap(image);
            imageOnScannerDetect.Source = Utils.BitmapToBitmapSource(bmp);
        }

        void _fpScanner_OnScannerDetect(System.Drawing.Image image)
        {
            displayMessage("Extracting Templates");

            _displayOnImage = false;

            Bitmap bmp = new Bitmap(image);
            imageOnScannerDetect.Source = Utils.BitmapToBitmapSource(bmp);
        }

        void _fpScanner_OnFingerprintsDetected(List<Fingerprint> fingerprints)
        {
            _fpScanner.StopCapturing();

            _userFingerprints = fingerprints;

            stackPanelFingerprints.Children.Clear();
            int imageWidth = (int)stackPanelFingerprints.RenderSize.Width / fingerprints.Count;
            foreach (Fingerprint fingerprint in fingerprints)
            {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                Bitmap bmp = new Bitmap(fingerprint.Image);
                img.Source = Utils.BitmapToBitmapSource(bmp);
                img.Width = imageWidth;
                img.Height = stackPanelFingerprints.Height;

                stackPanelFingerprints.Children.Add(img);
            }

            xamlButtonEnroll.IsEnabled = true;
            displayMessage("Press save to finish enrollment");
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCapturing();
        }

        public void StartCapturing()
        {
            BindFPScannerEvents();
            _fpScanner.StartCapturing();
        }

        public void StopCapturing()
        {
            UnbindFPScannerEvents();
            _fpScanner.StopCapturing();
        }

        private void displayMessage(String message)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                xamlLabelMessage.Text = message.ToUpper();
            }));
        }

        private void xamlButtonEnroll_Click(object sender, RoutedEventArgs e)
        {
            _enrolledUser = _afis.RegisterUser(_username, _userFingerprints);
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OpenEnterUsernameWindow();
        }
    }
}
