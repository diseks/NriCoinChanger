using System;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NRI.CoinChanger
{
    public enum CommunicationType { USB = 0x10000, COM1 = 0x00101, COM1x15 = 0x0010F }

    public static class CoinChangerConst
    {
        // Адрес для получения сообщений устанавливаем равным WM_USER + 1
        public const int WM_PAYMENTMESSAGE = 0x0401;
    }

    /// <summary>
    /// Аргументы события, возникающего при получении сообщений от монетника (испольщуется в NativeWindow)
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        private Message _WindowsMessage;

        public MessageEventArgs(Message message)
        {
            _WindowsMessage = message;
        }
        public Message Message
        {
            get { return _WindowsMessage; }
            set { _WindowsMessage = value; }
        }
    }

    /// <summary>
    /// Аргумент события получения/выдачи монет
    /// </summary>
    public class CoinEventArgs : EventArgs
    {
        public int CoinId { get; private set; }
        public int Nominal { get; private set; }

        public CoinEventArgs(int coinId, int nominal)
        {
            CoinId = coinId;
            Nominal = nominal;
        }
    }

    /// <summary>
    /// Аргумент события получения статуса монетника
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        public int Code { get; private set; }
        public string Message { get; private set; }

        public StatusEventArgs(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }

    /// <summary>
    /// Аргумент события получения статуса монетника
    /// </summary>
    public class EscrowEventArgs : EventArgs
    {
        public Coin Coin { get; private set; }

        public EscrowEventArgs(Coin coin)
        {
            Coin = coin;
        }
    }

    /// <summary>
    /// Класс, использующийся для расшифровки сообщений от монетника
    /// </summary>
    public class WParam
    {
        private int _wParamInt;
        private BitArray _wParamBit;
        private int _EventInt;
        private int _UnitInt;

        public int EventInt
        {
            get { return _EventInt; }
        }
        public int UnitInt
        {
            get { return _UnitInt; }
        }

        public WParam(IntPtr wparam)
        {
            /*
             * wparam состоит из двух байт (четырех полубайт): 0000 0000 0000 0000
             * первые слева направо два полубайта не используются (зарезервированы)
             * Нас интересуют оставшиеся два полубайта
             * Первый из них слева называется Event и содержит в себе информацию о произошедшем событии
             * Второй называется PaymentUnit и сожержит информацию о платежной устройстве (монетник, купюроприемник и т.д)
             * Нужно вычленить эти полубайты из массивов битов и преобрасовать в число
             * Значения числа см. в документации к монетнику
             * */

            // Число
            _wParamInt = wparam.ToInt32();
            // Массив бит
            _wParamBit = new BitArray(BitConverter.GetBytes(wparam.ToInt32()));

            // Создаем массив из 8 бит
            // Вставляем в него биты с 4-го по 7-й (второй полубайт
            BitArray EventBits = new BitArray(8);
            EventBits.Set(0, _wParamBit[4]);
            EventBits.Set(1, _wParamBit[5]);
            EventBits.Set(2, _wParamBit[6]);
            EventBits.Set(3, _wParamBit[7]);

            // преобразуем в число
            int[] array = new int[1];
            EventBits.CopyTo(array, 0);
            _EventInt = array[0];

            // Создаем массив из 8 бит
            // Вставляем в него биты с 0-го по 4-й (второй полубайт
            BitArray UnitBits = new BitArray(8);
            UnitBits.Set(0, _wParamBit[0]);
            UnitBits.Set(1, _wParamBit[1]);
            UnitBits.Set(2, _wParamBit[2]);
            UnitBits.Set(3, _wParamBit[3]);

            // преобразуем в число
            UnitBits.CopyTo(array, 0);
            _UnitInt = array[0];
        }
    }

    /// <summary>
    /// Сообщения монетника
    /// </summary>
    public class CoinMessage
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    // Список принимаемых монет
    public class CoinsList : List<Coin>
    {
        public int MinPayout { get; set; }
        public int MaxPayout { get; set; }

        public CoinsList() : base(){}

        public Coin GetCoinByNominal(int Nominal)
        {
            Coin result = null;

            foreach (Coin c in this)
            {
                if (c.Nominal == Nominal)
                {
                    result = c;
                    break;
                }
            }

            return result;
        }

        public int GetCoinIdByNominal(int Nominal)
        {
            int result = 0;

            foreach (Coin c in this)
            {
                if (c.Nominal == Nominal)
                {
                    result = c.Id;
                    break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Класс, описывающий монету
    /// </summary>
    public class Coin
    {
        private int _Id;
        private int _Nominal;
        private int _Qty;
        private int _Sum;
        private bool _IsTube;

        public int Id
        {
            get { return _Id; }
        }
        public int Nominal
        {
            get { return _Nominal; }
        }
        public int Qty
        {
            get { return _Qty; }
            set 
            { 
                _Qty = value;
                _Sum = _Qty * _Nominal;
            }
        }
        public int Sum
        {
            get { return _Sum; }
        }
        public bool IsTube
        {
            get { return _IsTube; }
            set { _IsTube = value; }
        }

        public Coin(int id, int nominal)
        {
            this._Id = id;
            this._Nominal = nominal;
            this._Qty = 0;
            this._Sum = 0;
            this._IsTube = false;
        }
    }

}
