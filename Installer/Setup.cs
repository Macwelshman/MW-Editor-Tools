using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

[assembly:AssemblyTitle("MW Editor Tools Installer")]
[assembly:AssemblyProduct("MW Editor Tools")]
[assembly:AssemblyVersion("0.4.180.0")]
[assembly:AssemblyFileVersion("0.4.180.0")]
namespace MWEditorToolsInstaller {
    internal static class Program {
        [STAThread] private static void Main() {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new SetupForm());
        }
    }
    internal sealed class SetupForm:Form {
        private readonly Label destination=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,BorderStyle=BorderStyle.FixedSingle};
        private readonly ToolTip hints=new ToolTip();
        private readonly TextBox result=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
        private readonly Button install=new Button{Text="Install / update",AutoSize=true};
        private readonly Button browse=new Button{Text="Choose folder…",AutoSize=true};
        internal SetupForm() {
            Text="MW Editor Tools "+InstallerCore.Version;StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(660,350);MinimumSize=new Size(580,360);Font=new Font("Segoe UI",10);MaximizeBox=false;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=6};
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label{Text="MW Editor Tools "+InstallerCore.Version,Font=new Font(Font,FontStyle.Bold),AutoSize=true,Margin=new Padding(0,0,0,10)});
            layout.Controls.Add(new Label{Text="Close Cities: Skylines II before installing. Existing mod files are backed up and every installed file is verified.",AutoSize=true,MaximumSize=new Size(610,0),Margin=new Padding(0,0,0,12)});
            layout.Controls.Add(new Label{Text="Installation folder",AutoSize=true});
            var pathRow=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,Margin=new Padding(0,0,0,12)};pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));pathRow.Controls.Add(destination);pathRow.Controls.Add(browse);layout.Controls.Add(pathRow);
            destination.Text=InstallerCore.DefaultDestination;hints.SetToolTip(destination,destination.Text);destination.TextChanged+=(s,e)=>hints.SetToolTip(destination,destination.Text);result.Text="Ready. No administrator privileges are required for your user Mods folder.";layout.Controls.Add(result);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Margin=new Padding(0,12,0,0)};
            var close=new Button{Text="Close",AutoSize=true};close.Click+=(s,e)=>Close();actions.Controls.Add(close);actions.Controls.Add(install);layout.Controls.Add(actions);Controls.Add(layout);
            browse.Click+=(s,e)=>{using(var dialog=new FolderBrowserDialog{Description="Select your Cities: Skylines II user Mods folder.",SelectedPath=System.IO.Path.GetDirectoryName(destination.Text)})if(dialog.ShowDialog(this)==DialogResult.OK){var chosen=dialog.SelectedPath;destination.Text=String.Equals(System.IO.Path.GetFileName(chosen),"MWEditorTools",StringComparison.OrdinalIgnoreCase)?chosen:System.IO.Path.Combine(chosen,"MWEditorTools");}};
            install.Click+=(s,e)=>{
                install.Enabled=false;browse.Enabled=false;UseWaitCursor=true;result.Text="Installing…";result.Refresh();
                try {var done=InstallerCore.Install(destination.Text,InstallerCore.ReadPayload());result.Text="Installed and verified "+done.Files+" files.\r\n\r\nFolder: "+done.Destination+"\r\n\r\n"+(done.Backup==null?"New installation; no prior mod files.":"Previous installation backed up to:\r\n"+done.Backup)+"\r\n\r\nYou can now open the Asset Editor and use MW Editor Tools.";}
                catch(Exception error){result.Text=error.Message;}
                finally {UseWaitCursor=false;install.Enabled=true;browse.Enabled=true;}
            };
            AcceptButton=install;CancelButton=close;
        }
        protected override void Dispose(bool disposing){if(disposing)hints.Dispose();base.Dispose(disposing);}
    }
}
