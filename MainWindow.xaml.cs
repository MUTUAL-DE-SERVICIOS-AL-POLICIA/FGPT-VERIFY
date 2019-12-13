using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dermalog.Imaging.Capturing;
using System.Reflection;

namespace DermalogMultiScannerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FPScanner _fpScanner;
        private LocalUser _selectedUser;
        private LocalAFIS _afis;
        
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                Title += " v" + Assembly.GetExecutingAssembly().GetName().Version;     

                //use any assembly versions
                RedirectAssembly("Dermalog.Afis.ImageContainer");
                RedirectAssembly("Dermalog.Imaging.Capturing");
                RedirectAssembly("Dermalog.AFIS.FourprintSegmentation");
                RedirectAssembly("Dermalog.AFIS.TwoPprintSegmentation");
                RedirectAssembly("Dermalog.Afis.NistQualityCheck");
                RedirectAssembly("Dermalog.Afis.FingerCode3");                
                DisplayMessage("Loading user database");
                _afis = new LocalAFIS();
                lbStorage.Content = "Storage: " + _afis.StoragePath;
                lbStorage.ToolTip = _afis.StoragePath;

                UpdateUserList();                
                EnableGUI(false);
            }
            catch(Exception ex)
            {                
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show(ex.Message+"\n"+ex.StackTrace, "ERROR");
                Close();
            }
        }

        public static void RedirectAssembly(string shortName)
        {
            ResolveEventHandler handler = null;
            handler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                if (requestedAssembly.Name != shortName)
                    return null;

                AppDomain.CurrentDomain.AssemblyResolve -= handler;
                return Assembly.LoadWithPartialName(shortName);
            };
            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }

        #region FPScanner Events
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
            if (_fpScanner == null)
                return;

            _fpScanner.OnScannerImage -= _fpScanner_OnScannerImage;
            _fpScanner.OnScannerDetect -= _fpScanner_OnScannerDetect;
            _fpScanner.OnScannerError -= _fpScanner_OnScannerError;
            _fpScanner.OnFingerprintsDetected -= _fpScanner_OnFingerprintsDetected;
        }

        void _fpScanner_OnScannerError(object sender, FPScanner.ScannerErrorEventArgs e)
        {
            DisplayError(e.Error);
            MessageBox.Show(e.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void _fpScanner_OnScannerImage(System.Drawing.Image image)
        {
            xamlImageOnScannerImage.Source = Utils.BitmapToBitmapSource(image as Bitmap);
        }

        void _fpScanner_OnScannerDetect(System.Drawing.Image image)
        {
            DisplayMessage("Extracting Templates");
            xamlImageOnScannerDetect.Source = Utils.BitmapToBitmapSource(image as Bitmap);
        }

        void _fpScanner_OnFingerprintsDetected(List<Fingerprint> fingerprints)
        {
            #region GUI - Display Fingerprints
            xamlStackPanelFingerprints.Children.Clear();
            int imageWidth = (int)xamlStackPanelFingerprints.RenderSize.Width / fingerprints.Count;
            foreach (Fingerprint fingerprint in fingerprints)
            {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                Bitmap bmp = new Bitmap(fingerprint.Image);
                img.Source = Utils.BitmapToBitmapSource(bmp);

                TextBlock tbNFIQ = new TextBlock();
                tbNFIQ.Text = "NFIQ2: " + fingerprint.NFIQ2;
                tbNFIQ.FontSize = 80;
                tbNFIQ.TextAlignment = TextAlignment.Center;
                tbNFIQ.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tbNFIQ.Foreground = Utils.GetBrushFromNFIQ2(fingerprint.NFIQ2);

                Grid grid = new Grid();
                RowDefinition gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(1, GridUnitType.Star);
                RowDefinition gridRow2 = new RowDefinition();
                gridRow2.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(gridRow1);
                grid.RowDefinitions.Add(gridRow2);

                img.SetValue(Grid.RowProperty, 0);
                tbNFIQ.SetValue(Grid.RowProperty, 1);

                grid.Children.Add(img);
                grid.Children.Add(tbNFIQ);

                grid.Margin = new Thickness(30, 0, 30, 0);

                xamlStackPanelFingerprints.Children.Add(grid);
            }
            #endregion

            if (_selectedUser != null)
            {
                try
                {
                    //Verify User
                    DisplayMessage("Verifying Templates");
                    AFISVerificationResult result = _afis.VerifyUser(_selectedUser.ID, fingerprints, Properties.Settings.Default.VerificationThreshold);
                    String scoreString = String.Format("{0:0.00}", result.Score);

                    if (result.Hit)
                        displayMessage(String.Format("User verified ({0})", scoreString), Utils.COLOR_DERMALOG_GREEN);
                    else
                        displayMessage(String.Format("User not verified ({0})", scoreString), Utils.COLOR_DERMALOG_RED);
                }
                catch (Exception e)
                {
                    DisplayError(e.Message);
                }
            }

            //dispose allocated fingerprint templates
            foreach (Fingerprint fingerprint in fingerprints)
            {
                fingerprint.Dispose();
            }

            _fpScanner.Freeze(false);                        
        }
        #endregion

        #region FPScanner capturing
        public void StartCapturing()
        {            
            BindFPScannerEvents();
            _fpScanner.StartCapturing();
        }

        public void StopCapturing()
        {            
            if (_fpScanner != null)
                _fpScanner.StopCapturing();

            UnbindFPScannerEvents();
        }
        #endregion

        #region GUI
        private void EnableGUI(bool enable)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                xamlButtonEnroll.IsEnabled = enable;
                xamlListBoxUsers.IsEnabled = enable;
            }));
        }

        private void ResetGUI()
        {            
            if (_afis.IsEmpty())
            {
                DisplayMessage("Press 'Enroll User'");
            }
            else
            {
                DisplayMessage("Select User to verify");
            }
            
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {                
                xamlImageOnScannerImage.Source = null;
                xamlImageOnScannerDetect.Source = null;
                xamlStackPanelFingerprints.Children.Clear();                
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseScanner();
        }

        private void CloseScanner()
        {
            if (_fpScanner == null)
                return;

            StopCapturing();

            _fpScanner.Dispose();
            _fpScanner = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenSelectFGDialog();
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void OpenSelectFGDialog()
        {
            CloseScanner();

            ResetGUI();
            DisplayMessage("Device configuration");

            try
            {
                SelectFGWindow selectFG = new SelectFGWindow(this);

                selectFG.ShowDialog();

                if (selectFG.DialogResult.HasValue && selectFG.DialogResult.Value)
                {
                    var selectedDeviceIdentity = selectFG.SelectedDeviceIdentity;

                    if (_fpScanner != null)
                    {
                        _fpScanner.Dispose();
                        _fpScanner = null;
                    }

                    OpenSelectDeviceDialog(selectedDeviceIdentity);
                }
                else
                {
                    if (_fpScanner == null)
                        DisplayError("No Frame-Grabber selected.");
                    EnableGUI(false);
                }
            }
            catch(Exception ex)
            {                
                DisplayError(ex.Message);
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void OpenSelectDeviceDialog(DeviceIdentity selectedDeviceIdentity)
        {
            ResetGUI();
            DisplayMessage("Device configuration");
            EnableGUI(false);

            try
            {
                SelectDeviceWindow selectDevice = new SelectDeviceWindow(selectedDeviceIdentity, this);
                if (_fpScanner != null)
                    selectDevice.FingerPrintScanner = _fpScanner;

                selectDevice.ShowDialog();

                if (selectDevice.DialogResult.HasValue && selectDevice.DialogResult.Value)
                {
                    DisplayMessage("Opening device");

                    _fpScanner = selectDevice.FingerPrintScanner;
                    if (_fpScanner != null)
                    {
                        ResetGUI();
                        EnableGUI(true);
                        if(xamlListBoxUsers.Items.Count > 0)
                        {
                            xamlListBoxUsers.SelectedIndex = 0;
                        }
                    }
                }
                else
                {
                    DisplayError("No device selected");
                    if (selectDevice.FingerPrintScanner != null)
                    {
                        selectDevice.FingerPrintScanner.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }
        }

        private void btnEnroll_Click(object sender, RoutedEventArgs e)
        {
            StopCapturing();
            ResetGUI();

            DisplayMessage("User enrollment");
            xamlListBoxUsers.SelectedIndex = -1; //Deselect User

            EnrollmentWindow enrollmentWindow = new EnrollmentWindow(_fpScanner, _afis);
            enrollmentWindow.Owner = this;
            enrollmentWindow.ShowDialog();

            if (enrollmentWindow.DialogResult.HasValue && enrollmentWindow.DialogResult.Value)
            {
                UpdateUserList();
            }

            DisplayMessage("Select User to verify");
        }

        private void UpdateUserList()
        {
            Dictionary<long, LocalUser> userList = _afis.GetUserList();
            xamlListBoxUsers.Items.Clear();
            foreach (LocalUser user in userList.Values)
            {
                xamlListBoxUsers.Items.Add(user);
            }
        }

        private void DisplayMessage(String message)
        {
            displayMessage(message, Utils.COLOR_DERMALOG_BLUE);
        }

        private void DisplayError(String error)
        {
            displayMessage(error, Utils.COLOR_DERMALOG_RED);
        }

        private void displayMessage(String message, System.Windows.Media.Brush brush)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                xamlLabelMessage.Foreground = brush;
                xamlLabelMessage.Text = message.ToUpper();
            }));
        }

      
        private void xamlListBoxUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopCapturing();

            LocalUser selectedUser = (LocalUser)xamlListBoxUsers.SelectedItem;

            if (selectedUser != null)
            {
                ResetGUI();

                _selectedUser = selectedUser;

                DisplayMessage("Please place finger(s) onto scanner");
                StartCapturing();
            }
        }

        private void xamlMenuItemSelectFG_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectFGDialog();
        }

        private void xamlMenuItemReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopCapturing();
                System.IO.Directory.Delete(_afis.StoragePath, true);
                _afis = new LocalAFIS();                
                UpdateUserList();
                ResetGUI();

            }catch(Exception ex)
            {
                DisplayError(ex.Message);
            }
        }


        private void xamlMenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
