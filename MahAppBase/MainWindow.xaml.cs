using Autofac;
using MahAppBase.ViewModel;
using MahApps.Metro.Controls;

namespace MahAppBase
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region Properties
        /// <summary>
        /// ViewModel
        /// </summary>
        public MainComponent ViewModel { get; set; } 
        #endregion

        #region Function
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = App.Container.Resolve<MainComponent>();
            this.DataContext = ViewModel;
            element.ToggleAutoHide();
        }
        #endregion
    }
}