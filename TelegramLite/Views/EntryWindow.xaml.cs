using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TelegramLite.Views
{
    /// <summary>
    /// Interaction logic for EntryWindow.xaml
    /// </summary>
    public partial class EntryWindow : BaseWindow
    {
        bool first = true;
        public EntryWindow()
        {
            InitializeComponent();
            Uri resource = new Uri(@"Views/EntryPages/Page1.xaml", UriKind.RelativeOrAbsolute);
            EntryFrame.NavigationService.Navigate(resource);
            first = false;
        }

        private void MainFrame_OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (!first)
            {
                var ta = new ThicknessAnimation();
                ta.Duration = TimeSpan.FromSeconds(0.3);
                ta.DecelerationRatio = 0.7;
                ta.To = new Thickness(0, 0, 0, 0);
                if (e.NavigationMode == NavigationMode.New)
                {
                    ta.From = new Thickness(500, 0, 0, 0);
                }
                else if (e.NavigationMode == NavigationMode.Back)
                {
                    ta.From = new Thickness(0, 0, 500, 0);
                }
                 EntryFrame.BeginAnimation(MarginProperty, ta);
            }
        }
    }
}
