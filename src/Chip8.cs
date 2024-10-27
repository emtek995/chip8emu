namespace sdl3;

public class Chip8 {
  private const int VIDEO_WIDTH = 64;
  private const int VIDEO_HEIGHT = 32;
  private const uint START_ADDRESS = 0x200;
  private const uint FONT_START_ADDRESS = 0x50;
  private const uint FONTSET_SIZE = 80;
  
  private readonly byte[] _fontset = [
    0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
    0x20, 0x60, 0x20, 0x20, 0x70, // 1
    0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
    0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
    0x90, 0x90, 0xF0, 0x10, 0x10, // 4
    0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
    0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
    0xF0, 0x10, 0x20, 0x40, 0x40, // 7
    0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
    0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
    0xF0, 0x90, 0xF0, 0x90, 0x90, // A
    0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
    0xF0, 0x80, 0x80, 0x80, 0xF0, // C
    0xE0, 0x90, 0x90, 0x90, 0xE0, // D
    0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
    0xF0, 0x80, 0xF0, 0x80, 0x80  // F
  ];
  
  private readonly Random _random = new();
  private delegate void Chip8Func();
  
  private Chip8Func[] _table = new Chip8Func[0xF + 1];
  private Chip8Func[] _table0 = new Chip8Func[0xE + 1];
  private Chip8Func[] _table8 = new Chip8Func[0xE + 1];
  private Chip8Func[] _tableE = new Chip8Func[0xE + 1];
  private Chip8Func[] _tableF = new Chip8Func[0x65 + 1];
  
  private byte[] _registers = new byte[16];
  private byte[] memory = new byte[4096];
  private ushort index;
  private ushort pc = (ushort)START_ADDRESS;
  private ushort[] stack = new ushort[16];
  private byte sp;
  private byte delayTimer;
  private byte soundTimer;
  private byte[] keypad = new byte[16];
  public uint[] Video = new uint[VIDEO_WIDTH * VIDEO_HEIGHT * 4];

  private ushort opcode;

  public void LoadROM(string filename) {
    var rom = File.ReadAllBytes(filename);
    for (var i = 0; i < rom.Length; i++) {
      memory[START_ADDRESS + i] = rom[i];
    }

    for (var i = 0; i < FONTSET_SIZE; i++) {
      memory[FONT_START_ADDRESS + i] = _fontset[i];
    }
  }

  public Chip8() {
    _table[0x0] = Table0;
    _table[0x1] = OP_1nnn;
    _table[0x2] = OP_2nnn;
    _table[0x3] = OP_3xkk;
    _table[0x4] = OP_4xkk;
    _table[0x5] = OP_5xy0;
    _table[0x6] = OP_6xkk;
    _table[0x7] = OP_7xkk;
    _table[0x8] = Table8;
    _table[0x9] = OP_9xy0;
    _table[0xA] = OP_Annn;
    _table[0xB] = OP_Bnnn;
    _table[0xC] = OP_Cxkk;
    _table[0xD] = OP_Dxyn;
    _table[0xE] = TableE;
    _table[0xF] = TableF;

    _table0[0x0] = OP_00E0;
    _table0[0xE] = OP_00EE;

    _table8[0x0] = OP_8xy0;
    _table8[0x1] = OP_8xy1;
    _table8[0x2] = OP_8xy2;
    _table8[0x3] = OP_8xy3;
    _table8[0x4] = OP_8xy4;
    _table8[0x5] = OP_8xy5;
    _table8[0x6] = OP_8xy6;
    _table8[0x7] = OP_8xy7;
    _table8[0xE] = OP_8xyE;

    _tableE[0x1] = OP_ExA1;
    _tableE[0xE] = OP_Ex9E;

    _tableF[0x07] = OP_Fx07;
    _tableF[0x0A] = OP_Fx0A;
    _tableF[0x15] = OP_Fx15;
    _tableF[0x18] = OP_Fx18;
    _tableF[0x1E] = OP_Fx1E;
    _tableF[0x29] = OP_Fx29;
    _tableF[0x33] = OP_Fx33;
    _tableF[0x55] = OP_Fx55;
    _tableF[0x65] = OP_Fx65;
  }

  private void Table0() {
    _table0[opcode & 0x000F]();
  }

  private void Table8() {
    _table8[opcode & 0x000F]();
  }

  private void TableE() {
    _tableE[opcode & 0x000F]();
  }

  private void TableF() {
    _tableF[opcode & 0x00FF]();
  }

  private void OP_NULL() { }

  public void Cycle() {
    opcode = (ushort)((memory[pc] << 8) | memory[pc + 1]);
    Console.WriteLine($"{opcode:X2}");
    pc += 2;
    
    _table[(opcode & 0xF000) >> 12]();

    if (delayTimer != 0) {
      delayTimer--;
    }

    if (soundTimer != 0) {
      soundTimer--;
    }
  }

  private void OP_00E0() {
    Array.Clear(Video);
  }

  private void OP_00EE() {
    sp--;
    pc = stack[sp];
  }

  private void OP_1nnn() {
    pc = (ushort)(opcode & 0x0FFF);
  }
  
  private void OP_2nnn() {
    stack[sp] = pc;
    sp++;
    pc = (ushort)(opcode & 0x0FFF);
  }

  private void OP_3xkk() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var b = (byte)(opcode & 0x00FF);
    if (_registers[vx] == b) {
      pc += 2;
    }
  }

  private void OP_4xkk() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var b = (byte)(opcode & 0x00FF);
    if (_registers[vx] != b) {
      pc += 2;
    }
  }

  private void OP_5xy0() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    if (_registers[vx] == _registers[vy]) {
      pc += 2;
    }
  }

  private void OP_6xkk() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var b = (byte)(opcode & 0x00FF);
    _registers[vx] = b;
  }

  private void OP_7xkk() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var b = (byte)(opcode & 0x00FF);
    _registers[vx] += b;
  }

  private void OP_8xy0() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    _registers[vx] = _registers[vy];
  }

  private void OP_8xy1() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    _registers[vx] |= _registers[vy];
  }

  private void OP_8xy2() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    _registers[vx] &= _registers[vy];
  }

  private void OP_8xy3() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    _registers[vx] ^= _registers[vy];
  }

  private void OP_8xy4() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    
    var sum = _registers[vx] + _registers[vy];
    if (sum > 255) {
      _registers[0xF] = 1;
    } else {
      _registers[0xF] = 0;
    }
    _registers[vx] = (byte)(sum & 0xFF);
  }

  private void OP_8xy5() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);

    if (_registers[vx] > _registers[vy]) {
      _registers[0xF] = 1;
    } else {
      _registers[0xF] = 0;
    }
    
    _registers[vx] -= _registers[vy];
  }

  private void OP_8xy6() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    _registers[0xF] = (byte)(_registers[vx] & 0x1);
    _registers[vx] >>= 1;
  }

  private void OP_8xy7() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);

    if (_registers[vy] > _registers[vx]) {
      _registers[0xF] = 1;
    } else {
      _registers[0xF] = 0;
    }
    
    _registers[vx] = (byte)(_registers[vy] - _registers[vx]);
  }

  private void OP_8xyE() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    _registers[0xF] = (byte)((_registers[vx] & 0x80) >> 7);
    _registers[vx] <<= 1;
  }

  private void OP_9xy0() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);

    if (_registers[vx] != _registers[vy]) {
      pc += 2;
    }
  }

  private void OP_Annn() {
    index = (ushort)(opcode & 0x0FFF);
  }

  private void OP_Bnnn() {
    pc = (ushort)(_registers[0] + opcode & 0x0FFF);
  }

  private void OP_Cxkk() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var b = (byte)(opcode & 0x00FF);
    
    _registers[vx] = (byte)(_random.Next(256) & b); 
  }

  private void OP_Dxyn() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var vy = (byte)((opcode & 0x00F0) >> 4);
    var height = (byte)(opcode & 0x00F);
    
    var x = _registers[vx] % VIDEO_WIDTH;
    var y = _registers[vy] % VIDEO_HEIGHT;
    
    _registers[0xF] = 0;

    for (var row = 0; row < height; row++) {
      var sprite = memory[index + row];
      for (var col = 0; col < 8; col++) {
        var spritepixel = (byte)(sprite & (0x80 >> col));
        var screenpixel = Video[(y + row) * VIDEO_WIDTH + x + col];

        if (spritepixel != 0) {
          if (screenpixel == 0xFFFFFFFF) {
            _registers[0xF] = 1;
          }
          Video[(y + row) * VIDEO_WIDTH + x + col] = screenpixel ^ 0xFFFFFFFF;
        }
      }
    }
  }

  private void OP_Ex9E() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var key = _registers[vx];
    if (keypad[key] != 0) {
      pc += 2;
    }
  }

  private void OP_ExA1() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var key = _registers[vx];
    if (keypad[key] == 0) {
      pc += 2;
    }
  }

  private void OP_Fx07() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    _registers[vx] = delayTimer;
  }

  private void OP_Fx0A() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    _registers[vx] = 16;
    foreach (var (key, i) in keypad.Select((k, i) => (k, i))) {
      if (key != 0) {
        _registers[vx] = (byte)i;
      }
    }

    if (_registers[vx] == 16) {
      pc -= 2;
    }
  }

  private void OP_Fx15() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    delayTimer = _registers[vx];
  }

  private void OP_Fx18() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    soundTimer = _registers[vx];
  }

  private void OP_Fx1E() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    index += _registers[vx];
  }

  private void OP_Fx29() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var digit = _registers[vx];
    
    index = (ushort)(FONT_START_ADDRESS + 5 * digit);
  }

  private void OP_Fx33() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    var value = _registers[vx];

    memory[index + 2] = (byte)(value % 10);
    value /= 10;
    memory[index + 1] = (byte)(value % 10);
    value /= 10;
    memory[index] = (byte)(value % 10);
  }

  private void OP_Fx55() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    for (var i = 0; i < vx; i++) {
      memory[index + i] = _registers[i];
    }
  }

  private void OP_Fx65() {
    var vx = (byte)((opcode & 0x0F00) >> 8);
    for (var i = 0; i < vx; i++) {
      _registers[i] = memory[index + i];
    }
  }
} 