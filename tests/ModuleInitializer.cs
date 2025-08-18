using System.Runtime.CompilerServices;
using System.Text;
using VerifyTests;
using DiffPlex;
using DiffPlex.DiffBuilder;

namespace Motely.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Configure Verify to use DiffPlex for better diff output
        VerifyDiffPlex.Initialize();
        
        // Optional: Configure Verify settings
        VerifierSettings.TreatAsString<StringBuilder>();
    }
}