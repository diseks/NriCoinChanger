using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using NRI.CoinCharger;


namespace NRI.CoinChanger
{
    
    public sealed class CoinChanger : IDisposable
    {
        #region Делегаты событий 
        // Событие, которое возникает при принятии монет
        public event EventHandler<CoinEventArgs> CoinAccepted;

        // Событие, которое возникает при выплате сдачи
        public event EventHandler<CoinEventArgs> CoinPayedout;

        // Событие, которое возникает при получении сообщения о статусе монетника
        public event EventHandler<StatusEventArgs> CoinChangerStatus;

        // Событие, которое возникает при получении сообщения об ошибке монетника
        public event EventHandler<StatusEventArgs> CoinChangerError;

        // Событие, которое возникает при открытии воронки сброса
        public event EventHandler<EscrowEventArgs> CoinChangerEscrow;

        #endregion 

        #region Методы DLL
        // Типы данных вызовов неуправляемого кода http://msdn.microsoft.com/ru-ru/library/ac7ay120.aspx
        // Сопоставление значений HRESULT и исключений http://msdn.microsoft.com/ru-ru/library/9ztbc5s1.aspx
        // Для работы с библиотекой см. файл BA_Payment_Manager.pdf

        [DllImport("PaymentManager.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int openpaymentmanagerex(Int32 port);

        [DllImport("PaymentManager.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int closepaymentmanager();

        [DllImport("PaymentManager.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int startpaymentmanager(Int32 hWnd, Int32 messageAddress, Int32 devices, Int32 MessageConfig, Int32 protocol);

        [DllImport("PaymentManager.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int stoppaymentmanager();

        [DllImport("PaymentManager.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int setpaymentmanager(int command, int selection, int info1, int info2);

        #endregion

        #region Закрытые члены

        private bool _Disposed;
        private bool _IsOpen;
        private int _LastError;
        private int _MessageAddress;

        private CommunicationType _CommunicationType;
        private CoinChangerErrorList _ErrorList;
        private CoinChangerStatusList _StatusList;
        private MessagePumpManager _MessagePump;
        private CoinsList _Coins;

        #endregion

        #region Свойства

        public IntPtr Handler { get; set; }
        public CoinMessage LastCoinChangerStatus { get; private set; }
        public CoinMessage LastCoinChangerError { get; private set; }
        public CoinsList Coins
        {
            get 
            {
                return _Coins; 
            }
        }

        public bool IsOpen
        {
            get { return _IsOpen; }
        }

        #endregion

        #region Конструкторы

        public CoinChanger()
        {
            this._Disposed = false;
            this._IsOpen = false;
            this._CommunicationType = CommunicationType.USB; // по умолчанию, USB
            this._ErrorList = new CoinChangerErrorList();
            this._StatusList = new CoinChangerStatusList();
            this.LastCoinChangerStatus = new CoinMessage();
            this.LastCoinChangerError = new CoinMessage();
            this._MessagePump = new MessagePumpManager(this);
            this._MessagePump.MessageReceived += new EventHandler<MessageEventArgs>(_MessagePump_MessageReceived);
            this._Coins = new CoinsList();
        }

        public CoinChanger(CommunicationType cType) : base()
        {
            this._CommunicationType = cType;
        }

        #endregion

        #region Деструктор

        // Деструктор для финализации кода
        ~CoinChanger() { Dispose(false); }

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
                        // Останавливаем наш цикл сообщений
                        _MessagePump.StopMessagePump();
                    }
                    catch { }
                }

                // Высовем соответствующие методы для освобождения неуправляемых ресурсов
                // Если disposing=false, то только следующий код буде выполнен
                try 
                {
                    if (this._IsOpen) { closepaymentmanager(); }
                }
                catch { }

                _Disposed = true;
            }
        }

        #endregion

        #region Методы открытые

        /// <summary>
        /// Метод открытия соединения с монетником
        /// </summary>
        public void Open()
        {
            CoinChangerLog.WriteLog("send: open " + ((int)_CommunicationType).ToString());
            _LastError = openpaymentmanagerex((int)_CommunicationType);

            if (_LastError != 0x00)
            {
                LastCoinChangerError.Code = _LastError;
                LastCoinChangerError.Message = _ErrorList.Errors[_LastError];

                CoinChangerLog.WriteLog("receive: open " + _ErrorList.Errors[_LastError]);

                throw new System.IO.FileNotFoundException(_ErrorList.Errors[_LastError]);
            }
            else
            {
                CoinChangerLog.WriteLog("receive: open OK");
                this._IsOpen = true;
            }
        }

        /// <summary>
        /// Метод инициализация библиотеки работы с монетником
        /// </summary>
        public void Start()
        {
            if (_IsOpen)
            {
                _MessagePump.StartMessagePump();
                /* Возвражаемое успешное значение
                 * 1 - Coin changer/validator valid
                 * 2 - Bill validator valid
                 * 3 - Coin changer/validator and bill validator found
                 * 4 - Cashless payment system found
                 * 8 - Hopper(s) found
                 * 16 - Escrow(s) found
                 * 32 - Display found
                */

                CoinChangerLog.WriteLog("send: start " + this.Handler.ToInt32() + ", " + CoinChangerConst.WM_PAYMENTMESSAGE + " 0, 0, 0");
                _LastError = startpaymentmanager(this.Handler.ToInt32(), CoinChangerConst.WM_PAYMENTMESSAGE, 0, 0, 0);

                // Если возвращаемое значение больше или равно 0x2000, то ошибка 
                if (_LastError >= 0x2000)
                {
                    LastCoinChangerError.Code = _LastError;
                    LastCoinChangerError.Message = _ErrorList.Errors[_LastError];
                    CoinChangerLog.WriteLog("receive: start " + _ErrorList.Errors[_LastError]);
                    throw new InvalidOperationException(_ErrorList.Errors[_LastError]);
                }

                CoinChangerLog.WriteLog("receive: start OK");

                // Получи список номиналов
                GetCashItems();
                UpdateCoins();
            }
            else
            {
                throw new InvalidOperationException("Соединение с монетником закрыто. Вызовите метод Open.");
            }
        }

        /// <summary>
        /// Метод остановки работы с библиотекой монетника
        /// </summary>
        public void Stop()
        {
            CoinChangerLog.WriteLog("send: stop");
            _LastError = stoppaymentmanager();
        }

        /// <summary>
        /// Включение приема монет
        /// </summary>
        public void EnableCoinAccepting()
        {
            // Если >= 0, то ок, иначе ошибка
            CoinChangerLog.WriteLog("send: enable 0, 0, 0, 0");
            _LastError = setpaymentmanager(0, 0, 0, 0);

            System.Threading.Thread.Sleep(1000);
        }

        /// <summary>
        /// Отключение приема монет
        /// </summary>
        public void DisableCoinAccepting()
        {
            // Если >= 0, то ок, иначе ошибка
            CoinChangerLog.WriteLog("send: disable 0, 1, 0, 0");
            _LastError = setpaymentmanager(0, 1, 0, 0);
        }

        /// <summary>
        /// Попытка выдачи сдачи
        /// </summary>
        /// <param name="Sum">Сумма сдачи</param>
        /// <returns>true - если указанная сумма сдачи выдана успешно, false - ошибка выдачи стачи</returns>
        public bool TryPayout(int Sum)
        {
            bool result = false;

            // Определим минимальную и максимальную сумму возврата
            // Если >= 0, то ок (это номинал возвращенной монеты), иначе ошибка
            CoinChangerLog.WriteLog("send: payout 1, 0, " + Sum + ", 0");
            _LastError = setpaymentmanager(1, 0, Sum, 0);

            System.Threading.Thread.Sleep(1000);

            if (_LastError == Sum)
            {
                CoinChangerLog.WriteLog("receive: payout OK");
                result = true;
                UpdateCoins();
            }
            else
            {
                CoinChangerLog.WriteLog("receive: payout error " + _LastError);
            }

            return result;
        }

        #endregion

        #region Методы закрытые

        // Получение списка номиналов монетника
        private void GetCashItems()
        {
            int i = 0, result = -1;

            this._Coins.Clear();

            // Получим список всех доступных номиналов
            while (true)
            {
                result = setpaymentmanager(2, 0, i, 0);
                if (result != 0) 
                {
                    Coin c = new Coin(i, result);
                    this._Coins.Add(c);

                    // Проверим, может ли монета попадать в трубки для сдачи или она выбрасывается в ящик
                    result = setpaymentmanager(2, 2, i, 0);

                    if (result != -1)
                    {
                        c.IsTube = (result == 1 ? true : false);
                    }
                }
                else { break; }

                i++;
            }
        }

        // Получение остатка
        private void UpdateCoins()
        {
            int result = 0;

            // Приостановим поток, чтобы монетник получил информацию о принятой монете
            System.Threading.Thread.Sleep(500);

            // Остаток
            foreach (Coin c in _Coins)
            {
                result = setpaymentmanager(2, 4, c.Id, 0);

                if (result >= 0) { c.Qty = result; }
                else { c.Qty = 0; }
            }

            // Мин. и макс. доступная сумма сдачи
            _Coins.MinPayout = setpaymentmanager(1, 2, 0, 0);
            _Coins.MaxPayout = setpaymentmanager(1, 3, 0, 0);
        }

        #endregion

        #region События

        private void _MessagePump_MessageReceived(object sender, MessageEventArgs e)
        {
            /* Входящее сообщение состоит из wparam и lparam
             * wparam содержит общую информацию о событии 
             * Первый полубайт справа (PaymentUnit) содержит информацию о платежном устройстве
             * 0 = Payment Manager DLL
             * 1 = Coin changer/validator
             * 2 = Bill validator
             * 3 = Cashless system
             * 4 = Hopper
             * 14 = Inputs
             * 
             * Второй полубайт справа (Event) содержит информацию о событии
             * 0 = Status of payment unit
             * 1 = Cash acceptance
             * 2 = Cash payout (manual)
             * 3 = Escrow/escrow lever activation
             * 4 = Error
             * */
            WParam wparam = new WParam(e.Message.WParam);

            // Если Coin changer/validator
            if (wparam.UnitInt == 1)
            {
                // Если в сообщении информация о статусе
                if (wparam.EventInt == 0)
                {
                    this.LastCoinChangerStatus.Code = e.Message.LParam.ToInt32();
                    this.LastCoinChangerStatus.Message = _StatusList.Status.ContainsKey(e.Message.LParam.ToInt32())
                                                             ? _StatusList.Status[e.Message.LParam.ToInt32()]
                                                             : "Unknown error!";

                    EventHandler<StatusEventArgs> handler = CoinChangerStatus;

                    if (handler != null)
                    {
                        CoinChangerLog.WriteLog("receive: status " + this.LastCoinChangerStatus.Message);
                        StatusEventArgs eventArg = new StatusEventArgs(this.LastCoinChangerStatus.Code, this.LastCoinChangerStatus.Message);
                        handler(this, eventArg);
                    }
                }
                // Если в сообщении информация об ошибке
                else if (wparam.EventInt == 4)
                {
                    this.LastCoinChangerError.Code = e.Message.LParam.ToInt32();
                    this.LastCoinChangerError.Message = _ErrorList.Errors[e.Message.LParam.ToInt32()];

                    EventHandler<StatusEventArgs> handler = CoinChangerError;

                    if (handler != null)
                    {
                        CoinChangerLog.WriteLog("receive: error " + this.LastCoinChangerError.Message);
                        StatusEventArgs eventArg = new StatusEventArgs(this.LastCoinChangerError.Code, this.LastCoinChangerError.Message);
                        handler(this, eventArg);
                    }
                }
                // Если прием денег
                else if (wparam.EventInt == 1)
                {
                    UpdateCoins();
                    EventHandler<CoinEventArgs> handler = CoinAccepted;

                    if (handler != null)
                    {
                        CoinChangerLog.WriteLog("receive: payin nominal " + e.Message.LParam.ToInt32());
                        CoinEventArgs eventArg = new CoinEventArgs(_Coins.GetCoinIdByNominal(e.Message.LParam.ToInt32()), e.Message.LParam.ToInt32());
                        handler(this, eventArg);
                    }

                }
                // если выдача сдачи
                else if (wparam.EventInt == 2)
                {
                    UpdateCoins();
                    EventHandler<CoinEventArgs> handler = CoinPayedout;

                    if (handler != null)
                    {
                        CoinChangerLog.WriteLog("receive: payout nominal " + e.Message.LParam.ToInt32());
                        CoinEventArgs eventArg = new CoinEventArgs(_Coins.GetCoinIdByNominal(e.Message.LParam.ToInt32()), e.Message.LParam.ToInt32());
                        handler(this, eventArg);
                    }
                }
                // если нажатие на кнопку прочистки воронки
                else if (wparam.EventInt == 3)
                {
                    UpdateCoins();

                    EventHandler<EscrowEventArgs> handler = CoinChangerEscrow;

                    if (handler != null)
                    {
                        EscrowEventArgs eventArg = null;

                        if (e.Message.LParam.ToInt32() > 0)
                        {
                            eventArg = new EscrowEventArgs(_Coins.GetCoinByNominal(e.Message.LParam.ToInt32()));
                        }
                        else
                        {
                            eventArg = new EscrowEventArgs(null);
                        }
                       
                        handler(this, eventArg);
                    }
                }
            }
        }

        #endregion

        #region Вспомогательные классы и структуры

        /// <summary>
        /// Класс виртуального окна для отлова сообщений Windows, посылаемых монетоприемником.
        /// Класс не должен быть виден снаружи
        /// </summary>
        private class MessageHook : NativeWindow
        {
            // Событие, которое возникает при получении сообщения от монетника
            public event EventHandler<MessageEventArgs> MessageReceived;

            public MessageHook(MessagePumpManager owner)
            {
                // Создаем окно
                this.CreateHandle(new System.Windows.Forms.CreateParams());

                // Свяжем жизнь класса с его владельцем
                owner.MessagePumpManagerStoped += new EventHandler(owner_MessagePumpManagerStoped);
            }

            private void owner_MessagePumpManagerStoped(object sender, EventArgs e)
            {
                // Освободим дескриптор окна
                this.ReleaseHandle();
            }

            [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            protected override void WndProc(ref Message m)
            {
                // Слушаем сообщения
                switch (m.Msg)
                {
                    case CoinChangerConst.WM_PAYMENTMESSAGE:
                        EventHandler<MessageEventArgs> handler = MessageReceived;

                        if (handler != null)
                        {
                            MessageEventArgs eventArg = new MessageEventArgs(m);
                            handler(this, eventArg);
                        }
                        break;
                }
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Класс запуска цикла прослушки/отправки сообщений Windows в отдельном потоке
        /// </summary>
        private class MessagePumpManager
        {
            // Событие, которое возникает при получении сообщения от монетника
            public event EventHandler<MessageEventArgs> MessageReceived;
            public event EventHandler MessagePumpManagerStoped;

            private Thread _messagePump;
            private AutoResetEvent _messagePumpRunning;
            private CoinChanger _coin;

            // В конструкторе получим указатель на объект монетоприемника для передачи ему дескриптора окна, которое будет ловить события
            public MessagePumpManager(CoinChanger c)
            {
                _messagePumpRunning = new AutoResetEvent(false);
                _coin = c;
            }

            public void StartMessagePump()
            {
                // запустим виртуальное окно и цикл получения сообщения в отдельном потоке
                _messagePump = new Thread(RunMessagePump) { Name = "ManualMessagePump" };
                _messagePump.Start();
                _messagePumpRunning.WaitOne();
            }

            public void StopMessagePump()
            {
                if (_messagePump != null && _messagePump.IsAlive)
                {
                    _messagePump.Abort();

                    // Зажгем событие завершения цикла сообщений
                    EventHandler handler = MessagePumpManagerStoped;
                    if (handler != null) { handler(this, new EventArgs()); }
                }
            }

            // Запуск потока прослушки/получения сообщений
            private void RunMessagePump()
            {
                // Создаем виртуальное окно
                MessageHook messageHandler = new MessageHook(this);
                messageHandler.MessageReceived += new EventHandler<MessageEventArgs>(messageHandler_MessageReceived);

                // Инициальзируем объект-монетоприемник дескриптором этого окна 
                _coin.Handler = messageHandler.Handle;

                // запускаем цикл сообщений и даем знать другим потокам, что мы закончили
                _messagePumpRunning.Set();
                Application.Run();
            }

            private void messageHandler_MessageReceived(object sender, MessageEventArgs e)
            {

                EventHandler<MessageEventArgs> handler = MessageReceived;

                if (handler != null)
                {
                    MessageEventArgs eventArg = e;
                    handler(this, eventArg);
                }
            }
        }

        #endregion
    }

}
