using System.Diagnostics;

public class Lab1
{
    public static void Main()
    {
        double a = 0.0;
        double b = 1.0;
        int n = 100;
        int m = 10;
        var x = new List<double>();
        var y = new List<double>();
        var s = new List<double>();
        double mstime;

        Configurate(out int inputMode, out int calculationMode, out int outputMode);

        if (inputMode == 0)
            RandomizeData(n, x, y);
        else
        {
            ReadDataFromFile(x, y);
            n = x.Count - 1;
        }

        var sw = Stopwatch.StartNew();

        if (calculationMode == 0)
            s = Solve(a, b, n, x, y);
        else
            s = ParallelSolve(a, b, n, x, m);

        sw.Stop();
        mstime = sw.ElapsedMilliseconds;

        if (outputMode == 0)
            PrintToConsole(x, y, s);
        else
            PrintToFile(x, y, s);

        Console.WriteLine($"Program get {(inputMode == 0 ? "randomly generated data" : "data from file")} " +
            $"calculate it in {(calculationMode == 0 ? "single thread" : "multithread")} in {mstime} ms " +
            $"and write result to {(outputMode == 0 ? "console" : "file")}.");
    }

    public static void Configurate(out int inputMode, out int calculationMode, out int outputMode) 
    {
        Console.WriteLine("Select input mode:\n" +
            "0 - random;\n" +
            "1 - from file;");
        inputMode = int.Parse(Console.ReadLine());
        while (inputMode != 0 && inputMode != 1)
        {
            Console.WriteLine("Invalid data.\n" +
                "Reenter:");
            inputMode = int.Parse(Console.ReadLine());
        }

        Console.WriteLine("Select calculation mode:\n" +
            "0 - in one thread;\n" +
            "1 - in multithread;");
        calculationMode = int.Parse(Console.ReadLine());
        while (calculationMode != 0 && calculationMode != 1)
        {
            Console.WriteLine("Invalid data.\n" +
                "Reenter:");
            calculationMode = int.Parse(Console.ReadLine());
        }

        Console.WriteLine("Select output mode:\n" +
            "0 - to console;\n" +
            "1 - to file;");
        outputMode = int.Parse(Console.ReadLine());
        while (outputMode != 0 && outputMode != 1)
        {
            Console.WriteLine("Invalid data.\n" +
                "Reenter:");
            outputMode = int.Parse(Console.ReadLine());
        }
    }
    public static void RandomizeData(int n, List<double> x, List<double> y)
    {
        Random rand = new Random();
        x.Clear();
        y.Clear();

        for (int i = 0; i <= n; i++)
        {
            x.Add(rand.NextDouble());
            y.Add(rand.NextDouble());
        }

        x.Sort();
        y.Sort();
    }
    public static void ReadDataFromFile(List<double> x, List<double> y)
    {
        x.Clear();
        y.Clear();

        using (StreamReader reader = new StreamReader("input.txt"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(';');
                x.Add(double.Parse(values[0]));
                y.Add(double.Parse(values[1]));
            }
        }
    }
    public static List<double> Solve(double a, double b, int N, List<double> x, List<double> y)
    {
        double h = (b - a) / N;

        // Initialize the tridiagonal matrix
        double[] A = new double[N + 1];
        double[] B = new double[N + 1];
        double[] C = new double[N + 1];
        double[] D = new double[N + 1];

        for (int i = 1; i < N; i++)
        {
            A[i] = 1 / (h * h);
            B[i] = -2 / (h * h) + 1;
            C[i] = 1 / (h * h);
            D[i] = x[i] * x[i];
        }

        // Apply boundary conditions
        B[0] = 1;
        C[0] = 0;
        D[0] = 0;

        A[N] = 0;
        B[N] = 1;
        D[N] = 1;

        // Perform the tridiagonal matrix algorithm
        for (int i = 1; i <= N; i++)
        {
            double m = A[i] / B[i - 1];
            B[i] -= m * C[i - 1];
            D[i] -= m * D[i - 1];
        }

        // Back-substitute to find the solution
        y[N] = D[N] / B[N];

        for (int i = N - 1; i >= 0; i--)
        {
            y[i] = (D[i] - C[i] * y[i + 1]) / B[i];
        }

        return y;
    }
    public static List<double> ParallelSolve(double a, double b, int N, List<double> x, int numThreads)
    {
        double h = (b - a) / N;

        // Initialize the tridiagonal matrix
        double[] A = new double[N + 1];
        double[] B = new double[N + 1];
        double[] C = new double[N + 1];
        double[] D = new double[N + 1];

        for (int i = 1; i < N; i++)
        {
            A[i] = 1 / (h * h);
            B[i] = -2 / (h * h) + 1;
            C[i] = 1 / (h * h);
            D[i] = x[i] * x[i];
        }

        // Apply boundary conditions
        B[0] = 1;
        C[0] = 0;
        D[0] = 0;

        A[N] = 0;
        B[N] = 1;
        D[N] = 1;

        // Define the range of indices for each thread
        List<Tuple<int, int>> ranges = new List<Tuple<int, int>>();
        int blockSize = N / numThreads;
        int remainder = N % numThreads;
        int start = 0;
        int end = 0;
        for (int i = 0; i < numThreads; i++)
        {
            start = end;
            end = start + blockSize - 1;
            if (remainder > 0)
            {
                end++;
                remainder--;
            }
            ranges.Add(new Tuple<int, int>(start, end));
        }

        // Solve the equation in parallel using the specified number of threads
        List<double> y = new List<double>(new double[N + 1]);
        Parallel.For(0, numThreads, i =>
        {
            Tuple<int, int> range = ranges[i];
            int threadStart = range.Item1;
            int threadEnd = range.Item2;

            List<double> subX = x.GetRange(threadStart, threadEnd - threadStart + 1);
            List<double> subY = new List<double>(new double[subX.Count]);
            for (int j = 0; j < subX.Count; j++)
            {
                int index = j + threadStart;
                subY[j] = D[index];
                if (index > 0)
                {
                    subY[j] -= A[index] * y[index - 1];
                }
                if (index < N)
                {
                    subY[j] -= C[index] * y[index + 1];
                }
                subY[j] /= B[index];
            }

            lock (y)
            {
                for (int j = 0; j < subX.Count; j++)
                {
                    int index = j + threadStart;
                    y[index] = subY[j];
                }
            }
        });

        return y;
    }
    public static void PrintToConsole(List<double> x, List<double> y, List<double> solution)
    {
        Console.WriteLine("Input data:");
        for (int i = 0; i < x.Count; i++)
        {
            Console.WriteLine("x: {0}\t y: {1}", x[i], y[i]);
        }

        Console.WriteLine("\nSolution:");
        for (int i = 0; i < x.Count; i++)
        {
            Console.WriteLine("x: {0}\t y: {1}", x[i], solution[i]);
        }
    }
    public static void PrintToFile(List<double> x, List<double> y, List<double> solution)
    {
        using (StreamWriter writer = new StreamWriter("output.txt"))
        {
            writer.WriteLine("Input data:");
            for (int i = 0; i < x.Count; i++)
            {
                writer.WriteLine("x: {0}\t y: {1}", x[i], y[i]);
            }

            writer.WriteLine("\nSolution:");
            for (int i = 0; i < x.Count; i++)
            {
                writer.WriteLine("x: {0}\t y: {1}", x[i], solution[i]);
            }
        }
    }
}