using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Data.SqlClient;
using System.Configuration;

namespace ibTMS
{
    public class TicketPoolManager
    {
        public TicketPoolManager() { }

        public static void SendTicket(Ticket tk)
        {
            //long sNum = TicketPoolRecovery.WriteToLog(tk.Sid, tk.ToString());
            
            var proc = new IBProcess(tk.Gid);

            proc.Run(tk);
        }

		public static void EnableGetAccountInfo(Ticket tk)
		{
			//long sNum = TicketPoolRecovery.WriteToLog(tk.Sid, tk.ToString());

			var proc = new IBProcess(tk.Gid);
			proc.EnableGetInfo(tk);
			//proc.Run(tk);
		}

        public static bool IsIBSocketConnected(Ticket tk)
        {
            //long sNum = TicketPoolRecovery.WriteToLog(tk.Sid, tk.ToString());

            var proc = new IBProcess(tk.Gid);
            return proc.IsIBSocketConnected(tk);
            //proc.Run(tk);
        }
        #region Helpers
        #endregion
    }
}
