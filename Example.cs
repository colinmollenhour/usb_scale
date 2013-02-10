/*
 *  * Created by SharpDevelop.
 *   * User: nricciar
 *    * Date: 10/7/2010
 *     * Time: 11:53 AM
 *      * 
 *       * To change this template use Tools | Options | Coding | Edit Standard Headers.
 *        */
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
      Console.Write(@"{{""success"":false,""error"":""{0}""}}", message);
    }
    public static void Main(string[] args)
    {
      int scale = 0;
      int retryTime = 100;
      int timeoutLength = 3000;
      for (int i = 0; i < args.Length; i++)
      {
        string arg = args[i];
        if (arg.StartsWith("--"))
        {
          // TODO make sure arg matches --[a-z-]+=[0-9]+
          // Command-line flags take the form `--arg-name=100`.
          // C#'s Substring() takes a starting index and a /length/.
          string argName = arg.Substring(2, arg.LastIndexOf('=') - 2);
          // We're assuming all argument values will be integers.
          int argValue = Int32.Parse(arg.Substring(arg.LastIndexOf('=') + 1));
          switch (argName)
          {
            case "retry":
              retryTime = argValue;
              break;
            case "fail":
              timeoutLength = argValue;
              break;
          }
        }
        // The last argument should always be the scale number.
        else if (!(Int32.TryParse(arg, out scale) && i == args.Length - 1))
        {
          Error(string.Format("Invalid command-line argument. -- {0}", arg));
        }
      }
      decimal? weight;
      bool? isStable;

      USBScale s = new USBScale(scale, retryTime, timeoutLength);
      s.Connect();

      if (s.IsConnected)
      {
        ScaleWeightStatus status = s.GetWeight(out weight, out isStable);
        if (status != ScaleWeightStatus.Stable)
        {
          Error(USBScale.ErrorStringFor(status));
        }
        s.DebugScaleData();
        s.Disconnect();
        // I'm writing out json manually because including a library to do this seems like overkill.
        Console.Write(@"{{""success"":true,""weight"":{0},""units"":""lbs""}}", weight);
      } else {
        Error("No Scale Connected.");
      }
    }
  }
}
