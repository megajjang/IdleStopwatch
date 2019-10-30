using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

internal struct LASTINPUTINFO
{
    public uint cbSize;
    public uint dwTime;
}

namespace IdleStopwatch
{
    public partial class Form1 : Form
    {
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED          = 0x00000001, 
            ES_DISPLAY_REQUIRED         = 0x00000002,
            ES_AWAYMODE_REQUIRED        = 0x00000040,
            ES_CONTINUOUS               = 0X80000000
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_LBUTTONDOWN = 0x0201;

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();
        [DllImport("Kernel32.dll", CharSet =CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCature();

        private HashSet<Control> controlToMove = new HashSet<Control>();

        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInput);
            GetLastInputInfo(ref lastInput);

            return ((uint)Environment.TickCount - lastInput.dwTime);
        }

        public static long GetLastInputTime()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInput);
            if (!GetLastInputInfo(ref lastInput))
                throw new Exception(GetLastError().ToString());

            return lastInput.dwTime;
        }

        public Form1()
        {
            InitializeComponent();

            controlToMove.Add(this);
            controlToMove.Add(this.label1);
        }

        private void FormInit(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer ftimer = new System.Windows.Forms.Timer();
            ftimer.Interval = 1000;
            ftimer.Tick += OnTimer;

            ftimer.Start();
            doWorking();
        }

        private void OnTimer(object sender, EventArgs e)
        {
            doWorking();
        }

        private void doWorking()
        {
            long hh = 0, mm = 0, ss = 0;
            string strText = "";
            long dwTick = GetIdleTime() / 1000;
            Console.WriteLine(dwTick.ToString());

            //if(dwTick % 550 <= 1)
            if(dwTick % 60 == 1)
            {
                Console.WriteLine("SetThreadExecutionState");
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }

            if (dwTick > 59)
            {
                long nQ = dwTick / 60;
                ss = dwTick % 60;
                if (nQ > 60)
                {
                    mm = nQ % 60;
                    hh = nQ / 60;
                }
                else
                    mm = nQ;
            }
            else
                ss = dwTick;

            strText = String.Format("{0:D2}:{1:D2}:{2:D2}", hh, mm, ss);
            label1.Text = strText;
        }

        public void doClose(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnClose(object sender, FormClosedEventArgs e)
        {
            //SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS):
        }

        private void OnMoveDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                label1.Capture = false;

                //ReleaseCature();
                Message msg = Message.Create(this.Handle, WM_NCLBUTTONDOWN, new IntPtr(HT_CAPTION), IntPtr.Zero);
                this.DefWndProc(ref msg);
            }
            else if(e.Button == MouseButtons.Right)
            {
                ContextMenu menu = new ContextMenu();
                MenuItem mitem = new MenuItem("Close");
                mitem.Click += new EventHandler(doClose);

                menu.MenuItems.Add(mitem);
                menu.Show(label1, e.Location);
            }
        }
    }
}
