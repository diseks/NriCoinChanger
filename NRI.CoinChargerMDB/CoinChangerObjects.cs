using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace NRI.CoinChangerMDB
{
    public static class CoinChangerConst
    {
        // Адрес для получения сообщений устанавливаем равным WM_USER + 1
        public const string DEVICE_NAME = "NRI-USB-HID-DEV-01";
    }

    public enum MDBPackageMode 
    {
        EightBitMode = 0x81, 
        Command = 0x85,
        NineBitDataModeWithoutModeBit = 0x86,
        NineBitDataModeWithModeBit = 0x88
    }

    public enum MDBPackageCommand
    {
        Reset = 0x08,
        Status = 0x09,
        TubeStatus = 0x0A,
        Poll = 0x0B,
        CoinType = 0x0C,
        Despense = 0x0D,
        ExpansionCommand = 0x0F
    }

    public sealed class MDBPackage
    {
        #region Закрытые члены

        private MDBPackageMode _Mode;
        private MDBPackageCommand _Command;
        private List<byte> _Data;

        #endregion

        #region Свойства

        public MDBPackageMode Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }
        public MDBPackageCommand Command
        {
            get { return _Command; }
            set { _Command = value; }
        }
        public List<byte> Data
        {
            get { return _Data; }
            set { _Data = value; }
        }
        public UInt32 Size
        {
            get
            {
                // Размер пакета - длина байтов данных плюс 1 байт длины и 1 байт команды
                return Convert.ToUInt32(this._Data.Count + 2);
            }
        }
        #endregion

        public MDBPackage()
        {
            // По умолчанию команда
            this._Mode = MDBPackageMode.Command;
            this._Command = MDBPackageCommand.Poll;
            this._Data = new List<byte>();
        }

        #region Методы

        // Получения строки из указателя на массив байт.
        public string GetPointerString()
        {
            /* Сформируем команду в виде строки. Для этого нужно сформировать пакет.
             * 1-й байт – количество байт следующих за текущим
             * 2-й байт – байт команды, бит 7 должен всегда быть равным “1”
             * 3 - n-й байты - байты данных
             * Для более детальной информации см. http://cashpoint.hu/upload/pdf/USB_DLL_PC_Protocol_GB.pdf
            */

            Byte[] data = new Byte[this.Size];
            data[0] = Convert.ToByte(this._Data.Count + 1); //  количество байт + байт команды (см. описание выше)
            data[1] = (Byte)this.Mode;

            // Байты команды
            for (int i = 0; i < this._Data.Count; i++)
            {
                data[2 + i] = this._Data[i];
            }

            // Вычислим размер массива для маршалинга указателя
            int size = Marshal.SizeOf(data[0]) * data.Length;

            IntPtr Pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, Pointer, data.Length);
            
            string ResultString = Marshal.PtrToStringAuto(Pointer);

            Marshal.FreeHGlobal(Pointer);

            return ResultString;
        }

        #endregion
    }
}
