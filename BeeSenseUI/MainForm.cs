using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using BeeSenseUI.Json;
using BeeSenseUI.Tests;
using Newtonsoft.Json;
using ZedGraph;

namespace BeeSenseUI
{
    public partial class MainForm : Form
    {
        private string _url = "https://data.sparkfun.com/output/QG8QpXOX9mInr1D5DoAj.json";
        private string _testJSON = "[{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.10\",\"humidity_1\":\"41.80\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T13:19:42.723Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.10\",\"humidity_1\":\"41.50\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T13:09:30.118Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.10\",\"humidity_1\":\"41.40\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T13:09:15.479Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.10\",\"humidity_1\":\"41.60\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T13:01:18.076Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.10\",\"humidity_1\":\"41.50\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T12:50:51.966Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.20\",\"humidity_1\":\"41.20\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T12:40:25.073Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.20\",\"humidity_1\":\"40.80\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T12:29:58.255Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.30\",\"humidity_1\":\"41.20\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T12:19:31.705Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.40\",\"humidity_1\":\"40.90\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T12:09:04.976Z\"},{\"temperature_2\":\" NaN\",\"temperature_1\":\"21.40\",\"humidity_1\":\"40.90\",\"humidity_2\":\" NaN\",\"timestamp\":\"2016-02-12T11:58:40.176Z\"}]";
        private bool _isRunning = false;

        public MainForm()
        {
            InitializeComponent();

            // Add version
            var version = Assembly.GetEntryAssembly().GetName().Version;
            Text += " v" + version;

            // Setup combo box
            comboBoxType.Items.Add("Temperature #1");
            comboBoxType.Items.Add("Humidity #1");
            comboBoxType.Items.Add("Temperature #2");
            comboBoxType.Items.Add("Humidity #2");
            comboBoxType.SelectedIndex = 0;

            // Setup graph
            zedGraphControl1.GraphPane.Title.Text = "Hive Sensor Data";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "Value";
            zedGraphControl1.GraphPane.YAxis.Scale.Min = 0;
            zedGraphControl1.GraphPane.YAxis.Scale.Max = 100;

            zedGraphControl1.GraphPane.XAxis.Title.Text = "Time";
            zedGraphControl1.GraphPane.XAxis.Type = AxisType.Date;
            zedGraphControl1.GraphPane.XAxis.Title.Text = "Time (HH:MM:SS)";
            zedGraphControl1.GraphPane.XAxis.Scale.Format = "HH:mm:ss";
            zedGraphControl1.GraphPane.XAxis.Scale.MajorUnit = DateUnit.Minute;
            zedGraphControl1.GraphPane.XAxis.Scale.MinorUnit = DateUnit.Minute;
            //zedGraphControl1.GraphPane.XAxis.Scale.Min = DateTime.Now.Subtract(new TimeSpan(0, 0, 10, 0, 0)).ToOADate();
            //zedGraphControl1.GraphPane.XAxis.Scale.Max = DateTime.Now.ToOADate();
            

            // Enable scrollbars if needed
            zedGraphControl1.IsShowHScrollBar = false;
            zedGraphControl1.IsShowVScrollBar = false;
            zedGraphControl1.IsAutoScrollRange = true;
            zedGraphControl1.IsShowPointValues = true;

            zedGraphControl1.AxisChange();
            // Make sure the Graph gets redrawn
            zedGraphControl1.Invalidate();

            // Set UI
            UpdateUI();

#if false
            // Tests
            JsonTest.DeserializeBeeJSON(_testJSON);

            JsonTest.DownloadAndDeserialiseJSON(_url);
#endif
        }

        private void UpdateUI()
        {
            textBoxServerURL.Text = _url;
            buttonStop.Enabled = _isRunning;
            buttonStart.Enabled = !_isRunning;
            comboBoxType.Enabled = !_isRunning;
            textBoxServerURL.Enabled = !_isRunning;
            textBoxUpdateIntervalSecs.Enabled = !_isRunning;
        }

        private void ButtonStartClick(object sender, System.EventArgs e)
        {
            try
            {
                timerPullJSON.Interval = int.Parse(textBoxUpdateIntervalSecs.Text) * 1000;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't parse value in update interval text box");
                return;
            }

            _url = textBoxServerURL.Text;

            _isRunning = true;
            UpdateUI();
            Application.DoEvents();

            // Kick off immediately
            TimerPullJsonTick(this, null);

            timerPullJSON.Start();

        }

        private void ButtonStopClick(object sender, System.EventArgs e)
        {
            timerPullJSON.Stop();

            _isRunning = false;
            UpdateUI();
        }

        private void TimerPullJsonTick(object sender, EventArgs e)
        {
            Debug.WriteLine("Updating");

            toolStripStatusLabel1.Text = "Updating";
            Application.DoEvents();

            try
            {
                // Grab data
                var json = new WebClient().DownloadString(_url);

                // Fixup NAN -> NaN
                json = json.Replace("NAN", "NaN");

                // Deserialize data
                var collData = JsonConvert.DeserializeObject<List<BeeData>>(json);

                UpdateGraphSeries(collData);

            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "Problem retrieving JSON:" + ex.Message;
                ButtonStopClick(this, null);
                return;
            }

            toolStripStatusLabel1.Text = "Done";
            Application.DoEvents();

            Debug.WriteLine("Update done");
        }

        private void UpdateGraphSeries(List<BeeData> collData)
        {
            var temp1List = new PointPairList();
            var temp2List = new PointPairList();
            var humidity1List = new PointPairList();
            var humidity2List = new PointPairList();

            foreach(var data in collData)
            {
                temp1List.Add(new XDate(data.timestamp), data.temperature_1);
                temp2List.Add(new XDate(data.timestamp), data.temperature_2);
                humidity1List.Add(new XDate(data.timestamp), data.humidity_1);
                humidity2List.Add(new XDate(data.timestamp), data.humidity_2);
            }

            zedGraphControl1.GraphPane.CurveList.Clear();

            switch(comboBoxType.SelectedIndex)
            {
                case 0:
                    zedGraphControl1.GraphPane.AddCurve("Temperature 1", temp1List, Color.Red, SymbolType.Diamond);
                    break;
                case 1:
                    zedGraphControl1.GraphPane.AddCurve("Humidity 1", humidity1List, Color.Blue, SymbolType.Diamond);
                    break;
                case 2:
                    zedGraphControl1.GraphPane.AddCurve("Temperature 2", temp2List, Color.Green, SymbolType.Diamond);
                    break;
                case 3:
                    zedGraphControl1.GraphPane.AddCurve("Humidity 2", humidity2List, Color.Orange, SymbolType.Diamond);
                    break;
            }

            // up the proper scrolling parameters
            zedGraphControl1.AxisChange();
            // Make sure the Graph gets redrawn
            zedGraphControl1.Invalidate();
        }
    }
}
