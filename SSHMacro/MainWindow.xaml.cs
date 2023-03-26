using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
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

namespace SSHMacro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc calback, IntPtr hInstance, uint threadid);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern void keybd_event(uint vk, uint scan, uint falgs, uint extraInfo);//( 가상 키, 스캔 코드, DOWN(0) or UP(2), 키보드타입); 일반키보드는 타입을 0으로

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(int wCode, int wMapType);//( 변환 할 키, 변환 동작 설정);
        /*
        keybd_event((byte) Keys.ControlKey, 0, 0x00, 0);
        keybd_event((byte) Keys.ControlKey, 0, 0x02, 0);
        //콘트롤키 눌렀다 뗌

        keybd_event(0, MapVirtualKey((int) Keys.D1, 0), 0x00, 0);
        keybd_event(0, MapVirtualKey((int) Keys.D1, 0), 0x02, 0);
        //숫자1키 눌렀다 뗌
        */

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr LParam);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;

        private IntPtr hook = IntPtr.Zero;
        private static LowLevelKeyboardProc _hook;

        const int LShift = 160;
        const int LCtrl = 162;
        const int TAB = 9;
        const int CapsLock = 20;
        const int Enter = 13;
        const int Space = 32;
        const int ChangeLanguage = 21;

        System.Windows.Forms.NotifyIcon noti;

        int[] specialKey = new int[7] { 160, 162, 9, 20, 13, 32, 21 };
        string[] specialKeyName = new string[7] { "LShift", "LCtrl", "TAB", "CapsLock", "Enter", "Space", "ChangeLanguage" };
        int setTrigger = 0;

        ArrayList moduleArrayList = new ArrayList();


        public MainWindow()
        {
            InitializeComponent();

            //start Hooking your keyboardevent
            IntPtr hInstance = LoadLibrary("user32");
            _hook = HookProc;
            hook = SetWindowsHookEx(WH_KEYBOARD_LL, _hook, hInstance, 0);

            //unhooking method append closing event
            this.Closing += MainWindow_Closing;

            //append trayicon
            noti = new System.Windows.Forms.NotifyIcon();
            noti.Icon = Properties.Resources.sshmacroico;
            noti.Visible = true;
            noti.Text = "CREATE MACRO JUST FOR YOU";
            noti.DoubleClick += delegate (object? sender, EventArgs e)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
            System.Windows.Forms.ToolStripMenuItem item1 = new System.Windows.Forms.ToolStripMenuItem();
            System.Windows.Forms.ToolStripMenuItem item2 = new System.Windows.Forms.ToolStripMenuItem();
            item1.Text = "Information";
            item1.Click += delegate (object? sender, EventArgs e)
            {
                MessageBox.Show("Since 2023.03.26.ssh");
            };
            item2.Text = "Close";
            item2.Click += delegate (object? sender, EventArgs e)
            {
                realCloseFlag = true;
                this.Close();
            };
            menu.Items.Add(item1);
            menu.Items.Add(item2);
            noti.ContextMenuStrip = menu;
        }


        private void KeyInput(char c)
        {
            byte result = 0;
            bool lShiftUse = false;
            if (c == '~') { result = 192; lShiftUse = true; }
            else if (c == '!') { result = 49; lShiftUse = true; }
            else if (c == '@') { result = 50; lShiftUse = true; }
            else if (c == '#') { result = 51; lShiftUse = true; }
            else if (c == '$') { result = 52; lShiftUse = true; }
            else if (c == '%') { result = 53; lShiftUse = true; }
            else if (c == '^') { result = 54; lShiftUse = true; }
            else if (c == '&') { result = 55; lShiftUse = true; }
            else if (c == '*') { result = 56; lShiftUse = true; }
            else if (c == '(') { result = 57; lShiftUse = true; }
            else if (c == ')') { result = 58; lShiftUse = true; }
            else if (c == '_') { result = 189; lShiftUse = true; }
            else if (c == '+') { result = 187; lShiftUse = true; }
            else if (c == '{') { result = 219; lShiftUse = true; }
            else if (c == '}') { result = 221; lShiftUse = true; }
            else if (c == ':') { result = 186; lShiftUse = true; }
            else if (c == '\"') { result = 222; lShiftUse = true; }
            else if (c == '<') { result = 188; lShiftUse = true; }
            else if (c == '>') { result = 190; lShiftUse = true; }
            else if (c == '?') { result = 191; lShiftUse = true; }
            else result = (byte)GetVkCode(c);

            if (lShiftUse) keybd_event(LShift, 0, 0, 0);
            keybd_event(result, 0, 0, 0);
            keybd_event(result, 0, 2, 0);
            if (lShiftUse) keybd_event(LShift, 0, 2, 0);
        }

        private int GetVkCode(char a)
        {
            Debug.WriteLine((int)a);
            if ((int)a >= 48 && (int)a <= 57) return (int)a;
            else if ((int)a >= 65 && (int)a <= 90) return (int)a;
            else if ((int)a >= 97 && (int)a <= 122) return (int)a - 32;
            else if (a.Equals('`')) return 192;
            else if (a.Equals('-')) return 189;
            else if (a.Equals('=')) return 187;
            else if (a.Equals('[')) return 219;
            else if (a.Equals(']')) return 221;
            else if (a.Equals(';')) return 186;
            else if (a.Equals('\'')) return 222;
            else if (a.Equals(',')) return 188;
            else if (a.Equals('.')) return 190;
            else if (a.Equals('/')) return 191;
            return 0;
        }

        bool inputingFlag = false;
        private IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (!inputingFlag && wParam == (IntPtr)WM_KEYDOWN)
            {
                inputingFlag = true;
                int vkCode = Marshal.ReadInt32(lParam);
                //Debug.WriteLine(vkCode);
                if (broadcastLabel.Visibility == Visibility.Visible)
                {
                    settingTextBox.Text = vkCode.ToString();
                    broadcastLabel.Visibility = Visibility.Collapsed;
                }
                else if (setTrigger > 0 && setTrigger == vkCode && moduleArrayList.Count > 0)
                {
                    keybd_event((byte)vkCode, 0, 2, 0);
                    foreach (Module m in moduleArrayList)
                    {
                        if (m.Mode == 0)
                        {
                            foreach (char c in m.GetStr())
                            {
                                KeyInput(c);
                            }
                        }
                        else
                        {
                            keybd_event((uint)specialKey[m.Mode - 1], 0, 0, 0);
                            keybd_event((uint)specialKey[m.Mode - 1], 0, 2, 0);
                        }
                    }
                }
                inputingFlag = false;
            }
            
            return CallNextHookEx(hook, code, (int)wParam, lParam);
        }

        bool realCloseFlag = false;
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (realCloseFlag)
            {
                UnhookWindowsHookEx(hook);
            }
            else
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void RefreshModule()
        {
            centerGrid.Children.Clear();
            foreach (Module m in moduleArrayList)
                centerGrid.Children.Add(m.Grid);
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MacroFile|*.smc";
            openFileDialog.Title = "Load your Macro";
            openFileDialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
            {
                moduleArrayList.Clear();
                string[] command = File.ReadAllText(openFileDialog.FileName).Split(new string[] { "\n", "\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                settingTextBox.Text = command[0];
                for (int i = 1; i < command.Length; i++)
                {
                    Module m = new Module(specialKey, specialKeyName, UpButton_Click, DownButton_Click, CloseButton_Click);
                    int mode = 0;
                    string s = string.Empty;
                    int.TryParse(command[i].Substring(0, 1), out mode);
                    s = command[i].Substring(1, command[i].Length - 1);
                    m.SetMode(mode, s);
                    moduleArrayList.Add(m);
                }
                RefreshModule();
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "MacroFile|*.smc";
            saveFileDialog.Title = "Save your Macro";
            saveFileDialog.ShowDialog();

            if (!string.IsNullOrWhiteSpace(saveFileDialog.FileName))
            {
                StringBuilder moduleTransStr = new StringBuilder();
                moduleTransStr.AppendLine(settingTextBox.Text);
                foreach (Module m in moduleArrayList)
                {
                    moduleTransStr.Append(m.Mode.ToString());
                    moduleTransStr.AppendLine(m.GetStr());
                }
                File.WriteAllText(saveFileDialog.FileName, moduleTransStr.ToString());
            }
        }

        private void PanelButton_Click(object sender, RoutedEventArgs e)//좌우패널을 접고 여는 버튼
        {
            if ((sender as Button).Content as string == "◀")
            {
                if ((sender as Button) == leftPanelButton) leftPanel.Width = new GridLength(0);
                else rightPanel.Width = new GridLength(100);
                (sender as Button).Content = "▶";
            }
            else
            {
                if ((sender as Button) == leftPanelButton) leftPanel.Width = new GridLength(100);
                else rightPanel.Width = new GridLength(0);
                (sender as Button).Content = "◀";
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)//모듈을 생성하는 버튼
        {
            Module m = new Module(specialKey, specialKeyName, UpButton_Click, DownButton_Click, CloseButton_Click);
            centerGrid.Children.Add(m.Grid);
            moduleArrayList.Add(m);
        }

        /*모듈 위치 조절 버튼들*/
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Module current = (sender as Button).Tag as Module;
            if (moduleArrayList[0] as Module == current) return;
            for (int i = 1; i < moduleArrayList.Count; i++)
            {
                if (moduleArrayList[i] as Module == current)
                {
                    moduleArrayList.RemoveAt(i);
                    moduleArrayList.Insert(i - 1, current);
                    RefreshModule();
                    return;
                }
            }
        }
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Module current = (sender as Button).Tag as Module;
            if (moduleArrayList[moduleArrayList.Count-1] as Module == current) return;
            for (int i = moduleArrayList.Count-2; i >=0; i--)
            {
                if (moduleArrayList[i] as Module == current)
                {
                    moduleArrayList.RemoveAt(i);
                    moduleArrayList.Insert(i + 1, current);
                    RefreshModule();
                    return;
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            centerGrid.Children.Remove(((sender as Button).Tag as Module).Grid);
            moduleArrayList.Remove((sender as Button).Tag as Module);
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            broadcastLabel.Visibility = Visibility.Visible;
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (setTrigger==0)
            {
                int value = 0;
                if (int.TryParse(settingTextBox.Text, out value))
                {
                    setTrigger = value;
                    startButton.Content = "정지";
                    settingTextBox.IsEnabled = false;
                    settingButton.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show("트리거키가 숫자가 아님");
                }
            }
            else
            {
                setTrigger = 0;
                startButton.Content = "시작";
                settingTextBox.IsEnabled = true;
                settingButton.IsEnabled = true;
            }
        }
        /*모듈 위치 조절 버튼들*/

    }

    public class Module
    {
        Grid grid = new Grid();
        TextBox textBox = new TextBox();
        Button button = new Button();
        Button closeButton = new Button();
        Button upButton = new Button();
        Button downButton = new Button();
        int mode = 0;
        int[] specialKey;
        string[] specialKeyName;
        public Module(int[] specialKey, string[] specialKeyName, RoutedEventHandler up, RoutedEventHandler down, RoutedEventHandler close)
        {
            this.specialKeyName = specialKeyName;
            this.specialKey = specialKey;

            grid.Width = 300;
            grid.Height = 30;
            grid.Background = new SolidColorBrush(Colors.White);

            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.Width = 30;
            button.Click += Button_Click;
            button.Tag = this;
            button.Content = "N";
            button.Background = new SolidColorBrush(Colors.White);
            grid.Children.Add(button);

            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.Width = 30;
            closeButton.Content = "X";
            closeButton.Tag = this;
            closeButton.Click += close;
            grid.Children.Add(closeButton);

            upButton.HorizontalAlignment = HorizontalAlignment.Right;
            upButton.Width = 30;
            upButton.Margin = new Thickness(0, 0, 50, 0);
            upButton.Content = "▲";
            upButton.Tag = this;
            upButton.Click += up;
            grid.Children.Add(upButton);

            downButton.HorizontalAlignment = HorizontalAlignment.Right;
            downButton.Width = 30;
            downButton.Margin = new Thickness(0, 0, 80, 0);
            downButton.Content = "▼";
            downButton.Tag = this;
            downButton.Click += down;
            grid.Children.Add(downButton);

            textBox.Width = 160;
            textBox.Margin = new Thickness(30, 0, 0, 0);
            textBox.HorizontalAlignment = HorizontalAlignment.Left;
            textBox.Style = Application.Current.Resources["WatermarkTextBox"] as Style;
            textBox.Tag = "입력할 문자";
            grid.Children.Add(textBox);
        }

        public void SetMode(int num, string str)
        {
            if (num == 0)
            {
                mode = num;
                textBox.IsEnabled = true;
                textBox.Text = str;
                button.Content = "N";
                button.Background = new SolidColorBrush(Colors.White);
            }
            else if (num > 0 && num <= specialKey.Length)
            {
                mode = num;
                textBox.IsEnabled = false;
                textBox.Text = specialKeyName[num - 1];
                button.Content = "S";
                button.Background = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                SetMode(0, str);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mode++;
            if (mode > specialKey.Length) mode = 0;
            SetMode(mode, string.Empty);
        }
        public string GetStr()
        {
            return textBox.Text;
        }
        public int Mode
        {
            get { return mode; }
        }
        public Grid Grid
        {
            get { return grid; }

        }
    }
}
