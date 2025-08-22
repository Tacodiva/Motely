using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

/// <summary>
/// 🐝 Welcome to the Buzzworthy Vector Hive! 🐝
/// 
/// This collection of utilities helps manage swarms of enum values in perfect formation,
/// just like bees working together in their hexagonal honeycomb cells. Each vector
/// contains exactly 8 enum values, creating a harmonious colony of data that can
/// buzz through calculations at lightning speed!
/// 
/// Just as bees communicate through dance, our vectors communicate through hardware-
/// accelerated operations, making your code both efficient and sweet as honey! 🍯
/// </summary>


/// <summary>
/// The Queen Bee's royal utilities for orchestrating swarms of enum values! 🐛👑
/// This static hive contains all the essential methods for creating and comparing
/// our precious vector colonies with the efficiency of a well-organized beehive.
/// </summary>
public unsafe static class VectorEnum256
{

    /// <summary>
    /// 🌻 Creates a buzzing swarm where every bee carries the same sweet nectar! 🌻
    /// 
    /// Takes a single enum value and clones it across all 8 cells of our honeycomb vector,
    /// like a queen bee's pheromones spreading throughout the entire hive.
    /// Perfect for when you need uniformity in your colony!
    /// </summary>
    /// <typeparam name="T">The type of nectar (enum) our bees will carry</typeparam>
    /// <param name="value">The sweet enum value to replicate across the hive</param>
    /// <returns>A vector hive filled with identical worker bees, all buzzing in harmony</returns>
    public static VectorEnum256<T> Create<T>(T value) where T : unmanaged, Enum
    {
        // Each bee in our hive gets a copy of the same precious nectar! 🍯
        return new(Vector256.Create(Unsafe.As<T, int>(ref value)));
    }

    /// <summary>
    /// 🎯 The Royal Forager's selective nectar gathering expedition! 🎯
    /// 
    /// Uses a set of indices (like scout bees' directions) to gather specific enum values
    /// from a flower field (array). Each index tells a worker bee exactly which flower
    /// to visit, creating a diverse bouquet of enum values in our vector hive.
    /// It's like having GPS for your bees! 🗺️
    /// </summary>
    /// <typeparam name="T">The variety of nectar our forager bees will collect</typeparam>
    /// <param name="indices">The flight path indices - where each bee should gather nectar</param>
    /// <param name="values">The flower garden array - our source of diverse enum nectars</param>
    /// <returns>A beautifully organized hive with hand-picked enum values from specific locations</returns>
    public static VectorEnum256<T> Create<T>(Vector256<int> indices, T[] values) where T : unmanaged, Enum
    {
        // Maybe TODO Use _mm512_mask_i32gather_epi32 - even more efficient honey gathering! 🚀

        // First, prepare our temporary honeycomb cells on the stack - super speedy! ⚡
        T* vector = stackalloc T[Vector256<int>.Count];

        // Send each worker bee to collect nectar from their assigned flower 🌺
        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            vector[i] = values[indices[i]]; // Bee #i flies to flower indices[i]
        }

        // Carefully arrange our collected nectar into the final honeycomb structure! 🍯
        // Each bee deposits their precious cargo into the perfect formation
        return new(Vector256.Create(
            Unsafe.As<T, int>(ref values[indices[0]]), // Worker bee 0's bounty
            Unsafe.As<T, int>(ref values[indices[1]]), // Worker bee 1's bounty
            Unsafe.As<T, int>(ref values[indices[2]]), // Worker bee 2's bounty
            Unsafe.As<T, int>(ref values[indices[3]]), // Worker bee 3's bounty
            Unsafe.As<T, int>(ref values[indices[4]]), // Worker bee 4's bounty
            Unsafe.As<T, int>(ref values[indices[5]]), // Worker bee 5's bounty
            Unsafe.As<T, int>(ref values[indices[6]]), // Worker bee 6's bounty
            Unsafe.As<T, int>(ref values[indices[7]])  // Worker bee 7's bounty
        ));
    }

    /// <summary>
    /// 🕵️‍♀️ The Hive Inspector's quality control check! 🕵️‍♀️
    /// 
    /// Compares an entire vector colony against a single reference enum value,
    /// like checking if all bees in the hive are carrying the same type of nectar.
    /// Returns a mask showing which worker bees match our golden standard - 
    /// it's quality assurance at the speed of light! ⚡
    /// </summary>
    /// <typeparam name="T">The type of nectar being inspected for consistency</typeparam>
    /// <param name="a">The buzzing vector hive to be inspected</param>
    /// <param name="b">The reference nectar - our quality standard</param>
    /// <returns>A vector mask revealing which bees carry nectar matching our standard</returns>
    public static Vector256<int> Equals<T>(in VectorEnum256<T> a, T b) where T : unmanaged, Enum
    {
        // Compare our entire bee colony against the reference nectar in one swift operation! 🎯
        return Vector256.Equals(a.HardwareVector, Vector256.Create(Unsafe.As<T, int>(ref b)));
    }

    /// <summary>
    /// 🤝 The Great Hive Harmony Check! 🤝
    /// 
    /// Performs a bee-to-bee comparison between two vector colonies, checking if
    /// corresponding worker bees from each hive carry matching nectar types.
    /// It's like synchronized dancing - when two hives move in perfect harmony,
    /// we get a beautiful pattern of matches! Think of it as a square dance
    /// where each bee finds their perfect partner! 💃🕺
    /// </summary>
    /// <typeparam name="T">The variety of nectar these competing hives are carrying</typeparam>
    /// <param name="a">The first buzzing vector colony</param>
    /// <param name="b">The second buzzing vector colony to compare against</param>
    /// <returns>A vector mask showing where the two hives dance in perfect synchronization</returns>
    public static Vector256<int> Equals<T>(in VectorEnum256<T> a, in VectorEnum256<T> b) where T : unmanaged, Enum
    {
        // Let the two hives dance together - hardware acceleration makes it lightning fast! ⚡
        return Vector256.Equals(a.HardwareVector, b.HardwareVector);
    }

}

/// <summary>
/// 🏠 The Perfect Honeycomb Structure! 🏠
/// 
/// Represents a beautifully organized hive containing exactly 8 enum values,
/// arranged in the most efficient hexagonal pattern that nature could design.
/// Each cell in our honeycomb holds precious enum nectar, accessible faster
/// than a bee can beat its wings! This is where the magic happens - 
/// hardware acceleration meets Mother Nature's perfect design! 🌿⚡
/// </summary>
/// <typeparam name="T">The sweet variety of enum nectar stored in each honeycomb cell</typeparam>
public unsafe struct VectorEnum256<T>(Vector256<int> hardwareVector) where T : unmanaged, Enum
{
    /// <summary>The core hardware-accelerated heart of our hive - where the real buzzing happens! 💖</summary>
    public Vector256<int> HardwareVector = hardwareVector;

    /// <summary>
    /// 👑 The Queen Bee's Royal Decree - Hive Construction Standards! 👑
    /// 
    /// Before any worker bee can join our colony, they must pass the sacred size test!
    /// Just like how all bees in a hive must be compatible, all enum types must be
    /// exactly 4 bytes to fit perfectly in our hexagonal memory cells. 
    /// No exceptions - even royalty must follow the laws of the hive! 📏✨
    /// </summary>
    static VectorEnum256()
    {
        // Quality control: Only perfectly sized enum bees allowed in this hive! 🔍
        if (sizeof(T) != 4) throw new ArgumentException($"Size of {nameof(T)} must be 4 bytes.");
    }

    /// <summary>
    /// 🔍 The Honeycomb Cell Inspector - Direct Access to Our Precious Nectar! 🔍
    /// 
    /// Want to peek into a specific cell of our honeycomb? This indexer lets you
    /// examine the enum nectar stored in any of the 8 hexagonal cells, faster than
    /// a bee can say "bzzzz"! It's like having a tiny periscope to look into each
    /// individual storage cell of our perfectly organized hive. 🍯🔬
    /// </summary>
    /// <param name="i">The cell number (0-7) - which hexagon do you want to inspect?</param>
    /// <returns>The delicious enum nectar stored in that particular honeycomb cell</returns>
    public readonly T this[int i]
    {
        get
        {
            // Extract the sweet nectar from the specified honeycomb cell! 🍯
            int value = HardwareVector[i];
            return Unsafe.As<int, T>(ref value); // Transform raw honey back to enum goodness!
        }
    }

    /// <summary>
    /// 📝 The Hive's Beautiful Display Case! 📝
    /// 
    /// Creates a gorgeous string representation of our honeycomb, showing off all
    /// 8 precious enum values in their natural hexagonal formation. It's like
    /// putting our hive on display at the county fair - everyone can admire the
    /// perfect organization and sweet contents! Perfect for debugging or just
    /// showing off your well-organized bee colony! 🏆✨
    /// </summary>
    /// <returns>A delightful string showcasing all the enum nectar in our honeycomb cells</returns>
    public override string ToString()
    {
        // Display our beautiful hive in all its hexagonal glory! 🌟
        // Each cell shows off its precious enum nectar for all to admire! 
        return $"<{this[0]}, {this[1]}, {this[2]}, {this[3]}, {this[4]}, {this[5]}, {this[6]}, {this[7]}>";
    }
}