namespace PrivateDeveloperInc.GuidFac;

using Microsoft.VisualStudio.PlatformUI;

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using FScreen = System.Windows.Forms.Screen;

/// <summary>
/// Interaction logic for GuidFacWindow.xaml
/// </summary>
public partial class GuidFacWindow : DialogWindow
{
    private readonly GuidFacConfig config;
    public string InsertGuid { get { return this.listViewFormats.SelectedItem as string; } }
    private const int MakeNiceMargin = 50;
    private const int CaretSize = 12;
    private Guid currentGuid;
    private readonly RadioButtonSelector buttonSelector;

    public GuidFacWindow(Point pos, UIElement host, GuidFacConfig config)
    {
        InitializeComponent();

        this.config = config;

        var window = FindWindow(host);
        this.Owner = window;
        var desiredScreenLocation = host.PointToScreen(pos);
        var windowOrigin = host.PointToScreen(new Point(0, 0));
        var lowerRightLocation = new Point(pos.X + this.Width, pos.Y + this.Height);
        var lowerRightScreenLocation = host.PointToScreen(lowerRightLocation) - windowOrigin;
        var screen = FScreen.FromHandle(new WindowInteropHelper(window).Handle);
        var area = screen.WorkingArea;

        if (lowerRightScreenLocation.Y + MakeNiceMargin > area.Height)
        {
            pos.Y -= this.Height + CaretSize;
        }

        if (lowerRightScreenLocation.X + MakeNiceMargin > area.Width + area.X)
        {
            pos.X -= this.Width + MakeNiceMargin;
        }

        this.Left = pos.X;
        this.Top = pos.Y;

        this.currentGuid = Guid.NewGuid();
        this.Update();
        this.UpdateGuidFormatButtonStates();
        this.toggleButtonSetClipboard.IsChecked = config.SetClipboard;

        var gfm = this.GetGuidFormatFromButtonStates();
        var selected = ((int)gfm) - 1;
        this.buttonSelector = new RadioButtonSelector(selected, this.radioButtonLower, this.radioButtonUpper, this.radioButtonBoth);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat) { return; }

        if (e.Key == Key.Space)
        {
            this.currentGuid = Guid.NewGuid();
            this.Update();
        }
        else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            this.Update();
        }
        else if (e.Key == Key.Up || e.Key == Key.K)
        {
            if (this.listViewFormats.SelectedIndex > 0)
            {
                this.listViewFormats.SelectedIndex--;
            }
        }
        else if (e.Key == Key.Down || e.Key == Key.J)
        {
            if (this.listViewFormats.SelectedIndex < this.listViewFormats.Items.Count - 1)
            {
                this.listViewFormats.SelectedIndex++;
            }
        }
        else if (e.Key == Key.H || e.Key == Key.Left)
        {
            this.buttonSelector.Previous();
        }
        else if (e.Key == Key.L || e.Key == Key.Right)
        {
            this.buttonSelector.Next();
        }
        else if (e.Key == Key.Escape)
        {
            this.CloseAndIgnore();
        }
        else if (e.Key == Key.Enter)
        {
            this.CloseAndInsert();
        }
        else if (e.Key == Key.C)
        {
            this.toggleButtonSetClipboard.IsChecked = !this.toggleButtonSetClipboard.IsChecked;
        }
        e.Handled = true;
    }

    private Window FindWindow(UIElement element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null && parent is not Window)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return (Window)parent;
    }

    private void Update(bool keepIndex = true)
    {
        var gfm = this.GetGuidFormatFromButtonStates();
        if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && gfm != GuidFormat.Both)
        {
            gfm = ~gfm;
        }

        var sequence = new GuidSequences(this.currentGuid);
        var sequenceList = sequence.GetInFormat(gfm);

        int lastIndex = this.listViewFormats.SelectedIndex;
        this.listViewFormats.Items.Clear();
        foreach (var i in sequenceList) { this.listViewFormats.Items.Add(i); }
        lastIndex = Math.Min(this.listViewFormats.Items.Count - 1, lastIndex);
        this.listViewFormats.SelectedIndex = keepIndex ? lastIndex : 0;
    }

    private void CloseAndInsert()
    {
        var gfmt = this.GetGuidFormatFromButtonStates();
        GuidFacPackage.Config.GuidFacMode = gfmt;
        GuidFacPackage.Config.SetClipboard = this.toggleButtonSetClipboard.IsChecked.Value;
        var t = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    GuidFacPackage.Config.Write();
                }
                catch { }
            });

        if (GuidFacPackage.Config.SetClipboard)
        {
            Clipboard.SetText(this.InsertGuid);
        }

        this.DialogResult = true;
        this.Close();
    }

    private void CloseAndIgnore()
    {
        this.DialogResult = false;
        this.Close();
    }

    private GuidFormat GetGuidFormatFromButtonStates()
    {
        GuidFormat gfm = GuidFormat.Both;
        if (this.radioButtonLower.IsChecked == true) { gfm = GuidFormat.Lower; }
        else if (this.radioButtonUpper.IsChecked == true) { gfm = GuidFormat.Upper; }

        return gfm;
    }

    private void OnListViewFormatsMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        this.CloseAndInsert();
    }

    private void UpdateGuidFormatButtonStates()
    {
        var mode = this.config.GuidFacMode;

        this.radioButtonLower.IsChecked =
            this.radioButtonUpper.IsChecked =
            this.radioButtonBoth.IsChecked = false;

        switch (mode)
        {
            case GuidFormat.Lower: radioButtonLower.IsChecked = true; break;
            case GuidFormat.Upper: radioButtonUpper.IsChecked = true; break;
            case GuidFormat.Both: radioButtonBoth.IsChecked = true; break;
        }
    }

    private void HandleRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        this.Update(keepIndex: false);
    }

    private void HandleDialogWindowKeyUp(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat) { return; }
        this.Update();
    }
}
