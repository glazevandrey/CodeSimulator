using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp;
using Microsoft.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;


namespace CodeSimulator.Controllers
{
    public class RoslynCompiler
    {
        readonly CSharpCompilation _compilation;
        Assembly _generatedAssembly;
        Type? _proxyType;
        string _assemblyName;
        string _typeName;

        public RoslynCompiler(string typeName, string code, Type[] typesToReference)
        {
            _typeName = typeName;
            var refs = typesToReference.Select(h => MetadataReference.CreateFromFile(h.Assembly.Location) as MetadataReference).ToList();

            //some default refeerences
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")));
            refs.Add(MetadataReference.CreateFromFile(typeof(Object).Assembly.Location));

            //generate syntax tree from code and config compilation options
            var syntax = CSharpSyntaxTree.ParseText(code);
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                optimizationLevel: OptimizationLevel.Release);

            _compilation = CSharpCompilation.Create(_assemblyName = Guid.NewGuid().ToString(), new List<SyntaxTree> { syntax }, refs, options);
        }

        public Type Compile()
        {

            if (_proxyType != null) return _proxyType;

            using (var ms = new MemoryStream())
            {
                var result = _compilation.Emit(ms);
                if (!result.Success)
                {
                    var compilationErrors = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error)
                        .ToList();
                    if (compilationErrors.Any())
                    {
                        var firstError = compilationErrors.First();
                        var errorNumber = firstError.Id;
                        var errorDescription = firstError.GetMessage();
                        var firstErrorMessage = $"{errorNumber}: {errorDescription};";
                        var exception = new Exception($"Compilation failed, first error is: {firstErrorMessage}");
                        compilationErrors.ForEach(e => { if (!exception.Data.Contains(e.Id)) exception.Data.Add(e.Id, e.GetMessage()); });
                        throw exception;
                    }
                }
                ms.Seek(0, SeekOrigin.Begin);

                _generatedAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                _proxyType = _generatedAssembly.GetType(_typeName);
                return _proxyType;
            }
        }
    }

    public class DynamicDelegateCacheExample
    {
        delegate void methodNoParams();
        delegate void methodWithParamas(string message);
        private static methodNoParams cachedDelegate;
        private static methodWithParamas cachedDelegateWeithParams;



        public void Main() => cachedDelegate();

        public void Main(string message) => cachedDelegateWeithParams(message);
    }
    public class COODE {
        public string code { get; set; }
        public int issueId { get; set; }
    }
    [ApiController]
    [Route("[controller]")]
    public class ExecuteController : ControllerBase
    {
        string code = @"
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(""Hello, world!"");
                    }
                }";
        string expectedOutput = "Hello, world!"; // Ожидаемый вывод
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        
        static Dictionary<bool, string> ExecuteCode(string code, string expectedOutput, int issueId)
        {
            var res = new Dictionary<bool, string>();
            var typesToReference = new Type[1] { typeof(Console) };
            var refs2 = new[] { typesToReference.Select(h => MetadataReference.CreateFromFile(h.Assembly.Location) as MetadataReference).First(), MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")), MetadataReference.CreateFromFile(typeof(Object).Assembly.Location) };
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: refs2,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    string s = "";
                    foreach (Diagnostic codeIssue in result.Diagnostics)
                    {
                        s += codeIssue + " ";
                    }
                    res.Add(false, s);

                    return res;
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    Type programType = null;
                    MethodInfo mainMethod = null;

                    if (issueId == 1)
                    {
                        programType = assembly.GetType("Lion");
                        mainMethod = programType.GetMethod("Jump", BindingFlags.Static | BindingFlags.Public);
                    }
                    else
                    {
                        programType = assembly.GetType("Program");
                    }
                    
                    

                    using (StringWriter sw = new StringWriter())
                    {
                        Console.SetOut(sw); // Перенаправление вывода консоли
                        mainMethod.Invoke(null, null);
                        string output = sw.ToString().Trim(); // Получение вывода консоли
                        res.Add(true, output);
                        return res;
                    }
                }
            }
        }


        [HttpPost]
        public string Compile([FromBody] COODE codeDto)
        {

            code = codeDto.code;
            string expectedOutput = "Hello, world!"; // Ожидаемый вывод

            var res = ExecuteCode(code, expectedOutput, codeDto.issueId);

            var model = new CompileResult();
            model.IssueId = 1;
            model.UserId = 1;

            if (res.ContainsKey(false))
            {
                model.Successed = false;

                foreach (var item in res)
                {
                    model.Result += item.Value;
                }
            }
            else
            {
                model.Successed = true;

                model.Result = res[true];
            }
            var m = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            return m;

        }
    }
    public class CompileResult
    {
        public int IssueId { get; set; }
        public int UserId { get; set; }
        public bool Successed { get; set; }
        public string Result { get; set; }
        // Класс для перенаправления вывода консоли
        public class ConsoleOutput : StringWriter
        {
            private static ConsoleOutput instance;

            public ConsoleOutput() { }

            public static ConsoleOutput Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new ConsoleOutput();
                    }
                    return instance;
                }
            }

            public static string GetOutput()
            {
                return Instance.ToString();
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(value);
            }
        }
    }
}
