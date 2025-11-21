using Sdcb.OpenVINO;
using System.Diagnostics;
using EZSockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.Sockets;
using RJCP.IO.Ports;
using System;
using System.Xml;
using System.Numerics;

namespace MtcLibrary1
{
    public class Infer
    {
        public float[] Get_temper(int bs)
        {
            byte[] msg = new byte[] { 0x01, 0x03, 0x00, 0x20, 0x00, 0x10, 0x45, 0xCC };
            List<List<float>> inputDatas = new List<List<float>>();
            List<float> inputData = new List<float>();
            using (SerialPortStream ser = new SerialPortStream("COM4", 9600))
            {
                ser.Open();
                for (int i = 0; i < bs; i++)
                {
                    ser.Write(msg, 0, msg.Length);
                    Thread.Sleep(9994);
                    byte[] returnedData = new byte[37];
                    ser.Read(returnedData, 0, 37);
                    for (int j = 3; j < 15; j += 2) // 3-14共6组数据
                    {
                        // 处理字节序转换（大端转系统字节序）
                        byte[] buffer = new byte[2];
                        if (BitConverter.IsLittleEndian)
                        {
                            buffer[0] = returnedData[j + 1];
                            buffer[1] = returnedData[j];
                        }
                        else
                        {
                            buffer[0] = returnedData[j];
                            buffer[1] = returnedData[j + 1];
                        }
                        short value = BitConverter.ToInt16(buffer, 0);
                        inputData.Add((float)(value / 10.0));
                    }
                    inputDatas.Add(inputData);
                    inputData = new List<float>();
                }
                ser.Close();
                int count = inputDatas.Count * inputDatas[0].Count;
                float[] reshaped = new float[count];
                for (int i = 0; i < inputDatas.Count; i++)
                {
                    for (int j = 0; j < inputDatas[0].Count; j++)
                    {
                        reshaped[i * 6 + j] = inputDatas[i][j];
                    }
                }
                string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "_温度值记录.txt";
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string logMessage = $"[本次温度: [{string.Join(", ", reshaped)}]\n\n";
                File.AppendAllText(filePath, logMessage);
                return reshaped;
            }
        }
        public void Run_infer(float[] para, float[] Final_tem, int bs, float[] first_tem, int[] last_compen, out int[] this_compen)
        {
            ReadOnlySpan<float> data;
            string password = "CHENZHONG";
            string ip = "192.168.200.1";
            FileEncryption fileEncryption = new FileEncryption(password);
            Compen compen = new Compen();

            float[] processed_tem = new float[bs * 6];
            for (int i = 0; i < bs; i++)
            {
                for (int j = 0; j < 6; j++) 
                {
                    processed_tem[i * 6 + j] = (Final_tem[i * 6 + j] - first_tem[j]) / para[0] + para[1];
                }
            }
            fileEncryption.DecryptFile();
            using Model rawModelz = OVCore.Shared.ReadModel("C:\\Nvidia\\PartList_all.xml");
            //using Model rawModely = OVCore.Shared.ReadModel("C:\\Nvidia\\PartList_y.xml");
            using CompiledModel cmz = OVCore.Shared.CompileModel(rawModelz, "CPU");
            //using CompiledModel cmy = OVCore.Shared.CompileModel(rawModely, "CPU");
            using InferRequest irz = cmz.CreateInferRequest();
            //using InferRequest iry = cmy.CreateInferRequest();
            long[] a = { 1, bs, 6 };
            Shape inputShape = new Shape(a);
            using (Tensor input = Tensor.FromArray<float>(processed_tem, inputShape)) { irz.Inputs.Primary = input; }
            //using (Tensor input = Tensor.FromArray<float>(processed_tem, inputShape)) { iry.Inputs.Primary = input; }
            Stopwatch stopwatch = new();
            stopwatch.Start();
            irz.Run();
            //iry.Run();
            stopwatch.Stop();
            double inferTime = stopwatch.Elapsed.TotalMilliseconds;

            using (Tensor outputz = irz.Outputs.Primary)
            //using (Tensor outputz = irz.Outputs.Primary, outputy = iry.Outputs.Primary)
            {
                data = outputz.GetData<float>();
                float z = -(data[0] - para[3]) * para[2] / para[6] * 5 * 1000; //z
                //datay = outputy.GetData<float>();
                float y = (data[4] - para[3]) * para[2] / para[5] * 5 * 1000* 1.25f; //yx
                float x = -(data[1] - para[3]) * para[2] / para[4] * 5 * 1000 ; //xx
                float[] compen_xyz = new float[] { x, y, z };
                Console.WriteLine($"infer time: {inferTime:F5}ms");
                this_compen = compen.GetDeviceWrite(compen_xyz, ip, last_compen);
            }

        }
    }
    public class Compen
    {
        private EZNCAUTLib.DispEZNcCommunication EZNcCom;//通讯库变量
        private int lResult = 1;
        private int lSystemType;
        public void GetSimConnect1(string machine_ip)
        {
            lSystemType = (int)sysType.EZNC_SYS_MELDAS800M;
            if (EZNcCom == null)
            {
                string ip = machine_ip;
                EZNcCom = new EZNCAUTLib.DispEZNcCommunication();
                lResult = EZNcCom.SetTCPIPProtocol(ip, 683);//683
                lResult = EZNcCom.Open2(lSystemType, 1, 1, "EZNC_LOCALHOST");//EZNC_LOCALHOST
            }
        }
        public int[] GetDeviceWrite(float[] compen_xyz, string ip, int[] last_compen)
        {
            GetSimConnect1(ip);
            lResult = 0;
            if (lResult != 0) { Console.WriteLine("[Error]:Socket warning"); return last_compen; }
            int[] compen = { 0, 0, 0, 0, 0, 0 };
            compen[0] = Convert.ToInt32(compen_xyz[0]);
            if (compen[0] < 0) { compen[1] = -1; }
            compen[2] = Convert.ToInt32(compen_xyz[1]);
            if (compen[2] < 0) { compen[3] = -1; }
            compen[4] = Convert.ToInt32(compen_xyz[2]);
            if (compen[4] < 0) { compen[5] = -1; }
            if ( Math.Abs(compen[0]) < 20 && Math.Abs(compen[2]) < 20 && Math.Abs(compen[4]) < 25)
            //if (Math.Abs(compen[0] - last_compen[0]) < 3 && Math.Abs(compen[2] - last_compen[2]) < 3 && Math.Abs(compen[4] - last_compen[4]) < 3 && Math.Abs(compen[0]) < 20 && Math.Abs(compen[2]) < 20 && Math.Abs(compen[4]) < 25)
                {
                lResult = EZNcCom.Device_WriteBlock(6, "R5700", 20, compen);
                if (lResult == 0)
                {
                    string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "_正常补偿值记录.txt";
                    string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                    string logMessage = $"[本次位移: [{string.Join(", ", compen)}]\n\n";
                    File.AppendAllText(filePath, logMessage);
                    Console.Write("Compensation: ");
                    Console.Write($"[x]:{compen[0]} ");
                    Console.Write($"[y]:{compen[2]} ");
                    Console.WriteLine($"[z]:{compen[4]}");
                    return compen;
                }
                else
                {
                    Console.WriteLine("[Error]:Device Write Warning");
                    return last_compen;
                }
            }
            else
            {
                //日志
                string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "补偿值异常日志.txt";
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string logMessage = $"[{timeStamp}] 上次位移: [{string.Join(", ", last_compen)}] 本次位移: [{string.Join(", ", compen)}]\n\n";
                File.AppendAllText(filePath, logMessage);
                Console.WriteLine("[ERROR]:补偿值异常，具体情况请查看日志");
                return last_compen;
            }

        }
    }
    public class FileEncryption
    {
        private readonly string _password;
        private readonly byte[] _salt = Encoding.UTF8.GetBytes("WUQISEN");

        public FileEncryption(string password)
        {
            _password = password;
        }

        // 1. 读取文件1，按密码加密并保存为文件2
        public void EncryptFile()
        {
            // 确保输出目录存在
            Directory.CreateDirectory(Path.GetDirectoryName("C:\\Users\\kaleidoscope\\Desktop\\PartList_all.xml"));
            Directory.CreateDirectory(Path.GetDirectoryName("C:\\Users\\kaleidoscope\\Desktop\\PartList_all.bin"));
            //Directory.CreateDirectory(Path.GetDirectoryName("C:\\Users\\kaleidoscope\\Desktop\\PartList_y.xml"));
            //Directory.CreateDirectory(Path.GetDirectoryName("C:\\Users\\kaleidoscope\\Desktop\\PartList_y.bin"));
            // 读取文件内容
            byte[] fileBytes_xml_z = File.ReadAllBytes("C:\\Users\\kaleidoscope\\Desktop\\all.xml");
            byte[] fileBytes_bin_z = File.ReadAllBytes("C:\\Users\\kaleidoscope\\Desktop\\all.bin");
            byte[] encryptedBytes_xml_z = Encrypt(fileBytes_xml_z, _password);
            byte[] encryptedBytes_bin_z = Encrypt(fileBytes_bin_z, _password);
            //byte[] fileBytes_xml_y = File.ReadAllBytes("C:\\Users\\kaleidoscope\\Desktop\\y.xml");
            //byte[] fileBytes_bin_y = File.ReadAllBytes("C:\\Users\\kaleidoscope\\Desktop\\y.bin");
            //byte[] encryptedBytes_xml_y = Encrypt(fileBytes_xml_y, _password);
            //byte[] encryptedBytes_bin_y = Encrypt(fileBytes_bin_y, _password);

            File.WriteAllBytes("C:\\Users\\kaleidoscope\\Desktop\\PartList_all.xml", encryptedBytes_xml_z);
            File.WriteAllBytes("C:\\Users\\kaleidoscope\\Desktop\\PartList_all.bin", encryptedBytes_bin_z);
            //File.WriteAllBytes("C:\\Users\\kaleidoscope\\Desktop\\PartList_y.xml", encryptedBytes_xml_y);
            //File.WriteAllBytes("C:\\Users\\kaleidoscope\\Desktop\\PartList_y.bin", encryptedBytes_bin_y);
        }

        // 2. 读取文件2，解密并保存为文件3
        public void DecryptFile()
        {
            // 确保输出目录存在
            Directory.CreateDirectory(Path.GetDirectoryName("C:\\Nvidia\\PartList_all.xml"));
            Directory.CreateDirectory(Path.GetDirectoryName("C:\\Nvidia\\PartList_all.bin"));
            //Directory.CreateDirectory(Path.GetDirectoryName("C:\\Nvidia\\PartList_y.xml"));
            //Directory.CreateDirectory(Path.GetDirectoryName("C:\\Nvidia\\PartList_y.bin"));

            // 读取加密内容
            byte[] encryptedBytes_xml_z = File.ReadAllBytes("C:\\Users\\Admin\\Desktop\\PartList_all.xml");
            byte[] encryptedBytes_bin_z = File.ReadAllBytes("C:\\Users\\Admin\\Desktop\\PartList_all.bin");
            byte[] decryptedBytes_xml_z = Decrypt(encryptedBytes_xml_z, _password);
            byte[] decryptedBytes_bin_z = Decrypt(encryptedBytes_bin_z, _password);
            //byte[] encryptedBytes_xml_y = File.ReadAllBytes("C:\\Users\\Admin\\Desktop\\PartList_y.xml");
            //byte[] encryptedBytes_bin_y = File.ReadAllBytes("C:\\Users\\Admin\\Desktop\\PartList_y.bin");
            //byte[] decryptedBytes_xml_y = Decrypt(encryptedBytes_xml_y, _password);
            //byte[] decryptedBytes_bin_y = Decrypt(encryptedBytes_bin_y, _password);

            File.WriteAllBytes("C:\\Nvidia\\PartList_all.xml", decryptedBytes_xml_z);
            File.WriteAllBytes("C:\\Nvidia\\PartList_all.bin", decryptedBytes_bin_z);
            //File.WriteAllBytes("C:\\Nvidia\\PartList_y.xml", decryptedBytes_xml_y);
            //File.WriteAllBytes("C:\\Nvidia\\PartList_y.bin", decryptedBytes_bin_y);


        }

        // 3. 删除文件3
        public void DeleteFile()
        {
            string filePath = "C:\\Nvidia\\PartList_all.xml";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            filePath = "C:\\Nvidia\\PartList_all.bin";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            //filePath = "C:\\Nvidia\\PartList_y.xml";
            //if (File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //}
            //filePath = "C:\\Nvidia\\PartList_y.bin";
            //if (File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //}
        }

        // AES 加密方法
        private byte[] Encrypt(byte[] plainBytes, string password)
        {
            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, _salt);
                aes.Key = key.GetBytes(32); // 256 位密钥
                aes.IV = key.GetBytes(16); // 128 位 IV

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.Close();
                    }
                    return ms.ToArray();
                }
            }
        }

        // AES 解密方法
        private byte[] Decrypt(byte[] cipherBytes, string password)
        {
            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, _salt);
                aes.Key = key.GetBytes(32); // 256 位密钥
                aes.IV = key.GetBytes(16); // 128 位 IV

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    return ms.ToArray();
                }
            }
        }
    }
    public class Test_dis
    {
        private EZNCAUTLib.DispEZNcCommunication EZNcCom;//通讯库变量
        private int lResult = 1;
        private int lSystemType;
        public void GetSimConnect1(string machine_ip)
        {
            lSystemType = (int)sysType.EZNC_SYS_MELDAS800M;
            if (EZNcCom == null)
            {
                string ip = machine_ip;
                EZNcCom = new EZNCAUTLib.DispEZNcCommunication();
                lResult = EZNcCom.SetTCPIPProtocol(ip, 683);//683
                lResult = EZNcCom.Open2(lSystemType, 1, 1, "EZNC_LOCALHOST");//EZNC_LOCALHOST
                //lResult = 0;
            }
        }
        public void Offset(int[] compen_xyz)
        {
            string ip = "192.168.200.1";
            GetSimConnect1(ip);
            Console.Write("         Compensation: ");
            if (lResult == 0) { }
            else { Console.Write("[Error]:Socket warning\n"); return; }
            lResult = EZNcCom.Device_WriteBlock(6, "R5700", 20, compen_xyz);
            if (lResult == 0) { Console.Write($"[x]:{compen_xyz[0]} [y]:{compen_xyz[2]} [z]:{compen_xyz[4]}  (um)\n"); }
            else { Console.Write("[Error]:Can not set offset\n"); }
        }
        public void Get_dis(out string[] dis_list)
        {
            Thread.Sleep(2000);
            string host = "192.168.200.20";
            int[] ports = { 1031, 1032, 1033, 1034, 1035 };
            dis_list = new string[5] { "0.0000", "0.0000", "0.0000", "0.0000", "0.0000" };
            byte[] msg1 = new byte[] { 0x05, 0x04, 0x00, 0x00, 0x00, 0x02, 0x70, 0x4F };
            byte[] msg2 = new byte[] { 0x04, 0x04, 0x00, 0x00, 0x00, 0x02, 0x71, 0x9E };
            byte[] msg3 = new byte[] { 0x01, 0x04, 0x00, 0x00, 0x00, 0x02, 0x71, 0xCB };
            byte[] msg4 = new byte[] { 0x03, 0x04, 0x00, 0x00, 0x00, 0x02, 0x70, 0x29 };
            byte[] msg5 = new byte[] { 0x02, 0x04, 0x00, 0x00, 0x00, 0x02, 0x71, 0xF8 };
            byte[][] msg = { msg1, msg2, msg3, msg4, msg5 };

            for (int i = 0; i < ports.Length; i++)
            {
                HandlePortCommunication(host, ports[i], msg[i], out dis_list[i]);
            }
            Console.WriteLine("  (mm)");
        }

        public void HandlePortCommunication(string host, int port, byte[] msg, out string dis)
        {
            dis = "0.0000";
            try
            {
                using TcpClient client = new TcpClient();
                client.Connect(host, port);
                using NetworkStream stream = client.GetStream();
                stream.Write(msg, 0, msg.Length);// 发送消息
                Thread.Sleep(100);
                byte[] buffer = new byte[9];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                byte[] floatBytes = buffer.Skip(3).Take(4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(floatBytes);// 处理大端序转换（假设原始数据是大端序）
                }
                float value = BitConverter.ToSingle(floatBytes, 0);
                dis = value.ToString("F4"); // 格式化为4位小数
                Console.Write($"[{embed(port - 1030)}]:{dis} ");
            }
            catch (Exception ex)
            {
                //Console.Write($"[{embed(port - 1030)}]:1 ");
                Console.WriteLine($"[{port}]:[Error]:{ex.Message}");
            }
        }
        public string embed(int number)
        {
            string point = "None";
            if (number == 1) { point = "z"; }
            if (number == 2) { point = "xx"; }
            if (number == 3) { point = "xs"; }
            if (number == 4) { point = "ys"; }
            if (number == 5) { point = "yx"; }
            return point;
        }
        public void compare(string[] dis_list, string[] dis_list_new)
        {
            double[] dis_0 = new double[dis_list.Length];
            double[] dis_new = new double[dis_list.Length];
            for (int i = 0; i < dis_list.Length; i++)
            {
                dis_0[i] = Convert.ToDouble(dis_list[i]);
                dis_new[i] = Convert.ToDouble(dis_list_new[i]);
            }
            Console.WriteLine($"[delta_z]:{((dis_new[0] - dis_0[0]) * 1000.0).ToString("F1")} [delta_xx]:{((dis_new[1] - dis_0[1]) * 1000.0).ToString("F1")} [delta_xs]:{((dis_new[2] - dis_0[2]) * 1000.0).ToString("F1")} [delta_ys]:{((dis_new[3] - dis_0[3]) * 1000.0).ToString("F1")} [delta_yx]:{((dis_new[4] - dis_0[4]) * 1000.0).ToString("F1")}  (um)\n");
        }
    }
}


