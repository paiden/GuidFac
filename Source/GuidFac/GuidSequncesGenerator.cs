namespace PrivateDeveloperInc.GuidFac;


using System;
using System.Collections.Generic;
using System.Text;
internal sealed class GuidSequences
{
    private readonly Guid g;
    public string DFormat { get; private set; }
    public string NFormat { get; private set; }
    public string SingleByteFormat { get; private set; }
    public string MultiByteFormat { get; private set; }

    public GuidSequences(Guid g)
        : this(g, isUpper: false)
    {

    }

    private GuidSequences(Guid g, bool isUpper)
    {
        this.g = g;
        if (isUpper)
        {
            this.InitUpper(g);
        }
        else
        {
            this.InitLower(g);
        }
    }

    private void InitUpper(Guid g)
    {
        this.DFormat = g.ToString("D").ToUpperInvariant();
        this.NFormat = g.ToString("N").ToUpperInvariant();
        this.SingleByteFormat = GetSingleByteFormat(g, isUpper: true);
        this.MultiByteFormat = GetMultiByteFormatUpper(g);
    }

    private void InitLower(Guid g)
    {
        this.DFormat = g.ToString("D").ToLowerInvariant();
        this.NFormat = g.ToString("N").ToLowerInvariant();
        this.SingleByteFormat = GetSingleByteFormat(g, isUpper: false);
        this.MultiByteFormat = GetMultiByteFormatLower(g);
    }

    private static string GetSingleByteFormat(Guid g, bool isUpper)
    {
        var bytes = g.ToByteArray();

        string byteStringUpper = string.Empty;
        string byteStringLower = string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (var b in bytes)
        {
            var byteString = b.ToString("X2");
            string byteWithFormat = isUpper ? byteString.ToUpperInvariant() : byteString.ToLowerInvariant();
            sb.AppendFormat("0x{0}, ", byteWithFormat);
        }

        sb.Remove(sb.Length - 2, 2);
        return "new byte[] { " + sb.ToString() + " }";
    }

    private static string GetMultiByteFormatUpper(Guid g)
    {
        var bytes = g.ToByteArray();
        string mbu = string.Format("0x{0}{1}{2}{3}, 0x{4}{5}, 0x{6}{7}, 0x{8}, 0x{9}, 0x{10}, 0x{11}, 0x{12}, 0x{13}, 0x{14}, 0x{15}",
            bytes[0].ToString("X2").ToUpperInvariant(),
            bytes[1].ToString("X2").ToUpperInvariant(),
            bytes[2].ToString("X2").ToUpperInvariant(),
            bytes[3].ToString("X2").ToUpperInvariant(),
            bytes[4].ToString("X2").ToUpperInvariant(),
            bytes[5].ToString("X2").ToUpperInvariant(),
            bytes[6].ToString("X2").ToUpperInvariant(),
            bytes[7].ToString("X2").ToUpperInvariant(),
            bytes[8].ToString("X2").ToUpperInvariant(),
            bytes[9].ToString("X2").ToUpperInvariant(),
            bytes[10].ToString("X2").ToUpperInvariant(),
            bytes[11].ToString("X2").ToUpperInvariant(),
            bytes[12].ToString("X2").ToUpperInvariant(),
            bytes[13].ToString("X2").ToUpperInvariant(),
            bytes[14].ToString("X2").ToUpperInvariant(),
            bytes[15].ToString("X2").ToUpperInvariant());
        return mbu;
    }

    private static string GetMultiByteFormatLower(Guid g)
    {
        var bytes = g.ToByteArray();
        string mbf = string.Format("0x{0}{1}{2}{3}, 0x{4}{5}, 0x{6}{7}, 0x{8}, 0x{9}, 0x{10}, 0x{11}, 0x{12}, 0x{13}, 0x{14}, 0x{15}",
            bytes[0].ToString("X2").ToLowerInvariant(),
            bytes[1].ToString("X2").ToLowerInvariant(),
            bytes[2].ToString("X2").ToLowerInvariant(),
            bytes[3].ToString("X2").ToLowerInvariant(),
            bytes[4].ToString("X2").ToLowerInvariant(),
            bytes[5].ToString("X2").ToLowerInvariant(),
            bytes[6].ToString("X2").ToLowerInvariant(),
            bytes[7].ToString("X2").ToLowerInvariant(),
            bytes[8].ToString("X2").ToLowerInvariant(),
            bytes[9].ToString("X2").ToLowerInvariant(),
            bytes[10].ToString("X2").ToLowerInvariant(),
            bytes[11].ToString("X2").ToLowerInvariant(),
            bytes[12].ToString("X2").ToLowerInvariant(),
            bytes[13].ToString("X2").ToLowerInvariant(),
            bytes[14].ToString("X2").ToLowerInvariant(),
            bytes[15].ToString("X2").ToLowerInvariant());

        return mbf;
    }

    private GuidSequences AsLower()
    {
        return new GuidSequences(this.g, isUpper: false);
    }

    private GuidSequences AsUpper()
    {
        return new GuidSequences(this.g, isUpper: true);
    }

    public List<string> AsList()
    {
        return new List<string>
        {
            this.DFormat,
            this.NFormat,
            this.SingleByteFormat,
            this.MultiByteFormat,
        };
    }

    public List<string> GetInFormat(GuidFormat format)
    {
        var lower = this.AsLower().AsList();
        var upper = this.AsUpper().AsList();
        List<string> result = new List<string>();

        for (int i = 0; i < lower.Count; i++)
        {
            if ((format & GuidFormat.Upper) != 0) { result.Add(upper[i]); }
            if ((format & GuidFormat.Lower) != 0) { result.Add(lower[i]); }
        }

        return result;
    }
}
