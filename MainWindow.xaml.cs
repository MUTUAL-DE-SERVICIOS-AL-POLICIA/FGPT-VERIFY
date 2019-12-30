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
using System.IO;

using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using Dermalog.Afis.FingerCode3;
//

namespace DermalogMultiScannerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        //GLOBAL VARIABLES
        public string path_local = "";  //Get from data.xml "D:\\tst\\04"
        public string path_fingerprint = "\\\\192.168.2.120\\pvt\\fingerprint";
        public string path_picture = "\\\\192.168.2.120\\pvt\\picture";
        public string path_fingerprint_dat = "\\\\192.168.2.120\\pvt\\fingerprint_dat";
        //REMOTE
        public string path_remote_user = "pvt";
        public string path_remote_password = "s4turn0";
        //USUSARIO
        public string affiliate_id = "12";
        public string affiliate_ci = "ci";
        public string affiliate_name = "name";
        //USER
        public string user_id = "";
        //HOST
        //public string host_ip = "http://192.168.2.99";
        public string host_ip = "";
        //
        //M4
        public List<Template> m_fingers = new List<Template>();
        public bool m_existFingerRegistry;

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
                //lbStorage.Content = "Storage: " + _afis.StoragePath;
                //lbStorage.ToolTip = _afis.StoragePath;

                //UpdateUserList();                
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

            //ResetGUI();
            //StopCapturing();


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

            //if (_selectedUser != null)
            //{
            try
            {


                
                
                //ResetGUI();
                //StartCapturing();
                //DisplayMessage("hola");

                //Verify User
                DisplayMessage("Verificando huellas");
                //AFISVerificationResult result = _afis.VerifyUser(_selectedUser.ID, fingerprints, Properties.Settings.Default.VerificationThreshold);
                AFISVerificationResult result = _afis.muserpol_verifyUser(0, fingerprints, Properties.Settings.Default.VerificationThreshold, this);                    
                String scoreString = String.Format("{0:0.00}", result.Score);




                //MessageBox.Show("hola como estan ");



                if (result.Hit) {
                    displayMessage(String.Format("AFILIADO VERIFICADO ({0})", scoreString), Utils.COLOR_DERMALOG_GREEN);
                }
                else {
                    if (result.Score < 0)
                    {
                        displayMessage(String.Format("AFILIADO NO BIOMETRIZADO ({0})", scoreString), Utils.COLOR_DERMALOG_BLUE);                    
                    }
                    else {
                        displayMessage(String.Format("AFILIADO NO VERIFICADO ({0})", scoreString), Utils.COLOR_DERMALOG_RED);                    
                    }

                }

            }
            catch (Exception e)
            {
                DisplayError(e.Message);
            }
            //}

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
            /*
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                xamlButtonEnroll.IsEnabled = enable;
                xamlListBoxUsers.IsEnabled = enable;
            }));
            */
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


          //  MessageBox.Show("hola com oestas");
            XmlDocument sFor = new XmlDocument();
            sFor.Load(System.IO.Path.GetFullPath("data.xml"));
            XmlNodeList users = sFor.SelectNodes("/items/users/user");
            foreach (XmlNode user in users)
            {
                comboBox1.Items.Add(user.InnerText.ToString().Trim());
            }

            //Get selected
            var nodes = sFor.SelectNodes("/items/selected");
            string selected = nodes[0].InnerText.ToString().Trim();
            comboBox1.SelectedIndex = comboBox1.Items.IndexOf(selected);

            //Get path_local
            nodes = sFor.SelectNodes("/items/pathlocal");
            string pathLocal = nodes[0].InnerText.ToString().Trim();
            this.path_local = pathLocal;

            //Get url
            nodes = sFor.SelectNodes("/items/url");
            string url = nodes[0].InnerText.ToString().Trim();
            this.host_ip = url;




            //Search value
            XmlNodeList xnList = sFor.SelectNodes("/items/users/user"); //xml.SelectNodes("/Names/Name[@type='M']");
            string v = "";
            string idUser = "";
            //XmlNode userSelected;
            foreach (XmlNode user in users)
            {
                v = user.InnerText.ToString().Trim();
                if (v == selected)
                {
                    //userSelected = user;
                    idUser = user.Attributes["id"].Value.ToString().Trim();
                }
            }

            this.user_id = idUser;












            try
            {
                //MessageBox.Show("hola como estas");

                OpenSelectFGDialog();
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        public FPScanner FingerPrintScanner { get; internal set; }
        private void OpenSelectFGDialog()
        {
            CloseScanner();

            ResetGUI();
            DisplayMessage("Device configuration");

            try
            {
                SelectFGWindow selectFG = new SelectFGWindow(this);
                //selectFG.ShowDialog();

                //selectFG.DialogResult.HasValue = true;
                //selectFG.DialogResult.Value = true;

                var selectedDeviceIdentity = Dermalog.Imaging.Capturing.DeviceIdentity.FG_ZF2;

                if (true)
                //if (selectFG.DialogResult.HasValue && selectFG.DialogResult.Value)
                {
                    //var selectedDeviceIdentity = selectFG.SelectedDeviceIdentity;

                    if (_fpScanner != null)
                    {
                        _fpScanner.Dispose();
                        _fpScanner = null;
                    }

                    //OpenSelectDeviceDialog(selectedDeviceIdentity);



                    //GET SCANNER SERIE
                    DeviceInformations[] dinfos = FPScanner.GetAttachedDevices(selectedDeviceIdentity); //Get all Fingerprint Scanners
                    var scannerSerie = 0;
                    //Display all Fingerprint-Scanners in GUI
                    foreach (DeviceInformations dinfo in dinfos)
                    {
                        DeviceInfos di = new DeviceInfos(dinfo.index, dinfo.name);
                        //xamlListBoxDevices.Items.Add(di);
                        scannerSerie = di.Index;
                    }




                    //AUTOMATIC SELECTED DEVICE WINDOWS 2
                    var _preCursor = this.Cursor;
                    try
                    {
                        this.Cursor = Cursors.Wait;
                        FingerPrintScanner = FPScanner.GetFPScanner(selectedDeviceIdentity, scannerSerie);
                        //DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "ERROR");
                    }
                    finally
                    {
                        this.Cursor = _preCursor;
                    }


                    //AUTOMATIC SELECTED DEVICE WINDOWS 2
                    SelectDeviceWindow selectDevice = new SelectDeviceWindow(selectedDeviceIdentity, this);
                    DisplayMessage("Opening device");
                    selectDevice.FingerPrintScanner = FingerPrintScanner;
                    _fpScanner = selectDevice.FingerPrintScanner;
                    if (_fpScanner != null)
                    {
                        ResetGUI();
                        EnableGUI(true);
                        /*
                        if (xamlListBoxUsers.Items.Count > 0)
                        {
                            xamlListBoxUsers.SelectedIndex = 0;
                        }
                        */
                    }






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
                        /*
                        if(xamlListBoxUsers.Items.Count > 0)
                        {
                            xamlListBoxUsers.SelectedIndex = 0;
                        }
                        */
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
            //xamlListBoxUsers.SelectedIndex = -1; //Deselect User

            EnrollmentWindow enrollmentWindow = new EnrollmentWindow(_fpScanner, _afis);
            enrollmentWindow.Owner = this;
            enrollmentWindow.ShowDialog();

            /*
            if (enrollmentWindow.DialogResult.HasValue && enrollmentWindow.DialogResult.Value)
            {
                UpdateUserList();
            }
            */

            DisplayMessage("Select User to verify");
        }

        /*
        private void UpdateUserList()
        {
            Dictionary<long, LocalUser> userList = _afis.GetUserList();
            xamlListBoxUsers.Items.Clear();
            foreach (LocalUser user in userList.Values)
            {
                xamlListBoxUsers.Items.Add(user);
            }
        }
        */

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

        /*      
    private void xamlListBoxUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {


        StopCapturing();

        LocalUser selectedUser = (LocalUser)xamlListBoxUsers.SelectedItem;

        if (selectedUser != null)
        {
            ResetGUI();

            _selectedUser = selectedUser;
            DisplayMessage("Coloque el dedo en el escaner.");
            //DisplayMessage("Please place finger(s) onto scanner");
            StartCapturing();
        }

    }
        */
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
                //UpdateUserList();
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

        private void button1_Click(object sender, RoutedEventArgs e)
        {


            // 1. Open the wsq file
            FileStream fs = File.OpenRead("C:\\Users\\douglas\\Documents\\DermalogMultiScannerDemo\\000004\\t00.wsq");
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, (int)fs.Length);
            
            // 2. Create decode the WSQ file and create a RawImage
            using (Dermalog.Afis.ImageContainer.Decoder decoder = new Dermalog.Afis.ImageContainer.Decoder())
            {
                Dermalog.Afis.ImageContainer.RawImage rawImage = decoder.Decode(bytes);

                // 3. Create a FingerCode3 encoder and create a template
                using (Dermalog.Afis.FingerCode3.Encoder encoder = new Dermalog.Afis.FingerCode3.Encoder())
                {
                    Dermalog.Afis.FingerCode3.Template template = encoder.Encode(rawImage);
                    // The function template.GetData() returns the raw byte data, which can be saved as a *.dat file.
                    //String templateString = String.Format("template{0}.dat",
                    //localUser.Fingerprints[i].Position.ToString("D2"));
                    //String templatePath = Path.Combine(idPath, templateString);
                    FileStream fs2 = File.Create("C:\\Users\\douglas\\Documents\\DermalogMultiScannerDemo\\000004\\image00.dat");
                    byte[] data = template.GetData();
                    fs2.Write(data, 0, data.Length);
                    fs2.Flush();
                    fs2.Close();

                }
            }



        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {

            g gg = new g();

            //Verifico afiliado url
            String afilliated_url = this.host_ip + "/api/v1/record?page=1&per_page=1&sortBy[]=created_at&sortDesc[]=true&user_id=" + user_id;
            bool afilliated_url_exist = false;
            if (gg.RemoteFileExists(afilliated_url))
            {
                afilliated_url_exist = true;
            }


            if (!afilliated_url_exist)
            {
                MessageBox.Show("No se pudo conectar al servidor, consulte con Sistemas");
                return;
            }


            //RED VERIFICAR
            bool newtworkConnectionExist = false;
            try
            {
                using (new NetworkConnection(path_fingerprint_dat, new NetworkCredential(this.path_remote_user, this.path_remote_password)))
                {
                    //_directoryPath = "\\\\192.168.2.120\\utic\\PVT\\fingerprint\\3.txt";
                    //File.Copy("D:\\2.txt", _directoryPath);                
                    //   MessageBox.Show("hola");
                }
                newtworkConnectionExist = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            if (!newtworkConnectionExist)
            {
                MessageBox.Show("No se pudo conectar a la direccion 192.168.2.120 verifique la conexion de red");
                return;
            }





            //Obtengo affiliate_id
            WebClient wc = new System.Net.WebClient();
            var json = wc.DownloadString(this.host_ip + "/api/v1/record?page=1&per_page=1&sortBy[]=created_at&sortDesc[]=true&user_id=" + user_id);
            var found1 = json.IndexOf("\"recordable_id\":");
            json = json.Substring(found1 + 16);
            var found2 = json.IndexOf(",");
            affiliate_id = json.Substring(0, found2);


            //Obtengo: 
            wc = new System.Net.WebClient();
            json = wc.DownloadString(this.host_ip + "/api/v1/affiliate/" + affiliate_id);
            var jsonArray = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(json);
            string afiliadoNombre = jsonArray["first_name"] + " " + jsonArray["last_name"];
            string afiliadoCi = jsonArray["identity_card"];


            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Desea iniciar la Biometrizacion de:\n\r \n\r NUP_________" + affiliate_id + "\n\r NOMBRE____" + afiliadoNombre + " \n\r C.I.__________" + afiliadoCi, "Sistema", 
                System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {

                f_matricula.Text = affiliate_id;
                f_ci.Text = afiliadoCi;
                f_afiliado.Text = afiliadoNombre;



                //M5
                //DATASYSTEM
                m_fingers = new List<Template>();
                var f_pathdata = path_fingerprint_dat; //
                var f_id = affiliate_id;
                //f_id = f_id + "ttt";
                //
                string[] m_fingersAffiliate = new string[10];

                m_fingersAffiliate[0] = f_pathdata + "\\" + f_id + "_left_little.dat";
                m_fingersAffiliate[1] = f_pathdata + "\\" + f_id + "_left_ring.dat";
                m_fingersAffiliate[2] = f_pathdata + "\\" + f_id + "_left_middle.dat";
                m_fingersAffiliate[3] = f_pathdata + "\\" + f_id + "_left_index.dat";
                m_fingersAffiliate[4] = f_pathdata + "\\" + f_id + "_left_thumb.dat";
                m_fingersAffiliate[5] = f_pathdata + "\\" + f_id + "_right_little.dat";
                m_fingersAffiliate[6] = f_pathdata + "\\" + f_id + "_right_ring.dat";
                m_fingersAffiliate[7] = f_pathdata + "\\" + f_id + "_right_middle.dat";
                m_fingersAffiliate[8] = f_pathdata + "\\" + f_id + "_right_index.dat";
                m_fingersAffiliate[9] = f_pathdata + "\\" + f_id + "_right_thumb.dat";

                m_existFingerRegistry = false;
                string f_finger = "";
                for (int i = 0; i < m_fingersAffiliate.Length; i++)
                {
                    //datasystem
                    f_finger = m_fingersAffiliate[i];

                    //finger add to array fingers
                    String dxPath = f_finger;
                    Template dx = new Template();

                    try
                    {

                        dx.Data = File.ReadAllBytes(dxPath);
                        m_fingers.Add(dx);
                        m_existFingerRegistry = true;
                    }
                    catch
                    {
                    }


                    //bool dxExist = System.IO.File.Exists(dxPath);
                    /*
                    if (dxExist)
                    {
                        m_existFingerRegistry = true;
                        dx.Data = File.ReadAllBytes(dxPath);
                        m_fingers.Add(dx);
                    }
                    */
                }



                if (!m_existFingerRegistry)
                { 
                  displayMessage(String.Format("AFILIADO NO BIOMETRIZADO", 0), Utils.COLOR_DERMALOG_RED);
                  return;
                }

                 



                ResetGUI();
                //_selectedUser = selectedUser;
                DisplayMessage("Coloque el dedo en el escaner.");
                //DisplayMessage("Please place finger(s) onto scanner");
                StopCapturing();
                ResetGUI();
                StartCapturing();


            }
            else
            {
            }


        }
    }
}
