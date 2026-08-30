using System;
using System.Reflection;
using System.Linq;

class Program {
    static void Main() {
        var path = System.IO.Path.GetFullPath(@"../src/05.Infrastructure/SIASUN.RCS.EntityFrameworkCore/bin/Debug/net10.0/SIASUN.RCS.EntityFrameworkCore.dll");
        var assembly = Assembly.LoadFrom(path);
        var types = assembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.Contains("Migrations"));
        foreach (var t in types) {
            var hasExclude = t.GetCustomAttributesData().Any(a => a.AttributeType.Name == "ExcludeFromCodeCoverageAttribute");
            Console.WriteLine($"{t.Name}: HasExcludeFromCodeCoverage={hasExclude}");
        }
    }
}
