# croco-dotnet

Command Line Utility to interface with [Croco RP2040 Based GameBoy Game Cartridge](https://github.com/shilga/rp2040-gameboy-cartridge-firmware)


Command checklist:
- [x] Read Serial
- [x] Read Device Info
- [x] Get Rom Utilization
- [x] Get Rom Info
- [x] Delete Rom
- [x] Request Rom Upload
- [x] Upload Rom Chunk
- [ ] Request Save Upload
- [ ] Upload Save Chunk
- [ ] Request Save Download
- [ ] Receive Save Chunk


Example usage of the CLI:

```
> croco

croco v1.0.0+dfcd41ac5c5eea07fe836c70aea0aac6ba62cc42

A command utility to interface with a Croco Cartridge

Usage:
  croco [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  -v, --version   Show version information

Commands:
  info        Retrieve information about the cartridge
  serial      Retrieve serial number of the cartridge
  hw-version  Retrieve the hardware version of the cartridge
  fw-version  Retrieve the firmware version of the cartridge
  rom         ROM commands
```


ROM commands
```
> croco rom

croco v1.0.0+dfcd41ac5c5eea07fe836c70aea0aac6ba62cc42

rom: ROM commands

Usage:
  croco rom [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  utilization  Retrieve details about what is stored on the cartridge and how much space is available.
  list         List ROMs installed on the cartridge
  delete       Deletes a ROM from the cartridge by it's ID. [default: 0]
  upload       Upload a ROM to the cartridge
```
