using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace JocysCom.Web.Security
{

	public class SessionData
	{
		public string SessionId;
		public DataRow UserExtraInfoRow;
		public DateTime LastUpdateDate;
	}
	
	/// <summary>
	/// Summary description for Sessions
	/// </summary>
	public class SessionList : IDisposable
	{

		public DataView Commands;

        private System.Collections.SortedList m_items;

        public System.Collections.SortedList Items
        {
            get { return m_items; }
            set { m_items = value; }
        }
	

		public DataTable GetNewCommandsTable()
		{
			DataTable commandsTable = new DataTable("Commands");
			commandsTable.Columns.Add("MessageId", typeof(string));
			commandsTable.Columns.Add("MessageType", typeof(string));
			commandsTable.Columns.Add("Header1", typeof(string)); // LEAVE
			commandsTable.Columns.Add("Header2", typeof(string));
			commandsTable.Columns.Add("Header3", typeof(string));
			commandsTable.Columns.Add("Header4", typeof(string));
			commandsTable.Columns.Add("CreationDate", typeof(DateTime));
			commandsTable.Columns.Add("ShippingDate", typeof(DateTime));
			commandsTable.Columns.Add("DeliveryDate", typeof(DateTime));
			commandsTable.Columns.Add("From", typeof(string)); // ME
			commandsTable.Columns.Add("To", typeof(string)); // #CHANNEL
			commandsTable.Columns.Add("Subject", typeof(string));
			commandsTable.Columns.Add("Body", typeof(string));
			return commandsTable;
		}

        public SessionList()
		{
            m_items = new System.Collections.SortedList();
			Commands = new DataView();
			Commands.Table = GetNewCommandsTable();
		}

		public void Clear()
		{
			//  Code that runs on application shutdown
			Items.Clear();
		}

		public void Add(string sessionId, SessionData sessionData)
		{
			if (Items.ContainsKey(sessionId))
			{
				// Remove old key... this must never happen!
				// Look later and throw error here.
				Items.Remove(sessionId);
			}
			Items.Add(sessionId, sessionData);
		}

		public void Remove(string sessionId)
		{
			Items.Remove(sessionId);
		}

		public void SetValue(string sessionId, SessionData sessionData)
		{
			Items[sessionId] = sessionData;
		}

		public SessionData GetValue(string sessionId)
		{
			SessionData sesionData = new SessionData();
			if (Items[sessionId] != null) sesionData = (SessionData)Items[sessionId];
			return sesionData;
		}


		public DataSet GetData()
		{
			DataSet dataSet = new DataSet("Sessions");
			DataTable table = new DataTable("List");
			dataSet.Tables.Add(table);
			table.Columns.Add("SessionID", typeof(string));
			table.Columns.Add("EmployeeName", typeof(string));
			table.Columns.Add("LastUpdateDate", typeof(DateTime));
			for (int i = 0; i < Items.Count; i++)
			{
				SessionData sessionData = (SessionData)this.Items.GetByIndex(i);
				string employeeName = "";
				DataRow row = sessionData.UserExtraInfoRow;
				if (row != null)
				{
					employeeName = row["firstName"].ToString() + " " + row["lastName"].ToString();
				}
				table.Rows.Add(new object[] { sessionData.SessionId, employeeName, sessionData.LastUpdateDate });
			}
			return dataSet;
		}

        public void InitOnSessionStart(object sender, EventArgs e)
		{
			HttpContext.Current.Application.Lock();
			// Add new session. 
			SessionData sessionData = new SessionData();
			sessionData.SessionId = HttpContext.Current.Session.SessionID;
			Add(HttpContext.Current.Session.SessionID, sessionData);
			HttpContext.Current.Application.UnLock();
		}

		public void InitOnSessionEnd(System.Web.HttpApplicationState application, System.Web.SessionState.HttpSessionState session)
		{
			if (session != null)
			{
				// Remove employee from list.
				application.Lock();
				Remove(session.SessionID);
				application.UnLock();
			}
			else
			{
				System.Diagnostics.Trace.WriteLine("Session is null!");
			}
		}

		public void InitOnApplicationEnd()
		{
			//  Code that runs on application shutdown
			Clear();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources.
			}
			// Free native resources.
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(true);
		}

	}
}