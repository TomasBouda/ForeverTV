using System.Text.RegularExpressions;
using MoonSharp.Interpreter;

namespace ForeverTV.Core.Tests;

public class AddonSourceTests
{
    private static string RepositoryPath(params string[] path)
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "ForeverTV.slnx"))) folder = folder.Parent;
        return Path.Combine([folder!.FullName, .. path]);
    }

    private static string ReadRepositoryFile(params string[] path) => File.ReadAllText(RepositoryPath(path));

    private static string Source() => ReadRepositoryFile("addon", "ForeverTV", "ForeverTV.lua");

    private static Script RunAddon(string savedVariables = "{}")
    {
        var script = new Script();
        script.DoString(ReadRepositoryFile("tests", "ForeverTV.Core.Tests", "AddonTestHost.lua"));
        script.DoString("ForeverTVDB = " + savedVariables);
        script.DoString(Source());
        script.DoString("Login()");
        return script;
    }

    [Fact]
    public void Lua_source_compiles() => new Script().LoadString(Source());

    [Fact]
    public void Lua_source_follows_the_Lua_5_0_rules()
    {
        var code = Regex.Replace(Source(), "--.*", "");
        code = Regex.Replace(code, "\"[^\"\n]*\"", "\"\"");
        Assert.DoesNotContain("#", code);
        Assert.DoesNotContain("%", code);
    }

    [Fact]
    public void Marker_size_matches_the_detector()
    {
        Assert.Matches($@"local CELL = {MarkerDetector.CellSize}\b", Source());
    }

    [Fact]
    public void Painted_texture_references_resolve_to_power_of_two_uncompressed_32_bit_tga_files()
    {
        var script = RunAddon();
        var expected = new Dictionary<string, (int Width, int Height)>
        {
            ["rails"] = (256, 128), ["controls"] = (64, 256),
            ["antenna"] = (128, 128), ["badge"] = (256, 32), ["switches"] = (128, 128)
        };
        var referenced = new HashSet<string>();
        const string prefix = @"Interface\AddOns\ForeverTV\art\";
        foreach (var value in script.Globals.Get("Objects").Table.Values)
        {
            var texture = value.Table.Get("texture");
            if (texture.Type != DataType.String) continue;
            Assert.StartsWith(prefix, texture.String);
            var name = texture.String[prefix.Length..];
            Assert.True(expected.ContainsKey(name), "Undocumented texture: " + name);
            referenced.Add(name);
            var coordinates = value.Table.Get("texCoords").Table.Values.Select(v => v.Number).ToArray();
            Assert.Equal(4, coordinates.Length);
            Assert.All(coordinates, coordinate => Assert.InRange(coordinate, 0, 1));
            Assert.True(coordinates[0] < coordinates[1] && coordinates[2] < coordinates[3]);
        }
        Assert.Equal(expected.Keys.Order(), referenced.Order());
        foreach (var (name, size) in expected)
        {
            var bytes = File.ReadAllBytes(RepositoryPath("addon", "ForeverTV", "art", name + ".tga"));
            Assert.Equal(0, bytes[0]); // No image ID.
            Assert.Equal(0, bytes[1]); // No colour map.
            Assert.Equal(2, bytes[2]); // Uncompressed true colour.
            var width = bytes[12] | bytes[13] << 8;
            var height = bytes[14] | bytes[15] << 8;
            Assert.Equal(size, (width, height));
            Assert.True(width > 0 && (width & (width - 1)) == 0);
            Assert.True(height > 0 && (height & (height - 1)) == 0);
            Assert.Equal(32, bytes[16]);
            Assert.Equal(8, bytes[17]); // Eight alpha bits, bottom-up rows.
            Assert.Equal(18 + width * height * 4, bytes.Length);
        }
    }

    [Fact]
    public void Tv_skin_defaults_off_and_toggles_in_combat_without_changing_original_geometry()
    {
        var script = RunAddon("nil");
        script.DoString("""
            assert(not ForeverTVSkin:IsShown())
            assert(ForeverTVDB.skin == nil)
            local before = OriginalGeometry()
            Combat = true
            SlashCmdList.FOREVERTV("tv")
            assert(ForeverTVDB.skin == "tv")
            assert(ForeverTVSkin:IsShown())
            assert(not ForeverTVFrame:IsShown())
            assert(OriginalGeometry() == before)
            SlashCmdList.FOREVERTV("tv")
            assert(ForeverTVDB.skin == nil)
            assert(not ForeverTVSkin:IsShown())
            assert(OriginalGeometry() == before)
            """);
    }

    [Fact]
    public void Saved_tv_skin_restores_and_existing_commands_and_controls_work()
    {
        var script = RunAddon("{ skin = 'tv', paused = true, shown = true, width = 800, height = 500 }");
        script.DoString("""
            assert(ForeverTVSkin:IsShown())
            assert(ForeverTVFrame:IsShown())
            assert(ForeverTVFrame:GetWidth() == 800 and ForeverTVFrame:GetHeight() == 500)
            Combat = true
            local geometry = OriginalGeometry()
            SlashCmdList.FOREVERTV("tv")
            SlashCmdList.FOREVERTV("tv")
            assert(OriginalGeometry() == geometry)
            assert(ForeverTVFrame:IsShown() and ForeverTVDB.paused)
            local play, grip, state
            for _, object in ipairs(Objects) do
                if object.template == "UIPanelButtonTemplate" then play = object end
                if object.scripts.OnMouseDown then grip = object end
                if object.kind == "Texture" and object.parent.width == 6 and object.parent.height == 6 then
                    state = object
                end
            end
            assert(play.text == "Play" and state.colours[1][3] == 1)
            SlashCmdList.FOREVERTV("play")
            assert(not ForeverTVDB.paused and play.text == "Pause" and state.colours[1][3] == 0)
            SlashCmdList.FOREVERTV("pause")
            assert(ForeverTVDB.paused and play.text == "Play" and state.colours[1][3] == 1)
            play.scripts.OnClick(play)
            assert(not ForeverTVDB.paused and play.text == "Pause")
            SlashCmdList.FOREVERTV("")
            assert(not ForeverTVFrame:IsShown() and not ForeverTVDB.shown)
            SlashCmdList.FOREVERTV("tv")
            SlashCmdList.FOREVERTV("tv")
            assert(not ForeverTVFrame:IsShown() and ForeverTVDB.skin == "tv")
            SlashCmdList.FOREVERTV("")
            assert(ForeverTVFrame:IsShown() and ForeverTVDB.shown)
            local rail
            for _, object in ipairs(Objects) do
                if object.parent == ForeverTVSkin and object.mouse then
                    rail = object
                    assert(object.drag == "LeftButton")
                    object.scripts.OnDragStart(object)
                    assert(ForeverTVFrame.movement == "moving")
                    object.scripts.OnDragStop(object)
                    assert(ForeverTVFrame.movement == nil)
                end
            end
            assert(rail)
            rail.scripts.OnDragStart(rail)
            SlashCmdList.FOREVERTV("tv")
            assert(ForeverTVFrame.movement == nil and not ForeverTVFrame.moving)
            SlashCmdList.FOREVERTV("tv")
            grip.scripts.OnMouseDown(grip)
            assert(ForeverTVFrame.movement == "BOTTOMRIGHT")
            grip.scripts.OnMouseUp(grip)
            assert(ForeverTVFrame.movement == nil)
            assert(ForeverTVDB.width == 800 and ForeverTVDB.height == 500)
            """);
    }

    [Theory]
    [InlineData(252, 195)]
    [InlineData(652, 420)]
    [InlineData(1100, 700)]
    public void Casing_art_enters_only_title_and_footer_and_preserves_markers_and_state(int width, int height)
    {
        var script = RunAddon("{ skin = 'tv', shown = true }");
        script.Globals["TestWidth"] = width;
        script.Globals["TestHeight"] = height;
        script.DoString("""
            ForeverTVFrame:SetSize(TestWidth, TestHeight)
            local frame = Bounds(ForeverTVFrame)
            local markers = 0
            for _, object in ipairs(Objects) do
                if IsDescendant(object, ForeverTVSkin) then
                    if object.kind == "Texture" then
                        local art = Bounds(object)
                        assert(art.left + art.width <= frame.left or art.left >= frame.left + frame.width
                            or art.top + art.height <= frame.top or art.top >= frame.top + frame.height
                            or (art.left >= frame.left and art.left + art.width <= frame.left + frame.width
                                and ((art.top >= frame.top and art.top + art.height <= frame.top + 26)
                                    or (art.top >= frame.top + frame.height - 22
                                        and art.top + art.height <= frame.top + frame.height))),
                            "Casing enters video or marker band, object " .. object.id)
                    end
                    for _, colour in ipairs(object.colours or {}) do
                        local red, green, blue = colour[1] * 255, colour[2] * 255, colour[3] * 255
                        assert(not (red > 200 and green < 70 and blue > 200))
                        assert(not (red < 70 and green > 200))
                    end
                elseif object.width == 18 and object.height == 6 then
                    markers = markers + 1
                    local marker = Bounds(object)
                    assert(marker.left == frame.left + 6 or marker.left == frame.left + frame.width - 24)
                    assert(marker.top == frame.top + 26 or marker.top == frame.top + frame.height - 28)
                end
            end
            assert(markers == 4)
            for _, object in ipairs(Objects) do
                if object.kind == "Frame" and object.width == 6 and object.height == 6 then
                    local state = Bounds(object)
                    assert(math.abs(state.left - frame.left - 24) < 0.001
                        and math.abs(state.top - frame.top - 26) < 0.001,
                        "State position " .. state.left .. "," .. state.top .. " frame " .. frame.left .. "," .. frame.top)
                end
            end
            """);
    }

    [Fact]
    public void Tv_mode_hides_plain_controls_and_text_and_restores_them_in_combat()
    {
        var script = RunAddon("{ shown = true }");
        script.DoString("""
            local plain = {}
            for _, object in ipairs(Objects) do
                if object.parent == ForeverTVFrame and
                    (object.kind == "FontString" or object.template or object.scripts.OnMouseDown) then
                    table.insert(plain, object)
                end
                if object.kind == "FontString" and string.find(object.text or "", "Start the ForeverTV") then
                    table.insert(plain, object)
                end
            end
            local count = 0
            for _, object in ipairs(plain) do count = count + 1 end
            assert(count == 6)
            for _, object in ipairs(plain) do assert(object:IsShown()) end
            Combat = true
            local geometry = OriginalGeometry()
            SlashCmdList.FOREVERTV("tv")
            for _, object in ipairs(plain) do assert(not object:IsShown()) end
            assert(OriginalGeometry() == geometry)
            SlashCmdList.FOREVERTV("tv")
            for _, object in ipairs(plain) do assert(object:IsShown()) end
            assert(OriginalGeometry() == geometry)
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Casing_controls_drive_existing_pause_hide_resize_and_tooltip_behaviour(bool paused)
    {
        var script = RunAddon("{ skin = 'tv', shown = true, paused = " + (paused ? "true" : "false") + " }");
        Assert.Matches(@"tune:SetScript\(""OnClick"", function\(\) SetPaused\(not ForeverTVDB.paused\) end\)", Source());
        script.DoString("""
            Combat = true
            local tune, power, crank = ForeverTVTune, ForeverTVPower, ForeverTVResize
            local pointer, state
            for _, object in ipairs(Objects) do
                if object.parent == tune then pointer = object end
                if object.kind == "Texture" and object.parent.width == 6 and object.parent.height == 6 then
                    state = object
                end
            end
            local paused = ForeverTVDB.paused
            assert(pointer.texCoords[1] == (paused and 0.5 or 0))
            tune.scripts.OnClick(tune)
            assert(ForeverTVDB.paused == not paused)
            assert(pointer.texCoords[1] == (paused and 0 or 0.5))
            assert(state.colours[1][3] == (paused and 0 or 1))
            SlashCmdList.FOREVERTV("pause")
            assert(pointer.texCoords[1] == 0.5 and state.colours[1][3] == 1)
            SlashCmdList.FOREVERTV("play")
            assert(pointer.texCoords[1] == 0 and state.colours[1][3] == 0)
            local messages = { "Play/Pause", "Power off (/ftv to turn on)", "Drag to resize" }
            for index, control in ipairs({ tune, power, crank }) do
                control.scripts.OnEnter(control)
                assert(GameTooltip:IsShown() and GameTooltip:GetOwner() == control)
                assert(GameTooltip.text == messages[index])
                control.scripts.OnLeave(control)
                assert(not GameTooltip:IsShown())
            end
            crank.scripts.OnMouseDown(crank, "LeftButton")
            assert(ForeverTVFrame.movement == "BOTTOMRIGHT" and ForeverTVFrame.sizing)
            ForeverTVFrame:SetSize(800, 500)
            crank.scripts.OnMouseUp(crank, "LeftButton")
            assert(ForeverTVFrame.movement == nil and not ForeverTVFrame.sizing)
            assert(ForeverTVDB.width == 800 and ForeverTVDB.height == 500)
            crank.scripts.OnMouseDown(crank, "LeftButton")
            crank.scripts.OnEnter(crank)
            SlashCmdList.FOREVERTV("tv")
            assert(ForeverTVFrame.movement == nil and not ForeverTVFrame.sizing)
            assert(not GameTooltip:IsShown())
            SlashCmdList.FOREVERTV("tv")
            crank.scripts.OnMouseDown(crank, "LeftButton")
            power.scripts.OnEnter(power)
            power.scripts.OnClick(power)
            assert(not ForeverTVFrame:IsShown() and not ForeverTVDB.shown)
            assert(ForeverTVFrame.movement == nil and not ForeverTVFrame.sizing)
            assert(not GameTooltip:IsShown())
            SlashCmdList.FOREVERTV("")
            assert(ForeverTVFrame:IsShown() and ForeverTVDB.skin == "tv")
            assert(not ForeverTVDB.paused)
            """);
    }
}
