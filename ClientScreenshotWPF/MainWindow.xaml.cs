using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace ClientScreenshotWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
        static int port = 7777;
        CancellationTokenSource cancelTokenSource;
        CancellationToken token;
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);
        }
        private async void Timer_Tick(object sender, EventArgs e)
        {
            await GetScreenshot();
        }
        private async void GetScreenButton_Click(object sender, RoutedEventArgs e)
        {
            await GetScreenshot();
        }
        public async Task GetScreenshot()
        {
            IPEndPoint endPoint = new IPEndPoint(iPAddress, port);
            using TcpClient client = new TcpClient();

            try
            {
                // виконуємо підключення
                await client.ConnectAsync(endPoint);

                using var writer = new StreamWriter(client.GetStream());
                {
                    writer.WriteLineAsync("Get screenshot");
                    writer.Flush();
                }
                //Зчитуємо скрішот з серверу і десеріалізуємо його
                var info = await JsonSerializer.DeserializeAsync<FileTransferInfo>(client.GetStream());
                //Показуємо скріншот у вікні View WPF 
                SetImageIntoViewAsync(info.Data);
                //Перевіряємо чи існує шлях для збереження скріншоту
                if (!Directory.Exists("Files"))
                    Directory.CreateDirectory("Files");
                //Зберігаємо скріншот на клієнті
                await File.WriteAllBytesAsync($"Files/{info.Name}", info.Data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                client.Close();
            }

        }
        private void SetImageIntoViewAsync(byte[] data)
        {
            BitmapImage bitmap = new BitmapImage();

            // Асинхронно створюємо MemoryStream і завантажуємо дані в нього
            using (MemoryStream stream = new MemoryStream(data))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }

            // Призначте BitmapImage об'єкт властивості Source вашого елемента Image в XAML
            myImage.Source = bitmap;
        }

        private void CheckBoxIntervalAction_Checked(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int intervalSeconds))
            {
                timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
                timer.Start();
            }
        }

        private void CheckBoxIntervalAction_Unchecked(object sender, RoutedEventArgs e)
        {
            timer.Stop();

        }
    }
}
