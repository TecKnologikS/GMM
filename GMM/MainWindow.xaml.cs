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
using Aspose.OCR;
using System.Net;
using System.Collections;
using System.Threading;
using System.Timers;

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
    public partial class MainWindow : Window, DownloadFinish, INGCallBack
    {

        Hashtable informations = new Hashtable();
        String banque = "0"; //1 credit agricole paris
        int etat = 0;
        String chemin = "C:\\Users\\robin\\Desktop\\test\\";

        String[] banques_url = { "", "https://www.ca-paris.fr/", "https://secure.ingdirect.fr/" };
       // OcrEngine ocrEngine;

        public MainWindow()
        {

            
            String[] args = Environment.GetCommandLineArgs();
            if (args.Length < 6)
                KillAndQuit(-1);

            this.banque = args[1];
            this.chemin = args[2];

            setArgument(args);


            if (Int32.Parse(banque) > banques_url.Length || banque.Equals("0"))
                KillAndQuit(-1);

            InitializeComponent();

        }

        public void setArgument(String[] args)
        {
            informations.Clear();
            switch (this.banque)
            {
                case "1":
                    informations.Add(CONSTANTE.CA_LOGIN,        args[3]);
                    informations.Add(CONSTANTE.CA_MDP,          args[4]);
                    informations.Add(CONSTANTE.CA_COMPTE,       args[5]);
                    break;
                case "2":
                    informations.Add(CONSTANTE.ING_NUM_CLIENT,  args[3]);
                    informations.Add(CONSTANTE.ING_JOUR,        args[4]);
                    informations.Add(CONSTANTE.ING_MOIS,        args[5]);
                    informations.Add(CONSTANTE.ING_ANNEE,       args[6]);
                    informations.Add(CONSTANTE.ING_MDP,         args[7]);
                    informations.Add(CONSTANTE.ING_COMPTE,      args[8]);

                    break;
            }
        }
        
        private void ChromiumWebBrowser_Initialized(object sender, EventArgs e)
        {
            chrome.RegisterJsObject("ingjs", new INGJs(this));
            chrome.Address = banques_url[Int32.Parse(this.banque)];
            chrome.DownloadHandler = new DownloadHandler(chemin, this);

        }

        private void chrome_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            if (chrome.GetBrowser().GetFrameIdentifiers()[0] == e.Frame.Identifier)
            {
                this.Dispatcher.Invoke(() => { CallGoodBanque(chrome.Address); });
            }
            
        }


        private void CallGoodBanque(String adresse)
        {
            switch(this.banque)
            {
                case "1": //CA
                    CreditAgricole(adresse);
                    break;
                case "2": //ING DIRECT
                    INGDirect(adresse);
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

                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync(" " + commande + " ; Login('" + informations[CONSTANTE.CA_LOGIN] + "', '" + informations[CONSTANTE.CA_MDP] + "')");
                    System.Diagnostics.Debug.WriteLine("login it");
                    etat = 2;
                    break;
                case 2:
                    CAMontant(this.chemin + "/" + this.informations[CONSTANTE.CA_COMPTE] + ".txt");
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

                    chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).ExecuteJavaScriptAsync(" " + commande + " ; Compte('" + informations[CONSTANTE.CA_COMPTE] + "')");
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

        private void INGDirect(String adresse)
        {
            switch (etat)
            {
                case 0:
                    chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync(@"function Login(compte, jour, mois, annee) {
                                                                            document.getElementById('zone1Form:numClient').value = compte;
                                                                            document.getElementById('zone1Form:dateDay').value = jour;
                                                                            document.getElementById('zone1Form:dateMonth').value = mois;
                                                                            document.getElementById('zone1Form:dateYear').value = annee;
                                                                            document.getElementById('zone1Form:submit').click();
                                                                        }
                                                                        Login('" + informations[CONSTANTE.ING_NUM_CLIENT] + "', '" + informations[CONSTANTE.ING_JOUR] + "', '" + informations[CONSTANTE.ING_MOIS] + "', '" + informations[CONSTANTE.ING_ANNEE] + "');");
                    this.etat = 1;
                    break;
                case 1:
                    chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync(@"function downloadIt(origine, dest) {
                                                                                var link = document.createElement('a');
                                                                                link.href = origine;
                                                                                link.download = dest;
                                                                                document.body.appendChild(link);
                                                                                link.click();
                                                                            }

                                                                            downloadIt(document.getElementById('clavierdisplayLogin').children[1].src, 'tmp_banque.png');");
                    this.etat = 2;
                    break;
                case 2:
                    IngGetMontant(this.chemin + "/" + this.informations[CONSTANTE.ING_COMPTE] + ".txt");
                    String command = @"function goToAccount(numCompte){
                                            var list = document.getElementsByClassName('mainclic');
                                            var index = 0;
                                            while(index < list.length) {
                                                if (list[index].children[2].innerText == numCompte)
                                                {
                                                    list[index].click();
                                                    break;
                                                }
                                                index++;
                                            }
                                        }
                                        goToAccount('" + informations[CONSTANTE.ING_COMPTE] + "');";
                    chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync(command);
                    this.etat = 3;
                    CommonUtil.Run(() =>
                    {
                        INGDirect("");
                    }, TimeSpan.FromMilliseconds(5000));
                    break;
                case 3:
                    chrome.GetBrowser().MainFrame.LoadUrl("https://secure.ingdirect.fr/protected/pages/common/download/downloadTransactionsContent.jsf");
                    this.etat = 4;
                    break;
                case 4:
                    chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync("document.getElementById('download_form:download_date_2').click();");
                    chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync("document.getElementById('download_form:downloadButton').click();");
                    break;
                default:
                    break;
            }
        }

        private async void CAMontant(String path)
        {
            String command = @"function Compte(compte) {
                                var td = document.getElementsByClassName('ca-table')[0].getElementsByTagName('td');
                                var index = 0;
                                while(index < td.length) {
                                    if (td[index].innerText.replace(/\D/g,'') == compte) {
                                        console.log(td[index].innerText);
                                        return td[index+2].children[0].innerText;
                                    }
                                    index++;
                                }
                            }";
            CefSharp.JavascriptResponse resp = await chrome.GetBrowser().GetFrame(chrome.GetBrowser().GetFrameIdentifiers()[0]).EvaluateScriptAsync(" " + command + " ; Compte('" + informations[CONSTANTE.CA_COMPTE] + "')");
            WriteFile(path, resp.Result.ToString().Trim());
            
        }

        private void WriteFile(String path, String montant)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (File.Exists(path))
                    File.Delete(path);

                File.Create(path).Dispose();
                TextWriter tw = new StreamWriter(path, true);
                tw.WriteLine(montant);
                tw.Close();
            });
        }

        private async void IngGetMontant(String path)
        {
            String command = @"function goToAccount(numCompte){
                                            var list = document.getElementsByClassName('mainclic');
                                            var index = 0;
                                            while(index < list.length) {
                                                if (list[index].children[2].innerText == numCompte)
                                                {
                                                    return list[index].children[4].innerText;
                                                    break;
                                                }
                                                index++;
                                            }
                                        }
                                        goToAccount('" + informations[CONSTANTE.ING_COMPTE] + "');";
            CefSharp.JavascriptResponse resp = await chrome.GetBrowser().MainFrame.EvaluateScriptAsync(command);
            WriteFile(path, resp.Result.ToString().Replace("€", "").Trim());

        }

        public void isFinish(string path)
        {
            switch(this.banque)
            {
                case "1": //credit agricole
                    chrome.GetBrowser().FocusedFrame.ExecuteJavaScriptAsync("close();");
                    if (path == null)
                        return;
                    if (path.Equals(""))
                        return;
                    else
                        this.Dispatcher.Invoke(() =>
                        {
                            KillAndQuit(1);
                        });
                    break;
                case "2":
                    switch(this.etat)
                    {
                        case 4:
                            this.Dispatcher.Invoke(() =>
                            {
                                KillAndQuit(1);
                            });
                            break;
                        default:
                            INGMdp(path);
                            break;
                    }
                    break;
            }
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            test();
            /*chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync("document.getElementById('download_form:download_date_2').click();");
            CommonUtil.Run(() =>
            {
                chrome.GetBrowser().FocusedFrame.ExecuteJavaScriptAsync("alert(document.getElementById('download_form:downloadButton').innerText);");
                chrome.GetBrowser().FocusedFrame.ExecuteJavaScriptAsync("document.getElementById('download_form:downloadButton').click();");
                // chrome.GetBrowser().MainFrame.ExecuteJavaScriptAsync("document.getElementById('download_form:downloadButton').click();");
            }, TimeSpan.FromMilliseconds(5000));*/
        }

        private async void test()
        {
            
            System.Diagnostics.Debug.WriteLine("Click on login");
            /*CefSharp.JavascriptResponse js = await chrome.GetBrowser().MainFrame.EvaluateScriptAsync("document.getElementById('download_form:download_date_2').click();");
            CefSharp.JavascriptResponse js2 = await chrome.GetBrowser().MainFrame.EvaluateScriptAsync("document.getElementById('download_form:downloadButton').click();");
            System.Diagnostics.Debug.WriteLine("Click on login");*/
            // LOAD THAT /protected/pages/common/download/downloadTransactionsContent.jsf
           
    }

        public void INGMdp(String chemin)
        {
            OcrEngine ocrEngine = new OcrEngine();

            // Clear notifier list
            ocrEngine.ClearNotifies();

            // Clear recognition blocks
            ocrEngine.Config.ClearRecognitionBlocks();

            // Add 2 rectangles to user defined recognition blocks
            // ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(5, 5, 31, 32)); // Detecting A
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(8, 8, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(50, 8, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(89, 8, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(130, 8, 25, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(171, 5, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(8, 48, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(50, 48, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(89, 48, 20, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(130, 48, 25, 20));
            ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(170, 48, 20, 20));
            // ocrEngine.Config.AddRecognitionBlock(RecognitionBlock.CreateTextBlock(209, 111, 28, 34)); // Detecting 6

            // Ignore everything else on the image other than the user defined recognition blocks
            ocrEngine.Config.DetectTextRegions = false;
            // Set the Image property by loading the image from file path location or an instance of MemoryStream 
            ocrEngine.Image = ImageStream.FromFile(chemin);
            String[] clavier = { "8", "1", "l", "9", "7", "Z", "5", "4", "3", "e" };
            // Process the image
            if (ocrEngine.Process())
            {
                // Display the recognized text
                //System.Diagnostics.Debug.WriteLine("img --> " + ocrEngine.Text);
                String[] splited = ocrEngine.Text.ToString().Split('\n');
                for(int i = 0; i < splited.Length; i++)
                {
                    clavier[i] = splited[i].ToString().Trim();
                }
            }

            String retour = "";
            for (int i = 0; i < clavier.Length; i++)
            {
                if (!retour.Equals(""))
                    retour += ",";

                switch(clavier[i])
                {
                    case "e":
                    case "G":
                        retour += "6";
                        clavier[i] = "6";
                        break;
                    case "Z":
                        retour += "2";
                        clavier[i] = "2";
                        break;
                    case "l":
                        retour += "0";
                        clavier[i] = "0";
                        break;
                    default:
                        retour += clavier[i];
                        break;
                }
            }

            chrome.GetBrowser().FocusedFrame.ExecuteJavaScriptAsync(@"function getMdpCache2(mdp, clavierligne) {
                                                                    var e = document.createEvent('MouseEvents');
                                                                    var e2 = document.createEvent('MouseEvents');
                                                                    e.initMouseEvent('mousedown', true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
                                                                    e2.initMouseEvent('mouseup', true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
                                                                    for (var x = 0; x < 3; x++) {
                                                                        document.getElementById('clavierdisplayLogin').dispatchEvent(e);
                                                                        document.getElementById('clavierdisplayLogin').dispatchEvent(e2);
                                                                    }
                                                                    var td = document.getElementById('digitpaddisplayLogin').children;
                                                                    var clavier = clavierligne.split(',');
                                                                    var coord = ['21,21', '61,17', '102, 17', '141,18', '180,16', '21,56', '61,58', '99,56', '143,57', '180,54'];
                                                                    var retour = '';
                                                                    var index = 0;
                                                                    while(index < td.length) {
                                                                        if (td[index].className != 'plein') {
                                                                            //ICI
                                                                            var i = 0;
                                                                            while (i < clavier.length) {
                                                                                var tmp = mdp.charAt(index);
                                                                                if (tmp == clavier[i]) {
                                                                                    if (retour != '') {
                                                                                        retour += ',';
                                                                                    }
                                                                                    retour += coord[i];
                                                                                    break;   
                                                                                }
                                                                                i++;
                                                                            } 
                                                                        }
                                                                        index++;
                                                                    }
                                                                    document.getElementById('mrc:mrldisplayLogin').value = retour;
                                                                    document.getElementById('mrc:mrg').click();
                                                                }
                                                                getMdpCache2('" + informations[CONSTANTE.ING_MDP] + "', '" + retour + "')                ");
        }

        public String IngDecodeMdp(String clavierligne, String mdpCache)
        {

            String[] clavier = clavierligne.Split(';');
            String[] coord = {"21,21", "61,17", "102, 17", "141,18", "180,16",
                                      "21,56", "61,58", "99,56", "143,57", "180,54"};
            String mdp = "041183";


            String retour = "";
            for (int i = 0; i < mdp.Length; i++)
            {
                if (mdpCache[i].Equals('O'))
                {
                    for (int j = 0; j < clavier.Length; j++)
                    {
                        if (clavier[j].Equals(mdp[i].ToString()))
                        {
                            if (!retour.Equals(""))
                                retour += ",";
                            retour += coord[j];
                        }
                    }
                }
            }

            return retour;
        }

        public void imageUrl(string mdp, string cachemdp)
        {
            this.etat = 2;
            //this.MDP = IngDecodeMdp(mdp, cachemdp);
            INGDirect("");
        }
    }


    

   
}
