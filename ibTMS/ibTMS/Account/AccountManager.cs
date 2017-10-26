using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ibTMS
{
    class AccountManager
    {
        public static string IBAccount = "";
        public static AccountInfo accountInfo;

        //only one IB account update can be subscribed at a time. Irrelevent for other brokers.
        //subscribe 2 accounts will cause errors
        public static void subscribeIBUpdate(string accountName)
        {
            IBProcess.GetIbClient((ibClient) =>
            {
                ibClient.ClientSocket.reqAccountUpdates(false, IBAccount);
                IBAccount = accountName;
                ibClient.ClientSocket.reqAccountUpdates(true, IBAccount);
            });
        }
    }
}
