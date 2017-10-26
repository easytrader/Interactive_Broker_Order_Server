using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ibTMS
{
    public class Ticket
    {
        #region Private Field
        private Guid gid;
        private string strategyName;
        private int strategyOrderNumber;
        private string instrumentType;
        private string symbol;
        private string currency;
        private TicketType action;
        private double quantity;
        private double avgFilledPrice;
        private long? timeout;
        
        #endregion

        #region Properties
        public Guid Gid
        {
            get { return gid; }
            set { gid = value; }
        }
        public string StrategyName
        {
            get { return strategyName; }
            set { strategyName = value; }
        }

        public int StrategyOrderNumber
        {
            get { return strategyOrderNumber; }
            set { strategyOrderNumber = value; }
        }
        public string InstrumentType
        {
            get { return instrumentType; }
            set { instrumentType = value; }
        }
        public string Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }
        public string Currency
        {
            get { return currency; }
            set { currency = value; }
        }
        public TicketType Action
        {
            get { return action; }
            set { action = value; }
        }
        public double Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }
        public long? Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }
        #endregion

        #region Constructor
        public Ticket()
        {
            Gid = Guid.NewGuid();
        }
        public Ticket(string strategyName, int strategyOrderNumber, string instrumentType, string symbol, string currency, TicketType action, double quantity, long? timeout = null) : base()
        {
            this.strategyName = strategyName;
            this.strategyOrderNumber = strategyOrderNumber;
            this.instrumentType = instrumentType;
            this.symbol = symbol;
            this.currency = currency;
            this.action = action;
            this.quantity = quantity;
            this.timeout = timeout;
        }
        #endregion
    }
}
