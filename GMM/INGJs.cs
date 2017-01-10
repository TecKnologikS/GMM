using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMM
{
    public interface INGCallBack {
        void imageUrl(String mdp, String cachemdp);
    }

    public class INGJs
    {
        INGCallBack ing;

        public INGJs(INGCallBack callback)
        {
            this.ing = callback;
        }

        public void imageLink(String mdp, String cachemdp)
        {
            ing.imageUrl(mdp, cachemdp);
        }
    }
}
