using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NXPresent;

class Program
{
    static int count;
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); count++; }
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Check(Cue.TryWebsite("example.com", out var url) && url == "https://example.com/", "HTTPS normalization");
            foreach (var invalid in new[] { "file:///C:/Windows/system.ini", "javascript://test", "ftp://example.com", "https://user:password@example.com", "" })
                Check(!Cue.TryWebsite(invalid, out _), "Reject unsafe/invalid URL " + invalid);
            Check(Cue.FromFile("example.MP4").Kind == CueKind.Video, "Video classification case insensitive");
            Check(Cue.FromFile("example.jpeg").Kind == CueKind.Image, "Image classification");
            bool rejected = false;
            try { Cue.FromFile("example.exe"); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Reject executable import");
            var folder = Path.Combine(AppContext.BaseDirectory, "runs", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            App.DataDirectory = folder;
            var path = Path.Combine(folder, "show.nxshow");
            var expected = new List<Cue> { Cue.FromFile(Path.Combine(folder, "Medien", "Bühne.png")), new(CueKind.Text, "Gruß", "ÄÖÜ · Bühne"), new(CueKind.Pattern, "Raster", "grid") };
            ShowFile.Save(path, expected);
            Check(ShowFile.Load(path).SequenceEqual(expected), "Show roundtrip with relative paths and Unicode");
            var aux = new List<Cue> { expected[0], Cue.Black, new(CueKind.Capture, "HDMI", "capture-device-id") };
            ShowFile.Save(path, expected, auxSources: aux);
            var routedShow = ShowFile.LoadDocument(path);
            Check(routedShow.Version == 4 && routedShow.AuxSources!.SequenceEqual(aux), "Show roundtrip preserves AUX file paths and capture device identity");
            rejected = false;
            try { ShowFile.Save(path, expected, auxSources: Enumerable.Repeat(Cue.Black, 11).ToArray()); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && ShowFile.LoadDocument(path).AuxSources!.SequenceEqual(aux), "Invalid AUX routing does not overwrite an existing show");
            File.WriteAllText(path, "{\"Version\":99,\"Cues\":[]}");
            rejected = false; try { ShowFile.Load(path); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Reject unknown show version");
            foreach (var pattern in new[] { "bars", "grid", "gray", "checker", "white", "red", "green", "blue", "black", "logo" })
            {
                var view = new PatternView { Pattern = pattern, Animate = false };
                view.Measure(new Size(1920, 1080)); view.Arrange(new Rect(0, 0, 1920, 1080));
                var bitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(view);
                byte[] pixels = new byte[1920 * 1080 * 4]; bitmap.CopyPixels(pixels, 1920 * 4, 0);
                if (pattern == "bars")
                {
                    Check(pixels[0] == 255 && pixels[1] == 255 && pixels[2] == 255, "Farbbalken white first bar");
                    int index = (int)(1920 * 6.5 / 7) * 4;
                    Check(pixels[index] == 255 && pixels[index + 1] == 0 && pixels[index + 2] == 0, "Farbbalken blue last bar");
                }
                if (pattern == "black") Check(pixels.Where((_, i) => i % 4 != 3).All(x => x == 0), "Black pattern is black");
                Check(pixels[3] == 255, "Opaque pattern " + pattern);
            }
            foreach (var pattern in new[] { "bars", "grid", "gray", "checker", "white", "red", "green", "blue", "black", "logo" })
            {
                byte[] first = RenderPattern(pattern, 0, true), later = RenderPattern(pattern, 2.75, true);
                Check(first.Where((b, i) => b != later[i]).Count() > 10, "Visible motion in " + pattern);
                Check(RenderPattern(pattern, 0, false).SequenceEqual(RenderPattern(pattern, 2.75, false)), "Static option freezes " + pattern);
            }
            var safetyBlack = RenderPattern("blackout", 5, true);
            Check(safetyBlack.Where((_, i) => i % 4 != 3).All(x => x == 0), "Safety blackout remains completely black with animation on");
            byte[] gridA = RenderPattern("grid", 0, true), gridB = RenderPattern("grid", 2.75, true);
            int center = (20 * 640 + 320) * 4;
            Check(gridA.AsSpan(center, 4).SequenceEqual(gridB.AsSpan(center, 4)), "Alignment center reference remains fixed");
            var midnight = new DateTime(2026, 10, 26, 23, 59, 59);
            Check(ClockView.TimeText(midnight.AddSeconds(1)) == "00:00:00", "24-hour clock midnight rollover");
            Check(!RenderElement(new ClockView { CurrentTime = midnight }).SequenceEqual(RenderElement(new ClockView { CurrentTime = midnight.AddSeconds(1) })), "Projected clock renders changing time and date");
            Check(!RenderElement(new ClockView { CurrentTime = midnight.AddMilliseconds(100) }).SequenceEqual(RenderElement(new ClockView { CurrentTime = midnight.AddMilliseconds(600) })), "Clock progress moves smoothly between whole seconds");
            expected.Add(new Cue(CueKind.Clock, "Uhr", "local"));
            var winCue = new Cue(CueKind.WindowCapture, "Notepad", "12345:notepad:Editor");
            expected.Add(winCue);
            var preShowModel = SceneModel.CreatePreShowTemplate();
            var sceneCue = new Cue(CueKind.Scene, "Pre-Show Szene", preShowModel.ToJson());
            expected.Add(sceneCue);
            ShowFile.Save(path, expected);
            Check(ShowFile.Load(path).SequenceEqual(expected), "Clock, WindowCapture and Scene cue show roundtrip preserves existing kinds");
            Check(winCue.Category == "ANWENDUNG", "WindowCapture category is ANWENDUNG");
            Check(sceneCue.Category == "SZENE", "Scene category is SZENE");
            bool winValidateFailed = false;
            try { Cue.Validate(new Cue(CueKind.WindowCapture, "Test", "")); } catch (InvalidDataException) { winValidateFailed = true; }
            Check(winValidateFailed, "WindowCapture without source is rejected");
            bool sceneValidateFailed = false;
            try { Cue.Validate(new Cue(CueKind.Scene, "Test", "")); } catch (InvalidDataException) { sceneValidateFailed = true; }
            Check(sceneValidateFailed, "Scene without source is rejected");
            var sceneModel = SceneModel.FromJson(sceneCue.Source);
            Check(sceneModel != null && sceneModel.Items.Count > 0, "SceneModel parses from PreShow JSON");
            var sceneRoundtrip = sceneModel!.ToJson();
            var sceneModel2 = SceneModel.FromJson(sceneRoundtrip);
            Check(sceneModel2 != null && sceneModel2.Items.Count == sceneModel.Items.Count, "SceneModel serialization roundtrip");
            Check(SceneModel.CreateMinimalSongTemplate().Items.Count > 0, "Minimal music scene template parses");
            Check(SceneModel.CreateLowerThirdTemplate().Items.Count > 0, "Lower third scene template parses");
            Check(SceneModel.CreateCinemaCountdownTemplate().Items.Count > 0, "Cinema countdown scene template parses");
            var videoRunCue = new RunCue(new RunCueData(Guid.NewGuid(), expected[0], InPointMilliseconds: 15000, OutPointMilliseconds: 160000));
            Check(videoRunCue.InPointMilliseconds == 15000 && videoRunCue.OutPointMilliseconds == 160000, "RunCue preserves video in/out trim times");
            var tempVideo = Path.Combine(folder, "sample.mp4");
            File.WriteAllBytes(tempVideo, [0]);
            var videoCue = new Cue(CueKind.Video, "Film", tempVideo);
            var invalidTrimCue = new RunCue(new RunCueData(Guid.NewGuid(), videoCue, InPointMilliseconds: 20000, OutPointMilliseconds: 10000));
            Check(invalidTrimCue.Problem != null && invalidTrimCue.Problem.Contains("In-Point liegt hinter oder auf dem Out-Point"), "RunCue detects invalid in/out trim points (in >= out)");
            invalidTrimCue.OutPointMilliseconds = 30000;
            Check(invalidTrimCue.Problem == null, "RunCue clears trim warning when out > in");
            var aspectBox = new AspectBox();
            aspectBox.Measure(new Size(1920, 1920));
            Check(Math.Abs(aspectBox.DesiredSize.Width - 1920) < 0.1 && Math.Abs(aspectBox.DesiredSize.Height - 1080) < 0.1, "AspectBox enforces 16:9 aspect ratio");
            var sceneCanvas = new SceneCanvasView(SceneModel.CreateCinemaCountdownTemplate()) { IsEditorMode = true };
            Check(sceneCanvas.Model.Items.Count > 0, "SceneCanvasView loads countdown items");
            sceneCanvas.Dispose();
            Check(true, "SceneCanvasView disposes cleanly");
            Check(Cue.ShowRunner.Kind == CueKind.CueShow && Cue.ShowRunner.Category == "CUE-SHOW", "Cue.ShowRunner category is CUE-SHOW");
            var openWindows = WindowCaptureFeed.FindWindows();
            Check(openWindows != null, "WindowCaptureFeed.FindWindows returns valid list");
            var factory = new FeedFactory(null, Path.Combine(folder, "web"));
            using (var feed = factory.Create(winCue))
            {
                Check(feed is WindowCaptureFeed, "FeedFactory creates WindowCaptureFeed for WindowCapture cue");
            }
            using (var feed = factory.Create(sceneCue))
            {
                Check(feed is SceneFeed, "FeedFactory creates SceneFeed for Scene cue");
            }
            using (var feed = factory.Create(Cue.ShowRunner))
            {
                Check(feed is CueShowFeed, "FeedFactory creates CueShowFeed for Cue.ShowRunner");
            }
            var settingsPath = Path.Combine(folder, "settings.json");
            var settings = new AppSettings { OutputDevice = "display-id", SelectedAudioDevice = "audio-selected", AppliedAudioDevice = "audio-active", Protection = false, Volume = 37, Muted = true, Repeat = true, ImageFit = 2, Transition = TransitionKind.ThroughBlack, TransitionMilliseconds = 1500, AnimatePatterns = false, AutoStartOutput = true, Cues = expected, Prepared = expected[0], WebsiteDraft = "https://example.com/", TextDraft = "Bühne bereit", WindowX = -1700, WindowY = 30, WindowWidth = 1500, WindowHeight = 900, Maximized = true };
            SettingsStore.Save(settings, settingsPath);
            var restored = SettingsStore.Load(settingsPath);
            Check(restored.OutputDevice == settings.OutputDevice && restored.SelectedAudioDevice == settings.SelectedAudioDevice && restored.AppliedAudioDevice == settings.AppliedAudioDevice, "Device selection and applied audio restore independently");
            Check(restored.Volume == 37 && restored.Muted && restored.Repeat && restored.ImageFit == 2 && !restored.Protection, "Playback and protection configuration roundtrip");
            Check(restored.Transition == TransitionKind.ThroughBlack && restored.TransitionMilliseconds == 1500 && !restored.AnimatePatterns && restored.AutoStartOutput, "Transition animation and automatic black-start preferences roundtrip");
            Check(restored.Cues!.SequenceEqual(expected) && restored.Prepared == expected[0] && restored.TextDraft == "Bühne bereit", "Session media and drafts roundtrip");
            Check(restored.WindowX == -1700 && restored.WindowWidth == 1500 && restored.Maximized, "Window placement including negative monitor coordinates persists");
            SettingsStore.Save(settings with { Volume = 200, ImageFit = -4 }, settingsPath);
            Check(SettingsStore.Load(settingsPath).Volume == 100 && SettingsStore.Load(settingsPath).ImageFit == 0, "Invalid configuration ranges are clamped");
            using (var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
            {
                var priority = currentProcess.PriorityClass;
                using (var performance = new PresentationPerformance())
                {
                    currentProcess.Refresh();
                    Check(currentProcess.PriorityClass == (priority is System.Diagnostics.ProcessPriorityClass.Normal or System.Diagnostics.ProcessPriorityClass.BelowNormal or System.Diagnostics.ProcessPriorityClass.Idle ? System.Diagnostics.ProcessPriorityClass.AboveNormal : priority), "Active presentation scheduling is applied");
                }
                currentProcess.Refresh();
                Check(currentProcess.PriorityClass == priority, "Presentation scheduling is restored after disposal");
            }
            RundownChecks.Run(Check, folder);
            UpdateChecks.Run(Check, folder).GetAwaiter().GetResult();
            PresentationChecks.Run(Check, folder, args);
            if (args.Contains("--audio"))
            {
                Directory.CreateDirectory(App.DataDirectory);
                using var audio = new AudioRouting();
                var devices = audio.Devices();
                Check(devices.Count > 0, "Audio endpoint enumeration");
                string[] before = [audio.DefaultId(0), audio.DefaultId(1), audio.DefaultId(2)];
                var target = devices.FirstOrDefault(x => x.Id != before[1]) ?? devices.First(x => x.Id == before[1]);
                audio.Select(target.Id);
                Check(Enumerable.Range(0, 3).All(r => audio.DefaultId(r) == target.Id), "Shared system audio selection across roles");
                audio.Restore();
                Check(Enumerable.Range(0, 3).All(r => audio.DefaultId(r) == before[r]), "Restore original audio roles");
            }
            Console.WriteLine($"SUCCESS: {count} checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static byte[] RenderPattern(string pattern, double time, bool animate) => RenderElement(new PatternView { Pattern = pattern, FrameSeconds = time, Animate = animate });
    static byte[] RenderElement(FrameworkElement element)
    {
        element.Measure(new Size(640, 360)); element.Arrange(new Rect(0, 0, 640, 360));
        var bitmap = new RenderTargetBitmap(640, 360, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        byte[] result = new byte[640 * 360 * 4]; bitmap.CopyPixels(result, 640 * 4, 0);
        return result;
    }
}
