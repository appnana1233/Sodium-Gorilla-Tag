# Sodium
<p align="center">
  <img src="Res/Sodium_Logo.png" alt="Sodium logo" width="300">
</p>

A Gorilla Tag performance mod for BepInEx. It reduces stutter and per-frame CPU work, with optional visual quality settings for extra FPS.

## What it does

- **Stutter fixes:** staggers scoreboard and remote-rig setup across frames, skips forced garbage collection on room leave, and skips timed unused-asset unloads.
- **Per-frame savings:** caches face materials for mouth and eye updates, and limits the rate of purely visual updates.
- **Quality settings:** LOD bias and other optional settings, configurable in the config file.

Every feature can be turned on or off in the config file.

## Credits

Source code by Potassium (niko-tar-gz), some code by Gorilla-Optimizer (sevvy-wevvy), and modified by appnana, 2026.

Thanks to both projects, this mod wouldn't be possible without them. Go check them out:

- Gorilla-Optimizer: https://github.com/sevvy-wevvy/Gorilla-Optimizer/
- Potassium: https://github.com/niko-tar-gz/Potassium

## Install (for regular players)

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) 5 for Gorilla Tag (tested with BepInEx 5.4.23.5).
2. Download `Sodium.zip` from the [Releases page](https://github.com/appnana1233/Sodium-Gorilla-Tag/releases).
3. Extract it into your Gorilla Tag folder so that `Sodium.dll` ends up in `BepInEx/plugins` (a `Sodium` subfolder inside it works too).
4. Launch the game once. The config file is created at `BepInEx/config/com.appnana.sodium.cfg`.

## Install (for the developers)

Build Sodium from source if you want to modify it or check what the DLL contains.

### Requirements

- A recent Visual Studio (2022 or newer) with the **.NET desktop development** workload. The solution uses the newer `.slnx` format.
- Gorilla Tag installed through Steam
- [BepInEx](https://github.com/BepInEx/BepInEx) installed in the game folder (launch the game once so it creates the `BepInEx` folder)

### Build

1. Clone the repo:
   ```
   git clone https://github.com/appnana1233/Sodium-Gorilla-Tag.git
   ```
2. Open `Sodium.slnx` in Visual Studio.
3. **Only if Steam isn't in the default location:** open `Directory.Build.props` and change `GamePath` to your Gorilla Tag folder. The default is:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag
   ```
   All the reference paths (`Gorilla Tag_Data\Managed`, `BepInEx\core`, `BepInEx\plugins`) are built from this one line.
4. Set the configuration to **Release** and build with **Ctrl+Shift+B**.
5. After a successful build, `Sodium.dll` is automatically copied to `BepInEx\plugins\Sodium\` in your game folder. No manual copy is needed.

### Notes

- The game's own DLLs aren't included in this repo, because they belong to their authors. The project reads them from your game install.
- Close Gorilla Tag before building. If the game is running, the DLL is locked and the copy step fails.
- The first launch creates the config file at `BepInEx\config\com.appnana.sodium.cfg`.
- Check `BepInEx\LogOutput.log` after launching. If a patch fails to apply after a game update, the log names it.
- **CI builds:** if the `CI` environment variable is `TRUE`, the project reads references from a `Libs` folder and skips the copy step.
- Pull requests are welcome. By contributing, you agree your changes are released under the GPL-3.0 license.

## Configuration

Open the config file in any text editor. Each setting has a description. Changes apply the next time you launch the game.

## License

GNU General Public License v3.0. See the `LICENSE` file.
