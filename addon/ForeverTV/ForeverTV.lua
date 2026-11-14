-- ForeverTV prototype. Draws a frame with four corner markers; the desktop app finds them on the screen and lays
-- its click-through video window over the black area. Clicks on the video therefore land here. The counters in the
-- title bar show whether they do, also in combat. The only thing the app reads besides the position is the play state
-- cell right of the top-left marker. No game data is read or shown to the app.

local TITLE_HEIGHT = 26
local FOOTER_HEIGHT = 22
local CELL = 6
local BAND = CELL
local MARKER_WIDTH = CELL * 3
local MIN_VIDEO_WIDTH, MIN_VIDEO_HEIGHT = 240, 135
local DEFAULT_VIDEO_WIDTH, DEFAULT_VIDEO_HEIGHT = 640, 360

-- The colours and their order per corner must match MarkerDetector.cs.
local MAGENTA, GREEN, CYAN = { 1, 0, 1 }, { 0, 1, 0 }, { 0, 1, 1 }

local clicks, wheels, combatClicks, moves = 0, 0, 0, 0
local PLAY_BUTTON_WIDTH = 64

local frame = CreateFrame("Frame", "ForeverTVFrame", UIParent)
frame:Hide()
frame:SetFrameStrata("MEDIUM")
frame:SetMovable(true)
frame:SetResizable(true)
frame:SetClampedToScreen(true)
frame:EnableMouse(true)

local background = frame:CreateTexture(nil, "BACKGROUND")
background:SetAllPoints(frame)
background:SetColorTexture(0.08, 0.08, 0.1, 1)

local title = frame:CreateFontString(nil, "OVERLAY", "GameFontNormalSmall")
title:SetPoint("LEFT", frame, "TOPLEFT", 8, -TITLE_HEIGHT / 2)
title:SetPoint("RIGHT", frame, "TOPRIGHT", -(TITLE_HEIGHT + PLAY_BUTTON_WIDTH + 8), -TITLE_HEIGHT / 2)
title:SetJustifyH("LEFT")

local closeButton = CreateFrame("Button", nil, frame, "UIPanelCloseButton")
closeButton:SetPoint("TOPRIGHT", frame, "TOPRIGHT", 0, 0)
closeButton:SetSize(TITLE_HEIGHT, TITLE_HEIGHT)

-- Play/pause. The app reads the state from a colour cell, so it works in combat too.
local playButton = CreateFrame("Button", nil, frame, "UIPanelButtonTemplate")
playButton:SetSize(PLAY_BUTTON_WIDTH, TITLE_HEIGHT - 6)
playButton:SetPoint("RIGHT", closeButton, "LEFT", -4, 0)

local footer = frame:CreateFontString(nil, "OVERLAY", "GameFontDisableSmall")
footer:SetPoint("LEFT", frame, "BOTTOMLEFT", 8, FOOTER_HEIGHT / 2)
footer:SetText("Drag to move, corner to resize. /ftv hides it, /ftv pause | play.")

-- One UI unit = one physical pixel, as on the PixelBridge strip, so that a marker cell is exactly 6 px.
local function ApplyScale()
    local _, height = GetPhysicalScreenSize()
    if height and height > 0 then
        frame:SetScale((768 / height) / UIParent:GetEffectiveScale())
    end
end

local function Round(value)
    return math.floor(value + 0.5)
end

local function Refresh()
    local inCombat = InCombatLockdown() and "yes" or "no"
    title:SetText("Clicks " .. clicks .. "  in combat " .. combatClicks .. "  wheel " .. wheels
        .. "  moves " .. moves .. "  combat: " .. inCombat)
end

local function SavePlace()
    local left, top = frame:GetLeft(), frame:GetTop()
    if not left or not top then return end
    left, top = Round(left), Round(top)
    frame:ClearAllPoints()
    frame:SetPoint("TOPLEFT", UIParent, "BOTTOMLEFT", left, top)
    local width, height = Round(frame:GetWidth()), Round(frame:GetHeight())
    frame:SetSize(width, height)
    ForeverTVDB.left, ForeverTVDB.top = left, top
    ForeverTVDB.width, ForeverTVDB.height = width, height
end

local function StopMoving()
    if frame.moving then
        frame.moving = false
        frame:StopMovingOrSizing()
        moves = moves + 1
        SavePlace()
        Refresh()
    end
end

local function StopSizing()
    if frame.sizing then
        frame.sizing = false
        frame:StopMovingOrSizing()
        SavePlace()
    end
end

local function StartSizing()
    frame.sizing = true
    frame:StartSizing("BOTTOMRIGHT")
end

-- The area under the video window. It takes the clicks that pass through.
local video = CreateFrame("Button", nil, frame)
video:SetPoint("TOPLEFT", frame, "TOPLEFT", BAND, -(TITLE_HEIGHT + BAND))
video:SetPoint("BOTTOMRIGHT", frame, "BOTTOMRIGHT", -BAND, FOOTER_HEIGHT + BAND)
video:RegisterForClicks("LeftButtonUp", "RightButtonUp")
video:RegisterForDrag("LeftButton")
video:EnableMouseWheel(true)

local videoBackground = video:CreateTexture(nil, "BACKGROUND")
videoBackground:SetAllPoints(video)
videoBackground:SetColorTexture(0, 0, 0, 1)

local waiting = video:CreateFontString(nil, "OVERLAY", "GameFontNormal")
waiting:SetPoint("CENTER", video, "CENTER", 0, 0)
waiting:SetText("Start the ForeverTV app. Its picture appears here.")

video:SetScript("OnClick", function()
    clicks = clicks + 1
    if InCombatLockdown() then combatClicks = combatClicks + 1 end
    Refresh()
end)
video:SetScript("OnMouseWheel", function()
    wheels = wheels + 1
    Refresh()
end)
video:SetScript("OnDragStart", function()
    frame.moving = true
    frame:StartMoving()
end)
video:SetScript("OnDragStop", StopMoving)

frame:RegisterForDrag("LeftButton")
frame:SetScript("OnDragStart", function()
    frame.moving = true
    frame:StartMoving()
end)
frame:SetScript("OnDragStop", StopMoving)

local grip = CreateFrame("Button", nil, frame)
grip:SetSize(FOOTER_HEIGHT, FOOTER_HEIGHT)
grip:SetPoint("BOTTOMRIGHT", frame, "BOTTOMRIGHT", 0, 0)
local gripTexture = grip:CreateTexture(nil, "OVERLAY")
gripTexture:SetAllPoints(grip)
gripTexture:SetColorTexture(0.6, 0.6, 0.65, 1)
grip:SetScript("OnMouseDown", StartSizing)
grip:SetScript("OnMouseUp", StopSizing)
grip:SetScript("OnHide", StopSizing)

-- Above the video and flush with its corners, in a band the video window never covers.
local function Marker(point, x, y, first, second, third)
    local holder = CreateFrame("Frame", nil, frame)
    holder:SetFrameLevel(frame:GetFrameLevel() + 10)
    holder:SetSize(MARKER_WIDTH, CELL)
    holder:SetPoint(point, frame, point, x, y)
    local colours = { first, second, third }
    local index = 1
    while index <= 3 do
        local colour = colours[index]
        local cell = holder:CreateTexture(nil, "OVERLAY")
        cell:SetSize(CELL, CELL)
        cell:SetPoint("TOPLEFT", holder, "TOPLEFT", (index - 1) * CELL, 0)
        cell:SetColorTexture(colour[1], colour[2], colour[3], 1)
        index = index + 1
    end
end

Marker("TOPLEFT", BAND, -TITLE_HEIGHT, MAGENTA, GREEN, CYAN)

-- Right of the top-left marker: green plays, cyan pauses. Must match MarkerDetector.ReadPlaying.
local stateHolder = CreateFrame("Frame", nil, frame)
stateHolder:SetFrameLevel(frame:GetFrameLevel() + 10)
stateHolder:SetSize(CELL, CELL)
stateHolder:SetPoint("TOPLEFT", frame, "TOPLEFT", BAND + MARKER_WIDTH, -TITLE_HEIGHT)
local stateCell = stateHolder:CreateTexture(nil, "OVERLAY")
stateCell:SetAllPoints(stateHolder)

local tvTuneTexture
local function ShowPlayState()
    local paused = ForeverTVDB and ForeverTVDB.paused
    if paused then
        stateCell:SetColorTexture(CYAN[1], CYAN[2], CYAN[3], 1)
        playButton:SetText("Play")
    else
        stateCell:SetColorTexture(GREEN[1], GREEN[2], GREEN[3], 1)
        playButton:SetText("Pause")
    end
    if tvTuneTexture then
        if paused then
            tvTuneTexture:SetTexCoord(0.5, 1, 0, 0.5)
        else
            tvTuneTexture:SetTexCoord(0, 0.5, 0, 0.5)
        end
    end
end

local function SetPaused(paused)
    ForeverTVDB.paused = paused
    ShowPlayState()
end

playButton:SetScript("OnClick", function()
    SetPaused(not ForeverTVDB.paused)
end)
Marker("TOPRIGHT", -BAND, -TITLE_HEIGHT, GREEN, CYAN, MAGENTA)
Marker("BOTTOMLEFT", BAND, FOOTER_HEIGHT, CYAN, MAGENTA, GREEN)
Marker("BOTTOMRIGHT", -BAND, FOOTER_HEIGHT, MAGENTA, CYAN, GREEN)

-- Rails and controls surround the original frame. Brass strips cover only the title/footer interiors;
-- the video, marker band and state cell keep their original anchors, sizes and colours.
local function CreateTVSkin()
    local skin = CreateFrame("Frame", "ForeverTVSkin", frame)
    skin:SetAllPoints(frame)
    skin:EnableMouse(false)
    skin:Hide()
    skin:SetScript("OnHide", function()
        StopMoving()
        StopSizing()
    end)

    -- Original painted masters, crop coordinates and the deterministic build live in tools/TVArt.
    -- UV rectangles exclude unused atlas pixels. Fixed caps never stretch with the frame.
    local function Art(parent, name, u1, u2, v1, v2)
        local texture = parent:CreateTexture(nil, "ARTWORK")
        texture:SetTexture("Interface\\AddOns\\ForeverTV\\art\\" .. name)
        texture:SetTexCoord(u1, u2, v1, v2)
        return texture
    end

    local function Panel()
        local panel = CreateFrame("Frame", nil, skin)
        panel:EnableMouse(true)
        panel:RegisterForDrag("LeftButton")
        panel:SetScript("OnDragStart", frame:GetScript("OnDragStart"))
        panel:SetScript("OnDragStop", StopMoving)
        return panel
    end

    local top = Panel()
    top:SetPoint("BOTTOMLEFT", frame, "TOPLEFT", -22, 0)
    top:SetPoint("BOTTOMRIGHT", frame, "TOPRIGHT", 60, 0)
    top:SetHeight(28)

    local bottom = Panel()
    bottom:SetPoint("TOPLEFT", frame, "BOTTOMLEFT", -22, 0)
    bottom:SetPoint("TOPRIGHT", frame, "BOTTOMRIGHT", 60, 0)
    bottom:SetHeight(22)

    local left = Panel()
    left:SetPoint("TOPRIGHT", frame, "TOPLEFT", 0, 0)
    left:SetPoint("BOTTOMRIGHT", frame, "BOTTOMLEFT", 0, 0)
    left:SetWidth(22)

    local right = Panel()
    right:SetPoint("TOPLEFT", frame, "TOPRIGHT", 0, 0)
    right:SetPoint("BOTTOMLEFT", frame, "BOTTOMRIGHT", 0, 0)
    right:SetWidth(60)

    local function Horizontal(rail, y, height)
        local first = Art(rail, "rails", 0, 22 / 256, y / 128, (y + height) / 128)
        first:SetPoint("TOPLEFT", rail, "TOPLEFT", 0, 0)
        first:SetPoint("BOTTOMLEFT", rail, "BOTTOMLEFT", 0, 0)
        first:SetWidth(22)
        local last = Art(rail, "rails", 196 / 256, 1, y / 128, (y + height) / 128)
        last:SetPoint("TOPRIGHT", rail, "TOPRIGHT", 0, 0)
        last:SetPoint("BOTTOMRIGHT", rail, "BOTTOMRIGHT", 0, 0)
        last:SetWidth(60)
        local middle = Art(rail, "rails", 22 / 256, 196 / 256, y / 128, (y + height) / 128)
        middle:SetPoint("TOPLEFT", first, "TOPRIGHT", 0, 0)
        middle:SetPoint("BOTTOMRIGHT", last, "BOTTOMLEFT", 0, 0)
    end
    Horizontal(top, 0, 28)
    Horizontal(bottom, 32, 22)

    local titleBezel = Art(skin, "rails", 22 / 256, 196 / 256, 0, 28 / 128)
    titleBezel:SetPoint("TOPLEFT", frame, "TOPLEFT", 0, 0)
    titleBezel:SetPoint("TOPRIGHT", frame, "TOPRIGHT", 0, 0)
    titleBezel:SetHeight(TITLE_HEIGHT)
    local footerBezel = Art(skin, "rails", 22 / 256, 196 / 256, 32 / 128, 54 / 128)
    footerBezel:SetPoint("BOTTOMLEFT", frame, "BOTTOMLEFT", 0, 0)
    footerBezel:SetPoint("BOTTOMRIGHT", frame, "BOTTOMRIGHT", 0, 0)
    footerBezel:SetHeight(FOOTER_HEIGHT)

    local side = Art(left, "rails", 0, 22 / 256, 64 / 128, 1)
    side:SetAllPoints(left)

    -- At the minimum frame height (195), 124 + 64 fixed pixels leave a 7-pixel middle.
    -- Controls and speaker keep their dimensions; only a pixel-identical brass row stretches.
    local controls = Art(right, "controls", 0, 60 / 64, 0, 124 / 256)
    controls:SetPoint("TOPLEFT", right, "TOPLEFT", 0, 0)
    controls:SetPoint("TOPRIGHT", right, "TOPRIGHT", 0, 0)
    controls:SetHeight(124)
    local speaker = Art(right, "controls", 0, 60 / 64, 160 / 256, 224 / 256)
    speaker:SetPoint("BOTTOMLEFT", right, "BOTTOMLEFT", 0, 0)
    speaker:SetPoint("BOTTOMRIGHT", right, "BOTTOMRIGHT", 0, 0)
    speaker:SetHeight(64)
    local middle = Art(right, "controls", 0, 60 / 64, 128 / 256, 132 / 256)
    middle:SetPoint("TOPLEFT", controls, "BOTTOMLEFT", 0, 0)
    middle:SetPoint("BOTTOMRIGHT", speaker, "TOPRIGHT", 0, 0)

    local function Tooltip(control, text)
        control:SetScript("OnEnter", function(self)
            GameTooltip:SetOwner(self, "ANCHOR_RIGHT")
            GameTooltip:SetText(text)
            GameTooltip:Show()
        end)
        control:SetScript("OnLeave", function() GameTooltip:Hide() end)
        control:SetScript("OnHide", function(self)
            if GameTooltip:GetOwner() == self then GameTooltip:Hide() end
        end)
    end

    -- Two painted pointer states avoid relying on texture rotation support in Interface 16001.
    local tune = CreateFrame("Button", "ForeverTVTune", right)
    tune:SetPoint("TOPLEFT", right, "TOPLEFT", 6, -8)
    tune:SetSize(48, 48)
    tvTuneTexture = Art(tune, "switches", 0, 0.5, 0, 0.5)
    tvTuneTexture:SetAllPoints(tune)
    tune:SetScript("OnClick", function() SetPaused(not ForeverTVDB.paused) end)
    Tooltip(tune, "Play/Pause")

    local power = CreateFrame("Button", "ForeverTVPower", right)
    power:SetPoint("TOPLEFT", right, "TOPLEFT", 6, -68)
    power:SetSize(48, 48)
    local powerTexture = Art(power, "switches", 0, 0.5, 0.5, 1)
    powerTexture:SetAllPoints(power)
    power:SetScript("OnClick", function() frame:Hide() end)
    Tooltip(power, "Power off (/ftv to turn on)")

    local crank = CreateFrame("Button", "ForeverTVResize", bottom)
    crank:SetPoint("BOTTOMRIGHT", bottom, "BOTTOMRIGHT", -6, 0)
    crank:SetSize(32, 32)
    local crankTexture = Art(crank, "switches", 0.5, 1, 0.5, 1)
    crankTexture:SetAllPoints(crank)
    crank:SetScript("OnMouseDown", StartSizing)
    crank:SetScript("OnMouseUp", StopSizing)
    Tooltip(crank, "Drag to resize")
    crank:SetScript("OnHide", function(self)
        StopSizing()
        if GameTooltip:GetOwner() == self then GameTooltip:Hide() end
    end)

    -- Copper rabbit ears are a separate transparent sprite, wholly above the top rail.
    local antenna = Art(top, "antenna", 0, 1, 0, 1)
    antenna:SetSize(104, 104)
    antenna:SetPoint("BOTTOMLEFT", top, "TOPLEFT", 10, 0)

    local badge = CreateFrame("Frame", nil, top)
    badge:SetSize(146, 24)
    badge:SetPoint("CENTER", top, "CENTER", 0, 0)
    local plate = Art(badge, "badge", 0, 146 / 256, 0, 24 / 32)
    plate:SetAllPoints(badge)
    local brand = badge:CreateFontString(nil, "OVERLAY", "GameFontNormalSmall")
    brand:SetPoint("CENTER", badge, "CENTER", 0, 0)
    brand:SetTextColor(0.25, 0.12, 0.025, 1)
    brand:SetShadowColor(1, 0.82, 0.35, 1)
    brand:SetShadowOffset(0, -1)
    brand:SetText("BILGEBOX")
    return skin
end

local tvSkin = CreateTVSkin()

local function ApplySkin()
    StopMoving()
    StopSizing()
    if ForeverTVDB.skin == "tv" then
        title:Hide()
        playButton:Hide()
        closeButton:Hide()
        footer:Hide()
        grip:Hide()
        waiting:Hide()
        tvSkin:Show()
    else
        tvSkin:Hide()
        title:Show()
        playButton:Show()
        closeButton:Show()
        footer:Show()
        grip:Show()
        waiting:Show()
    end
end

local function Restore()
    ForeverTVDB = ForeverTVDB or {}
    local db = ForeverTVDB
    ApplyScale()
    frame:SetSize(db.width or (DEFAULT_VIDEO_WIDTH + 2 * BAND),
        db.height or (DEFAULT_VIDEO_HEIGHT + TITLE_HEIGHT + FOOTER_HEIGHT + 2 * BAND))
    if frame.SetResizeBounds then
        frame:SetResizeBounds(MIN_VIDEO_WIDTH + 2 * BAND, MIN_VIDEO_HEIGHT + TITLE_HEIGHT + FOOTER_HEIGHT + 2 * BAND)
    end
    frame:ClearAllPoints()
    if db.left and db.top then
        frame:SetPoint("TOPLEFT", UIParent, "BOTTOMLEFT", db.left, db.top)
    else
        frame:SetPoint("CENTER", UIParent, "CENTER", 0, 0)
    end
    Refresh()
    ShowPlayState()
    ApplySkin()
    if db.shown then frame:Show() end
end

frame:SetScript("OnShow", function() ForeverTVDB.shown = true; Refresh() end)
frame:SetScript("OnHide", function()
    StopMoving()
    StopSizing()
    -- Alt+Z hides UIParent and this frame's OnHide with it; only a real close clears the setting.
    if UIParent:IsShown() then ForeverTVDB.shown = false end
end)

local events = CreateFrame("Frame")
events:RegisterEvent("PLAYER_LOGIN")
events:RegisterEvent("UI_SCALE_CHANGED")
events:RegisterEvent("DISPLAY_SIZE_CHANGED")
events:RegisterEvent("PLAYER_REGEN_DISABLED")
events:RegisterEvent("PLAYER_REGEN_ENABLED")
events:SetScript("OnEvent", function(_, event)
    if event == "PLAYER_LOGIN" then
        Restore()
    elseif event == "UI_SCALE_CHANGED" or event == "DISPLAY_SIZE_CHANGED" then
        ApplyScale()
    else
        Refresh()
    end
end)

SLASH_FOREVERTV1 = "/ftv"
SlashCmdList["FOREVERTV"] = function(message)
    local command = string.lower(message or "")
    if command == "pause" then
        SetPaused(true)
    elseif command == "play" then
        SetPaused(false)
    elseif command == "tv" then
        if ForeverTVDB.skin == "tv" then
            ForeverTVDB.skin = nil
        else
            ForeverTVDB.skin = "tv"
        end
        ApplySkin()
    elseif frame:IsShown() then
        frame:Hide()
    else
        frame:Show()
    end
end
