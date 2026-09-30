namespace RequestsManager.Application.Dtos;

/// <summary>RowVersion travels to the client as base64 (8 bytes in SQL Server).</summary>
public static class RowVersionCodec
{
    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool TryDecode(string? value, out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value)) return false;
        var buffer = new byte[8];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != 8) return false;
        rowVersion = buffer;
        return true;
    }

    public static byte[] Decode(string value) =>
        TryDecode(value, out var bytes) ? bytes : throw new FormatException("Invalid rowVersion.");
}
