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
                }).SetMinimumLevel(LogLevel.Debug));

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
    var source = 
  @"def fib(n)
        let a = 1
        let b = 0
        for i = 3 to n step 1
            let c = a + b
            b = a
            a = c
        next
        return a
    end
    sub main()
        for i = 3 to 11
            print fib(i)
            let c = 1
        next
    end";
    var gc = new GC();
    var c = new BasicCompiler(logger);
    var module = c.Compile(source, gc, false);

    var vm = new VM(gc, logger);
    var sb = new StringBuilder();
    module.Disassemble(logger, "Test Module");

    Console.WriteLine("Start interpret");
    vm.Interpret(module, "main");
}