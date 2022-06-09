namespace PrivateDeveloperInc.GuidFac;

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
///
/// The minimum requirement for a class to be considered a valid package for Visual Studio
/// is to implement the IVsPackage interface and register itself with the shell.
/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
/// to do it: it derives from the Package class that provides the implementation of the
/// IVsPackage interface and uses the registration attributes defined in the framework to
/// register itself and its components with the shell.
/// </summary>
// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
// a package.
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
// This attribute is used to register the information needed to show this package
// in the Help/About dialog of Visual Studio.
[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
// This attribute is needed to let the shell know that this package exposes some menus.
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(GuidList.guidGuidFacPkgString)]
[ProvideAutoLoad(UIContextGuids.CodeWindow, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class GuidFacPackage : AsyncPackage
{
    internal static GuidFacConfig Config { get; private set; }
    /// <summary>
    /// Default constructor of the package.
    /// Inside this method you can place any initialization code that does not require
    /// any Visual Studio service because at this point the package object is created but
    /// not sited yet inside Visual Studio environment. The place to do all the other
    /// initialization is the Initialize method.
    /// </summary>
    public GuidFacPackage()
    {
        Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

        try
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "GuidFac " + GuidList.guidGuidFacPkgString);
            path = Path.Combine(path, "config.ini");
            Config = new GuidFacConfig(path);
            Config.Read();
        }
        catch (Exception exc)
        {
            MessageBox.Show(Application.Current.MainWindow, exc.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }

    /////////////////////////////////////////////////////////////////////////////
    // Overridden Package Implementation
    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
        base.Initialize();

        // Add our command handlers for menu (commands must exist in the .vsct file)
        OleMenuCommandService mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (null != mcs)
        {
            // Create the command for the menu item.
            CommandID menuCommandID = new CommandID(GuidList.guidGuidFacCmdSet, (int)PkgCmdIDList.cmdidInsertGuid);
            MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
            mcs.AddCommand(menuItem);
        }

        await base.InitializeAsync(cancellationToken, progress);
    }
    #endregion

    /// <summary>
    /// This function is the callback used to execute a command when the a menu item is clicked.
    /// See the Initialize method to see how the menu item is associated to this function using
    /// the OleMenuCommandService service and the MenuCommand class.
    /// </summary>
    private void MenuItemCallback(object sender, EventArgs e)
    {
        IVsTextManager tm = (IVsTextManager)GetService(typeof(SVsTextManager));
        if (tm != null)
        {
            IVsTextView tv = null;
            tm.GetActiveView(1, null, out tv);
            var wpfView = GetWpfTextView(tv);

            if (tv != null)
            {
                int anchorLine, anchorCol, endLine, endCol;
                tv.GetSelection(out anchorLine, out anchorCol, out endLine, out endCol);

                if (anchorLine != endLine) return;

                int startCol = Math.Min(anchorCol, endCol);
                endCol = Math.Max(anchorCol, endCol);
                int toReplace = Math.Max(0, endCol - startCol);

                var pos = CalculateCaretPosition(wpfView);
                var window = new GuidFacWindow(pos, wpfView as UIElement, Config);
                var result = window.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    int line, col;
                    tv.GetCaretPos(out line, out col);
                    tv.ReplaceTextOnLine(line, startCol, toReplace, window.InsertGuid, window.InsertGuid.Length);

                    tv.SetCaretPos(line, startCol + window.InsertGuid.Length); //otherwise caret jumps sometimes don't know why
                }
            }
        }
    }

    private Point CalculateCaretPosition(IWpfTextView activeWpfTextView)
    {
        var textViewOrigin = (activeWpfTextView as UIElement).PointToScreen(new Point(0, 0));

        var caretPos = activeWpfTextView.Caret.Position.BufferPosition;
        var charBounds = activeWpfTextView
            .GetTextViewLineContainingBufferPosition(caretPos)
            .GetCharacterBounds(caretPos);
        double textBottom = charBounds.Bottom;
        double textX = charBounds.Right;

        double newLeft = textViewOrigin.X + textX - activeWpfTextView.ViewportLeft;
        double newTop = textViewOrigin.Y + textBottom - activeWpfTextView.ViewportTop;

        return new Point(newLeft, newTop);
    }

    private IWpfTextView GetWpfTextView(IVsTextView vTextView)
    {
        IWpfTextView view = null;
        IVsUserData userData = vTextView as IVsUserData;

        if (null != userData)
        {
            IWpfTextViewHost viewHost;
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out holder);
            viewHost = (IWpfTextViewHost)holder;
            view = viewHost.TextView;
        }

        return view;
    }
}
