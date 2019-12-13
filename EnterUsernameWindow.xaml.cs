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

namespace DermalogMultiScannerDemo
{
    /// <summary>
    /// Interaction logic for EnterUsernameWindow.xaml
    /// </summary>
    public partial class EnterUsernameWindow : Window
    {
        private String _username;
        public String Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public EnterUsernameWindow()
        {
            InitializeComponent();

            xamlTextBoxUsername.Focus();
        }

        private void xamlButtonOK_Click(object sender, RoutedEventArgs e)
        {
            String enteredUsername = xamlTextBoxUsername.Text.Trim();
            if (enteredUsername.Length == 0)
            {
                MessageBox.Show("Username must not be empty.");
            }
            else
            {
                _username = enteredUsername;

                DialogResult = true;
            }
        }
    }
}
