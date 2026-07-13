using System.Runtime.CompilerServices;

namespace Ifpa.Tests
{
    internal static class TestModuleInitializer
    {
        // sqlite-net-base does not bundle or initialise a SQLitePCLRaw provider; the app does this
        // once in MauiProgram (SQLitePCL.Batteries_V2.Init()). Do the same for the whole test
        // assembly so every test gets a working native SQLite via SQLitePCLRaw.bundle_e_sqlite3.
        // Init() is idempotent, so calling it here is safe even if a package initialises it too.
        [ModuleInitializer]
        internal static void Init() => SQLitePCL.Batteries_V2.Init();
    }
}
