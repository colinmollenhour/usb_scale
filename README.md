# USB Scale Reader

This project uses [Mike O’Brien’s USB HID library](http://github.com/mikeobrien/HidLibrary) (HidLibrary.dll). It compiles into a simple executable which reads from the scale and outputs some data in JSON format. Examples:

    {"success":true,"weight":23.4,"units":"lbs"}
    {"success":false,"error":"Scale is still in motion."}

## Usage

UsbScale.cs can be compiled into a binary that will take the following optional arguments:

    --debug        Print debug info
    --retry        Milliseconds to wait between retries when reading is not stable.
    --fail         Milliseconds before returning an unsuccessful status when the reading is not stable.
    [num]          The number of the scale to read. The first connected scale is 0, then 1, ...

## Currently Supported Scales

* Mettler Toledo PS* Scales (Tested with PS60 and PS90)
* Stamps.com USB Scale

Other scale support should be possible by adding their vendor/product id to the GetDevices() method in the Scale.cs file.

## Installation

Example compilation:

    C:\usb_scale> c:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:exe /out:UsbScale.exe UsbScale.cs Scale.cs /r:HidLibrary.dll
