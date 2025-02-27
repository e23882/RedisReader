using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MahAppBase.Command;
using Notifications.Wpf;
using StackExchange.Redis;

namespace MahAppBase.ViewModel
{
    public class MainComponent : ViewModelBase
    {
        #region Declarations
        private bool _DonateIsOpen = false;
        private bool _SettingIsOpen = true;
        private bool _UnlockingUI = true;

        private int _DBIndex = 0;
        private int _KeysCount;

        private string _Password = "";
        private string _Port = "";
        private string _Host = "";
        private string _FilterMultiCondition;
        private string _ConnectionInfo;

        private KeyListData _SelectedKey = new KeyListData();
        private ObservableCollection<KeyListData> _KeyList = new ObservableCollection<KeyListData>();
        private FavoriteConnection _SelectedConnection;

        #endregion

        #region Property
        public bool UnlockingUI
        {
            get
            {
                return _UnlockingUI;
            }
            set
            {
                _UnlockingUI = value;
                OnPropertyChanged();
            }
        }
        public string ConnectionInfo
        {
            get
            {
                return _ConnectionInfo;
            }
            set
            {
                _ConnectionInfo = value;
                OnPropertyChanged();
            }
        }
        public string FilterMultiCondition
        {
            get
            {
                return _FilterMultiCondition;
            }
            set
            {
                try
                {
                    _FilterMultiCondition = value;
                    OnPropertyChanged();
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async Task DoMultipleFilter(string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    bool passValidation = false;
                    if (_FilterMultiCondition.Contains("="))
                        passValidation = true;
                    if (_FilterMultiCondition.Contains("<>"))
                        passValidation = true;
                    if (_FilterMultiCondition.Contains(">"))
                        passValidation = true;
                    if (_FilterMultiCondition.Contains("<"))
                        passValidation = true;

                    if (!passValidation)
                        return;

                    var redis = ConnectionMultiplexer.Connect($"{Host}:{Port},password={Password}");
                    var db = redis.GetDatabase(DBIndex);

                    var allRule = value.Split(',');

                    List<KeyListData> savedData = new List<KeyListData>();
                    foreach (var item in KeyList)
                    {
                        bool saveThisData = false;
                        HashEntry[] hashEntries = await db.HashGetAllAsync(item.Name);
                        foreach (var detailItem in hashEntries)
                        {
                            var currentField = detailItem.Name;
                            var currentValue = detailItem.Value;
                            if (allRule.Any(x => x.IndexOf(currentField) != -1))
                            {

                                var foundRule = allRule.First(x => x.IndexOf(currentField) != -1);
                                if (foundRule.Contains("="))
                                {
                                    if (currentValue == foundRule.Split('=')[1])
                                        saveThisData = true;
                                }
                                if (foundRule.Contains("<>"))
                                {
                                    string[] result = foundRule.Split(new string[] { "<>" }, StringSplitOptions.None);
                                    if (currentValue != result[1])
                                        saveThisData = true;
                                }
                                if (foundRule.Contains(">"))
                                {
                                    if (double.Parse(currentValue.ToString()) > double.Parse(foundRule.Split('>')[1]))
                                        saveThisData = true;
                                }
                                if (foundRule.Contains("<"))
                                {
                                    if (double.Parse(currentValue.ToString()) < double.Parse(foundRule.Split('>')[1]))
                                        saveThisData = true;
                                }
                            }
                        }
                        if (saveThisData)
                        {
                            savedData.Add(item);
                        }
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        KeyList.Clear();
                    });
                    foreach (var item in savedData)
                    {

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            KeyList.Add(item);
                        });
                    }
                    KeysCount = KeyList.Count;
                    redis.Close();
                    redis.Dispose();
                }
            }
            catch (Exception ex)
            {
                Common.Notify($"發生例外: {ex.Message}\r\n{ex.StackTrace}", type: NotificationType.Error);
            }
        }
        public ObservableCollection<int> ComboboxList { get; set; } = new ObservableCollection<int>();
        public FavoriteConnection SelectedConnection
        {
            get
            {
                return _SelectedConnection;
            }
            set
            {
                _SelectedConnection = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<FavoriteConnection> FavoriteConnectionList { get; set; } = new ObservableCollection<FavoriteConnection>();
        public ObservableCollection<Tuple<string, object>> DetailDataList { get; set; } = new ObservableCollection<Tuple<string, object>>();

        private Tuple<string, object> _DetailData;
        public Tuple<string, object> DetailData
        {
            get
            {
                return _DetailData;
            }
            set
            {
                _DetailData = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<KeyListData> BeforeFilterKeyList { get; set; } = new ObservableCollection<KeyListData>();
        public ObservableCollection<KeyListData> KeyList
        {
            get
            {
                return _KeyList;
            }
            set
            {
                _KeyList = value;
                OnPropertyChanged();
            }
        }
        public KeyListData SelectedKey
        {
            get
            {
                return _SelectedKey;
            }
            set
            {
                if (value != null)
                {
                    _SelectedKey = value;
                    GetDetailData();
                    OnPropertyChanged();
                }
            }
        }
        public int KeysCount
        {
            get
            {
                return _KeysCount;
            }
            set
            {
                _KeysCount = value;
                OnPropertyChanged();
            }
        }
        public int DBIndex
        {
            get
            {
                return _DBIndex;
            }
            set
            {
                _DBIndex = value;
                GetKeyList();
                OnPropertyChanged();
            }
        }
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
                OnPropertyChanged();
            }
        }
        public string Port
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = value;
                OnPropertyChanged();
            }
        }
        public string Host
        {
            get
            {
                return _Host;
            }
            set
            {
                _Host = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DonateIsOpen
        {
            get
            {
                return _DonateIsOpen;
            }
            set
            {
                _DonateIsOpen = value;
                OnPropertyChanged();
            }
        }        

		public bool SettingIsOpen
        {
            get
            {
                return _SettingIsOpen;
            }
            set
            {
                _SettingIsOpen = value;
                OnPropertyChanged();
            }
        }


        public ICommand OpenOrCloseSettingCommand { get; set; }

        /// <summary>
        /// Donate Button Click Command
        /// </summary>
        public ICommand ButtonDonateClickCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand ConnectCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand ClosedWindowCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand SettingButtonClickCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand TestInvokeExceptionCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand ChooseConnectionCommand { get; set; }
        public ICommand ChooseItem1StringCommand { get; set; }

        public ICommand FilterCommand { get; set; }
        public ICommand CreateNewInstanceCommand { get; set; }

        public ConnectionMultiplexer Redis { get; set; }
        public IDatabase Database { get; set; }
        public IServer Server { get; set; }
        #endregion

        #region MemberFunction
        /// <summary>
        /// 
        /// </summary>
        [HandleException]
        public virtual void InitialCommand()
        {
            ClosedWindowCommand = new RelayCommand(ClosedWindowCommandAction);
            SettingButtonClickCommand = new RelayCommand(SettingButtonClickCommandAction);
            TestInvokeExceptionCommand = new RelayCommand(TestInvokeExceptionCommandAction);
            ConnectCommand = new RelayCommand(ConnectCommandAction);
            ChooseConnectionCommand = new RelayCommand(ChooseConnectionCommandAction);
            OpenOrCloseSettingCommand = new RelayCommand(OpenOrCloseSettingCommandAction);
            ChooseItem1StringCommand = new RelayCommand(ChooseItem1StringCommandAction);
            FilterCommand = new RelayCommand(FilterCommandAction);
            CreateNewInstanceCommand = new RelayCommand(CreateNewInstanceCommandAction);
			ButtonDonateClickCommand = new RelayCommand(ButtonDonateClickAction);
        }

        private void CreateNewInstanceCommandAction(object obj)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
        }

        private void FilterCommandAction(object obj)
        {
            if (BeforeFilterKeyList.Count > KeyList.Count)
            {
                KeyList.Clear();
                KeyList = BeforeFilterKeyList;
                KeysCount = KeyList.Count;
            }

            //將完整資料存到集合
            BeforeFilterKeyList = DeepCloneObservableCollection(KeyList);

            Common.Notify("篩選資料...", type: NotificationType.Information);
            Task.Run(async () =>
            {
                UnlockingUI = false;
                await DoMultipleFilter(_FilterMultiCondition);
                Common.Notify("篩選資料完成", type: NotificationType.Success);
                UnlockingUI = true;
            });
        }

        private void ChooseItem1StringCommandAction(object obj)
        {
            _FilterMultiCondition = $"{DetailData.Item1}={DetailData.Item2}";
            OnPropertyChanged("FilterMultiCondition");
        }

        private void OpenOrCloseSettingCommandAction(object obj)
        {
            SettingIsOpen = !SettingIsOpen;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MainComponent()
        {
            Common.Log("App running..");
            InitialCommand();
            InitialConnectionList();
            Common.Notify($"{DateTime.Now.ToString("HH:mm:ss")}程式啟動", "程式啟動", NotificationType.Success);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalCollection"></param>
        /// <returns></returns>
        public static ObservableCollection<T> DeepCloneObservableCollection<T>(ObservableCollection<T> originalCollection) where T : ICloneable
        {
            var clonedCollection = new ObservableCollection<T>(originalCollection.Select(item => (T)item.Clone()));
            return clonedCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void ChooseConnectionCommandAction(object obj)
        {
            Host = SelectedConnection.Host;
            Port = SelectedConnection.Port.ToString();
            Password = SelectedConnection.Password;
            _DBIndex = SelectedConnection.DefaultDB;
            OnPropertyChanged("DBIndex");
            SettingIsOpen = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        [HandleException]
        public virtual void ConnectCommandAction(object obj)
        {
            string connectionString = "";
            if (string.IsNullOrEmpty(Password))
                connectionString = $"{Host}:{Port}";
            else
                connectionString = $"{Host}:{Port},password={Password}";

            Redis = ConnectionMultiplexer.Connect(connectionString);
            Database = Redis.GetDatabase(DBIndex);
            Server = Redis.GetServer(Host, int.Parse(Port));
            ConnectionInfo = $"{Host}:{Port} ({DBIndex})";
            GetDBList();
            GetKeyList();
            Common.Notify("連線成功");
        }

        /// <summary>
        /// 
        /// </summary>
        [HandleException]
        public virtual void GetDetailData()
        {
            var redis = ConnectionMultiplexer.Connect($"{Host}:{Port},password={Password}");
            var db = redis.GetDatabase(DBIndex);
            HashEntry[] hashEntries = db.HashGetAll(SelectedKey.Name);
            DetailDataList.Clear();

            foreach (var item in hashEntries)
            {
                DetailDataList.Add(new Tuple<string, object>(item.Name.ToString(), item.Value.ToString()));
            }

            redis.Close();
            redis.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        [HandleException]
        public virtual void GetDBList()
        {
            ComboboxList.Clear();
            for (int i = 0; i < Server.DatabaseCount - 1; i++)
            {
                ComboboxList.Add(i);
            }
            DBIndex = SelectedConnection.DefaultDB;

        }

        /// <summary>
        /// 
        /// </summary>
        [HandleException]
        public virtual void GetKeyList()
        {
            if (KeyList.Any())
                KeyList.Clear();

            var result = Server.Keys(DBIndex, pattern: "*", pageSize: 100);
            foreach (var key in result)
            {
                KeyList.Add(new MahAppBase.KeyListData()
                {
                    Type = "HASH",
                    Name = key
                });
            }
            KeysCount = KeyList.Count();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        [HandleException]
        public virtual void TestInvokeExceptionCommandAction(object obj)
        {
            throw new NotImplementedException("故意放在這的例外，程式會自己handle不會crash");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public virtual void SettingButtonClickCommandAction(object obj)
        {
            SettingIsOpen = !SettingIsOpen;
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitialConnectionList()
        {
            FavoriteConnectionList.Add(new FavoriteConnection()
            {
                Host = "1.2.3.4",
                Password = "yourpassword",
                Port = 12345,
                DefaultDB = 6,
                ConnectionName = "ConnectionName"
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void ClosedWindowCommandAction(object obj)
        {
            Common.Log("App closed");
            Environment.Exit(0);
        }

 		/// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        private void ButtonDonateClickAction(object parameter)
        {
            try
            {
                DonateIsOpen = !DonateIsOpen;
            }
            catch (Exception ex)
            {
                Common.Log($"{ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }
        #endregion
    }
}