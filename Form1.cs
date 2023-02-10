using System;
using System.Windows.Forms;
using System.IO.Ports;

namespace KCHam
{
    public partial class Form1 : Form
    {
        SerialPort rsPort = new SerialPort();
        byte[] txBuffer = new byte[11] { 0x40, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d };
        byte[] rxBuffer = new byte[11];
        double freq;
        uint count = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //initialize RS-232 serial port
            rsPort.PortName = "COM1";
            rsPort.BaudRate = 9600;
            rsPort.Parity = Parity.None;
            rsPort.DataBits = 8;
            rsPort.StopBits = StopBits.One;
            rsPort.Handshake = Handshake.None;
            rsPort.ReadTimeout = 2000;
            rsPort.WriteTimeout = 500;
            rsPort.Open();
            //Raise RTS
            rsPort.RtsEnable = true;
        }

        private void ReadDirection()
        {
            if(radioButton1.Checked)
                txBuffer[7] = 0x00;//forward
            else if(radioButton2.Checked)
                txBuffer[7] = 0x40;//reverse
            else
                txBuffer[7] = 0x80;//bidir
        }

        private void ReadMode()
        {
            if (radioButton4.Checked)
                txBuffer[8] = 0x01;//normal
            else if(radioButton5.Checked)
                txBuffer[8] = 0x56;//calibrate
            else if (radioButton6.Checked)
                txBuffer[8] = 0x53;//home
            else if (radioButton7.Checked)
                txBuffer[8] = 0x52;//enable serial
            else
                txBuffer[8] = 0x55;//disable serial
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //clear rxBuffer
            Array.Clear(rxBuffer, 0, 11);
            //flush rx uart
            rsPort.DiscardInBuffer();
            //clear text box
            textBox1.Clear();

            //query message
            byte[] xBuffer = new byte[3] { 0x3f, 0x41, 0x0d };
            //write serial data
            rsPort.Write(xBuffer, 0, 3);

            //receive response and print to textBox1
            try
            {
                //read serial data
                int j = 0;
                while (j < rxBuffer.Length)
                {
                    rxBuffer[j] = (byte)rsPort.ReadByte();
                    textBox1.AppendText("0x" + rxBuffer[j].ToString("X") + " ");
                    j++;
                }
            }
            catch (TimeoutException) { return; }

            //convert bytes to freq
            freq = 10 * (65536 *rxBuffer[3] + 256 * rxBuffer[4] + rxBuffer[5]);
            double fMHz = freq / 1e6;
            textBox1.AppendText(fMHz.ToString("F2") + " MHz");

            //color light green if still, yellow if moving
            if (rxBuffer[6] != 0)
                textBox1.BackColor = System.Drawing.Color.Yellow;
            else
                textBox1.BackColor = System.Drawing.Color.LightGreen;

            //forward
            if (rxBuffer[7] == 0x15)
                radioButton1.BackColor= System.Drawing.Color.LightGreen;
            else
                radioButton1.BackColor = System.Drawing.Color.Transparent;

            //reverse
            if (rxBuffer[7] == 0x55)
                radioButton2.BackColor = System.Drawing.Color.LightGreen;
            else
                radioButton2.BackColor = System.Drawing.Color.Transparent;

            //bidir
            if (rxBuffer[7] == 0x95)
                radioButton3.BackColor = System.Drawing.Color.LightGreen;
            else
                radioButton3.BackColor = System.Drawing.Color.Transparent;

            count++;

            if (count == 1)
            {
                //determine base frequency
                freq /= 1e6;
           
                if (freq < 10.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("7.0");
                else if (freq < 14.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("10.0");
                else if (freq < 18.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("14.0");
                else if (freq < 24.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("18.0");
                else if (freq < 28.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("24.5");
                else if (freq < 28.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("28.0");
                else if (freq < 29.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("28.5");
                else if (freq < 29.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("29.0");
                else if (freq < 50.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("29.5");
                else if (freq < 50.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("50.0");
                else if (freq < 51.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("50.5");
                else if (freq < 51.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("51.0");
                else if (freq < 52.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("51.5");
                else if (freq < 52.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("52.0");
                else if (freq < 53.0)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("52.5");
                else if (freq < 53.5)
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("53.0");
                else
                    comboBox1.SelectedIndex = comboBox1.FindStringExact("53.5");

                //determine freq offset
                double freqOffset = Math.Round((freq % 0.5) / .025) * 25;
                comboBox2.SelectedIndex = comboBox2.FindStringExact(freqOffset.ToString());

                //align to combo box freq
                button1_Click(sender, e);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //convert combo boxes to freq
            freq = 1e6 * (Convert.ToDouble(comboBox1.Text) + Convert.ToDouble(comboBox2.Text) / 1e3);
            //pull out bytes of freq
            txBuffer[3] = (byte)((((uint)freq/10)>>16) & 0xff) ;
            txBuffer[4] = (byte)((((uint)freq/10) >> 8) & 0xff);
            txBuffer[5] = (byte)((uint)freq/10 & 0xff);
            ReadDirection();
            ReadMode();
            //write serial data
            rsPort.Write(txBuffer, 0, txBuffer.Length);
        }
    }
}   