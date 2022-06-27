namespace PrivateDeveloperInc.GuidFac;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

[Flags]
public enum GuidFormat
{
    Lower = 0x01,
    Upper = 0x02,
    Both = Lower | Upper
}

public sealed class GuidFacConfig
{
    private const string GuidFacModeKey = "GuidFaceMode";
    private const string SetClipboardKey = "SetClipboard";

    private readonly string path;

    private Dictionary<string, object> settings = new Dictionary<string, object>();

    public event EventHandler ConfigChanged = delegate { };

    public GuidFacConfig(string configPath)
    {
        this.path = configPath;
        this.settings[GuidFacModeKey] = GuidFormat.Both;
        this.settings[SetClipboardKey] = true;
    }

    public GuidFormat GuidFacMode
    {
        get => (GuidFormat)this.settings[GuidFacModeKey];
        set
        {
            this.settings[GuidFacModeKey] = value;
            this.ConfigChanged(this, EventArgs.Empty);
        }
    }

    public bool SetClipboard
    {
        get => (bool)this.settings[SetClipboardKey];
        set
        {
            this.settings[SetClipboardKey] = value;
            this.ConfigChanged(this, EventArgs.Empty);
        }
    }

    public void Write()
    {
        try
        {
            var dir = Path.GetDirectoryName(this.path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new StreamWriter(path, append: false))
            {
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}={1}", GuidFacModeKey, this.GuidFacMode));
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}={1}", SetClipboardKey, this.SetClipboard));
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(Application.Current.MainWindow, exc.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Read()
    {
        try
        {
            if (File.Exists(this.path))
            {
                using var reader = new StreamReader(this.path);
                string l;
                while ((l = reader.ReadLine()) != null)
                {
                    var parts = l.Split(new char[] { '=' });
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        switch (key)
                        {
                            case GuidFacModeKey: GuidFacMode = GuidFormatFromString(value); break;
                            case SetClipboardKey: SetClipboard = bool.Parse(value); break;
                        }
                    }
                }
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(Application.Current.MainWindow, exc.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static GuidFormat GuidFormatFromString(string s)
    {
        if (Enum.TryParse<GuidFormat>(s, out GuidFormat fmt)) return fmt;
        else return GuidFormat.Both;
    }
}
