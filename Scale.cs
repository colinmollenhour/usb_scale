/*
 * User: nricciar
 * Date: 10/8/2010
 */
namespace ScaleInterface
{
  using HidLibrary;
  using System.Threading;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  public enum ScaleWeightStatus
  {
    // Byte 1 == Scale Status (1 == Fault, 2 == Stable @ 0, 3 == In Motion, 4 == Stable, 5 == Under 0, 6 == Over Weight, 7 == Requires Calibration, 8 == Requires Re-Zeroing)
    Fault = 1,
    Empty,
    InMotion,
    Stable,
    NegativeWeight,
    OverWeight,
    RequiresCalibration,
    RequiresReZeroing
  }

  class USBScale
  {
    private HidDevice scale;
    private HidDeviceData inData;
    private int scaleNum;
    private int retryTime;
    private int timeoutLength;

    public static string ErrorStringFor(ScaleWeightStatus s)
    {
      switch (s)
      {
        case ScaleWeightStatus.Fault:
          return "Fault.";
        case ScaleWeightStatus.Empty:
          return "Scale is empty.";
        case ScaleWeightStatus.InMotion:
          return "Scale is still in motion.";
        case ScaleWeightStatus.Stable:
          return "Scale is stable. This isn't an error.";
        case ScaleWeightStatus.NegativeWeight:
          return "Scale read a negative weight.";
        case ScaleWeightStatus.OverWeight:
          return "Scale is overloaded.";
        case ScaleWeightStatus.RequiresCalibration:
          return "Scale requires calibration.";
        case ScaleWeightStatus.RequiresReZeroing:
          return "Scale requires re-zeroing.";
        default:
          return string.Format("Unkown error #{0}", (int)s);
      }
    }

    public bool IsConnected
    {
      get
      {
        return scale == null ? false : scale.IsConnected;
      }
    }

    public decimal ScaleStatus
    {
      get
      {
        return inData.Data[1];
      }
    }

    public decimal ScaleWeightUnits
    {
      get
      {
        return inData.Data[2];
      }
    }

    public USBScale(int scaleNum, int retryTime, int timeoutLength)
    {
      this.scaleNum = scaleNum;
      this.retryTime = retryTime;
      this.timeoutLength = timeoutLength;
    }

    // Devices are numbered from 0, Stamps.com scales first, then Metler Toledo scales.
    public HidDevice GetDevice()
    {
      HidDevice hidDevice;
      // Stamps.com Scale
      IEnumerable<HidDevice> stampsScales = HidDevices.Enumerate(0x1446, 0x6A73);
      // Metler Toledo
      IEnumerable<HidDevice> metlerScales = HidDevices.Enumerate(0x0eb8);

      IEnumerable<HidDevice> allScales = stampsScales.Concat(metlerScales);
      if (this.scaleNum >= allScales.Count())
      {
        return null;
      }
      hidDevice = allScales.ElementAt(this.scaleNum);
      if (hidDevice != null)
        return hidDevice;

      return null;
    }

    public bool Connect()
    {
      // Find a Scale
      HidDevice device = GetDevice();
      if (device != null)
        return Connect(device);
      else
        return false;
    }

    public bool Connect(HidDevice device)
    {
      scale = device;
      int waitTries = 0;
      scale.OpenDevice();

      // sometimes the scale is not ready immedietly after
      // Open() so wait till its ready
      while (!scale.IsConnected && waitTries < 10)
      {
        Thread.Sleep(50);
        waitTries++;
      }
      return scale.IsConnected;
    }

    public void Disconnect()
    {
      if (this.IsConnected)
      {
        scale.CloseDevice();
        scale.Dispose();
      }
    }

    public void DebugScaleData()
    {
      for (int i = 0; i < inData.Data.Length; ++i)
      {
        Console.WriteLine("Byte {0}: {1}", i, inData.Data[i]);
      }
    }

    private long millisecondsSinceEpoch()
    {
      return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public ScaleWeightStatus GetWeight(out decimal? weight)
    {
      weight = null;

      // Byte 0 == Report ID?
      // Byte 1 == Scale Status (1 == Fault, 2 == Stable @ 0, 3 == In Motion, 4 == Stable, 5 == Under 0,
      //                         6 == Over Weight, 7 == Requires Calibration, 8 == Requires Re-Zeroing)
      // Byte 2 == Weight Unit
      // Byte 3 == Data Scaling (decimal placement) - signed byte is power of 10
      // Byte 4 == Weight LSB
      // Byte 5 == Weight MSB

      long startTime = millisecondsSinceEpoch();
      // The argument to scale.Read() is a timeout value for the USB
      // connection, not the actual Scale Status, so I'm leaving it at 250 as
      // it was originally and waiting 100ms between the 250ms tries. Hope that
      // makes sense.
      inData = scale.Read(250);
      while ((ScaleWeightStatus)inData.Data[1] != ScaleWeightStatus.Stable
             && millisecondsSinceEpoch() - startTime < this.timeoutLength)
      {
        inData = scale.Read(250);
        Thread.Sleep(this.retryTime);
      }
      if ((ScaleWeightStatus)inData.Data[1] != ScaleWeightStatus.Stable)
      {
        return (ScaleWeightStatus)inData.Data[1];
      }

      // Convert weight into pounds always
      weight = (decimal?)BitConverter.ToInt16(new byte[] { inData.Data[4], inData.Data[5] }, 0) *
                         Convert.ToDecimal(Math.Pow(10, (sbyte)inData.Data[3]));

      switch (Convert.ToInt16(inData.Data[2]))
      {
        case 3:  // Kilos
          weight = weight * (decimal?)2.2;
          break;
        case 11: // Ounces
          weight = weight * (decimal?)0.0625;
          break;
        case 12: // Pounds
          // already in pounds, do nothing
          break;
      }
      return ScaleWeightStatus.Stable;
    }
  }
}
