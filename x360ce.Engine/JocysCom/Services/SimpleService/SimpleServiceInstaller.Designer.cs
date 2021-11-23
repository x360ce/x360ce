#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
namespace JocysCom.ClassLibrary.Services.SimpleService
{
	partial class SimpleServiceInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.AppEventLogInstaller = new System.Diagnostics.EventLogInstaller();
			this.AppServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.AppServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// AppEventLogInstaller
			// 
			this.AppEventLogInstaller.CategoryCount = 0;
			this.AppEventLogInstaller.CategoryResourceFile = null;
			this.AppEventLogInstaller.Log = "Company";
			this.AppEventLogInstaller.MessageResourceFile = null;
			this.AppEventLogInstaller.ParameterResourceFile = null;
			this.AppEventLogInstaller.Source = "CompanyProductSource";
			// 
			// AppServiceProcessInstaller
			// 
			this.AppServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
			this.AppServiceProcessInstaller.Password = null;
			this.AppServiceProcessInstaller.Username = null;
			// 
			// AppServiceInstaller
			// 
			this.AppServiceInstaller.Description = "Company Product";
			this.AppServiceInstaller.DisplayName = "Company Product";
			this.AppServiceInstaller.ServiceName = "CompanyProduct";
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.AppEventLogInstaller,
            this.AppServiceProcessInstaller,
            this.AppServiceInstaller});

		}

#endregion

		public System.Diagnostics.EventLogInstaller AppEventLogInstaller;
		public System.ServiceProcess.ServiceProcessInstaller AppServiceProcessInstaller;
		public System.ServiceProcess.ServiceInstaller AppServiceInstaller;

	}
}
#endif
