using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for SelectDeviceWindow.xaml
    /// </summary>
    public partial class SelectDeviceWindow : Window
    {
        /// <summary>
        /// DeviceIdentity of the selected Frame-Grabber
        /// </summary>
        private DeviceIdentity _selectedDeviceIdentity;

        /// <summary>
        /// Selected Fingerprint Scanner
        /// </summary>
        public FPScanner FingerPrintScanner { get; internal set; }
        
        private Thread _thread;

        public SelectDeviceWindow(DeviceIdentity selectedDeviceIdentity, Window owner)
        {
            _selectedDeviceIdentity = selectedDeviceIdentity;
            this.Owner = owner;
         
            InitializeComponent();

            this.Closing += SelectDeviceWindow_Closing;

            displayMessage("Loading Devices");
            xamlButtonOK.IsEnabled = false;

            _thread = new Thread(() =>
           {
               try
               {
                   DeviceInformations[] dinfos = FPScanner.GetAttachedDevices(_selectedDeviceIdentity); //Get all Fingerprint Scanners

                    //Display all Fingerprint-Scanners in GUI
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        xamlProgressBar.Visibility = System.Windows.Visibility.Hidden;
                        displayMessage("Select Fingerprint-Scanner");
                        foreach (DeviceInformations dinfo in dinfos)
                        {
                            DeviceInfos di = new DeviceInfos(dinfo.index, dinfo.name);
                            xamlListBoxDevices.Items.Add(di);
                        }
                        if (xamlListBoxDevices.Items.Count > 0)
                            xamlListBoxDevices.SelectedIndex = 0;
                    }));
              
               }
               catch (Exception ex)
               {
                   Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                   {
                       DialogResult = false;
                       MessageBox.Show(ex.Message, "ERROR");
                   }));
               }
           });
            _thread.Start();
        }

        void SelectDeviceWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_thread != null)
            {
                _thread.Join();
            }
        }

        private void displayMessage(String message)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                xamlLabelMessage.Text = message.ToUpper();
            }));
        }

        private void xamlButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (xamlListBoxDevices.SelectedIndex >= 0)
            {
                DeviceInfos selectedDInfos = xamlListBoxDevices.SelectedValue as DeviceInfos; //Get seleced Fingerprint Scanner
                if (selectedDInfos != null)
                {
                    var _preCursor = this.Cursor;
                    try
                    {
                        this.Cursor = Cursors.Wait;
                        FingerPrintScanner = FPScanner.GetFPScanner(_selectedDeviceIdentity, selectedDInfos.Index);
                        DialogResult = true;
                    }catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "ERROR");
                    }
                    finally
                    {
                        this.Cursor = _preCursor;
                    }
                }
            }            
        }

        private void xamlListBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            xamlButtonOK.IsEnabled = true;
        }
    }

    public class DeviceInfos
    {
        private int mIndex;
        public int Index
        {
            get { return mIndex; }
            set { mIndex = value; }
        }

        private String mName;
        public String Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public DeviceInfos(int index, string name)
        {
            this.Index = index;
            this.Name = name;
        }

        public override string ToString()
        {
            return mName;
        }
    }
}
