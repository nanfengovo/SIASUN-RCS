using System;
using System.Linq;
using System.Reflection;

class Program {
    static void Main() {
        var path = @"src/06.Hosting/SIASUN.RCS.HttpApi.Host/bin/Debug/net10.0/Swashbuckle.AspNetCore.SwaggerGen.dll";
        var asm = Assembly.LoadFrom(path);
        var type = asm.GetTypes().FirstOrDefault(t => t.Name == "IOperationFilter");
        if (type != null) {
            var method = type.GetMethod("Apply");
            var parameters = method.GetParameters();
            var opParam = parameters[0].ParameterType;
            Console.WriteLine("OpenApiOperation namespace: " + opParam.Namespace);
        }
    }
}
