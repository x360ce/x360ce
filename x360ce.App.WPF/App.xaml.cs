using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace x360ce.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        SolidColorBrush ColorBlue = new SolidColorBrush(Color.FromArgb(255, 38, 160, 218)); /*#ff26a0da*/
        SolidColorBrush ColorGray = new SolidColorBrush(Color.FromArgb(16, 0, 0, 0)); /*#10000000*/
        SolidColorBrush ColorTransparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); /*#10000000*/
        SolidColorBrush ColorMouseEnter = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); /*#01000000*/
    }
}
