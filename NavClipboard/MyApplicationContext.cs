using NavClipboard;
using NavClipboard.Properties;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

namespace NavDevClipboard
{
    class MyApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;

        public MyApplicationContext()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.app,
                ContextMenu = new ContextMenu(
                        new MenuItem[] {
                            new MenuItem("About...", ShowAbout),
                            new MenuItem("-"),
                            new MenuItem("Base64 to Clipboard", ConvertFromText),
                            new MenuItem("Clipboard to Base64", ConvertToText),                            
                            new MenuItem("-"),
                            new MenuItem("Load from file...", LoadFromFile),
                            new MenuItem("Save to file...", SaveToFile),
                            new MenuItem("-"),
                            new MenuItem("Exit", Exit)
                        }
                    ),
                Visible = true,                
                Text = "Nav Clipboard"
            };            
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show("Nav Clipboard Manager, ver: 1.3\n"
                            + "Sources: https://github.com/0xf1/navclipbrd");
        }

        private static void SaveToFile(object sender, EventArgs e)
        {
            string key = "", value = "";
            if (GetClipboardData(ref key, ref value))
            {
                SaveFileDialog dialog = new SaveFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, key);
                    File.AppendAllText(dialog.FileName, Environment.NewLine);
                    File.AppendAllText(dialog.FileName, value);
                }
            }                                    
        }

        private static void LoadFromFile(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();            
            if (dlg.ShowDialog() == DialogResult.OK)
            {                
                var cd = File.ReadAllLines(dlg.FileName);
                LoadToClipboard(cd[0], cd[1]);
            }                        
        }

        private static void LoadToClipboard(string format, string data)
        {
            var key = Encoding.UTF8.GetString(Convert.FromBase64String(format));
            var value = Decompress(Convert.FromBase64String(data));
            using (var ms = new MemoryStream(value))
            {
                Clipboard.SetData(key, ms);
            }
        }

        private static void ConvertFromText(object sender, EventArgs e)
        {
            var cd = Clipboard.GetText().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (cd.Length != 2)
            {
                MessageBox.Show("Unknown data in the clipboard");
                return;
            }
            LoadToClipboard(cd[0], cd[1]);
            var key = Encoding.UTF8.GetString(Convert.FromBase64String(cd[0]));
            MessageBox.Show($"{key} has been pushed to the clipboard");
        }

        private static void ConvertToText(object sender, EventArgs e)
        {
            string key = "", value = "";
            if (GetClipboardData(ref key, ref value)) {
                Clipboard.SetText(key + Environment.NewLine + value);
                var key2 = Encoding.UTF8.GetString(Convert.FromBase64String(key));
                MessageBox.Show($"{key2} has been converted to Base64 text format");
            }            
        }

        private static bool GetClipboardData(ref string key, ref string value)
        {
            var cd = Clipboard.GetDataObject();
            if (cd.GetFormats().Length == 0)
            {
                MessageBox.Show("No data in the clipboard");
                return false;
            }

            var fmt = cd.GetFormats()[0];
            var d = cd.GetData(fmt);
            if (d is MemoryStream stream)
            {            
                stream.Close();
                key = Convert.ToBase64String(Encoding.UTF8.GetBytes(fmt));
                value = Convert.ToBase64String(Compress(stream.ToArray()));                                
            }
            else
            {
                MessageBox.Show("unknown data format");
                return false;
            }
            return true;
        }


        private static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

    }
}
