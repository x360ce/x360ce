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
    /// Interaction logic for MainTreeView.xaml
    /// </summary>
    public partial class MainTreeView : UserControl
    {
        public MainTreeView()
        {
            InitializeComponent();
        }

        SolidColorBrush ColorApplication = new SolidColorBrush(Color.FromArgb(34, 0, 0, 136)); /*#22000088*/
        SolidColorBrush ColorGame = new SolidColorBrush(Color.FromArgb(34, 0, 136, 0)); /*#22008800*/
        SolidColorBrush ColorController = new SolidColorBrush(Color.FromArgb(34, 47, 79, 78)); /*#222f4f4e*/
        SolidColorBrush ColorDevice = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); /*#00000000*/
        SolidColorBrush ColorFocused = new SolidColorBrush(Color.FromArgb(128, 51, 153, 255)); /*#ff3399ff*/

        private void TreeViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            Border s = (Border)sender;
            s.Background = ColorFocused;
        }

        private void TreeViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            Border s = (Border)sender;
            if (s.Tag.ToString() == "Application") s.Background = ColorApplication;
            if (s.Tag.ToString() == "Game") s.Background = ColorGame;
            if (s.Tag.ToString() == "Controller") s.Background = ColorController;
            if (s.Tag.ToString() == "Device") s.Background = ColorDevice;
        }
    }
}
