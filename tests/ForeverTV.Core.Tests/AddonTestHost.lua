-- A small recording host for addon behaviour and anchor tests, not a WoW renderer.
Objects = {}
SlashCmdList = {}
Combat = false
local methods = {}
local objectCount = 0

local function Object(kind, name, parent)
    local object = { kind = kind, parent = parent, points = {}, scripts = {}, events = {}, shown = true }
    setmetatable(object, { __index = methods })
    table.insert(Objects, object)
    objectCount = objectCount + 1
    object.id = objectCount
    if name then _G[name] = object end
    return object
end

function CreateFrame(kind, name, parent, template)
    local object = Object(kind, name, parent)
    object.template = template
    return object
end
function methods:CreateTexture() return Object("Texture", nil, self) end
function methods:CreateFontString() return Object("FontString", nil, self) end
function methods:SetPoint(point, relative, relativePoint, x, y)
    table.insert(self.points, { point, relative, relativePoint, x, y })
end
function methods:ClearAllPoints() self.points = {} end
function methods:SetAllPoints(relative)
    self:ClearAllPoints()
    self:SetPoint("TOPLEFT", relative, "TOPLEFT", 0, 0)
    self:SetPoint("BOTTOMRIGHT", relative, "BOTTOMRIGHT", 0, 0)
end
function methods:SetSize(width, height) self.width, self.height = width, height end
function methods:SetWidth(width) self.width = width end
function methods:SetHeight(height) self.height = height end
function methods:GetWidth() return self.width end
function methods:GetHeight() return self.height end
function methods:SetScale(scale) self.scale = scale end
function methods:GetEffectiveScale() return self.scale or 1 end
function methods:SetFrameLevel(level) self.level = level end
function methods:GetFrameLevel() return self.level or 1 end
function methods:SetFrameStrata(strata) self.strata = strata end
function methods:SetMovable(value) self.movable = value end
function methods:SetResizable(value) self.resizable = value end
function methods:SetResizeBounds(width, height) self.minimum = { width, height } end
function methods:SetClampedToScreen(value) self.clamped = value end
function methods:EnableMouse(value) self.mouse = value end
function methods:EnableMouseWheel(value) self.wheel = value end
function methods:RegisterForClicks(...) self.clicks = { ... } end
function methods:RegisterForDrag(button) self.drag = button end
function methods:SetScript(event, handler) self.scripts[event] = handler end
function methods:GetScript(event) return self.scripts[event] end
function methods:RegisterEvent(event) self.events[event] = true end
function methods:SetJustifyH(value) self.justify = value end
function methods:SetText(text) self.text = text end
function methods:SetTextColor(...) self.textColour = { ... } end
function methods:SetColorTexture(...) self.colours = { { ... } } end
function methods:SetTexture(path) self.texture = path end
function methods:SetTexCoord(...) self.texCoords = { ... } end
function methods:SetShadowColor(...) self.shadowColour = { ... } end
function methods:SetShadowOffset(x, y) self.shadowOffset = { x, y } end
function methods:SetGradient(direction, first, second) self.colours = { first, second } end
function CreateColor(...) return { ... } end
function methods:IsShown() return self.shown end
function methods:Show()
    if self.shown then return end
    self.shown = true
    if self.scripts.OnShow then self.scripts.OnShow(self) end
end
function methods:Hide()
    if not self.shown then return end
    self.shown = false
    if self.scripts.OnHide then self.scripts.OnHide(self) end
    self:NotifyChildrenHidden()
end
function methods:NotifyChildrenHidden()
    for _, child in ipairs(Objects) do
        if child.parent == self and child.shown then
            if child.scripts.OnHide then child.scripts.OnHide(child) end
            child:NotifyChildrenHidden()
        end
    end
end
function methods:StartMoving() self.movement = "moving" end
function methods:StartSizing(corner) self.movement = corner end
function methods:StopMovingOrSizing() self.movement = nil end
function GetPhysicalScreenSize() return 1920, 1080 end
function InCombatLockdown() return Combat end

UIParent = Object("Frame", "UIParent", nil)
UIParent:SetSize(1920, 1080)
GameTooltip = Object("GameTooltip", "GameTooltip", UIParent)
GameTooltip:Hide()
function methods:SetOwner(owner, anchor) self.owner, self.anchor = owner, anchor end
function methods:GetOwner() return self.owner end

function Login()
    for _, object in ipairs(Objects) do
        if object.events.PLAYER_LOGIN then object.scripts.OnEvent(object, "PLAYER_LOGIN") end
    end
end

function IsDescendant(object, ancestor)
    while object do
        if object == ancestor then return true end
        object = object.parent
    end
    return false
end

-- Resolve opposing anchors in the frame's logical pixel units, with y increasing downwards.
local factors = {
    TOPLEFT = { 0, 0 }, TOP = { 0.5, 0 }, TOPRIGHT = { 1, 0 },
    LEFT = { 0, 0.5 }, CENTER = { 0.5, 0.5 }, RIGHT = { 1, 0.5 },
    BOTTOMLEFT = { 0, 1 }, BOTTOM = { 0.5, 1 }, BOTTOMRIGHT = { 1, 1 }
}
local function Axis(object, axis, size)
    local firstFactor, firstCoordinate
    for _, point in ipairs(object.points) do
        local relative = Bounds(point[2])
        local factor = factors[point[1]][axis]
        local offset = point[axis + 3]
        local coordinate
        if axis == 1 then
            coordinate = relative.left + factors[point[3]][axis] * relative.width + offset
        else
            coordinate = relative.top + factors[point[3]][axis] * relative.height - offset
        end
        if size then return coordinate - factor * size, size end
        if firstFactor and firstFactor ~= factor then
            size = (coordinate - firstCoordinate) / (factor - firstFactor)
            return coordinate - factor * size, size
        end
        firstFactor, firstCoordinate = factor, coordinate
    end
    error("Unresolved anchor on object " .. object.id)
end
function Bounds(object)
    if object == UIParent then return { left = 0, top = 0, width = 1920, height = 1080 } end
    local left, width = Axis(object, 1, object.width)
    local top, height = Axis(object, 2, object.height)
    return { left = left, top = top, width = width, height = height }
end
function methods:GetLeft() return Bounds(self).left end
function methods:GetTop() return 1080 - Bounds(self).top end

function OriginalGeometry()
    local parts = {}
    for _, object in ipairs(Objects) do
        if not IsDescendant(object, ForeverTVSkin) then
            table.insert(parts, tostring(object.width) .. "," .. tostring(object.height) .. "," .. tostring(object.scale))
            for _, point in ipairs(object.points) do
                table.insert(parts, point[1] .. "," .. point[2].id .. "," .. point[3] .. "," .. point[4] .. "," .. point[5])
            end
            for _, colour in ipairs(object.colours or {}) do table.insert(parts, table.concat(colour, ",")) end
        end
    end
    return table.concat(parts, "|")
end
