using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TelegramLite.ViewModel;
using TelegramLite.Core;

namespace TelegramLite.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        public MainViewModel mvm { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            mvm = new MainViewModel();
            DataContext = mvm;
        }

        private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
            }
        }

        private void MainWindowName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (mvm.OpenMenu)
                {
                    mvm.OpenMenu = false;
                }
                else if (mvm.IsChatSelected)
                {
                    mvm.SelectedChatIndex = -1;
                    mvm.SelectedChatIdTemp = -1;
                }
            }

        }


        private bool AutoScroll = true;
        private void MessagesList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ListView lv = sender as ListView;
            ScrollViewer sv = ViewsWork.GetChildOfType<ScrollViewer>(lv);
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (sv.VerticalOffset == sv.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    AutoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    AutoScroll = false;
                    
                }
            }
            else
            {
                if (mvm.Messages != null && mvm.User != null && mvm.PreviousMessages != null)
                {
                    if (mvm.Messages.Count > 0 && mvm.PreviousMessages.Count > 0)
                    {
                        if (mvm.AddedNewMessage == true)
                        {
                            AutoScroll = true;
                            mvm.AddedNewMessage = false;
                        }
                    }
                }
            }

            // Content scroll event : auto-scroll eventually
            if (AutoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                sv.ScrollToVerticalOffset(sv.ExtentHeight);
            }
        }
    }
}
