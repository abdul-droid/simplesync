namespace sync.client
{
    partial class ProjectInstaller
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
            this.UnitySyncServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.UnitySyncServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // UnitySyncServiceProcessInstaller
            // 
            this.UnitySyncServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.UnitySyncServiceProcessInstaller.Password = null;
            this.UnitySyncServiceProcessInstaller.Username = null;
            // 
            // UnitySyncServiceInstaller
            // 
            this.UnitySyncServiceInstaller.Description = "Sync data for unity database";
            this.UnitySyncServiceInstaller.DisplayName = "Unity Sync";
            this.UnitySyncServiceInstaller.ServiceName = "UnitySync";
            this.UnitySyncServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.UnitySyncServiceProcessInstaller,
            this.UnitySyncServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller UnitySyncServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller UnitySyncServiceInstaller;
    }
}