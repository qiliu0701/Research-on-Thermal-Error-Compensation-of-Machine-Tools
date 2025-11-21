using Sdcb.OpenVINO;
using System.Numerics;

namespace MtcLibrary1;

public static class Program
{
    static void Main()
    {
        string work = "compensation";

        //string work = "initialization";

        //string work = "try";

        //string work = "dis_test";

        if (work == "compensation")
        {
            Console.WriteLine("补偿加工实验10.18");
            int BatchSize = 32;
            Infer ov_infer = new Infer();
            string password = "CHENZHONG";
            FileEncryption fileEncryption = new FileEncryption(password);
            float[] first_tem = ov_infer.Get_temper(1);
            float[] test_tem = first_tem;
            for (int i = 0; i < 6; i++)
            {
                if (test_tem[i] < -10 || test_tem[i] > 50)
                {
                    Console.WriteLine("[Error]:PT100 warning");
                    return;
                }
            }
            int[] compen_value_storge = { 0, 0, 0, 0, 0, 0 };
            float[] para = { 15.0f, 0.2f, 0.08f, 0.45f, 3.7f, 3.4f, 5.2f };//tem_one, tem_b, dis_one, dis_b, pxx, pyx, pz
            while (true)
            {
                test_tem = ov_infer.Get_temper(1);
                for (int i = 0; i < 6; i++)
                {
                    if (test_tem[i] < -10 || test_tem[i] > 50)
                    {
                        Console.WriteLine("[Error]:PT100 warning");
                        return;
                    }
                }
                ov_infer.Run_infer(para, ov_infer.Get_temper(BatchSize), BatchSize, first_tem, compen_value_storge, out compen_value_storge);
                fileEncryption.DeleteFile();
            }
        }

        
        if (work == "initialization")
        {
            Infer ov_infer = new Infer();
            float[] test_tem = ov_infer.Get_temper(1);
            for (int i = 0; i < 6; i++)
            {
                if (test_tem[i] < -10 || test_tem[i] > 50)
                {
                    Console.WriteLine("[Error]:PT100 warning");
                    return;
                }
                else
                {
                    Console.WriteLine("[Success]:PT100");
                }
            }
            Compen compen = new Compen();
            float[] compen_xyz = new float[] { 0f, 0f, 0f };
            int[] this_compen = { 0, 0, 0, 0, 0, 0 };
            int[] last_compen = { 0, 0, 0, 0, 0, 0 };
            string ip = "192.168.200.1";
            this_compen = compen.GetDeviceWrite(compen_xyz, ip, last_compen);

        }

        if (work == "dis_test")
        {
            int[] compen_xyz = { 0, -1, 0, -1, 0, -1 };//offset of (x y z)

            int[] reset = { 0, 0, 0, 0, 0, 0 };

            string[] dis_list = { "0.0000", "0.0000", "0.0000", "0.0000", "0.0000" };
            string[] dis_list_new = { "0.0000", "0.0000", "0.0000", "0.0000", "0.0000" };
            Test_dis mission_2 = new Test_dis();

            Console.ReadLine();
            mission_2.Offset(reset);
            mission_2.Get_dis(out dis_list);

            while (true)
            {
                Console.ReadLine();
                //compen_xyz[4] = compen_xyz[4]-5;//0--x,2--y,4--z
                //mission_2.Offset(compen_xyz);
                mission_2.Get_dis(out dis_list_new);
                mission_2.compare(dis_list, dis_list_new);
                dis_list = dis_list_new;
            }
        }
        if (work == "try")
        {
            float[] first_tem = { 0f, 0f, 0f, -20f, 0f, 0f };
            for (int i = 0; i < 6; i++)
            {
                if (first_tem[i] < -10 || first_tem[i] > 50)
                {
                    Console.WriteLine("[Error]:PT100 warning");
                    return;
                }
                Console.WriteLine("if");
            }
            Console.WriteLine("for");
        }
    }

}

