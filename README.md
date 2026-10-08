# Echo Shift

A short, single-player puzzle game about cooperating with your past self. Built for desktop browsers with Unity **6000.3.10f1**, C#, the Input System, uGUI and TextMeshPro.

## Play and build

Run `Build-Browser.cmd` first to resolve the template packages for Unity 6000.3: Input System 1.18.0 is installed and unused Unity Version Control is removed. In PowerShell, use `& "D:/UNITY/unity project/EchoShift/Build-Browser.cmd"` (the quotes are required). The Editor-only package repair DLL is built from `Tools/PackageRepair.cs`; `Tools/Compile-PackageRepair.ps1` regenerates it with the installed Editor's compiler. Package repair uses Unity's Package Manager API and logs to `BuildReports/packages.log`.

1. Open this folder with Unity 6000.3.10f1. The first successful import creates the game scene and font atlases automatically.
2. Open `Assets/EchoShift/Scenes/EchoShift.unity` and press Play.
3. Choose **Echo Shift > 3 - Build browser version**, or close the Editor and run **Build-Browser.cmd** under your normal Windows account.
4. After a successful build, run **Play-Browser.cmd**. It uses the installed Node.js runtime and serves only on `127.0.0.1:8765`.

The exported game is in **Builds/WebGL**. Opening `index.html` directly with a `file:` URL is unsupported; serve the folder over HTTP or HTTPS.

## Controls

| Action | Key |
| --- | --- |
| Move | WASD or arrow keys |
| Start recording / release an echo | E |
| Cancel recording / remove latest echo | X |
| Restart chamber | R |
| Pause | Escape |
| Chamber hint | H |

Record up to five seconds. Releasing a recording returns you to its starting point and creates an echo. The echo retraces the recorded route and then holds its final position until removed. Use echoes to hold amber pressure plates while you cross their corresponding doors. Ghosts replay their saved positions regardless of later changes to doors. A closing door stays open while the player occupies it.

Memory fragments are optional. Chamber unlocks and sound preference are saved locally in the browser. The game is designed for a keyboard and a landscape desktop browser.

## Three chambers

- **First Contact:** one echo holds one plate.
- **Chain Reaction:** two echoes cooperate across two sequential doors.
- **In Good Company:** two plates must be held simultaneously.

Each chamber has an in-game hint, three optional memories, a restart action and a completion screen. There are no accounts, purchases or online services.

## Structure

- `EchoSimulation.cs`: rendering-independent simulation, movement/collision, recordings, pressure plates and room data.
- `EchoWorld.cs`: procedural floor panels, walls, characters, traces, echoes and portals.
- `EchoUI.cs`: menus, instructions, chamber selection, HUD, pause, hints and results.
- `EchoGame.cs`: Unity lifecycle, input, simulation updates and progress saves.
- `EchoAudio.cs`: original synthesized effects and ambient music.
- `Editor/EchoBuild.cs`: reproducible scene preparation, font generation and Web export.
- `Editor/EchoValidation.cs`: 35 deterministic checks, including complete solutions to all three rooms.
- `Assets/WebGLTemplates/EchoShift`: custom loading page, keyboard focus and fullscreen support.

**Echo Shift > 2 - Validate puzzles** writes `BuildReports/validation.txt`. Browser builds run the same checks before exporting. Generated `.meta` files must accompany assets if you later add source control. No remote repository is configured.

## Publish

For itch.io, zip the **contents** of `Builds/WebGL` so `index.html` is at the archive root. Create an HTML game project, upload the ZIP, and mark it playable in the browser. Use a 16:9 embed such as 960 by 540. Gzip compression with Unity's decompression fallback is configured for static hosts without custom response headers. Test the uploaded build before adding its link to your portfolio.

## Credits and portfolio notes

Game code, geometry, UI design and synthesized audio were created for this project with Codex assistance. Barlow Regular and SemiBold are distributed under the SIL Open Font License; see `Assets/EchoShift/Fonts/OFL.txt`. Unity and its standard packages retain their own licenses.

For your portfolio, describe your actual contributions and customizations accurately. Useful next steps are designing an original fourth chamber, refining the character art, and documenting a playtest-driven change.
