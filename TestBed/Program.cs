using System;
using System.Linq;

namespace SandPile
{
    using BenchmarkDotNet.Attributes;

    class Program
    {
        private readonly struct Cell : IComparable<Cell>
        {
            public int X { get; }
            public int Y { get; }
            public float DistanceSquared { get; }

            public Cell(int x, int y, float distanceSquared)
            {
                this.X = x;
                this.Y = y;
                this.DistanceSquared = distanceSquared;
            }

            public int CompareTo(Cell other) => this.DistanceSquared.CompareTo(other.DistanceSquared);

            public override string ToString() => $"X: {this.X}, Y: {this.Y}, Weight: {this.DistanceSquared}";
        }

        private static readonly ConsoleColor[] colorScale =
        {
            ConsoleColor.Magenta,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkRed,
            ConsoleColor.Red,
            ConsoleColor.DarkYellow,
            ConsoleColor.Yellow,
            ConsoleColor.Green,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.Cyan,
            ConsoleColor.Blue,
            ConsoleColor.DarkBlue,
            ConsoleColor.White,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
            ConsoleColor.Black
        };

        private enum SortMode
        {
            Closest,
            Farthest,
            Smallest,
            Greatest,
            Random
        }

        static void Main(string[] args)
        {
            var random = new Random();
            var sortMode = SortMode.Greatest;
            var startingCount = 450;
            var gridWidth = 1;
            int dataWidth;
            int gridMidPoint;
            double colorScale;
            float distanceScale;

            ushort[] table;
            PairingHeap<Cell> minimumHeap;
            PairingHeap<Cell>[] heapNodes;

            void Step(int x, int y)
            {
                bool UpdateMain(int x1, int y1, int index)
                {
                    void AddToHeap(int x2, int y2, int cellValue, int index2/*, bool remove*/)
                    {
                        int distance;
                        switch (sortMode)
                        {
                            case SortMode.Closest:
                                {
                                    var xDistance = gridMidPoint - x2; // 11837
                                    var yDistance = gridMidPoint - y2;
                                    distance = xDistance * xDistance + yDistance * yDistance;
                                    distance = (int)(distance * distanceScale);
                                }
                                break;
                            case SortMode.Farthest:
                                {
                                    var xDistance = gridMidPoint - x2; // 11837
                                    var yDistance = gridMidPoint - y2;
                                    distance = xDistance * xDistance + yDistance * yDistance;
                                    distance = startingCount - (int)(distance * distanceScale);
                                }
                                break;
                            case SortMode.Smallest:
                                distance = cellValue;
                                break;
                            case SortMode.Greatest:
                                distance = startingCount - cellValue;
                                break;
                            case SortMode.Random:
                                distance = random.Next(startingCount);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        //2222
                        //var xDistance = mid - x2; //11448
                        //var yDistance = mid - y2;
                        //var xDistance = 69 - x2; // 11837
                        //var yDistance = 69 - y2;
                        //var cell = new Cell(x2, y2, xDistance xDistance + yDistance yDistance);
                        //var distance = max - cellValue; // biggest 8807
                        //var distance = cellValue; // smallest 11914
                        //var distance = index2; // radial 11834
                        //distance = xDistance * xDistance + yDistance * yDistance; // distance from 

                        var previous = heapNodes[index2];
                        if (previous != null)
                            if (previous == minimumHeap)
                                minimumHeap = minimumHeap.RemoveMinimum();
                            else
                                previous.RemoveFromTree();

                        var cell = new Cell(x2, y2, distance);
                        var newHeap = new PairingHeap<Cell>(cell);
                        minimumHeap = minimumHeap?.Add(newHeap) ?? newHeap;
                        heapNodes[index2] = newHeap;

                    }

                    var val = table[index];
                    if (val < 4) // below activation level, nothing to do
                        return false;
                    var gift = (ushort)Math.DivRem(val, 4, out var center);
                    table[index] = (ushort)center;
                    // up
                    var up = table[index - 1] += gift; // new value
                    if (up >= 4) // will be activated
                        AddToHeap(x1, y1 - 1, up, index - 1/*, up >= 4 + gift*/);

                    // down
                    var down = 0; // dumb compiler
                    var onDiagonal = false;
                    var reflectedGift = gift;
                    switch (Math.Abs(y1 - x1)) // how close are we to the diagonal?
                    {
                        case 0: // on it, so below will be updated via reflection
                            onDiagonal = true;
                            break;
                        case 1 when y1 < gridMidPoint - 1: // touching, but not center
                            reflectedGift <<= 1;
                            break;
                        case 1: // touching and center
                            reflectedGift <<= 2;
                            break;
                    }
                    if (!onDiagonal)
                    {
                        down = table[index + 1] += reflectedGift;
                        if (down >= 4) // will
                            AddToHeap(x1, y1 + 1, down, index + 1/*,down >= 4 + gift1*/);
                    }

                    // left
                    int left;
                    if (x1 != y1) // never on the diagonal
                    {
                        reflectedGift = Math.Abs(y1 - x1) == 1 ? (ushort)(gift << 1) : gift; // touching diagonal
                        left = table[index - dataWidth] += reflectedGift;

                        if (left >= 4) // will
                            AddToHeap(x1 - 1, y1, left, index - dataWidth/*,left >= 4 + gift1*/);
                    }
                    else
                        left = up;

                    // right
                    int right;
                    if (x1 != gridMidPoint)
                    {
                        reflectedGift = x1 == gridMidPoint - 1 ? (ushort)(gift << 1) : gift; // touching center line
                        right = table[index + dataWidth] += reflectedGift;

                        if (right >= 4)
                            AddToHeap(x1 + 1, y1, right, index + dataWidth/*,right >= 4 + gift1*/);
                    }
                    else
                        right = left;
                    if (onDiagonal)
                        down = right;
                    PrintCross2(x1, y1, center, up, down, left, right);
                    return true;
                }

                void PrintCross2(int x1, int y1, int center, int up, int down, int left, int right)
                {
                    PrintAt(x1, y1, center);
                    PrintAt(x1, y1 - 1, up);
                    PrintAt(x1, y1 + 1, down);
                    PrintAt(x1 - 1, y1, left);
                    PrintAt(x1 + 1, y1, right);
                }

                void PrintAt(int x1, int y1, int val)
                {
                    Console.CursorLeft = x1 + x1;
                    Console.CursorTop = y1;
                    Console.BackgroundColor = val < 4
                    ? Program.colorScale[15 - val]
                    : Program.colorScale[11 - (int)(Math.Log(val - 3) / colorScale)];
                    Console.Write("  ");
                }

                void IdentityReflectSwapReflect(int x1, int y1, int index, bool isReflection = false)
                {

                    int center, up, down, left, right;
                    if (isReflection)
                    {
                        center = table[index];
                        left = table[index + dataWidth];
                        up = table[index + 1];
                        down = table[index - 1];
                        right = table[index - dataWidth];

                        PrintCross2(x1, y1, center, up, down, left, right);
                    }
                    else if (!UpdateMain(x1, y1, index))
                        return;
                    else
                    {
                        var xOrig = Math.DivRem(index, dataWidth, out var yOrig);
                        center = table[index];
                        up = table[index - 1];
                        left = xOrig != yOrig ? table[index - dataWidth] : up;
                        right = xOrig != gridMidPoint ? table[index + dataWidth] : left;
                        down = xOrig != yOrig ? table[index + 1] : right;
                    }

                    y1 = gridWidth - y1 - 1;

                    PrintCross2(x1, y1, center, down, up, left, right);

                    var t = x1;
                    x1 = y1;
                    y1 = t;

                    PrintCross2(x1, y1, center, left, right, down, up);
                    if (y1 == gridMidPoint)
                        PrintCross2(gridWidth - x1 - 1, y1, center, left, right, up, down);
                    else
                        PrintCross2(x1, gridWidth - y1 - 1, center, right, left, down, up);

                }

                var i = x * dataWidth + y;
                heapNodes[i] = null;
                if (x == gridMidPoint)
                    if (y == gridMidPoint)
                        UpdateMain(x, y, i);
                    else
                        IdentityReflectSwapReflect(x, y, i);
                else
                {
                    IdentityReflectSwapReflect(x, y, i);
                    if (x != y)
                        IdentityReflectSwapReflect(gridWidth - x - 1, gridWidth - y - 1, i, true);
                }
            }

            void Print()
            {
                Console.CursorLeft = 0;
                Console.CursorTop = 0;
                for (var y = 0; y < gridWidth; y++)
                {
                    for (int x = 0, index = y; x < gridWidth; x++, index += dataWidth)
                    {
                        //var val = table[index];
                        Console.BackgroundColor = ConsoleColor.Black;// val < 4
                                                                     //? Program.colorScale[15 - val]
                                                                     //: Program.colorScale[11 - (int)(Math.Log(val - 3) / d)];
                        Console.Write("  ");
                    }
                    Console.WriteLine();
                }

                Console.BackgroundColor = ConsoleColor.Black;
            }

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                if (int.TryParse(line, out var tryMax))
                    startingCount = tryMax;

                gridWidth = (int)MathF.Ceiling(MathF.Sqrt(startingCount / (2 * MathF.PI)) * 2) | 1;
                dataWidth = gridWidth;
                gridMidPoint = dataWidth >> 1;

                // !!!
                dataWidth = gridMidPoint + 1; // cool

                distanceScale = (float)startingCount / (gridWidth * gridWidth);

                var adjustedMax = startingCount - 2;
                var maxLog = Math.Log(adjustedMax);
                colorScale = maxLog / 12;

                table = new ushort[dataWidth * dataWidth];
                table[table.Length - 1] = (ushort)startingCount;
                minimumHeap = new PairingHeap<Cell>(new Cell(gridMidPoint, gridMidPoint, 0));
                heapNodes = new PairingHeap<Cell>[table.Length];
                heapNodes[heapNodes.Length - 1] = minimumHeap;
                Console.CursorVisible = false;
                Console.BackgroundColor = ConsoleColor.Black;
                Print();
                //Console.WriteLine(startingCount);
                //Console.WriteLine(steps);
                var steps = 0;
                while (true)//Console.ReadLine() != "q")
                {
                    //Console.ReadKey();
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.Q:
                                minimumHeap = null; // breaks out of loop
                                break;
                            case ConsoleKey.UpArrow:
                                sortMode = SortMode.Greatest;
                                break;
                            case ConsoleKey.DownArrow:
                                sortMode = SortMode.Smallest;
                                break;
                            case ConsoleKey.LeftArrow:
                                sortMode = SortMode.Closest;
                                break;
                            case ConsoleKey.RightArrow:
                                sortMode = SortMode.Farthest;
                                break;
                            case ConsoleKey.Enter:
                                sortMode = SortMode.Random;
                                break;
                        }
                    }

                    if (minimumHeap == null)
                        break;
                    var min = minimumHeap.Element;

                    minimumHeap = minimumHeap.RemoveMinimum();

                    Step(min.X, min.Y);
                    //System.Diagnostics.Debug.Assert(heaps.Count(h => h != null) == heap.Count);

                    //Print();
                    steps++;
                }
                
                Console.BackgroundColor = ConsoleColor.Black;
                Console.CursorTop = gridWidth;
                Console.CursorLeft = 0;
                Console.WriteLine(steps);
                startingCount += 1;
            }
            //Print();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.CursorTop = gridWidth;
        }
    }

    internal class PairingHeap<T> where T : IComparable<T>
    {
        public int Count => (this.child?.Count ?? 0) + (this.right?.Count ?? 0) + 1;

        public override string ToString() => $"Count: {this.Count}; {this.Element}";

        public T Element { get; set; }
        private PairingHeap<T> left, right, child;

        public PairingHeap(T element) : this(element, null) { }

        private PairingHeap(T element, PairingHeap<T> child)
        {
            this.Element = element;
            this.child = child;
            if (child != null)
                child.left = this;
        }

        private PairingHeap<T> Merge(PairingHeap<T> other)
        {
            if (other == null)
                return this;
            PairingHeap<T> oldChild;
            if (this.Element.CompareTo(other.Element) < 0)
            {
                oldChild = this.child;
                if (oldChild != null)
                    oldChild.left = other;
                this.child = other;
                other.left = this;
                other.right = oldChild;
                return this;
            }
            oldChild = other.child;
            if (oldChild != null)
                oldChild.left = this;
            other.child = this;
            this.left = other;
            this.right = oldChild;
            return other;
        }

        public PairingHeap<T> Add(T element) => this.Merge(new PairingHeap<T>(element));
        public PairingHeap<T> Add(PairingHeap<T> other) => this.Merge(other);

        public PairingHeap<T> RemoveMinimum() => PairingHeap<T>.MergePairs(this.child);

        public PairingHeap<T> RemoveFromTree()
        {
            var previous = this.left;
            if (previous == null)
            {
                previous = this.RemoveMinimum();
            }
            else
            {
                ref var left = ref (this == previous.child ? ref previous.child : ref previous.right);

                if (this.right == null)
                {
                    left = this.child;
                    if (this.child != null)
                        this.child.left = previous;
                }
                else
                {
                    left = this.right;
                    this.right.left = previous;
                    if (this.child != null)
                    {
                        var sibling = this.right;
                        while (sibling.right != null)
                            sibling = sibling.right;
                        sibling.right = this.child;
                        this.child.left = sibling;
                    }
                }
            }
            this.left = this.right = this.child = null;
            return previous;
        }
        private static PairingHeap<T> MergePairs(PairingHeap<T> list)
        {
            if (list == null)
                return null;
            list.left = null;
            var next = list.right;
            if (next == null)
                return list;
            var rest = next.right;
            list.left = list.right = next.left = next.right = null;
            return list.Merge(next).Merge(PairingHeap<T>.MergePairs(rest));
        }
    }


    [ShortRunJob]
    public class GcfTests
    {
        private readonly Random random = new Random();
        [Params(100, 1000)]
        public int Size { get; set; }

        [Benchmark]
        public int Modulus()
        {
            var a = this.random.Next(this.Size) + 1;
            var b = this.random.Next(this.Size) + 1;
            if (a > b)
            {
                var t = a;
                a = b;
                b = t;
            }
            var r = a % b;

            while (r != 0)
            {
                a = b;
                b = r;
                r = a % b;
            }

            return b;
        }

        [Benchmark]
        public int EuclidSubtraction()
        {

            var a = this.random.Next(this.Size) + 1;
            var b = this.random.Next(this.Size) + 1;

            while (a != b)
                if (a > b)
                    a -= b;
                else
                    b -= a;
            return a;
        }

        [Benchmark]
        public int Euclid()
        {

            var a = this.random.Next(this.Size) + 1;
            var b = this.random.Next(this.Size) + 1;

            while (b != 0)
            {
                var a2 = a;
                a = b;
                b = a2 % b;
            }
            return a;
        }

        [Benchmark]
        public int Euclid2()
        {

            var a = this.random.Next(this.Size) + 1;
            var b = this.random.Next(this.Size) + 1;

            while (b != 0)
            {
                a %= b;
                if (a == 0)
                    return b;
                b %= a;
            }
            return a;
        }

        [Benchmark]
        public int Binary()
        {
            var u = this.random.Next(this.Size) + 1;
            var v = this.random.Next(this.Size) + 1;
            int shift;


            for (shift = 0; ((u | v) & 1) == 0; ++shift)
            {
                u >>= 1;
                v >>= 1;
            }

            while ((u & 1) == 0)
                u >>= 1;

            do
            {
                while ((v & 1) == 0)
                    v >>= 1;

                if (u > v)
                {
                    var t = v;
                    v = u;
                    u = t;
                }

                v -= u;
            } while (v != 0);

            return u << shift;
        }

        //[Benchmark]
        //public int BinaryGcd()
        //{

        //    var a = this.random.Next(100) + 1;
        //    var b = this.random.Next(100) + 1;

        //    var d = 0;

        //    for (; ((a | b) & 1) == 0; d++)
        //    {
        //        a >>= 1;
        //        b >>= 1;
        //    }

        //}

        [Benchmark]
        public int Shifting()
        {
            var numerator = this.random.Next(this.Size) + 1;
            var denominator = this.random.Next(this.Size) + 1;

            //smear ones to the left
            var a1 = numerator;
            a1 |= a1 << 1;
            a1 |= a1 << 2;
            a1 |= a1 << 4;
            a1 |= a1 << 8;
            a1 |= a1 << 16;
            var b1 = denominator;
            b1 |= b1 << 1;
            b1 |= b1 << 2;
            b1 |= b1 << 4;
            b1 |= b1 << 8;
            b1 |= b1 << 16;

            // flip bits
            a1 = ~a1;
            b1 = ~b1;

            // count set bits
            a1 -= a1 >> 1 & 0x55555555;
            a1 = (a1 & 0x33333333) + (a1 >> 2 & 0x33333333);
            var a2 = (a1 + (a1 >> 4) & 0xF0F0F0F) * 0x1010101 >> 24;

            b1 -= b1 >> 1 & 0x55555555;
            b1 = (b1 & 0x33333333) + (b1 >> 2 & 0x33333333);
            var b2 = (b1 + (b1 >> 4) & 0xF0F0F0F) * 0x1010101 >> 24;


            var d = Math.Min(a2, b2);
            var a = numerator >> a2;
            var b = denominator >> b2;

            while (a != b)
                if ((a & 1) == 0)
                    a >>= 1;
                else if ((b & 1) == 0)
                    b >>= 1;
                else if (a > b)
                    a = a - b >> 1;
                else
                    b = b - a >> 1;

            var gcf = a * (1 << d);

            return gcf;
        }
    }
    [ShortRunJob]

    //[SimpleJob(1, 10, 10, 5000)]
    public class Memory64Tests
    {
        [Params(1, 100, 10000)] public int Size { get; set; }

        [Params(0, 1)] public int Offset { get; set; }

        private byte[] array1;
        private byte[] array2;

        [GlobalSetup]
        public void Setup()
        {
            this.array1 = Enumerable.Range(0, this.Size + 1 << 7).Select(i => (byte)i).ToArray();
            this.array2 = new byte[this.Size + 1 << 7];
        }

        [Benchmark]
        public void Copy1()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (ulong*)(pArray + this.Offset);
                    var pDest = (ulong*)pArray2;
                    for (var i = 0; i < this.Size; i++)
                    {
                        pDest[0] = pSrc[0];
                        pDest[1] = pSrc[1];
                        pDest += 2;
                        pSrc += 2;
                    }
                }
            }
        }

        [Benchmark]
        public void Copy2()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (ulong*)(pArray + this.Offset);
                    var pDest = (ulong*)pArray2;
                    for (var i = 0; i < this.Size << 1;)
                    {
                        pDest[i] = pSrc[i++];
                        pDest[i] = pSrc[i++];
                    }
                }
            }
        }

        [Benchmark]
        public void Copy3()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (ulong*)(pArray + this.Offset);
                    var pDest = (ulong*)pArray2;
                    for (var i = 0; i < this.Size << 1;)
                    {
                        pDest[i] = pSrc[i];
                        pDest[i + 1] = pSrc[i + 1];
                        i += 2;
                    }
                }
            }
        }

        [Benchmark]
        public void Copy4()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (ulong*)(pArray + this.Offset);
                    var pDest = (ulong*)pArray2;
                    var limit = pSrc + (this.Size << 1);
                    while (pSrc < limit)
                    {
                        *pDest++ = *pSrc++;
                        *pDest++ = *pSrc++;
                    }
                }
            }
        }
    }

    //[ShortRunJob]
    [SimpleJob(1, 10, 10, 5000)]
    public class MemoryTests
    {
        [Params(100)]
        public int Size { get; set; }

        [Params(0, 1, 2, 4, 8)]
        public int Offset { get; set; }

        private byte[] array1;
        private byte[] array2;

        [GlobalSetup]
        public void Setup()
        {
            this.array1 = Enumerable.Range(0, this.Size + 1 << 7).Select(i => (byte)i).ToArray();
            this.array2 = new byte[this.Size + 1 << 7];
        }

        [Benchmark]
        public void CopyUnaligned64()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (ulong*)(pArray + this.Offset);
                    var pDest = (ulong*)pArray2;
                    //for (var i = 0; i < this.Size << 1;)
                    //{
                    //    pDest[i] = pSrc[i++];
                    //    pDest[i] = pSrc[i++];
                    //}
                    for (var i = 0; i < this.Size; i++)
                    {
                        pDest[0] = pSrc[0];
                        pDest[1] = pSrc[1];
                        pDest += 2;
                        pSrc += 2;
                    }
                }
            }
        }

        [Benchmark]
        public void CopyUnaligned128()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = (decimal*)(pArray + this.Offset);
                    var pDest = (decimal*)pArray2;
                    for (var i = 0; i < this.Size; i++)
                        pDest[i] = pSrc[i];
                }
            }
        }

        [Benchmark]
        public void CopyAligned64OrBytes()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    if (this.Offset == 0)
                    {
                        var pSrc = (ulong*)pArray;
                        var pDest = (ulong*)pArray2;
                        for (var i = 0; i < this.Size << 1;)
                        {
                            pDest[i] = pSrc[i++];
                            pDest[i] = pSrc[i++];
                        }
                    }
                    else
                    {
                        var pSrc = pArray + this.Offset;
                        var pDest = pArray2;
                        for (var i = 0; i < this.Size; i++)
                        {
                            pDest[0] = pSrc[0];
                            pDest[1] = pSrc[1];
                            pDest[2] = pSrc[2];
                            pDest[3] = pSrc[3];
                            pDest[4] = pSrc[4];
                            pDest[5] = pSrc[5];
                            pDest[6] = pSrc[6];
                            pDest[7] = pSrc[7];
                            pDest[8] = pSrc[8];
                            pDest[9] = pSrc[9];
                            pDest[10] = pSrc[10];
                            pDest[11] = pSrc[11];
                            pDest[12] = pSrc[12];
                            pDest[13] = pSrc[13];
                            pDest[14] = pSrc[14];
                            pDest[15] = pSrc[15];
                            pDest += 16;
                            pSrc += 16;
                        }
                    }
            }
        }

        [Benchmark]
        public void CopyAligned128OrBytes()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    if (this.Offset == 0)
                    {
                        var pSrc = (decimal*)pArray;
                        var pDest = (decimal*)pArray2;
                        for (var i = 0; i < this.Size; i++)
                            pDest[i] = pSrc[i];
                    }
                    else
                    {
                        var pSrc = pArray + this.Offset;
                        var pDest = pArray2;
                        for (var i = 0; i < this.Size; i++)
                        {
                            pDest[0] = pSrc[0];
                            pDest[1] = pSrc[1];
                            pDest[2] = pSrc[2];
                            pDest[3] = pSrc[3];
                            pDest[4] = pSrc[4];
                            pDest[5] = pSrc[5];
                            pDest[6] = pSrc[6];
                            pDest[7] = pSrc[7];
                            pDest[8] = pSrc[8];
                            pDest[9] = pSrc[9];
                            pDest[10] = pSrc[10];
                            pDest[11] = pSrc[11];
                            pDest[12] = pSrc[12];
                            pDest[13] = pSrc[13];
                            pDest[14] = pSrc[14];
                            pDest[15] = pSrc[15];
                            pDest += 16;
                            pSrc += 16;
                        }
                    }
            }
        }

        [Benchmark]
        public void CopyBytes()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                {
                    var pSrc = pArray + this.Offset;
                    var pDest = pArray2;
                    for (var i = 0; i < this.Size; i++)
                    {
                        pDest[0] = pSrc[0];
                        pDest[1] = pSrc[1];
                        pDest[2] = pSrc[2];
                        pDest[3] = pSrc[3];
                        pDest[4] = pSrc[4];
                        pDest[5] = pSrc[5];
                        pDest[6] = pSrc[6];
                        pDest[7] = pSrc[7];
                        pDest[8] = pSrc[8];
                        pDest[9] = pSrc[9];
                        pDest[10] = pSrc[10];
                        pDest[11] = pSrc[11];
                        pDest[12] = pSrc[12];
                        pDest[13] = pSrc[13];
                        pDest[14] = pSrc[14];
                        pDest[15] = pSrc[15];
                        pDest += 16;
                        pSrc += 16;
                    }
                }
            }
        }

        [Benchmark]
        public void CopyAligned64()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    switch (this.Offset)
                    {
                        case 1:
                            {
                                var pSrc = pArray + 1;
                                var pDest = pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest[4] = pSrc[4];
                                    pDest[5] = pSrc[5];
                                    pDest[6] = pSrc[6];
                                    pDest[7] = pSrc[7];
                                    pDest[8] = pSrc[8];
                                    pDest[9] = pSrc[9];
                                    pDest[10] = pSrc[10];
                                    pDest[11] = pSrc[11];
                                    pDest[12] = pSrc[12];
                                    pDest[13] = pSrc[13];
                                    pDest[14] = pSrc[14];
                                    pDest[15] = pSrc[15];
                                    pDest += 16;
                                    pSrc += 16;
                                }

                                break;
                            }
                        case 2:
                            {
                                var pSrc = (ushort*)(pArray + 2);
                                var pDest = (ushort*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest[4] = pSrc[4];
                                    pDest[5] = pSrc[5];
                                    pDest[6] = pSrc[6];
                                    pDest[7] = pSrc[7];
                                    pDest += 8;
                                    pSrc += 8;
                                }

                                break;
                            }
                        case 4:
                            {
                                var pSrc = (uint*)(pArray + 4);
                                var pDest = (uint*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest += 4;
                                    pSrc += 4;
                                }

                                break;
                            }
                        case 0:
                        case 8:
                            {
                                var pSrc = (ulong*)(pArray + this.Offset);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest += 2;
                                    pSrc += 2;
                                }

                                break;
                            }
                    }
            }
        }

        [Benchmark]
        public void CopyAligned128()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    switch (this.Offset)
                    {
                        case 0:
                            {
                                var pSrc = (decimal*)pArray;
                                var pDest = (decimal*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                    pDest[i] = pSrc[i];
                                break;
                            }
                        case 1:
                            {
                                var pSrc = pArray + 1;
                                var pDest = pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest[4] = pSrc[4];
                                    pDest[5] = pSrc[5];
                                    pDest[6] = pSrc[6];
                                    pDest[7] = pSrc[7];
                                    pDest[8] = pSrc[8];
                                    pDest[9] = pSrc[9];
                                    pDest[10] = pSrc[10];
                                    pDest[11] = pSrc[11];
                                    pDest[12] = pSrc[12];
                                    pDest[13] = pSrc[13];
                                    pDest[14] = pSrc[14];
                                    pDest[15] = pSrc[15];
                                    pDest += 16;
                                    pSrc += 16;
                                }

                                break;
                            }
                        case 2:
                            {
                                var pSrc = (ushort*)(pArray + 2);
                                var pDest = (ushort*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest[4] = pSrc[4];
                                    pDest[5] = pSrc[5];
                                    pDest[6] = pSrc[6];
                                    pDest[7] = pSrc[7];
                                    pDest += 8;
                                    pSrc += 8;
                                }

                                break;
                            }
                        case 4:
                            {
                                var pSrc = (uint*)(pArray + 4);
                                var pDest = (uint*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest[2] = pSrc[2];
                                    pDest[3] = pSrc[3];
                                    pDest += 4;
                                    pSrc += 4;
                                }

                                break;
                            }
                        case 8:
                            {
                                var pSrc = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    pDest[0] = pSrc[0];
                                    pDest[1] = pSrc[1];
                                    pDest += 2;
                                    pSrc += 2;
                                }

                                break;
                            }
                    }
            }
        }

        [Benchmark]
        public void CopyAligned64Compact()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    switch (this.Offset)
                    {
                        case 1:
                            {
                                var pSrcB = pArray + 1;
                                var pSrcW = (ushort*)(pArray + 2);
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = pSrcB[0]
                                               | (ulong)pSrcW[0] << 8
                                               | pSrcD[0] << 24
                                               | pSrcQ[0] << 56;
                                    *pDest++ = pSrcQ[0] >> 8
                                               | (ulong)pSrcB[15] << 56;
                                    pSrcB += 16;
                                    pSrcW += 8;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 2:
                            {
                                var pSrcW = (ushort*)(pArray + 2);
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = (ulong)pSrcW[0]
                                               | pSrcD[0] << 16
                                               | pSrcQ[0] << 24;
                                    *pDest++ = pSrcQ[0] >> 40
                                               | (ulong)pSrcW[7] << 48;
                                    pSrcW += 8;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 4:
                            {
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = pSrcD[0]
                                               | pSrcQ[0] << 32;
                                    *pDest++ = pSrcQ[0] >> 32
                                               | pSrcD[3] << 32;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 0:
                        case 8:
                            {
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = *pSrcQ++;
                                    *pDest++ = *pSrcQ++;
                                }

                                break;
                            }
                    }
            }
        }

        [Benchmark]
        public void CopyAligned128Compact()
        {
            unsafe
            {

                fixed (byte* pArray = this.array1)
                fixed (byte* pArray2 = this.array2)
                    switch (this.Offset)
                    {
                        case 0:
                            {
                                var pSrc = (decimal*)pArray;
                                var pDest = (decimal*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                    pDest[i] = pSrc[i];
                                break;
                            }
                        case 1:
                            {
                                var pSrcB = pArray + 1;
                                var pSrcW = (ushort*)(pArray + 2);
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = pSrcB[0]
                                               | (ulong)pSrcW[0] << 8
                                               | pSrcD[0] << 24
                                               | pSrcQ[0] << 56;
                                    *pDest++ = pSrcQ[0] >> 8
                                               | (ulong)pSrcB[15] << 56;
                                    pSrcB += 16;
                                    pSrcW += 8;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 2:
                            {
                                var pSrcW = (ushort*)(pArray + 2);
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = (ulong)pSrcW[0]
                                               | pSrcD[0] << 16
                                               | pSrcQ[0] << 24;
                                    *pDest++ = pSrcQ[0] >> 40
                                               | (ulong)pSrcW[7] << 48;
                                    pSrcW += 8;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 4:
                            {
                                var pSrcD = (uint*)(pArray + 4);
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = pSrcD[0]
                                               | pSrcQ[0] << 32;
                                    *pDest++ = pSrcQ[0] >> 32
                                               | pSrcD[3] << 32;
                                    pSrcD += 4;
                                    pSrcQ += 2;
                                }

                                break;
                            }
                        case 8:
                            {
                                var pSrcQ = (ulong*)(pArray + 8);
                                var pDest = (ulong*)pArray2;
                                for (var i = 0; i < this.Size; i++)
                                {
                                    *pDest++ = *pSrcQ++;
                                    *pDest++ = *pSrcQ++;
                                }

                                break;
                            }
                    }
            }
        }
    }
}
