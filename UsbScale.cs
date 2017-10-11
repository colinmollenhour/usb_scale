using System;
using System.Threading;
using HidLibrary;
using ScaleInterface;

namespace ScaleReader
{
  class Program
  {
    public static void Error(string message)
    {
      Console.WriteLine(@"{{""success"":false,""error"":""{0}""}}", message);
    }
    public static void Main(string[] args)
    {
      int scale = 0;
      int retryTime = 100;
      int timeoutLength = 3000;
      bool debug = false;
      for (int i = 0; i < args.Length; i++)
      {
        string arg = args[i];
        if (arg.StartsWith("--"))
        {
          // Command-line flags take the form `--arg-name=100` or `--arg-name`
          string argName;
          int argValue = 0;
          // We're assuming all argument values will be integers.
          if (arg.IndexOf('=') != -1)
          {
            argName = arg.Substring(2, arg.LastIndexOf('=') - 2);
            argValue = Int32.Parse(arg.Substring(arg.LastIndexOf('=') + 1));
          } else {
            argName = arg.Substring(2, arg.Length - 2);
          }
          switch (argName)
          {
            case "retry":
              retryTime = argValue;
              break;
            case "fail":
              timeoutLength = argValue;
              break;
            case "debug":
              debug = true;
              break;
          }
        }
        // The last argument should always be the scale number.
        else if (!(Int32.TryParse(arg, out scale) && i == args.Length - 1))
        {
          Error(string.Format("Invalid command-line argument. -- {0}", arg));
          Environment.Exit(1);
        }
      }
      
      decimal? weight;

      USBScale s = new USBScale(scale, retryTime, timeoutLength);
      s.Connect();

      if (s.IsConnected)
      {
        ScaleWeightStatus status = s.GetWeight(out weight);
        s.Disconnect();
        if (status != ScaleWeightStatus.Stable)
        {
          Error(USBScale.ErrorStringFor(status));
        } else {
          Console.WriteLine(@"{{""success"":true,""weight"":{0},""units"":""lbs""}}", weight);
        }
        if (debug) {
          s.DebugScaleData();
        }
      } else {
        Error("No Scale Connected.");
      }
    }
  }
}
