using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ibTMS
{
    public class AccountInfo
    {
        public string accountName = "";
        public string broker = "";
        public double availableFunds = -1; //account available funds
        public double buyingPower = -1; //account available funds (from all currencies) * leverage
        public string updatedTime = "";
        public double initMarginReq = -1; //account total initial margin requirement

        public AccountInfo() { }
        public AccountInfo(string accountName, string broker, double availableFunds, double buyingPower, double initMarginReq, string updatedTime)
        {
            this.accountName = accountName;
            this.broker = broker;
            this.availableFunds = availableFunds;
            this.buyingPower = buyingPower;
            this.initMarginReq = initMarginReq;
            this.updatedTime = updatedTime;
        }
        
    }
}
