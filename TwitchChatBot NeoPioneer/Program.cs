namespace TwitchChatBot_NeoPioneer
{
    internal class Program
    {
        static int i = 5;
        static float f = 5.0f;
        static bool b = i.Equals(f);

        static public TwitchClientContainer ClientContainer = new TwitchClientContainer();
        static void Main(string[] args){
            ClientContainer.Initialize();
            Console.WriteLine(b);
            while (true) {
                Console.ReadLine();
            }
        }
    }
}
