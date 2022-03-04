using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime;

namespace pomsta
{
    public partial class Form1 : Form
    {

        BindingList<string> sites;

        int hour, min;

        //-- INITIALIZE FORM --

        public Form1()
        {

            string jsonString = "";

            if (File.Exists("data.json"))
                jsonString = File.ReadAllText("data.json");

            InitializeComponent();

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

            if (hour * 3600 + min * 60 > time.Hour * 3600 + time.Minute * 60)
            {

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                startInfo.CreateNoWindow = true;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = String.Format("/C docker run --name pomsta -ti --rm alpine/bombardier -c 1000 -d {0}s -l https://{1}", (hourNUD.Value - time.Hour) * 3600 + (minNUD.Value - time.Minute) * 60, comboBox1.Text);

                process.StartInfo = startInfo;
                process.Start();

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

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C docker stop pomsta";

            process.StartInfo = startInfo;
            process.Start();

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
    }
}