namespace AntiDebugDotNet
{
    internal class CreateHiddenThreadExample
    {
        static void Main_copy(string[] args)
        {
            Console.WriteLine("Place a breakpoint on NtCreateThreadEx...");
            Console.ReadLine();
            Console.WriteLine("Creating a new thread");
            Thread thread = new Thread(doNothing);
            thread.Start();
            thread.Join();
        }

        private static void doNothing()
        {
            Console.WriteLine("Doing Nothing");
        }
    }
}
