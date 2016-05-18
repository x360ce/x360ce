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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DeviceButtonsMenu : UserControl
    {
        public DeviceButtonsMenu()
        {
            InitializeComponent();
        }

        SolidColorBrush ColorRed = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)); /*#ff26a0da*/
        SolidColorBrush ColorWhite = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); /*#ff26a0da*/

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            string s = menuItem.Header.ToString();

            if (s.Contains("empty"))
            {
                MenuControlDock.Text = "";
            }
            else
            {
                MenuControlDock.Text = s;
            }

            if (s.Contains("Record"))
            {
                MenuControlDock.Background = ColorRed;
            }
            else
            {
                MenuControlDock.Background = ColorWhite;
            }

        }
    }
}
