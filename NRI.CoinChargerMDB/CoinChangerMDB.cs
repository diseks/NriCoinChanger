using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace NRI.CoinChangerMDB
{
    public class CoinChangerMDB : IDisposable
    {
        #region Методы DLL

        [DllImport("NriHidAPI.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr OpenFileHID(String DeviceName);

        [DllImport("NriHidAPI.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool ReadFileHID(IntPtr Handle, IntPtr ReplyBuffer, uint siz, ref uint BytesRead, uint timeout, [MarshalAsAttribute(UnmanagedType.Bool)] bool fPartial);
        
        [DllImport("NriHidAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet=CharSet.Auto, SetLastError = true)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool WriteFileHID(IntPtr Handle, [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPStr)] string pBuffer, uint iLen, ref uint written);

        [DllImport("Kernel32")]
        private extern static Boolean CloseHandle(IntPtr handle);

        #endregion

        #region Закрытые члены

        private bool _Disposed;
        private bool _IsOpen;
        private IntPtr _DeviceHandle;
        private IntPtr INVALID_HANDLE_VALUE;

        #endregion

        #region Конструкторы

        public CoinChangerMDB()
        {
            this._Disposed = false;
            this.INVALID_HANDLE_VALUE = new IntPtr(-1);
        }

        #endregion

        #region Деструктор

        // Деструктор для финализации кода
        ~CoinChangerMDB() { Dispose(false); }

        // Реализует интерфейс IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Прикажем GC не финализировать объект после вызова Dispose, так как он уже освобожден
        }

        // Dispose(bool disposing) выполняется по двум сценариям
        // Если disposing=true, метод Dispose вызывается явно или неявно из кода пользователя
        // Управляемые и неуправляемые ресурсы могут быть освобождены
        // Если disposing=false, то метод может быть вызван runtime из финализатора
        // В таком случае только неуправляемые ресурсы могут быть освобождены.
        private void Dispose(bool disposing)
        {
            // Проверим вызывался ли уже метод Dispose
            if (!this._Disposed)
            {
                // Если disposing=true, освободим все управляемые и неуправляемые ресурсы
                if (disposing)
                {
                    // Здесь освободим управляемые ресурсы
                    try
                    {
                        
                    }
                    catch { }
                }

                // Высовем соответствующие методы для освобождения неуправляемых ресурсов
                // Если disposing=false, то только следующий код буде выполнен
                try
                {
                    CloseHandle(this._DeviceHandle);
                }
                catch { }

                _Disposed = true;
            }
        }

        #endregion

        #region Свойства

        public bool IsOpen
        {
            get { return _IsOpen; }
        }

        #endregion

        #region Методы

        /// <summary>
        /// Метод открытия соединения с монетником
        /// </summary>
        public void Open()
        {
            _DeviceHandle = OpenFileHID(CoinChangerConst.DEVICE_NAME);

            if (_DeviceHandle == INVALID_HANDLE_VALUE)
            { throw new InvalidOperationException("Ошибка подключения к устройству"); }
            else
            {this._IsOpen = true;}
        }

        /// <summary>
        /// Начало инициализации монетника
        /// </summary>
        public void Start()
        {
            if (_IsOpen)
            {
                // Для более полной информации см. папку Docs, файл USB_DLL_PC_Protocol_GB.pdf
                // Команды MDB см.  папку Docs, файл MDB_3.0.pdf, раздел 5 или http://en.wikipedia.org/wiki/Multidrop_bus

                bool Result = false;
                UInt32 PackegeSize = 0;
                // Пакет
                MDBPackage p = new MDBPackage();

                // Отправляем команду TubeStatus
                p.Mode = MDBPackageMode.Command;
                p.Data.Add((Byte)MDBPackageCommand.TubeStatus);
                PackegeSize = p.Size;

                Result = WriteFileHID(this._DeviceHandle, p.GetPointerString(), p.Size, ref PackegeSize);

                if (!Result)
                {
                    throw new InvalidOperationException("Ошибка выполнения команды MDB changer initialization");
                }

                //Отправим команду RESET
                p.Mode = MDBPackageMode.NineBitDataModeWithoutModeBit;
                p.Data.Clear();
                p.Data.Add((Byte)MDBPackageCommand.Reset);
                PackegeSize = p.Size;

                Result = WriteFileHID(this._DeviceHandle, p.GetPointerString(), p.Size, ref PackegeSize);

                if (!Result)
                {
                    throw new InvalidOperationException("Ошибка выполнения команды MDB changer initialization");
                }

                // Отправим команду POLL
                p.Data.Clear();
                p.Data.Add((Byte)MDBPackageCommand.Poll);
                PackegeSize = p.Size;

                Result = WriteFileHID(this._DeviceHandle, p.GetPointerString(), p.Size, ref PackegeSize);

                if (!Result)
                {
                    throw new InvalidOperationException("Ошибка выполнения команды MDB changer initialization");
                }

                // Отправим команду COINTYPE
                p.Data.Clear();
                p.Data.Add((Byte)MDBPackageCommand.CoinType);
                p.Data.Add(0xFF); // Разрешаем работать со всеми номиналами монет
                p.Data.Add(0xFF);
                p.Data.Add(0xFF); // Разрешаем выдавать сзачу вчеми номиналами монет
                p.Data.Add(0xFF);
                PackegeSize = p.Size;

                Result = WriteFileHID(this._DeviceHandle, p.GetPointerString(), p.Size, ref PackegeSize);

                if (!Result)
                {
                    throw new InvalidOperationException("Ошибка выполнения команды MDB changer initialization");
                }
            }
            else
            {
                throw new NullReferenceException("Отсутствует подключегие к монетоприемнику. Необходимо осуществлением операции вызвать метод Open()");
            }
        }
        #endregion
    }

   

}
