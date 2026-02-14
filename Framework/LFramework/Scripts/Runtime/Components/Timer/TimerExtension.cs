namespace LFramework.Runtime
{
    public static class TimerExtension
    {

        /// <summary>
        /// Ticks -> Seconds
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public static float ToSeconds(this long ticks)
        {
            return ticks / 1000f / 10000f; 
        }

        /// <summary>
        /// Seconds -> Ticks
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static long ToTicks(this float seconds)
        {
            return  (long)(seconds * 1000 * 10000);
        }
    }
}