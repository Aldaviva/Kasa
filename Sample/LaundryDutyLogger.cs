using Kasa;
using System.Text;
using Timer = System.Threading.Timer;

namespace Sample;

public class LaundryDutyLogger {

    public static void Main() {
        using IKasaOutlet outlet = new KasaOutlet("washingmachine.outlets.aldaviva.com");

        using FileStream tsvFile = File.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "washingmachine-power.tsv"), FileMode.Append, FileAccess.Write,
            FileShare.Read);
        using StreamWriter tsvWriter = new(tsvFile, new UTF8Encoding(false, true));

        CancellationTokenSource cts   = new();
        DateTime                start = DateTime.Now;

        WriteTsvLine(string.Join('\t', "elapsed_sec", "current_mA", "voltage_mV", "power_mW"));

        Timer timer = new(async _ => {
            PowerUsage power = await outlet.EnergyMeter.GetInstantaneousPowerUsage();
            WriteTsvLine(string.Join('\t', (DateTime.Now - start).TotalSeconds.ToString("N0"), power.Current.ToString("N0"), power.Voltage.ToString("N0"), power.Power.ToString("N0")));
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Console.CancelKeyPress += (_, _) => cts.Cancel();
        cts.Token.WaitHandle.WaitOne();
        timer.Dispose();

        void WriteTsvLine(string line) {
            tsvWriter.WriteLine(line);
            tsvWriter.Flush();
            Console.WriteLine(line);
        }
    }

}