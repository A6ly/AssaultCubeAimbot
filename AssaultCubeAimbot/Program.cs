using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Memory;

namespace AssaultCubeAimbot
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(uint vk);
        private const uint VK_LBUTTON = 0x00000001;

        public struct Player
        {
            public float x, y, z;
            public int teamnum;
        }

        public struct Entity
        {
            public float distance, x, y, z;
            public int hp, teamnum;
        }

        static public float GetDistance(Entity enemy, Player player)
        {
            return Convert.ToSingle(Math.Sqrt(Math.Pow(enemy.x - player.x, 2) + Math.Pow(enemy.y - player.y, 2) + Math.Pow(enemy.z - player.z, 2)));
        }

        static public int GetProximateEnemy(float[] List, int total)
        {
            int ProximateEnemyNum = 0;
            float ProximateDistance = List[0];

            for (int i = 1; i < total; i++)
            {
                if(List[i] < ProximateDistance)
                {
                    ProximateDistance = List[i];
                    ProximateEnemyNum = i;   
                }
            }
            return ProximateEnemyNum;
        }

        static public float[] GetAngle(Entity enemy, Player player)
        {
            float[] degree = { 0, 0 };

            if (player.y > enemy.y && player.x < enemy.x)
            {
                degree[0] = (float)(Math.Atan((player.y - enemy.y) / (enemy.x - player.x)) * 180 / Math.PI); //degree:= atan((enemyy % closest % -myy) / (enemyx % closest % -myx)) * 57.3
                degree[0] = 90 - degree[0];
            }
            if (player.y > enemy.y && player.x > enemy.x)
            {
                degree[0] = (float)(Math.Atan((player.y - enemy.y) / (player.x - enemy.x)) * 180 / Math.PI);
                degree[0] += 270;
            }
            if (player.y < enemy.y && player.x < enemy.x)
            {
                degree[0] = (float)(Math.Atan((enemy.y - player.y) / (enemy.x - player.x)) * 180 / Math.PI);
                degree[0] += 90;
            }
            if (player.y < enemy.y && player.x > enemy.x)
            {
                degree[0] = (float)(Math.Atan((enemy.y - player.y) / (player.x - enemy.x)) * 180 / Math.PI);
                degree[0] = 270 - degree[0];
            }
            if (player.z > enemy.z)
            {
                degree[1] = (float)(-1 * Math.Asin((player.z - enemy.z) / enemy.distance) * 180 / Math.PI);
            }
            else if (player.z < enemy.z)
            {
                degree[1] = (float)(1 * Math.Asin((enemy.z - player.z) / enemy.distance) * 180 / Math.PI);
            }
            return degree;
        }

        static void Main(string[] args)
        {
            Console.Title = "AssaultCubeAimbot@";
            Console.SetWindowSize(35, 15);

            int PID, total, ProximateEnemy;
            bool isopened;
            float[] Distance = new float[31];
            float[] angle = { 0, 0 };
            Mem mem = new Mem();
            Player player;
            Entity[] entities = new Entity[31];
            Console.WriteLine("Process searching...");
            while (true)
            {
                PID = mem.getProcIDFromName("ac_client");

                if (PID != 0)
                    break;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Process found!");
            Thread.Sleep(1400);
            isopened = mem.OpenProcess(PID);

            if (isopened)
            {
                Console.Beep();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Process attach success!!\n");
                Console.WriteLine("Have fun playing games!!!");
            }

            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Process attach fail...");
                Thread.Sleep(2000);
                Environment.Exit(0);
            }

            while (true)
            {
                total = mem.readInt("ac_client.exe+0x110D98");

                player.x = mem.readFloat("ac_client.exe+0011E20C,0x34");
                player.y = mem.readFloat("ac_client.exe+0x0011E20C,0x38");
                player.z = mem.readFloat("ac_client.exe+0x0011E20C,0x3C");
                player.teamnum = mem.readInt("ac_client.exe+0x0011E20C,0x32C");

                for (int i = 0; i < total; i++)
                {
                    entities[i].x = mem.readFloat("ac_client.exe+0x110D90," + (i * 4).ToString("x2") + ",0x34");
                    entities[i].y = mem.readFloat("ac_client.exe+0x110D90," + (i * 4).ToString("x2") + ",0x38");
                    entities[i].z = mem.readFloat("ac_client.exe+0x110D90," + (i * 4).ToString("x2") + ",0x3C");
                    entities[i].hp = mem.readInt("ac_client.exe+0x110D90," + (i * 4).ToString("x2") + ",0xF8");
                    entities[i].teamnum = mem.readInt("ac_client.exe+0x110D90," + (i * 4).ToString("x2") + ",0x32C");

                    if (entities[i].hp > 0 && entities[i].teamnum != player.teamnum)
                        Distance[i] = GetDistance(entities[i], player);

                    else
                        Distance[i] = float.MaxValue;

                    entities[i].distance = Distance[i];
                    //Console.WriteLine((i+1) + "번째 적의 거리 : " + Distance[i]);
                }
                ProximateEnemy = GetProximateEnemy(Distance, total);
                angle = GetAngle(entities[ProximateEnemy], player);

                if(GetAsyncKeyState(VK_LBUTTON) != 0 && Distance[ProximateEnemy] != float.MaxValue)
                {
                    mem.writeMemory("ac_client.exe+0x109B74,0x40", "float", angle[0].ToString());
                    mem.writeMemory("ac_client.exe+0x109B74,0x44", "float", angle[1].ToString());
                }
               //Console.WriteLine("가장 가까운 적 : " + ProximateEnemy + " 거리 : " + Distance[ProximateEnemy]);
            }
        }
    }
}