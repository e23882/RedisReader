using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Castle.Components.DictionaryAdapter.Xml;
using MahAppBase.Command;
using Notifications.Wpf;
using StackExchange.Redis;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace MahAppBase.ViewModel
{
    public class MainComponent:ViewModelBase
    {
        #region Declarations
        private bool _DonateIsOpen = false;
        private bool _SettingIsOpen = false;

        private int _DBIndex = 0;
        private int _KeysCount;

        private string _Password = "";
        private string _Port = "";
        private string _Host = "";
        private string _KeyListFilter;
        private string _DetailValueFilter;
        private string _FilterMultiCondition;

        private KeyListData _SelectedKey = new KeyListData();
        public ObservableCollection<KeyListData> _KeyList = new ObservableCollection<KeyListData>();

        #endregion

        #region Property
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
                    if (BeforeFilterKeyList.Count > KeyList.Count) 
                    {
                        KeyList.Clear();
                        KeyList = BeforeFilterKeyList;
                        KeysCount = KeyList.Count;
                    }

                    //將完整資料存到集合
                    BeforeFilterKeyList = DeepCloneObservableCollection(KeyList);
                    _FilterMultiCondition = value;
                    OnPropertyChanged();
                    if (!string.IsNullOrEmpty(value))
                    {
                        var redis = ConnectionMultiplexer.Connect($"{Host}:{Port},password={Password}");
                        var db = redis.GetDatabase(DBIndex);

                        var allRule = value.Split(',');
                        List<KeyListData> savedData = new List<KeyListData>();
                        foreach (var item in KeyList)
                        {
                            bool saveThisData = true;
                            HashEntry[] hashEntries = db.HashGetAll(item.Name);
                            foreach (var detailItem in hashEntries)
                            {

                                //目前這個Key資料中的欄位跟值
                                var currentField = detailItem.Name;
                                var currentValue = detailItem.Value;
                                //逐個欄位檢查
                                if (allRule.Any(x => x.IndexOf(currentField) != -1))
                                {
                                    var foundRule = allRule.First(x => x.IndexOf(currentField) != -1);
                                    //有符合
                                    if (foundRule.Split(':')[1].IndexOf(currentValue) == -1)
                                        saveThisData = false;
                                }
                            }
                            if (saveThisData)
                            {
                                savedData.Add(item);
                            }
                        }

                       
                        KeyList.Clear();
                        foreach (var item in savedData)
                        {
                            KeyList.Add(item);
                        }
                        KeysCount = KeyList.Count;
                        redis.Close();
                        redis.Dispose();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
        public ObservableCollection<int> ComboboxList { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<Tuple<string, object>> BeforeFilterDetaiDataList { get; set; } = new ObservableCollection<Tuple<string, object>>();
        public ObservableCollection<Tuple<string, object>> DetaiDataList { get; set; } = new ObservableCollection<Tuple<string, object>>();
        
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
        public string KeyListFilter
        {
            get
            {
                return _KeyListFilter;
            }
            set
            {
                _KeyListFilter = value;
                OnPropertyChanged();

                //有篩選的話先復原
                if (BeforeFilterKeyList.Count> KeyList.Count) 
                    KeyList = BeforeFilterKeyList;

                //將完整資料存到集合
                BeforeFilterKeyList = DeepCloneObservableCollection(KeyList);


                //篩選資料到暫存變數，把不要顯示的選出來
                List<KeyListData> removeList = new List<KeyListData>();
                foreach(var item in BeforeFilterKeyList) 
                {
                    if (item.Name.IndexOf(value) == -1) 
                        removeList.Add(item);
                }

                //如果要要移除的資料
                if (removeList.Count > 0) 
                {
                    //移除
                    foreach (var item in removeList)
                    {
                        KeyList.Remove(KeyList.First(x => x.Name == item.Name));
                    }
                }
                else 
                {
                    KeyList.Clear();
                    foreach(var item in BeforeFilterKeyList) 
                    {
                        KeyList.Add(item);
                    }
                }
                KeysCount = KeyList.Count();
            }
        }
        public string DetailValueFilter
        {
            get
            {
                return _DetailValueFilter;
            }
            set
            {
                _DetailValueFilter = value;
                OnPropertyChanged();
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
        
        /// <summary>
        /// 
        /// </summary>
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
        public ICommand ConnectCommand { get; set; }

        /// <summary>
        /// Donate Button Click Command
        /// </summary>
        public ICommand ButtonDonateClickCommand { get; set; }
        
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
        public ICommand TestButtonClickCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICommand TestInvokeExceptionCommand { get; set; }

        public ConnectionMultiplexer Redis { get; set; }
        public IDatabase Database { get; set; }
        public IServer Server { get; set; }
        #endregion

        #region MemberFunction
        public static ObservableCollection<T> DeepCloneObservableCollection<T>(ObservableCollection<T> originalCollection) where T : ICloneable
        {
            var clonedCollection = new ObservableCollection<T>(originalCollection.Select(item => (T)item.Clone()));
            return clonedCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void InitialCommand()
        {
            try
            {
                ButtonDonateClickCommand = new RelayCommand(ButtonDonateClickAction);
                ClosedWindowCommand = new RelayCommand(ClosedWindowCommandAction);
                SettingButtonClickCommand = new RelayCommand(SettingButtonClickCommandAction);
                TestButtonClickCommand = new RelayCommand(TestButtonClickCommandAction);
                TestInvokeExceptionCommand = new RelayCommand(TestInvokeExceptionCommandAction);
                ConnectCommand = new RelayCommand(ConnectCommandAction);
            }
            catch (Exception ex)
            {
                Common.Log($"{ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        [HandleException]
        private void ConnectCommandAction(object obj)
        {

            string connectionString = "";
            if (string.IsNullOrEmpty(Password))
                connectionString = $"{Host}:{Port}";
            else
                connectionString = $"{Host}:{Port},password={Password}";
            
            Redis = ConnectionMultiplexer.Connect(connectionString);
            Database = Redis.GetDatabase(0);
            Server = Redis.GetServer(Host, int.Parse(Port));  // 根據您的 Redis 地址和端口選擇伺服器
            Common.Notify("連線成功");
            GetDBList();
            GetKeyList();
            
        }
       
        
        public void GetDetailData() 
        {
            var redis = ConnectionMultiplexer.Connect($"{Host}:{Port},password={Password}");
            var db = redis.GetDatabase(DBIndex);
            HashEntry[] hashEntries = db.HashGetAll(SelectedKey.Name);
            DetaiDataList.Clear();

            foreach (var item in hashEntries)
            {
                DetaiDataList.Add(new Tuple<string, object>(item.Name.ToString(), item.Value.ToString()));
            }

            redis.Close();
            redis.Dispose();
        }

        private void GetDBList()
        {

            var cursor = 0;

            try
            {
                do
                {
                    var result = Server.Keys(cursor, pattern: "*", pageSize: 100);  // "*": 匹配所有鍵，pageSize: 每次掃描返回的鍵數量

                    cursor += 1;
                    foreach (var key in result)
                    {
                    }

                } while (cursor != 0); // 當游標為 0 時，表示遍歷結束
            }
            catch (Exception ie)
            {
            }
            
            for(int i=0;i< cursor-1; i++) 
            {
                ComboboxList.Add(i);
            }
            DBIndex = 0;


        }

        private void GetKeyList()
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
    

        [HandleException]
        public virtual void TestInvokeExceptionCommandAction(object obj)
        {
            throw new NotImplementedException("故意放在這的例外，程式會自己handle不會crash");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public virtual void TestButtonClickCommandAction(object obj)
        {
            DemoWindow win = new DemoWindow();
            win.Show();
            TestInterceptorWorking();
        }

        [HandleException]
        public virtual void TestInterceptorWorking()
        {
            Console.WriteLine("123");
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
        /// Constructor
        /// </summary>
        public MainComponent()
        {
            Common.Log("App running..");
            InitialCommand();
            Common.Notify($"{DateTime.Now.ToString("HH:mm:ss")}程式啟動", "程式啟動", NotificationType.Success);
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