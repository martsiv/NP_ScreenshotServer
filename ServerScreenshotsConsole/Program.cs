using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using Shared;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Net.Http;

namespace ServerScreenshotsConsole
{
    class Program
    {
        static IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
        static int port = 7777;
        static async Task Main(string[] args)
        {
            IPEndPoint localEndPoint = new IPEndPoint(iPAddress, port);

            // створюємо екземпляр сервера вказуючи кінцеву точку для приєднання
            TcpListener server = new TcpListener(localEndPoint);
            // запускаємо прослуховування вказаної кінцевої точки
            server.Start(10);

            await StartListeningAsync(server);
        }
        static async Task StartListeningAsync(TcpListener server)
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("\tWaiting for command...");
                    // отримуємо зв'язок з клієнтом асинхронно
                    TcpClient client = await server.AcceptTcpClientAsync(); // waiting...

                    Task.Run(async () => await HandleClientAsync(client));
                    //await HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                server.Stop();
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            //Імітація затримки в роботі сервера, щоб потестувати чи не фрізить інтерфейс
            //await Task.Delay(5000);

            using var reader = new StreamReader(client.GetStream());
            var response = await reader.ReadLineAsync();
            Console.WriteLine(response);

            FileTransferInfo info = new FileTransferInfo();
            info.Name = Path.GetFileName($"Screen_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.jpg");

            using (MemoryStream stream = new MemoryStream())
            {
                // Зберігаємо зображення у форматі PNG в потік
                ScreenCapture.GetFullScreenshot().Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

                // Отримуємо масив байтів із потоку
                info.Data = stream.ToArray();
            }

            using (NetworkStream stream = client.GetStream())
            {
                // серіалізуємо об'єкт класу та відправляємо його на клієнта асинхронно
                await JsonSerializer.SerializeAsync(stream, info);
            }
        }
    }

    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();
        static Size GetMonitorSize()
        {
            IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
            using Graphics g = Graphics.FromHwnd(hwnd);
            return new Size((int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height);
        }
        static Bitmap TakeScreenshot(Size size)
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height);
            using Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(Point.Empty, Point.Empty, bmp.Size);
            return bmp;
        }
        public static Bitmap GetFullScreenshot()
        {
            SetProcessDPIAware();
            Size monitorSize = GetMonitorSize();
            return TakeScreenshot(monitorSize);
        }
        public static void SaveScreenshot(string path, ImageFormat format)
        {
            GetFullScreenshot().Save(path, format);
        }
    }
}