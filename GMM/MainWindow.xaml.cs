using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Permissions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;

namespace GMM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    interface DownloadFinish
    {
        void isFinish(String path);
        
    }


    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MainWindow : Window, DownloadFinish
    {

        String banque = "0"; //1 credit agricole paris
        String login = "65002410170";
        String MDP = "259681";
        String compte = "65002410170";
        int etat = 0;
        String chemin = "C:\\Users\\robin\\Desktop\\test\\";

        String[] banques_url = { "", "https://www.ca-paris.fr/" };

        public MainWindow()
        {
            String[] args = Environment.GetCommandLineArgs();
            if (args.Length < 6)
                KillAndQuit(-1);

            this.banque = args[1];
            this.login = args[2];
            this.MDP = args[3];
            this.compte = args[4];
            this.chemin = args[5];

            if (Int32.Parse(banque) > banques_url.Length || banque.Equals("0"))
                KillAndQuit(-1);
            //web.Navigated += new NavigatedEventHandler(web_Navigated);
            /*Uri uri = new Uri(@"https://www.ca-paris.fr/");
            Stream source = Application.GetContentStream(uri).Stream;
            web.NavigateToStream(source);*/
            //web.Navigate("https://www.ca-paris.fr/");
            InitializeComponent();

        }
        
        private void ChromiumWebBrowser_Initialized(object sender, EventArgs e)
        {
            chrome.Address = banques_url[Int32.Parse(this.banque)];
            chrome.DownloadHandler = new DownloadHandler(chemin, this);

        }

        private void chrome_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            if (chrome.GetBrowser().GetFrameIdentifiers()[0] == e.Frame.Identifier)
            {
                this.Dispatcher.Invoke(() => { CreditAgricole(chrome.Address); });
            }
            
        }


        private void CallGoodBanque(String adresse)
        {
            switch(this.banque)
            {
                case "1": //CA
                    CreditAgricole(adresse);
                    break;
            }
        }

        private void CreditAgricole(String adresse)
        {
            System.Diagnostics.Debug.WriteLine("Credit Agricol " + etat);
            switch (etat)
            {
                case 0:
                    if (!adresse.Equals("https://www.ca-paris.fr/"))
                        return;
                    break;
                case 1:
                    if (!adresse.Equals("https://www.paris-g4-enligne.credit-agricole.fr/stb/entreeBam"))
                        return;
                    break;
                default:
                    break;
            }


            String commande = "";
            switch (etat)
            {
                case 0:
                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync("bamv3_validation();");
                    etat = 1;
                    System.Diagnostics.Debug.WriteLine("Click on login");
                    break;
                case 1:
                    commande = @"function Login(compte, mdp) {
                                    var index = 0;
                                    document.getElementsByName('CCPTE')[0].value = compte;
                                    for (var i = 0; i < mdp.length; i++) {
                                        var td = document.getElementById('pave-saisie-code').getElementsByTagName('td');
                                        while(index < td.length) {
                                            var test =  td[index].innerText.replace(/\D/g,'');
                                            if (test.length > 0) {
                                                if (mdp[i] == test) {
                                                    var position = '';
                                                    if (index < 9) 
                                                        position = '0'
                                                    position += (index + 1) + '';

                                                    clicPosition(position);
                                                    index = 0;
                                                    break;
                                                }
                                            }
                                            index++;
                                        }
                                    }
                                    ValidCertif();
                                }";

                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync(" " + commande + " ; Login('" + login + "', '" + MDP + "')");
                    System.Diagnostics.Debug.WriteLine("login it");
                    etat = 2;
                    break;
                case 2:
                    commande = @"function Compte(compte) {
                                var td = document.getElementsByClassName('ca-table')[0].getElementsByTagName('td');
                                var index = 0;
                                while(index < td.length) {
                                    if (td[index].innerText.replace(/\D/g,'') == compte) {
                                        console.log(td[index].innerText);
                                        td[index].children[0].click();
                                    }
                                    index++;
                                }
                            }";

                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync(" " + commande + " ; Compte('" + compte + "')");
                    System.Diagnostics.Debug.WriteLine("compte it");
                    etat = 3;
                    break;
                case 3:
                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync("myOuvrirTelechargement();");
                    break;
                default:
                    break;
            }
        }

        public void isFinish(string path)
        {
            if (path == null)
                return;
            if (path.Equals(""))
                return;
            else
                this.Dispatcher.Invoke(() =>
                {
                    KillAndQuit(1);
                });
            //throw new NotImplementedException();
        }

        public void KillAndQuit(int code)
        {
            if (chrome != null)
                if (!chrome.IsDisposed)
                    chrome.Dispose();
            System.Diagnostics.Debug.WriteLine("Credit Agricol QUITTED");
            
            Environment.ExitCode = code;
            
            if (!this.IsInitialized)
                Environment.Exit(code);
            else
                Application.Current.Shutdown(code);
           // this.Close();
        }
    }

   
}
