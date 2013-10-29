using System.Collections.Generic;

namespace NRI.CoinChanger
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CoinChangerStatusList
    {
        public Dictionary<int, string> Status { get; private set; }

        public CoinChangerStatusList()
        {
            Status = new Dictionary<int, string>();

            Status.Add(0, "Found (during start-up)");
            Status.Add(1, "Ready");
            Status.Add(2, "Out of order (hardware problem)");
            Status.Add(3, "Not found (while running)");
            Status.Add(4, "Tube of coin changer empty");
            Status.Add(5, "Not found (during start-up)");
            Status.Add(6, "Reconnected");
            Status.Add(7, "Bills removed from dispenser");

            Status.Add(16, "Coin routed to cash-box");
            Status.Add(17, "Coin routed to tube");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class CoinChangerErrorList
    {
        public Dictionary<int, string> Errors { get; private set; }

        public CoinChangerErrorList()
        {
            Errors = new Dictionary<int, string>();

            Errors.Add(-1, "Cash ID not found, device not found, execution error");
            Errors.Add(-2, "Value too small");
            Errors.Add(-3, "No device attached");
            Errors.Add(-4, "Device error");
            Errors.Add(-5, "Command unknown/not supported");
            Errors.Add(-6, "Payment Manager is not running");

            Errors.Add(0, "Communication error");
            Errors.Add(1, "Coin changer/validator reset");
            Errors.Add(2, "Bill validator reset");
            Errors.Add(3, "Sensor problem");
            Errors.Add(4, "Defective coin changer motor");
            Errors.Add(5, "Coin jam in changer tube");
            Errors.Add(6, "Coin jam in validator");
            Errors.Add(7, "Bill validator ROM checksum error");
            Errors.Add(8, "Coin changer/validator ROM checksum error");
            Errors.Add(9, "Bill validator cash-box out of position");
            Errors.Add(10, "Defective tube sensor");
            Errors.Add(11, "Payment unit disabled");
            Errors.Add(12, "Validator unplugged");
            Errors.Add(13, "Coin jam");
            Errors.Add(14, "Coin sorting error");
            Errors.Add(15, "String recognition (coin inserted on a string)");
            Errors.Add(16, "Cash-box full");
            Errors.Add(17, "Jam in cash-box");
            Errors.Add(18, "Cash-box error");
            Errors.Add(19, "Hopper motor blocked");
            Errors.Add(20, "Hopper empty");
            Errors.Add(21, "Hopper optics blocked");
            Errors.Add(22, "Hopper optics error");
            Errors.Add(23, "Hopper payout blocked");
            Errors.Add(24, "Bills in dispenser of bill-to-bill unit to be removed");
            Errors.Add(25, "Tube cassette removed");
            Errors.Add(26, "Sorting opened");

            // Ошибки монентника
            Errors.Add(0x1000, "NRIHIDAPI.dll not found");
            Errors.Add(0x2000, "NRI Payment Manager is not open (call Open() first)");
            Errors.Add(0x2001, "Unknown protocol selected");
            Errors.Add(0x2002, "Error in MDB/USB adapter communication");
            Errors.Add(0x2003, "Error in MDB/USB adapter communication");
            Errors.Add(0x2004, "No communication with payment units");
            Errors.Add(0x2005, "Payment Manager is already running");
            Errors.Add(0x2006, "Payment Manager could not start thread");
        }
    }
}
