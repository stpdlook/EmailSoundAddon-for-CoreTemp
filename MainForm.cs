using GetCoreTempInfoNET;
using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;

namespace AddonCoreTemp
{
    public partial class MainForm : Form
    {
        private CoreTempInfo CTInfo;
        private System.Timers.Timer RefreshInfo;
        private System.Timers.Timer WaitHour;

        private string dir = Directory.GetCurrentDirectory();
        private int avr = 0;
        private string average = null;

        private string smtpAddress = null;
        private int smtpPort = 0;
        private string emailFrom = null;
        private string passwrd = null;
        private string emailTo = null;
        private int maxTemp = 70;
        private string fromName = null;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                smtpAddress = smtpAddressText.Text;
                smtpPort = Convert.ToInt32(smtpPortText.Text);
                emailFrom = emailFromText.Text;
                passwrd = passwrdText.Text;
                emailTo = emailToText.Text;
                maxTemp = Convert.ToInt32(maxTempText.Text);
                fromName = fromNameText.Text;

                LogText.Text = "Сохранено!\n";
            }
            catch
            {
                MessageBox.Show("Необходимо заполнить все поля", "Ошибка");
            }
            
        }

        public MainForm()
        {
            InitializeComponent();
            Handler();
        }

        // Создание необходимых объектов
        public void Handler()
        {
                CTInfo = new CoreTempInfo();
                CTInfo.ReportError += new ErrorOccured(CTInfo_ReportError);

                RefreshInfo = new System.Timers.Timer();
                RefreshInfo.Interval = 1000;
                RefreshInfo.Elapsed += new ElapsedEventHandler(RefreshInfo_Elapsed);

                RefreshInfo_Elapsed(null, null);
                RefreshInfo.Start();
                TempLabel.Text = average;

                WaitHour = new System.Timers.Timer();
                WaitHour.Interval = 1000;
                WaitHour.Elapsed += new ElapsedEventHandler(Check);
                WaitHour.Start();
        }

        // Проверка на макс. температуру
        // Запуск всех видов алармов
        // Переопределение времени таймера с одной секунды (строчка 72) на один час, указывается в миллисекундах
        private void Check(object sender, ElapsedEventArgs ee)
        {
            bool bReadSuccess = CTInfo.GetData();
            if (Convert.ToInt32(average) >= maxTemp)
            {

                if (bReadSuccess)
                {
                    WaitHour.Stop();
                    AlarmWav();
                    EmailAlert();
                    WaitHour.Interval = 1000*60*60;
                    WaitHour.Start();
                }
            }
        }


        //Отправка email
        private void EmailAlert()
        {
            SmtpClient smtp = new SmtpClient()
            {
                Host = smtpAddress,
                Port = smtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential()
                {
                    UserName = emailFrom,
                    Password = passwrd
                }
            };

            MailAddress FromEmail = new MailAddress(emailFrom, fromName);
            MailAddress ToEmail = new MailAddress(emailTo, fromName);

            // Формирование сообщения
            MailMessage Message = new MailMessage()
            {
                From = FromEmail,
                Subject = "Email Alert",
                Body = "<h2>Температура: " + average + " градусов</h2>",
                IsBodyHtml = true
            };

            Message.To.Add(emailTo);

            try
            {
                smtp.Send(Message);
                LogText.Invoke((MethodInvoker)delegate
                {
                    LogText.Text += "Сообщение отправлено \n";
                });
                
            }
            catch (Exception ex)
            {
                LogText.Invoke((MethodInvoker)delegate
                {
                    LogText.Text += "Что-то не так \n" + ex.Message;
                });
            }

        }


        //Включение сигнализации
        private void AlarmWav()
        {
            SoundPlayer sp = new SoundPlayer(dir + "\\CoreTemperatureSignal.wav");
            sp.Play();
        }


        // Сбор данных от coretemp'а
        private void RefreshInfo_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool bReadSuccess = CTInfo.GetData();

            if (bReadSuccess)
            {
                for (int cpu = 0; cpu < CTInfo.GetCPUCount; cpu++)
                {
                    for (int core = 0; core < CTInfo.GetCoreCount; core++)
                    {
                        int index = Convert.ToInt32(cpu * CTInfo.GetCoreCount + core);
                        int temp = Convert.ToInt32(CTInfo.GetTemp[index]);
                        avr = (temp + avr);
                    }
                    average = (avr / Convert.ToInt32(CTInfo.GetCoreCount)).ToString();
                    avr = 0;
                }
            }
        }


        // Обработка ошибок
        private void CTInfo_ReportError(ErrorCodes ErrCode, string ErrMsg)
        {
            if (IsHandleCreated)
            {
                LogText.Invoke((MethodInvoker)delegate // Убирает ошибку "Запуск не в том потоке..."
                {
                    LogText.ForeColor = Color.Red;
                    LogText.Text = ErrMsg;
                });
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            TempLabel.Text = average;
        }

        // Назначение максимальной температуры
        private void button2_Click(object sender, EventArgs e)
        {
            maxTemp = Convert.ToInt32(maxTempText.Text);
        }


        //
        // Чтобы сворачивать в область уведомлений
        //

        private void MainForm_Load(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "Addon for CoreTemp";
            notifyIcon1.BalloonTipText = "Приложение свернуто";
            notifyIcon1.Text = "Addon for CoreTemp";
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }
            else if (FormWindowState.Normal == this.WindowState)
                notifyIcon1.Visible = false;
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
