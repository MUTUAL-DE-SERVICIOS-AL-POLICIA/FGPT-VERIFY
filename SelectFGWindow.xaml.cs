using System;
using System.Collections.Generic;
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
    /// Interaction logic for SelectFGWindow.xaml
    /// </summary>
    public partial class SelectFGWindow : Window
    {
        /// <summary>
        /// DeviceIdentity of the selected Frame-Grabber
        /// </summary>
        public DeviceIdentity SelectedDeviceIdentity { get; set; }

        public SelectFGWindow(Window owner)
        {
            this.Owner = owner;
            
            InitializeComponent();

            displayMessage("Select Frame-Grabber");
            xamlButtonOK.IsEnabled = false;

            DeviceIdentity[] dis = FPScanner.GetDevices();
            xamlListBoxFG.ItemsSource = dis;
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
            DialogResult = true;
        }

        private void xamlListBoxFG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceIdentity selectedDI = (DeviceIdentity)xamlListBoxFG.SelectedItem; //Get selected Frame-Grabber
            this.SelectedDeviceIdentity = selectedDI;

            xamlButtonOK.IsEnabled = true;
        }
    }
}
