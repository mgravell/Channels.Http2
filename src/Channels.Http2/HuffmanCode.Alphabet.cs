namespace Channels.Http2
{
    public partial struct HuffmanCode
    {
        // see: Appendix B
        static readonly uint[] _codes =
        {
0x1ff8
,0x7fffd8
,0xfffffe2
,0xfffffe3
,0xfffffe4
,0xfffffe5
,0xfffffe6
,0xfffffe7
,0xfffffe8
,0xffffea
,0x3ffffffc
,0xfffffe9
,0xfffffea
,0x3ffffffd
,0xfffffeb
,0xfffffec
,0xfffffed
,0xfffffee
,0xfffffef
,0xffffff0
,0xffffff1
,0xffffff2
,0x3ffffffe
,0xffffff3
,0xffffff4
,0xffffff5
,0xffffff6
,0xffffff7
,0xffffff8
,0xffffff9
,0xffffffa
,0xffffffb
,0x14
,0x3f8
,0x3f9
,0xffa
,0x1ff9
,0x15
,0xf8
,0x7fa
,0x3fa
,0x3fb
,0xf9
,0x7fb
,0xfa
,0x16
,0x17
,0x18
,0x0
,0x1
,0x2
,0x19
,0x1a
,0x1b
,0x1c
,0x1d
,0x1e
,0x1f
,0x5c
,0xfb
,0x7ffc
,0x20
,0xffb
,0x3fc
,0x1ffa
,0x21
,0x5d
,0x5e
,0x5f
,0x60
,0x61
,0x62
,0x63
,0x64
,0x65
,0x66
,0x67
,0x68
,0x69
,0x6a
,0x6b
,0x6c
,0x6d
,0x6e
,0x6f
,0x70
,0x71
,0x72
,0xfc
,0x73
,0xfd
,0x1ffb
,0x7fff0
,0x1ffc
,0x3ffc
,0x22
,0x7ffd
,0x3
,0x23
,0x4
,0x24
,0x5
,0x25
,0x26
,0x27
,0x6
,0x74
,0x75
,0x28
,0x29
,0x2a
,0x7
,0x2b
,0x76
,0x2c
,0x8
,0x9
,0x2d
,0x77
,0x78
,0x79
,0x7a
,0x7b
,0x7ffe
,0x7fc
,0x3ffd
,0x1ffd
,0xffffffc
,0xfffe6
,0x3fffd2
,0xfffe7
,0xfffe8
,0x3fffd3
,0x3fffd4
,0x3fffd5
,0x7fffd9
,0x3fffd6
,0x7fffda
,0x7fffdb
,0x7fffdc
,0x7fffdd
,0x7fffde
,0xffffeb
,0x7fffdf
,0xffffec
,0xffffed
,0x3fffd7
,0x7fffe0
,0xffffee
,0x7fffe1
,0x7fffe2
,0x7fffe3
,0x7fffe4
,0x1fffdc
,0x3fffd8
,0x7fffe5
,0x3fffd9
,0x7fffe6
,0x7fffe7
,0xffffef
,0x3fffda
,0x1fffdd
,0xfffe9
,0x3fffdb
,0x3fffdc
,0x7fffe8
,0x7fffe9
,0x1fffde
,0x7fffea
,0x3fffdd
,0x3fffde
,0xfffff0
,0x1fffdf
,0x3fffdf
,0x7fffeb
,0x7fffec
,0x1fffe0
,0x1fffe1
,0x3fffe0
,0x1fffe2
,0x7fffed
,0x3fffe1
,0x7fffee
,0x7fffef
,0xfffea
,0x3fffe2
,0x3fffe3
,0x3fffe4
,0x7ffff0
,0x3fffe5
,0x3fffe6
,0x7ffff1
,0x3ffffe0
,0x3ffffe1
,0xfffeb
,0x7fff1
,0x3fffe7
,0x7ffff2
,0x3fffe8
,0x1ffffec
,0x3ffffe2
,0x3ffffe3
,0x3ffffe4
,0x7ffffde
,0x7ffffdf
,0x3ffffe5
,0xfffff1
,0x1ffffed
,0x7fff2
,0x1fffe3
,0x3ffffe6
,0x7ffffe0
,0x7ffffe1
,0x3ffffe7
,0x7ffffe2
,0xfffff2
,0x1fffe4
,0x1fffe5
,0x3ffffe8
,0x3ffffe9
,0xffffffd
,0x7ffffe3
,0x7ffffe4
,0x7ffffe5
,0xfffec
,0xfffff3
,0xfffed
,0x1fffe6
,0x3fffe9
,0x1fffe7
,0x1fffe8
,0x7ffff3
,0x3fffea
,0x3fffeb
,0x1ffffee
,0x1ffffef
,0xfffff4
,0xfffff5
,0x3ffffea
,0x7ffff4
,0x3ffffeb
,0x7ffffe6
,0x3ffffec
,0x3ffffed
,0x7ffffe7
,0x7ffffe8
,0x7ffffe9
,0x7ffffea
,0x7ffffeb
,0xffffffe
,0x7ffffec
,0x7ffffed
,0x7ffffee
,0x7ffffef
,0x7fffff0
,0x3ffffee
,0x3fffffff
        };

        static readonly int[] _codeLengths =
        {
            13,
            23,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            24,
            30,
            28,
            28,
            30,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            30,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
             6,
            10,
            10,
            12,
            13,
             6,
             8,
            11,
            10,
            10,
             8,
            11,
             8,
             6,
             6,
             6,
             5,
             5,
             5,
             6,
             6,
             6,
             6,
             6,
             6,
             6,
             7,
             8,
            15,
             6,
            12,
            10,
            13,
             6,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             7,
             8,
             7,
             8,
            13,
            19,
            13,
            14,
             6,
            15,
             5,
             6,
             5,
             6,
             5,
             6,
             6,
             6,
             5,
             7,
             7,
             6,
             6,
             6,
             5,
             6,
             7,
             6,
             5,
             5,
             6,
             7,
             7,
             7,
             7,
             7,
            15,
            11,
            14,
            13,
            28,
            20,
            22,
            20,
            20,
            22,
            22,
            22,
            23,
            22,
            23,
            23,
            23,
            23,
            23,
            24,
            23,
            24,
            24,
            22,
            23,
            24,
            23,
            23,
            23,
            23,
            21,
            22,
            23,
            22,
            23,
            23,
            24,
            22,
            21,
            20,
            22,
            22,
            23,
            23,
            21,
            23,
            22,
            22,
            24,
            21,
            22,
            23,
            23,
            21,
            21,
            22,
            21,
            23,
            22,
            23,
            23,
            20,
            22,
            22,
            22,
            23,
            22,
            22,
            23,
            26,
            26,
            20,
            19,
            22,
            23,
            22,
            25,
            26,
            26,
            26,
            27,
            27,
            26,
            24,
            25,
            19,
            21,
            26,
            27,
            27,
            26,
            27,
            24,
            21,
            21,
            26,
            26,
            28,
            27,
            27,
            27,
            20,
            24,
            20,
            21,
            22,
            21,
            21,
            23,
            22,
            22,
            25,
            25,
            24,
            24,
            26,
            23,
            26,
            27,
            26,
            26,
            27,
            27,
            27,
            27,
            27,
            28,
            27,
            27,
            27,
            27,
            27,
            26,
            30,

        };

        // What is this? k; start with the first 5 bits, because all the codes require at least  5
        // bits; each set of numbers is a False/True pair. So jump to the first 0-31 via the 5 bits;
        // if the numbers are identical and non-positive, you have a 5-bit leaf, so: negate and return.
        // otherwise, keep reading additional bits; for a 0 take the left half of the pair; for 1, the right.
        // if the value is non-positive, you have a leaf; negate and return. Otherwise, the value is the index of the
        // next **pair**, so double it, use it as your position, and keep going.

        // how was it generated? see the commented out garbage
        static readonly short[] _linearizedTree = {
            -48, -48, -49, -49, -50, -50, -97, -97, -99, -99, -101, -101, -105, -105, -111, -111,
            -115, -115, -116, -116, -32, -37, -45, -46, -47, -51, -52, -53, -54, -55, -56, -57,
            -61, -65, -95, -98, -100, -102, -103, -104, -108, -109, -110, -112, -114, -117, 58, 59,
            60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 77,
            -32, -32, -37, -37, -45, -45, -46, -46, -47, -47, -51, -51, -52, -52, -53, -53,
            -54, -54, -55, -55, -56, -56, -57, -57, -61, -61, -65, -65, -95, -95, -98, -98,
            -100, -100, -102, -102, -103, -103, -104, -104, -108, -108, -109, -109, -110, -110, -112, -112,
            -114, -114, -117, -117, -58, -66, -67, -68, -69, -70, -71, -72, -73, -74, -75, -76,
            -77, -78, -79, -80, -81, -82, -83, -84, -85, -86, -87, -89, -106, -107, -113, -118,
            -119, -120, -121, -122, 76, 75, -44, -59, -38, -42, 260, 78, 257, 79, 255, 80,
            253, 81, 250, 82, 249, 83, 248, 84, 247, 85, -123, 86, 233, 87, 211, 88,
            192, 89, 177, 90, 167, 91, 158, 92, 142, 93, 127, 94, 113, 95, 106, 96,
            103, 97, 102, 98, -249, 99, 101, 100, -22, -256, -10, -13, -127, -220, 105, 104,
            -30, -31, -28, -29, 110, 107, 109, 108, -26, -27, -24, -25, 112, 111, -21, -23,
            -19, -20, 121, 114, 118, 115, 117, 116, -17, -18, -15, -16, 120, 119, -12, -14,
            -8, -11, 125, 122, 124, 123, -6, -7, -4, -5, -254, 126, -2, -3, 135, 128,
            132, 129, 131, 130, -252, -253, -250, -251, 134, 133, -247, -248, -245, -246, 139, 136,
            138, 137, -241, -244, -222, -223, 141, 140, -214, -221, -211, -212, 151, 143, 148, 144,
            147, 145, -255, 146, -203, -204, -242, -243, 150, 149, -238, -240, -218, -219, 155, 152,
            154, 153, -210, -213, -202, -205, 157, 156, -200, -201, -192, -193, 164, 159, 163, 160,
            162, 161, -234, -235, -199, -207, -236, -237, 166, 165, -215, -225, -171, -206, 174, 168,
            172, 169, 171, 170, -148, -159, -144, -145, -239, 173, -9, -142, 176, 175, -197, -231,
            -188, -191, 185, 178, 182, 179, 181, 180, -182, -183, -175, -180, 184, 183, -168, -174,
            -165, -166, 189, 186, 188, 187, -157, -158, -152, -155, 191, 190, -150, -151, -147, -149,
            204, 193, 201, 194, 198, 195, 197, 196, -141, -143, -139, -140, 200, 199, -137, -138,
            -1, -135, 203, 202, -232, -233, -198, -228, 208, 205, 207, 206, -190, -196, -187, -189,
            210, 209, -185, -186, -178, -181, 226, 212, 220, 213, 217, 214, 216, 215, -170, -173,
            -164, -169, 219, 218, -160, -163, -154, -156, 224, 221, 223, 222, -136, -146, -133, -134,
            -230, 225, -129, -132, 230, 227, 229, 228, -227, -229, -216, -217, 232, 231, -179, -209,
            -176, -177, 243, 234, 240, 235, 239, 236, 238, 237, -167, -172, -153, -161, -224, -226,
            242, 241, -184, -194, -131, -162, 246, 244, -208, 245, -128, -130, -92, -195, -60, -96,
            -94, -125, -93, -126, 252, 251, -64, -91, 0, -36, -124, 254, -35, -62, -63, 256,
            -39, -43, 259, 258, -40, -41, -33, -34, -88, -90,
        };

        //static readonly HuffmanNode _root;
        //static HuffmanCode()
        //{
        //    var root = new HuffmanNode();
        //    List<HuffmanNode> allNodes = new List<HuffmanNode>();
        //    Func<HuffmanNode, HuffmanNode> createNode = parent =>
        //    {
        //        var child = new HuffmanNode { Depth = parent.Depth + 1 };
        //        allNodes.Add(child);
        //        return child;
        //    };
        //    for (int i = 0; i < _codes.Length; i++)
        //    {
        //        var code = _codes[i];
        //        int bit = 1 << ((int)_codeLengths[i] - 1);
        //        var node = root;
        //        while (bit != 0)
        //        {
        //            if ((code & bit) == 0)
        //            {
        //                node = node.False ?? (node.False = createNode(node));
        //            }
        //            else
        //            {
        //                node = node.True ?? (node.True = createNode(node));
        //            }
        //            bit >>= 1;
        //        }
        //        node.Value = i;
        //    }

        //    // check they make sense and set the ids so that
        //    // they are always incremental as you navigate downwards
        //    var stack = new Stack<string>();
        //    int nextId = 1;
        //    Dive(root, stack, ref nextId);

        //    allNodes.Sort((x, y) =>
        //    {
        //        int delta = x.Depth.CompareTo(y.Depth);
        //        if (delta == 0) delta = x.Id.CompareTo(y.Depth);
        //        return delta;
        //    });

        //    // Depth analysis: 10x5, 4x30
        //    int minLeafDepth = allNodes.Where(x => x.IsLeaf).Min(x => x.Depth),
        //        maxLeafDepth = allNodes.Where(x => x.IsLeaf).Max(x => x.Depth),
        //        minLeafCount = allNodes.Count(x => x.IsLeaf && x.Depth == minLeafDepth),
        //        maxLeafCount = allNodes.Count(x => x.IsLeaf && x.Depth == maxLeafDepth);
        //    Console.WriteLine($"Depth analysis: {minLeafCount}x{minLeafDepth}, {maxLeafCount}x{maxLeafDepth}");

        //    // min count = 5, so we'll start by short-cutting the first 32 nodes in order
        //    int nextArrayIndex = 32;
        //    for (int i = 0; i < 32; i++)
        //    {
        //        var node = GetNode(root, i, 0x10);
        //        node.ArrayIndex = i;
        //        SetArrayIndexing(node.False, ref nextArrayIndex);
        //        SetArrayIndexing(node.True, ref nextArrayIndex);
        //    }

        //    short[] linearized = new short[nextArrayIndex * 2];

        //    foreach (var node in allNodes)
        //    {
        //        int index = 2 * node.ArrayIndex;
        //        if (index >= 0)
        //        {
        //            if (node.IsLeaf)
        //            {
        //                var val = (short)-node.Value;
        //                linearized[index] = linearized[index + 1] = val;
        //            }
        //            else
        //            {
        //                if (node.False.IsLeaf)
        //                {
        //                    linearized[index] = (short)-node.False.Value;
        //                }
        //                else
        //                {
        //                    linearized[index] = (short)node.False.ArrayIndex;
        //                }
        //                if (node.True.IsLeaf)
        //                {
        //                    linearized[index + 1] = (short)-node.True.Value;
        //                }
        //                else
        //                {
        //                    linearized[index + 1] = (short)node.True.ArrayIndex;
        //                }
        //            }
        //        }
        //    }

        //    var sb = new StringBuilder();
        //    for(int i = 0; i < linearized.Length;i++)
        //    {
        //        if (i != 0 && (i % 16) == 0) sb.AppendLine();
        //        sb.Append(linearized[i]).Append(", ");
        //    }
        //    Console.WriteLine(sb.ToString());

        //    var outputs = new string[257];
        //    for(int i = 0; i < 32; i++)
        //    {
        //        string prefix = Convert.ToString(i, 2).PadLeft(5, '0');
        //        WriteTree(prefix, i, outputs);
        //    }
        //    for(int i = 0; i < outputs.Length; i++)
        //    {
        //        Console.WriteLine($"[{i}] ({outputs[i]?.Length??-1}): {outputs[i] ?? "(nil)"}");
        //    }

        //    _root = root;
        //}

        //private static void WriteTree(string prefix, int index, string[] outputs)
        //{
        //    index <<= 1;
        //    int left = _linearizedTree[index], right = _linearizedTree[index + 1];
        //    if(left == right)
        //    {
        //        outputs[(-left)] = prefix;
        //    }
        //    else
        //    {
        //        if(left <= 0)
        //        {
        //            outputs[-left] = prefix + "0";
        //        }
        //        else
        //        {
        //            WriteTree(prefix + "0", left, outputs);
        //        }
        //        if (right <= 0)
        //        {
        //            outputs[-right] = prefix + "1";
        //        }
        //        else
        //        {
        //            WriteTree(prefix + "1", right, outputs);
        //        }
        //    }
        //}

        //private static void SetArrayIndexing(HuffmanNode node, ref int nextArrayIndex)
        //{
        //    if (node != null)
        //    {
        //        node.ArrayIndex = nextArrayIndex++;
        //        if (node.True != null && !node.True.IsLeaf)
        //        {
        //            SetArrayIndexing(node.True, ref nextArrayIndex);
        //        }
        //        if (node.False != null && !node.False.IsLeaf)
        //        {
        //            SetArrayIndexing(node.False, ref nextArrayIndex);
        //        }
        //    }
        //}

        //private static HuffmanNode GetNode(HuffmanNode node, int data, int mask)
        //{
        //    while (mask != 0 && node != null)
        //    {
        //        node = ((data & mask) == 0) ? node.False : node.True;
        //        mask >>= 1;
        //    }
        //    return node;
        //}

        //private static void Dive(HuffmanNode node, Stack<string> stack, ref int nextId)
        //{
        //    // either both or neither nodes must be set
        //    if ((node.False == null) != (node.True == null))
        //    {
        //        throw new InvalidOperationException($"The huffman tree for {string.Concat(stack)} is invalid; mismatched sub-tree (Value={node.Value})");
        //    }
        //    else if (node.False == null && node.Value < 0)
        //    {
        //        throw new InvalidOperationException($"The huffman tree for {string.Concat(stack)} is invalid; missing value");
        //    }
        //    node.Id = nextId++;
        //    if (node.True != null)
        //    {
        //        stack.Push("1");
        //        Dive(node.True, stack, ref nextId);
        //        stack.Pop();
        //    }
        //    if (node.False != null)
        //    {
        //        stack.Push("0");
        //        Dive(node.False, stack, ref nextId);
        //        stack.Pop();
        //    }
        //}
        //
        //class HuffmanNode
        //{
        //    public HuffmanNode True, False;
        //    public int Value = -1;

        //    public bool IsLeaf => True == null;

        //    public int Depth, Id, ArrayIndex = -1;

        //    public override string ToString()
        //    {
        //        return IsLeaf ? $"[{Id}] ({Depth}) = {Value}" : $"[{Id}] ({Depth}) {False?.Id}/{True?.Id}";
        //    }
        //}
    }
}
