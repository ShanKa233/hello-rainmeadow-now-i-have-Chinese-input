using System;
using System.Reflection;

namespace GoodMorningRainMeadow
{
    public static class References
    {
        public static Assembly RainMeadowAssembly { get; private set; }

        public static void Initialize()
        {
            try
            {
                RainMeadowAssembly = Assembly.Load("Rain Meadow");
                if (RainMeadowAssembly == null)
                {
                    throw new Exception("无法加载Rain Meadow程序集");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("加载Rain Meadow程序集失败", ex);
            }
        }
    }
} 