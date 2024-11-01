namespace TimezoneGenerator.Data;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

public class KasaTimezone {

    public bool dst { get; set; }
    public string? description { get; set; }
    public string? description_dst { get; set; }
    public string offset { get; set; }
    public string offset_dst { get; set; }
    public int index { get; set; }

}