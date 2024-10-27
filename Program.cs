using System.Runtime.InteropServices;

namespace sdl3;

using SDL;

internal static class Program {
  public static unsafe void Main(string[] args) {
    const int SCREEN_WIDTH = 64 * 10;
    const int SCREEN_HEIGHT = 32 * 10;

    if (args.Length != 3) {
      Console.WriteLine("Usage: sdl3 <scale> <delay> <rom>");
      return;
    }

    var videoScale = args[0];
    var cycleDelay = args[1];
    
    Chip8 chip8 = new();
    chip8.LoadROM(args[2]);
    
    SDL_Window* window;
    SDL_Renderer* renderer;
    SDL_Texture* texture;
    
    if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS)) {
      return;
    }
    
    if (!SDL3.SDL_CreateWindowAndRenderer("Derp", SCREEN_WIDTH, SCREEN_HEIGHT, 0, &window, &renderer)) { }
    
    texture = SDL3.SDL_CreateTexture(renderer, SDL_PixelFormat.SDL_PIXELFORMAT_RGBA8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 64, 32);
    SDL3.SDL_SetTextureScaleMode(texture, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
    
    var pinnedBuffer = GCHandle.Alloc(chip8.Video, GCHandleType.Pinned);
    var bufferPointer = pinnedBuffer.AddrOfPinnedObject();
    
    var done = false;
    var events = new SDL_Event();
    
    while (!done) {
      while (SDL3.SDL_PollEvent(&events)) {
        if (events.Type == SDL_EventType.SDL_EVENT_QUIT) {
          done = true;
        }
        if (events.key.key == SDL_Keycode.SDLK_ESCAPE) {
          done = true;
        }
      }
      chip8.Cycle();

      SDL3.SDL_UpdateTexture(texture, null, bufferPointer, 64 * 4);
      SDL3.SDL_RenderClear(renderer);
      SDL3.SDL_RenderTexture(renderer, texture, null, null);
      SDL3.SDL_RenderPresent(renderer);
    }
    
    pinnedBuffer.Free();
    SDL3.SDL_DestroyRenderer(renderer);
    SDL3.SDL_DestroyWindow(window);
    SDL3.SDL_Quit();
  }
}