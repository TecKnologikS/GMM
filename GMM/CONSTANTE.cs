using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GMM
{
    class CONSTANTE
    {
        public const String CA_LOGIN = "login";
        public const String CA_MDP = "mdp";
        public const String CA_COMPTE = "compte";


        public const String ING_NUM_CLIENT = "numclient";
        public const String ING_JOUR = "jour";
        public const String ING_MOIS = "mois";
        public const String ING_ANNEE = "annee";
        public const String ING_MDP = "mdp";
        public const String ING_COMPTE = "compte";
    }

    public static class ActionExtensions
    {
        public static void RunAfter(this Action action, TimeSpan span)
        {
            var dispatcherTimer = new DispatcherTimer { Interval = span };
            dispatcherTimer.Tick += (sender, args) =>
            {
                var timer = sender as DispatcherTimer;
                if (timer != null)
                {
                    timer.Stop();
                }

                action();
            };
            dispatcherTimer.Start();
        }
    }
    
    public static class CommonUtil
    {
        public static void Run(Action action, TimeSpan afterSpan)
        {
            action.RunAfter(afterSpan);
        }
    }
}
