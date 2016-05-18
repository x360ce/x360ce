using System;
using System.Collections.Generic;
using System.Linq;
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

namespace x360ce.App.Controls
{
    /// <summary>
    /// Interaction logic for DeviceTabControlGeneralTab.xaml
    /// </summary>
    public partial class DeviceTabControlGeneralTab : UserControl
    {
        public DeviceTabControlGeneralTab()
        {
            InitializeComponent();
        }

        SolidColorBrush ColorBlue = new SolidColorBrush(Color.FromArgb(255, 38, 160, 218)); /*#ff26a0da*/
        SolidColorBrush ColorGray = new SolidColorBrush(Color.FromArgb(16, 0, 0, 0)); /*#10000000*/
        SolidColorBrush ColorTransparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); /*#00000000*/
        SolidColorBrush ColorMouseEnter = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); /*#01000000*/

        #region Static JocysTime data available to all app
        //static object ControllerButtonsLock = new object();

        //static List<ControllerButtonItem> _JocysButtons;

        //private static List<ControllerButtonItem> JocysButtons
        //{
        //    get
        //    {
        //        lock (ControllerButtonsLock)
        //        {
        //            if (_JocysButtons == null)
        //            {
        //                var list = new List<ControllerButtonItem>();
        //                list.Add(new ControllerButtonItem("Name", "Title"));
        //                _JocysButtons = list;
        //            }
        //            return _JocysButtons;
        //        }
        //    }
        //}

        //public StackPanel ColorDStackPanel { get; private set; }
        #endregion

        //static List<ControllerButtonItem> ControllerButtonItems;
        private void ButtonsMenu_Loaded(object sender, RoutedEventArgs e)
        {
            // Menu Control and Name.
            Control menuControl = (Control)sender;                           
            string menuName = menuControl.Name;
            string mainName = menuName.Replace("MenuControl_", "");
            // Menu Title.
            string menuTitle = menuName.Replace("MenuControl_L_", "");
            menuTitle = menuTitle.Replace("MenuControl_R_", "");
            menuTitle = menuTitle.Replace("MenuControl_", "");
            menuTitle = menuTitle.Replace("_", " ");
            TextBlock menuControlTitle = (TextBlock)menuControl.FindName("MenuControlTitle");
            menuControlTitle.Text = menuTitle;
            // Button Name and Control.
            string buttonName = menuName.Replace("MenuControl_", "ButtonControl_");
            Control buttonControl = (Control)this.FindName(buttonName);

            //ControllerButtonItems.Add(new ControllerButtonItem(mainName, menuControl, buttonControl));

            // Menu Title Dock Left or Right.
            StackPanel p = menuControl.Parent as StackPanel;
            if (p.HorizontalAlignment == HorizontalAlignment.Right)
            {
                TextBlock d = (TextBlock)menuControl.FindName("MenuControlDock");
                DockPanel.SetDock(d, Dock.Right);
            }
        }

        private void MouseEnterLeave(object sender, SolidColorBrush color, double opacity)
        {
            Control s = (Control)sender;
            Border b = (Border)s.FindName("MouseEnterColor");
            if (s.Name.Contains("MenuControl") == true)
            {              
                b.Background = color;
                string n = s.Name.Replace("MenuControl", "ButtonControl");
                Control c = (Control)this.FindName(n);
                Border b2 = (Border)c.FindName("MouseEnterColor");
                b2.Opacity = opacity;

            }
            else if (s.Name.Contains("ButtonControl"))
            {
                b.Opacity = opacity;
                string n = s.Name.Replace("ButtonControl", "MenuControl");
                Control c = (Control)this.FindName(n);
                Border b2 = (Border)c.FindName("MouseEnterColor");
                b2.Background = color;
            }        
        }

        private void Start_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseEnterLeave(sender, ColorBlue, 1);
        }

        private void Start_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseEnterLeave(sender, ColorTransparent, 0);
        }
    }
}
