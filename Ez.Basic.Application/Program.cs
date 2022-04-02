using Ez.Basic.Compiler.Lexer;
using Ez.Basic.VirtualMachine;
using System.Text;

Console.WriteLine("Ez.Basic");

var vm = new VM();

var chunk = new Chunk();
//var constant = chunk.AddConstant(new() { Double = 3.4 });

//chunk.Write(Opcode.Constant);
//chunk.WriteVarint(constant);
//chunk.Write(Opcode.Negate);
//chunk.Write(Opcode.Return);

var constant = chunk.AddConstant(1.2);
chunk.Write(Opcode.Constant);
chunk.WriteVarint(constant);

constant = chunk.AddConstant(3.4);
chunk.Write(Opcode.Constant);
chunk.WriteVarint(constant);

chunk.Write(Opcode.Add);

constant = chunk.AddConstant(5.6);
chunk.Write(Opcode.Constant);
chunk.WriteVarint(constant);

chunk.Write(Opcode.Divide);
chunk.Write(Opcode.Negate);

chunk.Write(Opcode.Return);

//var sb = new StringBuilder();
//chunk.DisassembleChunk(sb, "Test Chunk");
//Console.WriteLine(sb);

vm.Interpret(chunk);

Scanner scanner = new(@"
def pow(x, y)
    let tmp = 1
    let p = 0
    while p < y
        tmp = tmp * x
        p = p + 1
    next
    return tmp
end");

Token token;

do
{
    token = scanner.ScanToken();
    Console.WriteLine(token.ToString());
} while (token.Type != TokenType.EoF);