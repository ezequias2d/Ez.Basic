using Ez.Basic;
using Ez.Basic.Compiler.Lexer;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System.Text;
using GC = Ez.Basic.VirtualMachine.GC;

Console.WriteLine("Ez.Basic");

using ILoggerFactory loggerFactory =
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();


//{
//    var vm = new VM(logger);
//    var chunk = new Chunk();
//    //var constant = chunk.AddConstant(new() { Double = 3.4 });

//    //chunk.Write(Opcode.Constant);
//    //chunk.WriteVarint(constant);
//    //chunk.Write(Opcode.Negate);
//    //chunk.Write(Opcode.Return);

//    var constant = chunk.AddNumericConstant(1.2);
//    chunk.Write(Opcode.NumericConstant);
//    chunk.WriteVarint(constant);

//    constant = chunk.AddNumericConstant(3.4);
//    chunk.Write(Opcode.NumericConstant);
//    chunk.WriteVarint(constant);

//    chunk.Write(Opcode.Add);

//    constant = chunk.AddNumericConstant(5.6);
//    chunk.Write(Opcode.NumericConstant);
//    chunk.WriteVarint(constant);

//    chunk.Write(Opcode.Divide);
//    chunk.Write(Opcode.Negate);

//    chunk.Write(Opcode.Return);

//    //var sb = new StringBuilder();
//    //chunk.DisassembleChunk(sb, "Test Chunk");
//    //Console.WriteLine(sb);

//    vm.Interpret(chunk);
//}

//{
//    Scanner scanner = new(@"
//    def pow(x, y)
//        let tmp = 1
//        let p = 0
//        while p < y
//            tmp = tmp * x
//            p = p + 1
//        next
//        return tmp
//    end");

//    Token token;

//    do
//    {
//        token = scanner.ScanToken();
//        Console.WriteLine(token.ToString());
//    } while (token.Type != TokenType.EoF);
//}

{
    //var source = @"
    //def pow(x, y)
    //    let tmp = 1
    //    let p = 0
    //    while p < y
    //        tmp = tmp * x
    //        p = p + 1
    //    next
    //    return tmp
    //end";
    var source = @"
    print ""Hello world""";
    var gc = new GC();
    var c = new BasicCompiler(logger);
    var chunk = new Chunk(gc);
    c.Compile(source, chunk);

    var vm = new VM(gc, logger);
    var sb = new StringBuilder();
    chunk.DisassembleChunk(sb, "Test Chunk");
    Console.WriteLine(sb);

    Console.WriteLine("Start interpret");
    vm.Interpret(chunk);
}